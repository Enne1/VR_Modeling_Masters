using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

public class WireframeWithVertices : MonoBehaviour
{
    private ProBuilderMesh _pbMesh;
    private GameObject _marker;
    private List<LineRenderer> _lineRenderers = new List<LineRenderer>();
    
    public float vertexSize = 0.02f; // Adjust for VR visibility
    public GameObject markerPrefab;
    public Material lineMaterial; // Material for the edges
    public bool updateWireframe = false;
    
    
    private void Start()
    {
        _pbMesh = GetComponent<ProBuilderMesh>();
        
        if (_pbMesh == null)
        {
            Debug.Log("No ProBuilderMesh component found on this GameObject.");
            return;
        }
        UpdateVertexMarker();
        DrawEdges();
    }

    private void Update()
    {
        if (updateWireframe)
        {
            UpdateVertexMarker();
            DrawEdges();
        }
    }

    private void UpdateVertexMarker()
    {
        Transform parentTransform = transform;

        // Remove existing markers
        foreach (Transform child in parentTransform)
        {
            if (child.CompareTag("VertexMarker"))
            {
                Destroy(child.gameObject);
            }
        }

        HashSet<Vector3> uniqueVertexPositions = new HashSet<Vector3>();
        
        foreach (var face in _pbMesh.faces)
        {
            foreach (var index in face.indexes)
            {
                uniqueVertexPositions.Add(_pbMesh.positions[index]);
            }
        }

        foreach (Vector3 vertex in uniqueVertexPositions)
        {
            _marker = Instantiate(markerPrefab, transform.position, Quaternion.identity);
            _marker.transform.SetParent(transform);
            _marker.transform.localScale = Vector3.one * vertexSize;
            _marker.transform.localPosition = vertex;
        }
    }
    
    private void DrawEdges()
    {
        // Clean up existing lines
        foreach (var line in _lineRenderers)
        {
            Destroy(line.gameObject);
        }
        _lineRenderers.Clear();
        
        HashSet<(Vector3, Vector3)> wireframeEdges = new HashSet<(Vector3, Vector3)>();
        Dictionary<(int, int), int> edgeCounts = new Dictionary<(int, int), int>();

        foreach (var face in _pbMesh.faces)
        {
            int[] indices = face.indexes.ToArray();
            int edgeCount = indices.Length;
            
            for (int i = 0; i < edgeCount; i++)
            {
                int startIdx = indices[i];
                int endIdx = indices[(i + 1) % edgeCount]; // Loop back at end
                
                //Debug.Log(_pbMesh.name + " edge " + startIdx + ", " + endIdx);

                // Ensure consistent ordering (smallest index first)
                var edge = startIdx < endIdx ? (startIdx, endIdx) : (endIdx, startIdx);
                
                //Debug.Log(_pbMesh.name + " edge full " + edge);
                
                // Merge identical edges by counting occurrences
                if (edgeCounts.ContainsKey(edge))
                    edgeCounts[edge]++;
                else
                    edgeCounts[edge] = 1;
            }
        }

        // Draw only unique perimeter edges (edges appearing once)
        foreach (var edge in edgeCounts.Keys)
        {
            if (edgeCounts[edge] == 1) // Only draw edges that appear once
            {
                Vector3 start = transform.TransformPoint(_pbMesh.positions[edge.Item1]);
                Vector3 end = transform.TransformPoint(_pbMesh.positions[edge.Item2]);
                
                //wireframeEdges.Add((start, end));
                
                if (CompareVectors(start, end) > 0)
                {
                    wireframeEdges.Add((end, start)); // Add in sorted order
                }
                else
                {
                    wireframeEdges.Add((start, end)); // Add in sorted order
                }  // Only add the edge if it's not already present
                
                //CreateEdgeLine(start, end);
            }
        }
        
        foreach (var edge in wireframeEdges)
        {
            CreateEdgeLine(edge.Item1, edge.Item2);
            //Debug.Log(_pbMesh.name + " : (" + edge.Item1 + ", " + edge.Item2 + ")");
        }
        
        /*
        foreach (var face in _pbMesh.faces)
        {
            int[] indices = face.indexes.ToArray();
            int edgeCount = indices.Length;

            for (int i = 0; i < edgeCount; i++)
            {
                int startIdx = indices[i];
                int endIdx = indices[(i + 1) % edgeCount]; // Loop back at end
        
                Vector3 start = transform.TransformPoint(_pbMesh.positions[startIdx]);
                Vector3 end = transform.TransformPoint(_pbMesh.positions[endIdx]);

                // Sort the vectors based on their components (x, y, z)
                if (CompareVectors(start, end) > 0)
                {
                    wireframeEdges.Add((end, start)); // Add in sorted order
                }
                else
                {
                    wireframeEdges.Add((start, end)); // Add in sorted order
                }
            }
        }
        
        foreach (var edge in wireframeEdges)
        {
            CreateEdgeLine(edge.Item1, edge.Item2);
            //Debug.Log(_pbMesh.name + " : (" + edge.Item1 + ", " + edge.Item2 + ")");
        }
        */
        // Dictionary to count edge occurrences
        // HashSet<(Vector3, Vector3)> wireframeEdges = new HashSet<(Vector3, Vector3)>();
        //List<Tuple<Vector3, Vector3>> wireframeEdges = new List<Tuple<Vector3, Vector3>>();
        
        
        /*
        Dictionary<(int, int), int> edgeCounts = new Dictionary<(int, int), int>();

        // Count how many times each edge appears across all faces
        foreach (var face in _pbMesh.faces)
        {
            int[] indices = face.indexes.ToArray();
            int edgeCount = indices.Length;
            
            for (int i = 0; i < edgeCount; i++)
            {
                int startIdx = indices[i];
                int endIdx = indices[(i + 1) % edgeCount]; // Loop back at end
                
                //Debug.Log(_pbMesh.name + " edge " + startIdx + ", " + endIdx);

                // Ensure consistent ordering (smallest index first)
                var edge = startIdx < endIdx ? (startIdx, endIdx) : (endIdx, startIdx);
                
                //Debug.Log(_pbMesh.name + " edge full " + edge);
                
                // Merge identical edges by counting occurrences
                if (edgeCounts.ContainsKey(edge))
                    edgeCounts[edge]++;
                else
                    edgeCounts[edge] = 1;
            }
        }

        // Draw only unique perimeter edges (edges appearing once)
        foreach (var edge in edgeCounts.Keys)
        {
            if (edgeCounts[edge] == 1) // Only draw edges that appear once
            {
                Vector3 start = transform.TransformPoint(_pbMesh.positions[edge.Item1]);
                Vector3 end = transform.TransformPoint(_pbMesh.positions[edge.Item2]);
                
                //wireframeEdges.Add((start, end));
                
                wireframeEdges.Add((start, end));  // Only add the edge if it's not already present
                
                
                //CreateEdgeLine(start, end);
            }
        }
        */
        
        //Debug.Log(_pbMesh.name + " : " + wireframeEdges.Count);
    }
    
    // Compare vectors component-wise to enforce an order
    private int CompareVectors(Vector3 a, Vector3 b)
    {
        if (a.x != b.x) return a.x.CompareTo(b.x);
        if (a.y != b.y) return a.y.CompareTo(b.y);
        return a.z.CompareTo(b.z);
    }
    
    private void CreateEdgeLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("Edge");
        lineObj.transform.SetParent(transform);
        
        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.material = lineMaterial;
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.005f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        
        _lineRenderers.Add(lineRenderer);
    }
}


/*
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
*/