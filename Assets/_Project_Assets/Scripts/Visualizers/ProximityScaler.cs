using System.Collections.Generic;
using UnityEngine;

public class ProximityScaler : MonoBehaviour
{
    public Transform leftController;
    public Transform rightController;
    public LayerMask scalableLayer;
    public float minDistance = 0.2f;
    public float maxDistance = 3.0f;
    public float startScale = 0.02f;

    private GameObject _currObj;
    private List<Transform> _scalable = new();
    private HashSet<Transform> _scalableSet = new();
    private Dictionary<Transform, Vector3> _originalScale = new();
    private float _invRange;

    void Awake()
    {
        // Precompute inverse distance range
        _invRange = 1f / (maxDistance - minDistance);
    }
    
    public void SetScales(GameObject pbObj)
    {
        _currObj = pbObj;
        InitializeSignifiers();
    }
    
    /// <summary>
    /// Reset the scaling whenever Undo/Redo or a mesh operation is done, to ensure all signifiers are scaled correctly
    /// </summary>
    public void ResetScales()
    {
        InitializeSignifiers();
    }
    
    /// <summary>
    /// Prepare signifiers for scaling based on distance to controller
    /// </summary>
    void InitializeSignifiers()
    {
        _scalable.Clear();
        _scalableSet.Clear();
        _originalScale.Clear();
        
        if (_currObj == null) return;

        // Find only the children of the current object on the scalable layer
        foreach (var t in _currObj.GetComponentsInChildren<Transform>(true))
        {
            if (((1 << t.gameObject.layer) & scalableLayer) != 0)
            {
                _scalable.Add(t);
                _scalableSet.Add(t);
                _originalScale[t] = Vector3.one * startScale;
            }
        }
    }

    void Update()
    {
        if (_currObj == null) return;
        ScaleSignifiers();
    }

    /// <summary>
    /// Use the distance between signifier and controller to determine signifiers scale
    /// Long distance = smaller scale
    /// Close distance = bigger scale
    /// </summary>
    void ScaleSignifiers()
    {

        int _maxCount = 300;
        
        Vector3 lp = leftController.position;
        Vector3 rp = rightController.position;

        int nearbyCount = 0;
        float maxDistSqr = maxDistance * maxDistance;

        // First pass: count how many signifiers are within maxDistance
        foreach (var t in _scalable)
        {
            if (t == null || ParentIsScaled(t)) continue;

            float sqrD1 = (t.position - lp).sqrMagnitude;
            float sqrD2 = (t.position - rp).sqrMagnitude;

            if (Mathf.Min(sqrD1, sqrD2) < maxDistSqr)
            {
                nearbyCount++;
                if (nearbyCount > _maxCount)
                    break;
            }
        }

        bool collapseAll = nearbyCount > _maxCount;

        // Second pass: apply scale
        foreach (var t in _scalable)
        {
            if (t == null || ParentIsScaled(t)) continue;

            Vector3 maxS = _originalScale[t];
            Vector3 minS = maxS * 0.1f;

            if (collapseAll)
            {
                t.localScale = minS;
                continue;
            }

            float sqrD1 = (t.position - lp).sqrMagnitude;
            float sqrD2 = (t.position - rp).sqrMagnitude;
            float d = Mathf.Sqrt(Mathf.Min(sqrD1, sqrD2));
            float norm = Mathf.Clamp01((d - minDistance) * _invRange);

            t.localScale = Vector3.Lerp(maxS, minS, norm);
        }
    }

    
    bool ParentIsScaled(Transform t)
    {
        for (var p = t.parent; p != null; p = p.parent)
            if (_scalableSet.Contains(p))
                return true;
        return false;
    }
}