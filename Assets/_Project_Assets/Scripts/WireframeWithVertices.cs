using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder;

public class WireframeWithVertecies : MonoBehaviour
{
    private ProBuilderMesh _pbMesh;
    public float vertexSize = 0.02f; // Adjust for VR visibility
    public GameObject markerPrefab;
    private GameObject _marker;
    
    private void Start()
    {
        _pbMesh = GetComponent<ProBuilderMesh>();
        
        if (_pbMesh == null)
        {
            Debug.Log("No ProBuilderMesh component found on this GameObject.");
            return;
        }
        UpdateVertexMarker();
        
    }

    public void UpdateVertexMarker()
    {
        Transform parentTransform = transform;

        // Find all the children of the parent object with the "VertexMarker" tag
        foreach (Transform child in parentTransform)
        {
            if (child.CompareTag("VertexMarker"))
            {
                // Destroy the child if it has the "VertexMarker" tag
                Destroy(child.gameObject);
            }
        }

        // Use a HashSet to ensure we get only distinct vertex positions
        HashSet<Vector3> uniqueVertexPositions = new HashSet<Vector3>();

        // Loop through all the faces and collect the vertex positions
        foreach (var face in _pbMesh.faces)
        {
            // Add each vertex position referenced by the face
            foreach (var index in face.indexes)
            {
                uniqueVertexPositions.Add(_pbMesh.positions[index]);
            }
        }

        // Instanciate marker on each vertex
        foreach (Vector3 vertex in uniqueVertexPositions)
        {
            // Instantiate the vertex marker prefab
            _marker = Instantiate(markerPrefab, transform.position, Quaternion.identity);
            
            // Make the marker a child of the ProBuilder object to follow its position
            _marker.transform.SetParent(transform);
            
            // Define the scale of the marker
            _marker.transform.localScale = Vector3.one * vertexSize;
            
            // Optionally, if the marker is far from the object, you might want to adjust the position
            _marker.transform.localPosition = vertex;  // Local position relative to ProBuilder object
        }
    }
}