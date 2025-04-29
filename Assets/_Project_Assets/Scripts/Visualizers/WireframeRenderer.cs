using UnityEngine;
 using UnityEngine.ProBuilder;
 using UnityEngine.ProBuilder.MeshOperations;
 using System.Collections.Generic;
 
 [RequireComponent(typeof(ProBuilderMesh))]
 public class WireframeRenderer : MonoBehaviour
 {
     public Color wireColor = Color.green;
     private ProBuilderMesh _pbMesh;
     private HashSet<Edge> _quadEdges;
     private int _lastVertexCount;
     public Material wireframeMaterial;

     private void Start()
     {
         _pbMesh = GetComponent<ProBuilderMesh>();
         UpdateWireframe();
     }

    private void Update()
    {
        // Check if the mesh has changed
        if (_pbMesh.vertexCount != _lastVertexCount)
        {
            UpdateWireframe();
            _lastVertexCount = _pbMesh.vertexCount;
        }
    }
    
    /// <summary>
    /// GL Lines used to draw the lines for the wireframe
    /// </summary>
    private void OnRenderObject()
    {
        if (_quadEdges.Count == 0) return;

        wireframeMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(wireColor);

        foreach (Edge edge in _quadEdges)
        {
            DrawEdge(_pbMesh.positions[edge.a], _pbMesh.positions[edge.b]);
        }

        GL.End(); 
    }

 
    /// <summary>
    /// Continuously find the edges that should have a wireframe line drawn between them 
    /// </summary>
     private void UpdateWireframe()
     {
         _quadEdges.Clear(); 
         Dictionary<Edge, int> edgeCount = new Dictionary<Edge, int>();
 
         // Loop over each face in the mesh
         foreach (Face face in _pbMesh.faces)
         {
             if (face.edges.Count == 4)
             {
                 foreach (Edge edge in face.edges)
                 {
                     edgeCount.TryGetValue(edge, out int count);
                     edgeCount[edge] = count + 1;
                 }
             }
         }
 
         // Only add edges that are part of quads
         foreach (var kvp in edgeCount)
         {
             if (kvp.Value == 1)
                 _quadEdges.Add(kvp.Key);
         }
 
         _pbMesh.ToMesh();
         _pbMesh.Refresh();
     }
     
 
    /// <summary>
    /// Get point A and B for the edge which a line should be drawn between
    /// </summary>
     private void DrawEdge(Vector3 v0, Vector3 v1)
     {
         GL.Vertex(transform.TransformPoint(v0));
         GL.Vertex(transform.TransformPoint(v1));
     }
 }