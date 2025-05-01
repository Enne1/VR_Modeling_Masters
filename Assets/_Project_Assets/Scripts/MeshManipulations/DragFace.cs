using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

public class DragFace : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    private Face _selectedFace;
    private bool _isDragging;
    private Vector3 _initialControllerPos;
    private Vector3 _initialFaceCenter;
    private Vector3 _currControllerPos;
    private List<Face> _dragAlongFaces = new();

    private HashSet<int> _selectedVertexIndices = new();
    private Dictionary<int, Vector3> _initialVertexWorldPositions = new();
    
    private LineRenderer _normalAxisLineRenderer;
    
    public GameObject leftController;
    public GameObject rightController;
    public float minDragDistance;
    public float angledSnapTolerance;
    public Material normalAxisMaterial;

    void Start()
    {
        // Prepare lineRendere for snapping to normal
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
    
    /// <summary>
    /// Prepare dragging of face when index trigger is pressed down
    /// </summary>
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
        
        // Retrieve multi-face selection list
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
        
        // Include any vertices in the entire mesh that share the same world coordinate 
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
        
        // Draw line in the direction of the faces normal vector
        _isDragging = true;
        Vector3 localNormal = Math.Normal(_pbMesh, _selectedFace);
        Vector3 faceNormal = _pbMesh.transform.TransformDirection(localNormal);
        _normalAxisLineRenderer.enabled = true;
        _normalAxisLineRenderer.SetPosition(0, faceNormal * 100 + _initialFaceCenter);
        _normalAxisLineRenderer.SetPosition(1, faceNormal * -100 + _initialFaceCenter);
    }
    
    /// <summary>
    /// Stop dragging and reset variables
    /// Run the index trigger is released
    /// </summary>
    public void StopDraggingFace()
    {
        _isDragging = false;
        _selectedFace = null;
        _dragAlongFaces.Clear();
        _selectedVertexIndices.Clear();
        _initialVertexWorldPositions.Clear();
        _normalAxisLineRenderer.enabled = false;
    }

    /// <summary>
    /// Runs every update cycle
    /// continuously moved face to controller position
    /// </summary>
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
    
        // Decide if we snap to movement along the normal or use full freedom movement.
        float snapThreshold = 0.05f;
        float deviation = (movementDelta - constrainedMovement).magnitude;
        Vector3 finalMovement = deviation < snapThreshold ? constrainedMovement : movementDelta;
        
        // Find face close to right controller for possible snapping based on that face' plane
        Face rightControllerFace = GetClosestFace(rightController.transform);
        if (rightControllerFace != null)
        {
            // Get right controller face' center point and normal vector
            Vector3 rightControllerFaceCenterpoint = GetFaceCenter(rightControllerFace);
            Vector3 rightcontrollerLocalNormal = Math.Normal(_pbMesh, rightControllerFace);

            // Create plane based on the center position and the facs' normal vector
            Vector3 snappingPlaneNormal = _pbMesh.transform.TransformDirection(rightcontrollerLocalNormal).normalized;
            Plane snappingPlane = new Plane(snappingPlaneNormal, rightControllerFaceCenterpoint);

            // Get world-space normal of the dragged face
            Vector3 rightFaceNormal = _pbMesh.transform.TransformDirection(Math.Normal(_pbMesh, _selectedFace)).normalized;

            float angleBetweenNormals = Vector3.Angle(rightFaceNormal, snappingPlaneNormal);

            // If the angle between the two faces is below threshold, snap
            if (angleBetweenNormals < angledSnapTolerance && rightControllerFace != _selectedFace)
            {
                Vector3 movedFaceCenter = _initialFaceCenter + finalMovement;
                float distanceToPlane = snappingPlane.GetDistanceToPoint(movedFaceCenter);

                if (Mathf.Abs(distanceToPlane) < 0.01f) 
                {
                    Vector3 snappedCenter = movedFaceCenter - snappingPlane.normal * distanceToPlane;
                    finalMovement = snappedCenter - _initialFaceCenter;
                }
            }
        }
        
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

    /// <summary>
    /// Calculates the closest face to the controller
    /// </summary>
    Face GetClosestFace(Transform referenceTransform)
    {
        if (_pbMesh == null || referenceTransform == null) return null;
        
        Vector3 referencePos = referenceTransform.position;
        float minDistance = minDragDistance;
        Face closestFace = null;

        // Loops over all faces to find the closest
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

    /// <summary>
    /// Calculates the center of a face
    /// </summary>
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
