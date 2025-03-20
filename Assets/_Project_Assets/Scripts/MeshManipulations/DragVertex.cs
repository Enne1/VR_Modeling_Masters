using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class DragVertex : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    private HashSet<int> _selectedVertexIndices = new HashSet<int>(); // Store all shared vertices
    private bool _isDragging;
    private Vector3 _initialControllerPos;
    private Vector3 _initialVertexPos;
    private Transform _activeController; // The transform of the controller currently used for dragging
    
    void Start()
    {
        _objSelector = FindFirstObjectByType<ObjSelector>();
    }

    void Update()
    {
        if (_isDragging)
        {
            DragVertexMove();
        }
    }

    // Start dragging a vertex (Now receives the controller transform as input)
    public void StartDraggingVertex(Transform controllerTransform)
    {
        if (_objSelector == null || _objSelector.ClosestObj == null) return;

        _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        if (_pbMesh == null) return;

        int closestVertex = GetClosestVertex(controllerTransform);

        if (closestVertex != -1)
        {
            // Store active controller
            _activeController = controllerTransform;

            // Find and store all connected (shared) vertices
            _selectedVertexIndices.Clear();
            foreach (var sharedVertex in _pbMesh.sharedVertices)
            {
                if (sharedVertex.Contains(closestVertex))
                {
                    _selectedVertexIndices.UnionWith(sharedVertex);
                    break;
                }
            }

            _initialControllerPos = _activeController.position;
            _initialVertexPos = _pbMesh.transform.TransformPoint(_pbMesh.positions[closestVertex]);
            _isDragging = true;
        }
    }

    // Stop dragging and reset variables
    public void StopDraggingVertex()
    {
        _isDragging = false;
        _selectedVertexIndices.Clear();
        _activeController = null;
    }

    // Move the selected vertex along with all connected vertices
    void DragVertexMove()
    {
        if (_selectedVertexIndices.Count == 0 || _pbMesh == null || _activeController == null) return;

        Vector3 currentControllerPos = _activeController.position;
        Vector3 movementDelta = currentControllerPos - _initialControllerPos;

        // Copy vertex positions so we can modify them
        List<Vector3> newPositions = _pbMesh.positions.ToList();

        foreach (int vertexIndex in _selectedVertexIndices)
        {
            Vector3 newVertexPos = _initialVertexPos + movementDelta;
            newPositions[vertexIndex] = _pbMesh.transform.InverseTransformPoint(newVertexPos);
        }

        // Apply changes
        _pbMesh.positions = newPositions;
        _pbMesh.ToMesh();
        _pbMesh.Refresh();
    }

    // Get the closest vertex index to the given controller transform
    int GetClosestVertex(Transform controllerTransform)
    {
        if (_pbMesh == null) return -1;

        Vector3 controllerPos = controllerTransform.position;
        float minDistance = float.MaxValue;
        int closestVertex = -1;

        for (int i = 0; i < _pbMesh.positions.Count; i++)
        {
            float distance = Vector3.Distance(controllerPos, _pbMesh.transform.TransformPoint(_pbMesh.positions[i]));
            if (distance < minDistance)
            {
                minDistance = distance;
                closestVertex = i;
            }
        }

        return closestVertex;
    }
}
