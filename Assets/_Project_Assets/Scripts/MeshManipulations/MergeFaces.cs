using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class MergeFaces : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    
    public float vertexMergeThreshold = 0.01f;

    void Start()
    {
        _objSelector = FindFirstObjectByType<ObjSelector>();
    }

    void Update()
    {
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        }
    }

    /// <summary>
    /// Call this to check if two faces can be merged together, if so the faces gets merged together
    /// Removes the faces of the parts merged together, and clean up the mesh and signifiers
    /// </summary>
    public void MergeCloseFaces()
    {
        if (_pbMesh == null) {
            return;
        }

        List<Face> faces = new List<Face>(_pbMesh.faces);

        for (int i = 0; i < faces.Count; i++)
        {
            for (int j = i + 1; j < faces.Count; j++)
            {
                Face a = faces[i];
                Face b = faces[j];

                if (FacesCanBeMerged(a, b))
                {
                    // Combine the vertex indexes
                    HashSet<int> combinedIndexes = new HashSet<int>(a.indexes);
                    foreach (int idx in b.indexes)
                        combinedIndexes.Add(idx);

                    // Save original face references BEFORE modifying mesh
                    Face originalA = a;
                    Face originalB = b;

                    // Save for undo
                    _pbMesh.GetComponent<UndoTracker>()?.SaveState();

                    foreach (Transform child in _pbMesh.transform)
                        Destroy(child.gameObject);

                    _pbMesh.GetComponent<HandleUpdater>()?.ClearAll();
                    _pbMesh.GetComponent<VertexVisualizer>()?.ClearAll();
                    _pbMesh.GetComponent<EdgeVisualizer>()?.ClearAll();

                    // Weld vertices first (modifies mesh)
                    VertexEditing.WeldVertices(_pbMesh, combinedIndexes, vertexMergeThreshold);

                    // Remove the exact face objects merged
                    var newFaces = new List<Face>(_pbMesh.faces);
                    newFaces.RemoveAll(f => f == originalA || f == originalB);
                    _pbMesh.faces = newFaces;
                    
                    // Refresh mesh and visuals
                    _pbMesh.ToMesh();
                    _pbMesh.Refresh();

                    _pbMesh.GetComponent<HandleUpdater>()?.RebuildHandles();
                    _pbMesh.GetComponent<VertexVisualizer>()?.RebuildVertices();
                    _pbMesh.GetComponent<EdgeVisualizer>()?.ReBuildEdges();
                }
            }
        }
        _pbMesh.ToMesh();
        _pbMesh.Refresh();
    }

    /// <summary>
    /// Boolean operation used to check if all four vertices of one face is close enough to all four vertices of another face, so they can merge
    /// </summary>
    private bool FacesCanBeMerged(Face a, Face b)
    {
        var aDistinct = a.distinctIndexes;
        var bDistinct = b.distinctIndexes;

        // make sure they have the same number of vertices
        if (aDistinct.Count != bDistinct.Count)
            return false;

        // work in squared units to avoid repeated sqrt
        float thrSqr = vertexMergeThreshold * vertexMergeThreshold;

        // for each corner on A, find a partner on B
        foreach (int ai in aDistinct)
        {
            Vector3 worldA = _pbMesh.transform.TransformPoint(_pbMesh.positions[ai]);
            bool found = false;

            foreach (int bi in bDistinct)
            {
                Vector3 worldB = _pbMesh.transform.TransformPoint(_pbMesh.positions[bi]);
                float distSqr = (worldA - worldB).sqrMagnitude;

                if (distSqr <= thrSqr)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        // all corners matched
        return true;
    }
} 