using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

public class UndoTracker : MonoBehaviour
{
    private ProBuilderMesh _pbMesh;
    private Stack<GameObject> _undoStack = new Stack<GameObject>();
    public int maxUndoSteps = 50;

    private void Awake()
    {
        _pbMesh = GetComponent<ProBuilderMesh>();
    }

    public void SaveState()
    {
        if (_pbMesh == null) return;

        // Create a snapshot
        GameObject snapshot = Instantiate(gameObject);
        snapshot.name = gameObject.name + "_UndoSnapshot";
        snapshot.SetActive(false);

        // Remove UndoTracker from snapshot
        DestroyImmediate(snapshot.GetComponent<UndoTracker>());

        // Push the snapshot onto the stack
        _undoStack.Push(snapshot);

        // If stack exceeds the limit, remove the *oldest* item (bottom of stack)
        if (_undoStack.Count > maxUndoSteps)
        {
            // To remove the bottom item, we need to:
            // 1. Pop all into a temp list
            // 2. Remove the last item (bottom)
            // 3. Push them back in original order

            List<GameObject> temp = new List<GameObject>();

            while (_undoStack.Count > 0)
                temp.Add(_undoStack.Pop());

            // Remove oldest (last in list)
            GameObject oldest = temp[temp.Count - 1];
            temp.RemoveAt(temp.Count - 1);
            Destroy(oldest);

            // Rebuild the stack in correct order
            for (int i = temp.Count - 1; i >= 0; i--)
                _undoStack.Push(temp[i]);
        }
    }

    public void Undo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        GameObject snapshot = _undoStack.Pop();
        ProBuilderMesh snapshotMesh = snapshot.GetComponent<ProBuilderMesh>();

        if (snapshotMesh == null || _pbMesh == null)
        {
            return;
        }

        // Clear existing children (signifiers)
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Restore mesh data
        _pbMesh.Clear();
        _pbMesh.positions = new List<Vector3>(snapshotMesh.positions);
        _pbMesh.faces = snapshotMesh.faces.Select(f => new Face(f)).ToList();
        _pbMesh.sharedVertices = snapshotMesh.sharedVertices.Select(sv => new SharedVertex(sv)).ToArray();
        _pbMesh.ToMesh();
        _pbMesh.Refresh();

        // Clear multi-selection state
        MultiSelectedList currentSel = GetComponent<MultiSelectedList>();
        if (currentSel != null)
        {
            currentSel.selectedPadlocks.Clear();
            currentSel.selectedFaces.Clear();
        }
        
        Destroy(snapshot); // Cleanup

        GetComponent<VertexVisualizer>()?.RebuildVertices();
        GetComponent<HandleUpdater>()?.RebuildHandles();
    }
}
