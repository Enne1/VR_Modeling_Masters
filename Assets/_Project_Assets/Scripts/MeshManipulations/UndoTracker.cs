using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Linq;

public class UndoTracker : MonoBehaviour
{
    private ProBuilderMesh _pbMesh;
    private Stack<MeshState> _undoStack = new Stack<MeshState>();
    private int maxUndoSteps = 10;

    private void Start()
    {
        _pbMesh = GetComponent<ProBuilderMesh>();
    }

    public void SaveState()
    {
        if (_pbMesh == null) return;

        var state = new MeshState
        {
            positions = new List<Vector3>(_pbMesh.positions),
            faces = _pbMesh.faces.Select(f => new Face(f)).ToList(),
            sharedVertices = _pbMesh.sharedVertices
                .Select(sv => new SharedVertex(sv)).ToList()
        };

        _undoStack.Push(state);
        if (_undoStack.Count > maxUndoSteps)
            _undoStack = new Stack<MeshState>(_undoStack.Take(maxUndoSteps));
    }


    public void Undo()
    {
        if (_undoStack.Count == 0 || _pbMesh == null)
        {
            Debug.LogWarning("Undo failed: No states or _pbMesh is null.");
            return;
        }

        var prevState = _undoStack.Pop();

        foreach (Transform child in _pbMesh.transform)
        {
            Destroy(child.gameObject);
        }
        GetComponent<HandleUpdater>()?.ClearAll();
        GetComponent<VertexVisualizer>()?.ClearAll();
        
        _pbMesh.Clear();

        // Restore mesh data
        _pbMesh.positions = new List<Vector3>(prevState.positions);
        _pbMesh.faces = prevState.faces.Select(f => new Face(f)).ToList();
        _pbMesh.sharedVertices = prevState.sharedVertices
            .Select(sv => new SharedVertex(sv)).ToArray();

        _pbMesh.ToMesh();
        _pbMesh.Refresh(); 
    }
    
    [System.Serializable]
    public class MeshState
    {
        public List<Vector3> positions;
        public List<Face> faces;
        public List<SharedVertex> sharedVertices;
    }
}