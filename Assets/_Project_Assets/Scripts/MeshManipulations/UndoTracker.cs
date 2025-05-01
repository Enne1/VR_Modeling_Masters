using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

public class UndoTracker : MonoBehaviour
{
    private ProBuilderMesh _pbMesh;
    private Stack<GameObject> _undoStack = new();
    private Stack<GameObject> _redoStack = new();

    public int maxUndoSteps = 50;

    void Awake()
    {
        _pbMesh = GetComponent<ProBuilderMesh>();
    }

    /// <summary>
    /// Call this to record the current mesh state before a new change is made to the mesh
    /// Should be called before operations such as Extrude, Drag, Loop-Cut, ect. 
    /// Clears redo history, since new edits invalidate the redo stack.
    /// </summary>
    public void SaveState()
    {
        if (_pbMesh == null) return;

        _redoStack.Clear();

        // Create a snapshot of the GameObject in current state
        GameObject snapshot = Instantiate(gameObject);
        snapshot.name = $"{gameObject.name}_UndoSnapshot";
        snapshot.SetActive(false);

        // Remove this component from the snapshot so it doesn't keep stacking
        DestroyImmediate(snapshot.GetComponent<UndoTracker>());

        // Push it onto undo stack
        _undoStack.Push(snapshot);

        // Trim the oldest undo if list exceed max steps
        if (_undoStack.Count > maxUndoSteps)
        {
            // Pop everything out, drop the last, then re‐push
            List<GameObject> temp = new List<GameObject>();
            while (_undoStack.Count > 0) temp.Add(_undoStack.Pop());
            Destroy(temp[temp.Count - 1]); // delete the oldest 
            temp.RemoveAt(temp.Count - 1);
            for (int i = temp.Count - 1; i >= 0; i--)
                _undoStack.Push(temp[i]);
        }
    }

    /// <summary>
    /// Reverts to the last snapshot.  Captures the current state into the redo stack.
    /// </summary>
    public void Undo(bool isExtrude)
    {
        if (_pbMesh == null || _undoStack.Count == 0) return;

        // Snapshot current state for redo
        GameObject redoSnapshot = Instantiate(gameObject);
        redoSnapshot.name = $"{gameObject.name}_RedoSnapshot";
        redoSnapshot.SetActive(false);
        DestroyImmediate(redoSnapshot.GetComponent<UndoTracker>());
        if (!isExtrude)
        {
            _redoStack.Push(redoSnapshot);
        }

        // Apply the  undo snapshot
        GameObject snapshot = _undoStack.Pop();
        ApplySnapshot(snapshot);
        Destroy(snapshot);
    }

    /// <summary>
    /// Reapplies a change that was previously undone.  
    /// Captures the current state back into the undo stack.
    /// </summary>
    public void Redo()
    {
        if (_pbMesh == null || _redoStack.Count == 0) return;
        
        GameObject undoSnapshot = Instantiate(gameObject);
        undoSnapshot.name = $"{gameObject.name}_UndoSnapshot";
        undoSnapshot.SetActive(false);
        DestroyImmediate(undoSnapshot.GetComponent<UndoTracker>());
        _undoStack.Push(undoSnapshot);

        // Apply the redo snapshot
        GameObject snapshot = _redoStack.Pop();
        ApplySnapshot(snapshot);
        Destroy(snapshot);
    }

    /// <summary>
    /// Common code to restore mesh data from a snapshot GameObject.
    /// </summary>
    private void ApplySnapshot(GameObject snapshot)
    {
        var snapMesh = snapshot.GetComponent<ProBuilderMesh>();
        if (snapMesh == null || _pbMesh == null) return;

        // Remove any existing visual children (handles, markers, etc.)
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        // Copy mesh data
        _pbMesh.Clear();
        _pbMesh.positions = new List<Vector3>(snapMesh.positions);
        _pbMesh.faces = snapMesh.faces.Select(f => new Face(f)).ToList();
        _pbMesh.sharedVertices = snapMesh.sharedVertices
            .Select(sv => new SharedVertex(sv)).ToArray();

        // Rebuild
        _pbMesh.ToMesh();
        _pbMesh.Refresh();

        // Clear any multi‐selection state
        var sel = GetComponent<MultiSelectedList>();
        if (sel != null)
        {
            sel.selectedPadlocks.Clear();
            sel.selectedFaces.Clear();
        }

        // Restore visuals
        GetComponent<VertexVisualizer>()?.RebuildVertices();
        GetComponent<HandleUpdater>()?.RebuildHandles();
        GetComponent<EdgeVisualizer>()?.ReBuildEdges();
    }
}