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
    
    private List<Transform> _scalableObjects = new List<Transform>();
    private Dictionary<Transform, Vector3> _originalScales = new Dictionary<Transform, Vector3>();
    private GameObject _currObj;
    
    public void SetScales(GameObject pbObj)
    {
        _currObj = pbObj;
        _scalableObjects.Clear();
        _originalScales.Clear();
        FindScalableObjects();
    }

    void Update()
    {
        if (_currObj == null) // Works even if pbObj was destroyed in another script
        {
            return;
        }
        
        UpdateScalableObjects();
        ScaleObjects();
    }

    void FindScalableObjects()
    {
        GameObject[] objects = GameObject.FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in objects)
        {
            if (((1 << obj.layer) & scalableLayer) != 0)
            {
                _scalableObjects.Add(obj.transform);
                _originalScales[obj.transform] = Vector3.one * startScale; //_originalScales[obj.transform] = new Vector3(1, 1, 1) * startScale; // Set original scale to 1x1x1 / startScale
            }
        }
    }

    void UpdateScalableObjects()
    {
        GameObject[] objects = GameObject.FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in objects)
        {
            if (((1 << obj.layer) & scalableLayer) != 0 && !_scalableObjects.Contains(obj.transform))
            {
                _scalableObjects.Add(obj.transform);
                _originalScales[obj.transform] = Vector3.one * startScale;//_originalScales[obj.transform] = new Vector3(1, 1, 1) * startScale; // Set original scale to 1x1x1 / startScale
            }
        }
    }

    void ScaleObjects()
    {
        foreach (Transform obj in _scalableObjects)
        {
            if (IsParentScalable(obj))
                continue; // Skip scaling if the parent is already being scaled
            
            float distanceToController1 = Vector3.Distance(obj.position, leftController.position);
            float distanceToController2 = Vector3.Distance(obj.position, rightController.position);
            float closestDistance = Mathf.Min(distanceToController1, distanceToController2);

            float normalizedDistance = Mathf.Clamp01((closestDistance - minDistance) / (maxDistance - minDistance));
            Vector3 maxScale = _originalScales[obj];// * startScale;
            Vector3 minScale = maxScale * 0.1f; // 10% of original scale
            obj.localScale = Vector3.Lerp(maxScale, minScale, normalizedDistance);
        }
    }
    
    bool IsParentScalable(Transform obj)
    {
        Transform parent = obj.parent;
        while (parent != null)
        {
            if (_scalableObjects.Contains(parent))
            {
                return true;
            }
            parent = parent.parent;
        }
        return false;
    }
}
