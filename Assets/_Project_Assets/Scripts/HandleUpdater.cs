using System;
using UnityEngine;
using System.Linq;
using UnityEngine.ProBuilder;
using Math = UnityEngine.ProBuilder.Math;

public class HandleUpdater : MonoBehaviour
{
    // private variables
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    
    //public variables
    public GameObject handlePrefab;
    
    // Find the object selector
    void Start()
    {
        _objSelector = FindFirstObjectByType<ObjSelector>();
    }
    
    // Get the selected object
    private void Update()
    {
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        }
    }

    // Calculate the center of each face
    Vector3 GetFaceCenter(Face face)
    {
        Vector3 sum = Vector3.zero;
        int count = face.indexes.Count;
        
        foreach (int index in face.indexes)
        {
            sum += _pbMesh.transform.TransformPoint(_pbMesh.positions[index]); // Convert local space to world space
        }
    
        return sum / count;
    }
    
    // Place Handle on each face center of the object
    public void HandleOnFace()
    {
        // Delete all previous instance of handles (prepare for new placements)
        GameObject[] handles = GameObject.FindGameObjectsWithTag("FaceHandle");
        foreach (GameObject handle in handles)
        {
            Destroy(handle);
        }
        
        // Set a handle on each face of the selected object
        foreach (Face face in _pbMesh.faces)
        {
            Vector3 faceCenter = GetFaceCenter(face);
            Vector3 faceNormal = _pbMesh.transform.TransformDirection(Math.Normal(_pbMesh, face));

            float shortestEdge = GetShortestEdge(_pbMesh, face);
            
            // Get a stable "up" direction using one of the edges
            Edge firstEdge = face.edges[0];
            Vector3 edgeDirection = (_pbMesh.positions[firstEdge.a] - _pbMesh.positions[firstEdge.b]).normalized;
            edgeDirection = _pbMesh.transform.TransformDirection(edgeDirection);

            // Ensure the up direction is perpendicular to the normal
            Vector3 up = Vector3.Cross(faceNormal, edgeDirection).normalized;
            Vector3 right = Vector3.Cross(up, faceNormal).normalized;

            // Create a rotation matrix that aligns the handle correctly
            Quaternion rotation = Quaternion.LookRotation(faceNormal, up);

            GameObject handle = Instantiate(handlePrefab, faceCenter, rotation);
            
            // If the face is small, scale down the handle, to not clip out from the face
            if (shortestEdge < 0.09f)
            {
                handle.transform.localScale = Vector3.one * (shortestEdge * 10);
            }
            
            // Attach handle to the shape
            handle.transform.SetParent(_pbMesh.transform, true); 
        }
    }
    
    // calculate the shortest edge on a face, to know if the handle should be scaled down
    float GetShortestEdge(ProBuilderMesh pb, Face face)
    {
        Vector3[] vertices = pb.positions.ToArray(); 
        Edge[] edges = face.edges.ToArray(); 

        float minLength = float.MaxValue;

        foreach (Edge edge in edges)
        {
            Vector3 v1 = vertices[edge.a];
            Vector3 v2 = vertices[edge.b];

            float length = Vector3.Distance(v1, v2); // Compute edge length

            if (length < minLength)
                minLength = length;
        }
        // Return the smallest edge length
        return minLength;
    }
    
    // Clear handles on old selected object
    public void ClearHandles()
    {
        GameObject[] handles = GameObject.FindGameObjectsWithTag("FaceHandle");

        foreach (GameObject handle in handles)
        {
            Destroy(handle);
        }
    }
}
