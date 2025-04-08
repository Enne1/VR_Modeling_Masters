using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class MergeFaces : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    
    public float vertexMergeThreshold = 0.001f;

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

    public void MergeCloseFaces()
    {
        if (_pbMesh == null) {
            return;
        }

        List<Face> faces = new List<Face>(_pbMesh.faces);
        int mergeCount = 0;

        for (int i = 0; i < faces.Count; i++)
        {
            for (int j = i + 1; j < faces.Count; j++)
            {
                Face a = faces[i];
                Face b = faces[j];

                if (FacesCanBeMerged(a, b))
                {
                    // Combine the indexes from both faces
                    HashSet<int> combinedIndexes = new HashSet<int>(a.indexes);
                    foreach (int idx in b.indexes)
                    {
                        combinedIndexes.Add(idx);
                    }
                    
                    //Store current mesh state in undo Stack
                    //_pbMesh.GetComponent<UndoTracker>()?.SaveState();
                    
                    foreach (Transform child in _pbMesh.transform)
                    {
                        Destroy(child.gameObject);
                    }
                    _pbMesh.GetComponent<HandleUpdater>()?.RebuildHandles();
                    _pbMesh.GetComponent<VertexVisualizer>()?.RebuildVertices();
                    
                    VertexEditing.WeldVertices(_pbMesh, combinedIndexes, vertexMergeThreshold);
                    
                    // Clear face and padlock selections to prevent broken references after merge
                    MultiSelectedList selList = _pbMesh.GetComponent<MultiSelectedList>();
                    if (selList != null)
                    {
                        selList.selectedFaces.Clear();
                        selList.selectedPadlocks.Clear();
                        Debug.Log("[Merge] Cleared multi-selections after merge.");
                    }
                    
                    mergeCount++;
                    break; // Avoid modifying structure mid-loop
                }
            }
        }

        _pbMesh.ToMesh();
        _pbMesh.Refresh();
    }

    private bool FacesCanBeMerged(Face a, Face b)
    {
        List<Vector3> aVerts = GetWorldPositions(a);
        List<Vector3> bVerts = GetWorldPositions(b);

        int closeCount = 0;
        foreach (var av in aVerts)
        {
            foreach (var bv in bVerts)
            {
                if (Vector3.Distance(av, bv) < vertexMergeThreshold)
                {
                    closeCount++;
                    break;
                }
            }
        }

        return closeCount >= 4;
    }

    private List<Vector3> GetWorldPositions(Face face)
    {
        List<Vector3> verts = new List<Vector3>();
        foreach (int i in face.indexes)
        {
            verts.Add(_pbMesh.transform.TransformPoint(_pbMesh.positions[i]));
        }
        return verts;
    }
} 
