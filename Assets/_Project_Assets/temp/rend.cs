using UnityEngine;
 using UnityEngine.ProBuilder;
 using UnityEngine.ProBuilder.MeshOperations;
 using System.Collections.Generic;
 
 [RequireComponent(typeof(ProBuilderMesh))]
 public class rend : MonoBehaviour
 {
     public Color wireColor = Color.green; // Wireframe color
     private ProBuilderMesh pbMesh;
     private HashSet<Edge> quadEdges = new HashSet<Edge>();
     private int lastVertexCount = 0; // Track mesh changes
     public Material WireframeMaterial;
 
     private void Start()
     {
         pbMesh = GetComponent<ProBuilderMesh>();
         UpdateWireframe(); // Initial setup
     }
 
     private void Update()
     {
         // Check if the mesh has changede
         if (pbMesh.vertexCount != lastVertexCount)
         {
             UpdateWireframe();
             lastVertexCount = pbMesh.vertexCount;
         }
     }
 
     private void OnRenderObject()
     {
         if (quadEdges.Count == 0) return;
 
         // Use GL to render the edges
         //Material lineMaterial = WireframeMaterial;//new Material(Shader.Find("Hidden/Internal-Colored"));
         WireframeMaterial.SetPass(0);//lineMaterial.SetPass(0);
         GL.Begin(GL.LINES);
         GL.Color(wireColor);
 
         foreach (Edge edge in quadEdges)
         {
             DrawEdge(pbMesh.positions[edge.a], pbMesh.positions[edge.b]);
         }
 
         GL.End();
     }
 
     private void UpdateWireframe()
     {
         quadEdges.Clear(); // Clear previous edges
         Dictionary<Edge, int> edgeCount = new Dictionary<Edge, int>();
 
         foreach (Face face in pbMesh.faces)
         {
             if (face.edges.Count == 4) // Ensure the face is a quad
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
             if (kvp.Value == 1) // Unique edges (not internal)
                 quadEdges.Add(kvp.Key);
         }
 
         pbMesh.ToMesh();
         pbMesh.Refresh();
     }
 
     private void DrawEdge(Vector3 v0, Vector3 v1)
     {
         GL.Vertex(transform.TransformPoint(v0));
         GL.Vertex(transform.TransformPoint(v1));
     }
 }