using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class DragFace : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    private Face _selectedFace;
    private bool _isDragging;
    private Vector3 _initialControllerPos;
    private Vector3 _initialFaceCenter;
    private Vector3 _currControllerPos;
    private List<Face> _dragAlongFaces = new List<Face>();

    // New: Store union of all selected vertex indices and their initial positions
    private HashSet<int> _selectedVertexIndices = new HashSet<int>();
    private Dictionary<int, Vector3> _initialVertexWorldPositions = new Dictionary<int, Vector3>();

    public GameObject leftController;
    public float minDragDistance;
    private LineRenderer _normalAxisLineRenderer;
    public Material normalAxisMaterial;

    void Start()
    {
        _normalAxisLineRenderer = gameObject.AddComponent<LineRenderer>();
        _normalAxisLineRenderer.startWidth = 0.005f;
        _normalAxisLineRenderer.endWidth = 0.005f;
        _normalAxisLineRenderer.positionCount = 2;
        _normalAxisLineRenderer.material = normalAxisMaterial;
        _normalAxisLineRenderer.enabled = false;
        
        _objSelector = FindFirstObjectByType<ObjSelector>();
    }

    void Update()
    {
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        }
        if (_isDragging)
        {
            DragFaceUpdater();
        }
    }
    
    public void StartDraggingFace()
    {
        //Store current mesh state in undo Stack
        _pbMesh.GetComponent<UndoTracker>()?.SaveState();
        
        _selectedFace = GetClosestFace(leftController.transform);
        if (_selectedFace == null) return;
        
        _initialControllerPos = leftController.transform.position;
        _initialFaceCenter = GetFaceCenter(_selectedFace);
        
        _dragAlongFaces.Clear();
        // Clear the vertex set and dictionary
        _selectedVertexIndices.Clear();
        _initialVertexWorldPositions.Clear();
        
        // Retrieve multi-face selection list (assumed to be set up already)
        MultiSelectedList facesSelectedList = _pbMesh.transform.GetComponent<MultiSelectedList>();
        
        if (facesSelectedList != null && facesSelectedList.selectedFaces != null)
        {
            foreach (var faceObj in facesSelectedList.selectedFaces)
            {
                Face closestFace = GetClosestFace(faceObj.transform.parent);
                if (closestFace != null && closestFace != _selectedFace && !_dragAlongFaces.Contains(closestFace))
                {
                    _dragAlongFaces.Add(closestFace);
                }
            }
        }
        
        // Build a union of vertex indices from the main face and the additional faces.
        foreach (int index in _selectedFace.distinctIndexes)
        {
            _selectedVertexIndices.Add(index);
        }
        foreach (Face face in _dragAlongFaces)
        {
            foreach (int index in face.distinctIndexes)
            {
                _selectedVertexIndices.Add(index);
            }
        }
        
        // Now, include any vertices in the entire mesh that share the same world coordinate 
        // as any vertex already in _selectedVertexIndices.
        var selectedIndicesCopy = new List<int>(_selectedVertexIndices);
        foreach (int selectedIndex in selectedIndicesCopy)
        {
            Vector3 selectedWorldPos = _pbMesh.transform.TransformPoint(_pbMesh.positions[selectedIndex]);
            for (int i = 0; i < _pbMesh.positions.Count; i++)
            {
                if (!_selectedVertexIndices.Contains(i))
                {
                    Vector3 worldPos = _pbMesh.transform.TransformPoint(_pbMesh.positions[i]);
                    // Use a small tolerance to account for floating point precision.
                    if (Vector3.Distance(worldPos, selectedWorldPos) < 0.0001f)
                    {
                        _selectedVertexIndices.Add(i);
                    }
                }
            }
        }
        
        // Record the initial world positions for each vertex in the final selection.
        foreach (int index in _selectedVertexIndices)
        {
            Vector3 worldPos = _pbMesh.transform.TransformPoint(_pbMesh.positions[index]);
            _initialVertexWorldPositions[index] = worldPos;
        }
        
        _isDragging = true;
        Vector3 localNormal = Math.Normal(_pbMesh, _selectedFace);
        Vector3 faceNormal = _pbMesh.transform.TransformDirection(localNormal);
        _normalAxisLineRenderer.enabled = true;
        _normalAxisLineRenderer.SetPosition(0, faceNormal * 100 + _initialFaceCenter);
        _normalAxisLineRenderer.SetPosition(1, faceNormal * -100 + _initialFaceCenter);
    }
    
    public void StopDraggingFace()
    {
        _isDragging = false;
        _selectedFace = null;
        _dragAlongFaces.Clear();
        _selectedVertexIndices.Clear();
        _initialVertexWorldPositions.Clear();
        _normalAxisLineRenderer.enabled = false;
    }

    void DragFaceUpdater()
    {
        if (_selectedFace == null || _pbMesh == null) return;
    
        _currControllerPos = leftController.transform.position;
        Vector3 movementDelta = _currControllerPos - _initialControllerPos;
    
        // Compute movement along the main face normal.
        Vector3 localNormal = Math.Normal(_pbMesh, _selectedFace);
        Vector3 faceNormal = _pbMesh.transform.TransformDirection(localNormal);
        float movementAlongNormal = Vector3.Dot(movementDelta, faceNormal);
        Vector3 constrainedMovement = faceNormal * movementAlongNormal;
    
        // Decide if we snap to movement along the normal or use full movement.
        float snapThreshold = 0.05f;
        float deviation = (movementDelta - constrainedMovement).magnitude;
        Vector3 finalMovement = deviation < snapThreshold ? constrainedMovement : movementDelta;
    
        // Create a mutable copy of the positions.
        List<Vector3> newPositions = new List<Vector3>(_pbMesh.positions);
    
        // Update each vertex only once using the stored initial positions.
        foreach (int index in _selectedVertexIndices)
        {
            Vector3 newWorldPos = _initialVertexWorldPositions[index] + finalMovement;
            newPositions[index] = _pbMesh.transform.InverseTransformPoint(newWorldPos);
        }
    
        // Assign the updated list back to the mesh.
        _pbMesh.positions = newPositions;
    
        _pbMesh.ToMesh();
        _pbMesh.Refresh();
    }

    
    Face GetClosestFace(Transform referenceTransform)
    {
        if (_pbMesh == null || referenceTransform == null) return null;
        
        Vector3 referencePos = referenceTransform.position;
        float minDistance = minDragDistance;
        Face closestFace = null;

        foreach (Face face in _pbMesh.faces)
        {
            Vector3 faceCenter = GetFaceCenter(face);
            float distance = Vector3.Distance(referencePos, faceCenter);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestFace = face;
            }
        }
        return closestFace;
    }

    Vector3 GetFaceCenter(Face face)
    {
        Vector3 sum = Vector3.zero;
        int count = face.indexes.Count;
        
        foreach (int index in face.indexes)
        {
            sum += _pbMesh.transform.TransformPoint(_pbMesh.positions[index]);
        }
        
        return sum / count;
    }
}
