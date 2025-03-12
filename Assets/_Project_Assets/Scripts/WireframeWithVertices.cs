using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

public class WireframeWithVertices : MonoBehaviour
{
    // private variables
    private ProBuilderMesh _pbMesh;
    private GameObject _marker;
    private List<LineRenderer> _lineRenderers = new List<LineRenderer>();
    
    // public varaibles
    public float vertexSize = 0.02f;
    public GameObject markerPrefab;
    public Material lineMaterial;
    public bool updateWireframe = false;
    
    
    // Set the initial layout of the wireframe
    private void Start()
    {
        _pbMesh = GetComponent<ProBuilderMesh>();
        
        if (_pbMesh == null)
        {
            return;
        }
        UpdateVertexMarker();
        DrawEdges();
    }

    // Update wireframe continuously when the object is being manipulated
    private void Update()
    {
        if (updateWireframe)
        {
            UpdateVertexMarker();
            DrawEdges();
        }
    }

    // Place vertex markers on every vertex of the selected object
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
    
    // Place edge markers on every edge of the selected object
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

                // Ensure consistent ordering (smallest index first)
                var edge = startIdx < endIdx ? (startIdx, endIdx) : (endIdx, startIdx);
                
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
                
                if (CompareVectors(start, end) > 0)
                {
                    wireframeEdges.Add((end, start)); // Add in sorted order
                }
                else
                {
                    wireframeEdges.Add((start, end)); // Add in sorted order
                }  // Only add the edge if it's not already present
            }
        }
        
        foreach (var edge in wireframeEdges)
        {
            CreateEdgeLine(edge.Item1, edge.Item2);
        }
    }
    
    // Compare vectors component-wise to enforce an order
    private int CompareVectors(Vector3 a, Vector3 b)
    {
        if (a.x != b.x) return a.x.CompareTo(b.x);
        if (a.y != b.y) return a.y.CompareTo(b.y);
        return a.z.CompareTo(b.z);
    }
    
    // use the start and end coordinate of an edge (its two vertecies) to draw the edge as a LineRenderer
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