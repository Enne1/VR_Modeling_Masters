using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

[RequireComponent(typeof(ProBuilderMesh))]
public class EdgeVisualizer : MonoBehaviour
{
    private ProBuilderMesh _pbMesh;

    private Dictionary<(int, int), GameObject> _edgeMarkers = new();
    private Dictionary<(int, int), Vector3> _lastMidpoints = new();

    private Dictionary<int, int> _vertexToSharedGroup = new();
    private HashSet<(int, int)> _knownEdges = new();

    public GameObject edgePrefab;

    void Start()
    {
        _pbMesh = GetComponent<ProBuilderMesh>();
        if (_pbMesh != null)
        {
            //RebuildEdgeMarkers();
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (_pbMesh == null) return;

        // Step 1: Rebuild lookup
        BuildVertexToSharedGroupLookup();

        // Step 2: Scan all current shared edges
        HashSet<(int, int)> currentEdges = new();
        foreach (Face face in _pbMesh.faces)
        {
            foreach (Edge edge in face.edges)
            {
                if (!_vertexToSharedGroup.ContainsKey(edge.a) || !_vertexToSharedGroup.ContainsKey(edge.b))
                    continue;

                int sharedA = _vertexToSharedGroup[edge.a];
                int sharedB = _vertexToSharedGroup[edge.b];
                int a = Mathf.Min(sharedA, sharedB);
                int b = Mathf.Max(sharedA, sharedB);
                currentEdges.Add((a, b));
            }
        }

        // Step 3: Add new signifiers for new edges
        foreach ((int a, int b) in currentEdges)
        {
            if (!_knownEdges.Contains((a, b)))
            {
                CreateEdgeMarker(a, b);
                _knownEdges.Add((a, b));
            }
        }

        // Step 4: Check if existing midpoints have changed
        foreach ((int a, int b) in _knownEdges)
        {
            int repA = _pbMesh.sharedVertices[a][0];
            int repB = _pbMesh.sharedVertices[b][0];

            if (repA >= _pbMesh.positions.Count || repB >= _pbMesh.positions.Count)
                continue;

            Vector3 posA = _pbMesh.transform.TransformPoint(_pbMesh.positions[repA]);
            Vector3 posB = _pbMesh.transform.TransformPoint(_pbMesh.positions[repB]);
            Vector3 midpoint = (posA + posB) * 0.5f;

            if (!_lastMidpoints.ContainsKey((a, b)) || Vector3.Distance(_lastMidpoints[(a, b)], midpoint) > 0.001f)
            {
                Quaternion rotation = Quaternion.LookRotation(posB - posA);
                _edgeMarkers[(a, b)].transform.position = midpoint;
                _edgeMarkers[(a, b)].transform.rotation = rotation;
                _lastMidpoints[(a, b)] = midpoint;
            }
        }
    }

    void CreateEdgeMarker(int sharedA, int sharedB)
    {
        int repA = _pbMesh.sharedVertices[sharedA][0];
        int repB = _pbMesh.sharedVertices[sharedB][0];

        if (repA >= _pbMesh.positions.Count || repB >= _pbMesh.positions.Count)
            return;

        Vector3 posA = _pbMesh.transform.TransformPoint(_pbMesh.positions[repA]);
        Vector3 posB = _pbMesh.transform.TransformPoint(_pbMesh.positions[repB]);
        Vector3 midpoint = (posA + posB) * 0.5f;
        Quaternion rotation = Quaternion.LookRotation(posB - posA);

        GameObject marker = Instantiate(edgePrefab, midpoint, rotation);
        marker.name = $"Edge_{sharedA}_{sharedB}";
        marker.transform.SetParent(_pbMesh.transform, true);
        _edgeMarkers[(sharedA, sharedB)] = marker;
        _lastMidpoints[(sharedA, sharedB)] = midpoint;
    }

    void BuildVertexToSharedGroupLookup()
    {
        _vertexToSharedGroup.Clear();
        for (int i = 0; i < _pbMesh.sharedVertices.Count; i++)
        {
            foreach (int vertexIndex in _pbMesh.sharedVertices[i])
            {
                _vertexToSharedGroup[vertexIndex] = i;
            }
        }
    }

    public void ClearAll()
    {
        _edgeMarkers.Clear();
        _lastMidpoints.Clear();
        _vertexToSharedGroup.Clear();
        _knownEdges.Clear();

        List<Transform> toDestroy = new();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Edge_"))
            {
                toDestroy.Add(child);
            }
        }

        foreach (Transform child in toDestroy)
        {
            Destroy(child.gameObject);
        }
    }
}
