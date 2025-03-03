using UnityEngine;

public class WireframeRenderer : MonoBehaviour
{
    private Mesh _mesh;
    private Material _lineMaterial;

    void Start()
    {
        // Get the ProBuilder mesh
        _mesh = GetComponent<MeshFilter>().mesh;

        // Create a simple unlit material for lines
        _lineMaterial = new Material(Shader.Find("Unlit/Color"));
        _lineMaterial.color = Color.green;
    }

    void OnRenderObject()
    {
        if (_mesh == null || _lineMaterial == null)
            return;

        // Apply the material
        _lineMaterial.SetPass(0);

        GL.Begin(GL.LINES);
        GL.Color(Color.green);

        int[] triangles = _mesh.triangles;
        Vector3[] vertices = _mesh.vertices;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            DrawLine(vertices[triangles[i]], vertices[triangles[i + 1]]);
            DrawLine(vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
            DrawLine(vertices[triangles[i + 2]], vertices[triangles[i]]);
        }

        GL.End();
    }

    private void DrawLine(Vector3 start, Vector3 end)
    {
        GL.Vertex(transform.TransformPoint(start));
        GL.Vertex(transform.TransformPoint(end));
    }
}