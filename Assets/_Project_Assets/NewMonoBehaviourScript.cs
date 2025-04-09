using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class CheckSignifiers : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _objSelector = FindFirstObjectByType<ObjSelector>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        }
    }

    public void EnsureSignifierUpdate()
    {
        Debug.Log("EnsureSignifierUpdate");
        _pbMesh.GetComponent<VertexVisualizer>()?.EnsureSignifierUpdate();
    }
}