using System;
using UnityEngine;
using UnityEngine.ProBuilder;

public class VertexLocker : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    private Vector3 _controllerPosition;

    private void Start()
    {
        _objSelector = FindFirstObjectByType<ObjSelector>();
    }

    public void TogglePadlock(Vector3 controller)
    {
        if (_objSelector == null || _objSelector.ClosestObj == null) return;
        
        _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        if (_pbMesh == null) return;
        
        
    }
}
