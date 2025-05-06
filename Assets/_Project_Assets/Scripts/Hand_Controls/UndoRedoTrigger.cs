using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder;

public class UndoRedoTrigger : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProximityScaler _proximityScaler;
    private ProBuilderMesh _pbMesh;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _objSelector = FindFirstObjectByType<ObjSelector>();
        _proximityScaler = FindObjectOfType<ProximityScaler>(); 
    }
    
    void Update()
    {
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        }
    }
    
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger enter");
        if (other.transform.name.Contains("Hand") && _pbMesh != null)
        {
            if(transform.name.Contains("Undo"))
            {
                Debug.Log(transform.name);
            
                _pbMesh.GetComponent<UndoTracker>()?.Undo(false);
                _proximityScaler.ResetScales();
            }
            else if (transform.name.Contains("Redo"))
            {
                Debug.Log(transform.name);
            
                _pbMesh.GetComponent<UndoTracker>()?.Redo();
                _proximityScaler.ResetScales();
            }       
        }
    }
}
