using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class DragVertex : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    private HashSet<int> _selectedVertexIndices; // Store all shared vertices
    private bool _isDragging;
    private Vector3 _initialControllerPos;
    private Dictionary<int, Vector3> _initialVertexPositions;
    private Transform _activeController;
    private List<GameObject> _dragAlongList;
    private GameObject[] _dragAlongGameObjects;
    
    void Start()
    {
        _dragAlongList = new List<GameObject>();
        _objSelector = FindFirstObjectByType<ObjSelector>();
        _selectedVertexIndices = new HashSet<int>();
        _initialVertexPositions = new Dictionary<int, Vector3>();
    }

    void Update()
    {
        if (_isDragging)
        {
            DragVertexMove();
        }
    }

    // Start dragging vertices (Now receives the controller transform as input)
    public void StartDraggingVertex(Transform controllerTransform)
    {
        if (_objSelector == null || _objSelector.ClosestObj == null) return;

        _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        if (_pbMesh == null) return;

        //Store current mesh state in undo Stack
        _pbMesh.GetComponent<UndoTracker>()?.SaveState();
        
        // Get list of vertices to move
        _selectedVertexIndices.Clear();
        _initialVertexPositions.Clear();

        MultiSelectedList padlockSelectedList = _pbMesh.transform.GetComponent<MultiSelectedList>();
        if (padlockSelectedList.selectedPadlocks != null)
        {
            foreach (var padlock in padlockSelectedList.selectedPadlocks)
            {
                _dragAlongList.Add(padlock.transform.parent.gameObject);
            }
            _dragAlongGameObjects = _dragAlongList.ToArray();
        }

        
        // Find initial vertex to drag
        int closestVertexInitial = GetClosestVertex(controllerTransform);
        if (closestVertexInitial != -1)
        {
            foreach (var sharedVertex in _pbMesh.sharedVertices)
            {
                if (sharedVertex.Contains(closestVertexInitial))
                {
                    _selectedVertexIndices.UnionWith(sharedVertex);
                    break;
                }
            }
        }
        
        
        // Collect all locked vertecies, to drag along
        foreach (GameObject obj in _dragAlongGameObjects)
        {
            int closestVertex = GetClosestVertex(obj.transform);
            if (closestVertex != -1)
            {
                foreach (var sharedVertex in _pbMesh.sharedVertices)
                {
                    if (sharedVertex.Contains(closestVertex))
                    {
                        _selectedVertexIndices.UnionWith(sharedVertex);
                        break;
                    }
                }
            }
        }

        if (_selectedVertexIndices.Count > 0)
        {
            _activeController = controllerTransform;
            _initialControllerPos = _activeController.position;
            
            foreach (int vertexIndex in _selectedVertexIndices)
            {
                _initialVertexPositions[vertexIndex] = _pbMesh.transform.TransformPoint(_pbMesh.positions[vertexIndex]);
            }
            
            _isDragging = true;
        }
    }

    // Stop dragging and reset variables
    public void StopDraggingVertex()
    {
        _dragAlongList.Clear();
        Array.Clear(_dragAlongGameObjects, 0, _dragAlongGameObjects.Length);
        _isDragging = false;
        _selectedVertexIndices.Clear();
        _initialVertexPositions.Clear();
        _activeController = null;
    }

    // Move the selected vertices
    void DragVertexMove()
    {
        if (_selectedVertexIndices.Count == 0 || _pbMesh == null || _activeController == null) return;

        Vector3 currentControllerPos = _activeController.position;
        Vector3 movementDelta = currentControllerPos - _initialControllerPos;

        List<Vector3> newPositions = _pbMesh.positions.ToList();

        foreach (int vertexIndex in _selectedVertexIndices)
        {
            Vector3 newVertexPos = _initialVertexPositions[vertexIndex] + movementDelta;
            newPositions[vertexIndex] = _pbMesh.transform.InverseTransformPoint(newVertexPos);
        }

        _pbMesh.positions = newPositions;
        _pbMesh.ToMesh();
        _pbMesh.Refresh();
    }

    // Get the closest vertex index to the given transform
    int GetClosestVertex(Transform referenceTransform)
    {
        if (_pbMesh == null) return -1;

        Vector3 referencePos = referenceTransform.position;
        float minDistance = float.MaxValue;
        int closestVertex = -1;

        for (int i = 0; i < _pbMesh.positions.Count; i++)
        {
            float distance = Vector3.Distance(referencePos, _pbMesh.transform.TransformPoint(_pbMesh.positions[i]));
            if (distance < minDistance)
            {
                minDistance = distance;
                closestVertex = i;
            }
        }

        return closestVertex;
    }
}
