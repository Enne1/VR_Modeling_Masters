using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

[RequireComponent(typeof(ProBuilderMesh))]
public class EdgeVisualizer : MonoBehaviour
{
    private ProBuilderMesh _pbMesh;

    private Dictionary<(int, int), GameObject> _edgeMarkers = new();
    private Dictionary<(int, int), Vector3> _lastMidpoints = new();
    private Dictionary<int, Vector3> _lastVertexPositions = new();


    private Dictionary<int, int> _vertexToSharedGroup = new();
    private HashSet<(int, int)> _knownEdges = new();

    public GameObject edgePrefab;

    void Start()
    {
        _pbMesh = GetComponent<ProBuilderMesh>();
        if (_pbMesh != null)
        {
            BuildEdgeMarkers();
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (_pbMesh == null) return;

        List<int> modifiedGroups = GetModifiedSharedVertexGroups();
        if (modifiedGroups.Count > 0)
        {
            BuildEdgeMarkers();
        }
    }

    void BuildEdgeMarkers()
    {
        if (_pbMesh == null) return;

        // Step 1: Count all edges (as raw vertex index pairs)
        Dictionary<(int, int), int> edgeCount = new();
        foreach (Face face in _pbMesh.faces)
        {
            if (face.edges.Count != 4) continue;

            foreach (Edge edge in face.edges)
            {
                int a = Mathf.Min(edge.a, edge.b);
                int b = Mathf.Max(edge.a, edge.b);
                var key = (a, b);

                if (!edgeCount.ContainsKey(key))
                    edgeCount[key] = 1;
                else
                    edgeCount[key]++;
            }
        }

        // Step 2: Place markers only on edges that appear once
        HashSet<(int, int)> currentEdges = new();

        foreach (var kvp in edgeCount)
        {
            if (kvp.Value == 1)
            {
                (int a, int b) = kvp.Key;

                if (!_edgeMarkers.ContainsKey((a, b)))
                {
                    CreateEdgeMarker(a, b);
                }

                currentEdges.Add((a, b));
            }
        }

        // Step 3: Update marker positions
        foreach ((int a, int b) in currentEdges)
        {
            if (!_edgeMarkers.TryGetValue((a, b), out GameObject marker)) continue;

            Vector3 posA = _pbMesh.transform.TransformPoint(_pbMesh.positions[a]);
            Vector3 posB = _pbMesh.transform.TransformPoint(_pbMesh.positions[b]);
            Vector3 midpoint = (posA + posB) * 0.5f;

            if (!_lastMidpoints.ContainsKey((a, b)) || Vector3.Distance(_lastMidpoints[(a, b)], midpoint) > 0.001f)
            {
                marker.transform.position = midpoint;
                marker.transform.rotation = Quaternion.LookRotation(posB - posA);
                _lastMidpoints[(a, b)] = midpoint;
            }
        }

        _knownEdges = new HashSet<(int, int)>(currentEdges);
    }

    
    void CreateEdgeMarker(int a, int b)
    {
        Vector3 posA = _pbMesh.transform.TransformPoint(_pbMesh.positions[a]);
        Vector3 posB = _pbMesh.transform.TransformPoint(_pbMesh.positions[b]);
        Vector3 midpoint = (posA + posB) * 0.5f;
        Quaternion rotation = Quaternion.LookRotation(posB - posA);

        GameObject marker = Instantiate(edgePrefab, midpoint, rotation);
        marker.name = $"Edge_{a}_{b}";
        marker.transform.SetParent(_pbMesh.transform, true);

        _edgeMarkers[(a, b)] = marker;
        _lastMidpoints[(a, b)] = midpoint;
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
    
    List<int> GetModifiedSharedVertexGroups()
    {
        List<int> modifiedGroups = new();

        for (int groupIndex = 0; groupIndex < _pbMesh.sharedVertices.Count; groupIndex++)
        {
            int representative = _pbMesh.sharedVertices[groupIndex][0];
            if (representative >= _pbMesh.positions.Count) continue;

            Vector3 worldPos = _pbMesh.transform.TransformPoint(_pbMesh.positions[representative]);

            if (!_lastVertexPositions.ContainsKey(groupIndex) || _lastVertexPositions[groupIndex] != worldPos)
            {
                modifiedGroups.Add(groupIndex);
                _lastVertexPositions[groupIndex] = worldPos;
            }
        }

        return modifiedGroups;
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