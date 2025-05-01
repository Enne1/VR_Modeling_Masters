using System.Collections.Generic;
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
    
    /// <summary>
    /// Remap every vertex index to its shared‐group representative raw index
    /// </summary>
    void BuildVertexToSharedGroupLookup()
    {
        _vertexToSharedGroup.Clear();
        // for each shared‐vert group, pick group[0] as the rep
        foreach (var shared in _pbMesh.sharedVertices)
        {
            int rep = shared[0];
            foreach (int vi in shared)
                _vertexToSharedGroup[vi] = rep;
        }
    }
    
    /// <summary>
    /// Rebuild (and update) all edge markers
    /// </summary>
    void BuildEdgeMarkers()
    {
        if (_pbMesh == null) return;

        // make sure shared‐vertex lookup is up to date
        BuildVertexToSharedGroupLookup();

        // collect all unique edges, as (repA,repB)
        var uniqueEdges = new HashSet<(int, int)>();
        foreach (var face in _pbMesh.faces)
        {
            if (face.edges.Count != 4) continue;
            foreach (var e in face.edges)
            {
                int repA = _vertexToSharedGroup[e.a];
                int repB = _vertexToSharedGroup[e.b];
                
                // sort so (3,7) not (7,3)
                int a = Mathf.Min(repA, repB);
                int b = Mathf.Max(repA, repB);
                uniqueEdges.Add((a, b));
            }
        }

        // create any new edge markers
        foreach (var edge in uniqueEdges)
        {
            if (!_edgeMarkers.ContainsKey(edge))
                CreateEdgeMarker(edge.Item1, edge.Item2);
        }

        // update positions + rotations of all existing markers
        foreach (var kvp in _edgeMarkers)
        {
            (int a, int b) = kvp.Key;
            var marker = kvp.Value;

            Vector3 posA = _pbMesh.transform.TransformPoint(_pbMesh.positions[a]);
            Vector3 posB = _pbMesh.transform.TransformPoint(_pbMesh.positions[b]);
            Vector3 mid  = (posA + posB) * 0.5f;

            // only move if it’s actually changed
            if (!_lastMidpoints.TryGetValue(kvp.Key, out Vector3 prev) 
                || Vector3.Distance(prev, mid) > 0.001f)
            {
                marker.transform.position = mid;
                marker.transform.rotation = Quaternion.LookRotation(posB - posA);
                _lastMidpoints[kvp.Key] = mid;
            }
        }
    }
    
    /// <summary>
    /// Instantiate a marker for edge (repA,repB)
    /// </summary>
    void CreateEdgeMarker(int repA, int repB)
    {
        Vector3 posA = _pbMesh.transform.TransformPoint(_pbMesh.positions[repA]);
        Vector3 posB = _pbMesh.transform.TransformPoint(_pbMesh.positions[repB]);
        Vector3 mid  = (posA + posB) * 0.5f;
        Quaternion rot = Quaternion.LookRotation(posB - posA);

        var marker = Instantiate(edgePrefab, mid, rot, _pbMesh.transform);
        marker.name = $"Edge_{repA}_{repB}";

        marker.GetComponent<EdgeMarkerData>()?.SetEdge(repA, repB);
        
        _edgeMarkers[(repA, repB)] = marker;
        _lastMidpoints[(repA, repB)] = mid;
    }

    /// <summary>
    /// Check if any changes have been made to the mesh, and thus if the edge visualizer needs to update
    /// </summary>
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

    /// <summary>
    /// Clear signifiers, used for rebuild of all mesh signifiers
    /// </summary>
    public void ClearAll()
    {
        _edgeMarkers.Clear();
        _lastMidpoints.Clear();
        _vertexToSharedGroup.Clear();

        List<Transform> childrenToDestroy = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Edge_"))
            {
                childrenToDestroy.Add(child);
            }
        }

        foreach (var child in childrenToDestroy)
        {
            Destroy(child.gameObject);
        }
    }
    
    /// <summary>
    /// Begin rebuild of signifiers
    /// </summary>
    public void ReBuildEdges()
    {
        if (_pbMesh == null)
            _pbMesh = GetComponent<ProBuilderMesh>();

        ClearAll();
        BuildEdgeMarkers();
    }
}