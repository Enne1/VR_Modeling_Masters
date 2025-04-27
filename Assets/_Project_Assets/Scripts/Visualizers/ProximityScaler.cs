using System.Collections.Generic;
using UnityEngine;

public class ProximityScaler : MonoBehaviour
{
    [SerializeField] Transform leftController;
    [SerializeField] Transform rightController;
    [SerializeField] LayerMask scalableLayer;
    [SerializeField] float minDistance = 0.2f;
    [SerializeField] float maxDistance = 3.0f;
    [SerializeField] float startScale = 0.02f;

    GameObject _currObj;
    List<Transform> _scalable = new List<Transform>();
    HashSet<Transform> _scalableSet = new HashSet<Transform>();
    Dictionary<Transform, Vector3> _originalScale = new Dictionary<Transform, Vector3>();
    float _invRange;

    void Awake()
    {
        // Precompute inverse distance range
        _invRange = 1f / (maxDistance - minDistance);
    }

    public void SetScales(GameObject pbObj)
    {
        Debug.Log("Updating scales");
        _currObj = pbObj;
        InitializeSignifiers();
    }

    public void ResetScales()
    {
        InitializeSignifiers();
    }

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
        //Debug.Log("Update");
    }

    void ScaleSignifiers()
    {
        Vector3 lp = leftController.position;
        Vector3 rp = rightController.position;

        for (int i = 0, n = _scalable.Count; i < n; i++)
        {
            var t = _scalable[i];
            if (t == null || ParentIsScaled(t)) continue;

            // single sqrt instead of two Distance() calls
            float sqrD1 = (t.position - lp).sqrMagnitude;
            float sqrD2 = (t.position - rp).sqrMagnitude;
            float d = Mathf.Sqrt(Mathf.Min(sqrD1, sqrD2));

            float norm = Mathf.Clamp01((d - minDistance) * _invRange);

            Vector3 maxS = _originalScale[t];
            Vector3 minS = maxS * 0.1f;
            t.localScale = Vector3.Lerp(maxS, minS, norm);
        }
    }

    bool ParentIsScaled(Transform t)
    {
        // fast O(1) check via HashSet
        for (var p = t.parent; p != null; p = p.parent)
            if (_scalableSet.Contains(p))
                return true;
        return false;
    }
}
