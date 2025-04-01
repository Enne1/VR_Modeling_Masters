using UnityEngine;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(ProBuilderMesh))]
[RequireComponent(typeof(LineRenderer))]
public class WireframeRenderer : MonoBehaviour
{
    public Color wireColor = Color.green;
    public float lineWidth = 0.005f;
    public Material wireframeMaterial;

    private ProBuilderMesh pbMesh;
    private LineRenderer lineRenderer;
    private Vector3[] lastVertexPositions;

    private void Start()
    {
        pbMesh = GetComponent<ProBuilderMesh>();
        lineRenderer = GetComponent<LineRenderer>();

        // Set LineRenderer properties
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;

        // Assign material
        if (wireframeMaterial != null)
            lineRenderer.material = wireframeMaterial;
        else
            Debug.LogError("Wireframe material is missing! Assign it in the Inspector.");

        StoreVertexPositions();
        UpdateWireframe();
    }

    private void LateUpdate()
    {
        if (HasMeshChanged() || transform.hasChanged)
        {
            UpdateWireframe();
            transform.hasChanged = false;
        }
    }

    private void StoreVertexPositions()
    {
        lastVertexPositions = pbMesh.positions.ToArray();
    }

    private bool HasMeshChanged()
    {
        if (pbMesh.vertexCount != lastVertexPositions.Length)
            return true;

        for (int i = 0; i < pbMesh.positions.Count; i++)
        {
            if (pbMesh.positions[i] != lastVertexPositions[i])
                return true;
        }

        return false;
    }

    private void UpdateWireframe()
    {
        List<Vector3> wireframePoints = new List<Vector3>();
        Dictionary<Edge, int> edgeCount = new Dictionary<Edge, int>();

        pbMesh.ToMesh();
        pbMesh.Refresh();

        // First, count how many times each edge appears across all faces
        foreach (Face face in pbMesh.faces)
        {
            foreach (Edge edge in face.edges)
            {
                edgeCount.TryGetValue(edge, out int count);
                edgeCount[edge] = count + 1;
            }
        }

        // Now we will filter out internal edges, keeping only those that are shared by one face.
        List<Edge> outerEdges = new List<Edge>();
        foreach (var kvp in edgeCount)
        {
            if (kvp.Value == 1) // Only include edges that appear once (i.e., outer edges)
            {
                outerEdges.Add(kvp.Key);
            }
        }

        // Add these outer edges to the wireframe points
        foreach (Edge edge in outerEdges)
        {
            // Transform the edge vertices into world space
            Vector3 startVertex = transform.TransformPoint(pbMesh.positions[edge.a]);
            Vector3 endVertex = transform.TransformPoint(pbMesh.positions[edge.b]);

            wireframePoints.Add(startVertex);
            wireframePoints.Add(endVertex);
        }

        // Update LineRenderer with the outer edges
        lineRenderer.positionCount = wireframePoints.Count;
        lineRenderer.SetPositions(wireframePoints.ToArray());

        StoreVertexPositions();
    }
}
