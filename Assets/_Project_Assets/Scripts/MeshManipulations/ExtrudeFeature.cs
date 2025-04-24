using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.UIElements;

public class ExtrudeFeature : MonoBehaviour
{
    // Private variables needed for extrusion
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    // _selectedFace will be the primary (first) face—now, always the face under the controller.
    private Face _selectedFace;
    // Additional faces that are extruded.
    private List<Face> _dragAlongFaces = new List<Face>(); 
    private bool _isDragging;
    private Vector3 _initialControllerPos;
    private Vector3 _initialFaceCenter;
    private Vector3 _currControllerPos;
    
    // Multi-extrusion tracking: union of vertex indices from all extruded faces
    private HashSet<int> _selectedVertexIndices = new HashSet<int>();
    private Dictionary<int, Vector3> _initialVertexWorldPositions = new Dictionary<int, Vector3>();

    private float totalDraggedDistance;
    
    // Public variables
    public GameObject rightController;
    public float minExtrudeDistance;
    
    void Start()
    {
        _objSelector = FindFirstObjectByType<ObjSelector>();
    }
    
    void Update()
    {
        // Continuously update the mesh reference
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        }
        
        // During an extrusion operation, update the dragged shape.
        if (_isDragging)
        {
            DragFace();
        }
    }
    
    // Initiate dragging and record initial positions for all vertices in extruded faces.
    public void StartDraggingFace()
    {
        if (_selectedFace == null) return;
        
        _initialControllerPos = rightController.transform.position;
        _initialFaceCenter = GetFaceCenter(_selectedFace);
        _isDragging = true;
        
        // Clear any previous vertex selections.
        _selectedVertexIndices.Clear();
        _initialVertexWorldPositions.Clear();
        
        // Build a union of vertex indices from the primary face...
        foreach (int index in _selectedFace.distinctIndexes)
        {
            _selectedVertexIndices.Add(index);
        }
        // ...and from all additional extruded faces.
        foreach (Face face in _dragAlongFaces)
        {
            foreach (int index in face.distinctIndexes)
            {
                _selectedVertexIndices.Add(index);
            }
        }
        
        // Also include any vertices in the entire mesh that share the same world position.
        List<int> selectedIndicesCopy = new List<int>(_selectedVertexIndices);
        foreach (int selectedIndex in selectedIndicesCopy)
        {
            Vector3 selectedWorldPos = _pbMesh.transform.TransformPoint(_pbMesh.positions[selectedIndex]);
            for (int i = 0; i < _pbMesh.positions.Count; i++)
            {
                if (!_selectedVertexIndices.Contains(i))
                {
                    Vector3 worldPos = _pbMesh.transform.TransformPoint(_pbMesh.positions[i]);
                    // Using a small tolerance for floating point imprecision.
                    if (Vector3.Distance(worldPos, selectedWorldPos) < 0.0001f)
                    {
                        _selectedVertexIndices.Add(i);
                    }
                }
            }
        }
        
        // Record the initial world position for each vertex in our union.
        foreach (int index in _selectedVertexIndices)
        {
            Vector3 worldPos = _pbMesh.transform.TransformPoint(_pbMesh.positions[index]);
            _initialVertexWorldPositions[index] = worldPos;
        }
        
        Vector3 faceNormal = _pbMesh.transform.TransformDirection(Math.Normal(_pbMesh, _selectedFace));
    }
    
    // Reset variables when stopping the extrusion.
    public void StopDraggingFace()
    {
        if (totalDraggedDistance < 0.01f)
        {
            _pbMesh.GetComponent<UndoTracker>()?.Undo();
        }
        _isDragging = false;
        _selectedFace = null;
        _dragAlongFaces.Clear();
        _selectedVertexIndices.Clear();
        _initialVertexWorldPositions.Clear();
    }
    
    // Update the extruded geometry based on controller movement.
    void DragFace()
    {
        if (_selectedFace == null || _pbMesh == null) return;
        
        _currControllerPos = rightController.transform.position;
        Vector3 movementDelta = _currControllerPos - _initialControllerPos;
        
        // Use the primary face normal as the extrusion direction.
        Vector3 localNormal = Math.Normal(_pbMesh, _selectedFace);
        Vector3 faceNormal = _pbMesh.transform.TransformDirection(localNormal);
        
        // Project the controller movement onto the face normal.
        float movementAlongNormal = Vector3.Dot(movementDelta, faceNormal);
        Vector3 constrainedMovement = faceNormal * movementAlongNormal;
        
        // For extrusion, we use the constrained movement.
        Vector3 finalMovement = constrainedMovement;
        
        // Make a mutable copy of the mesh positions.
        List<Vector3> newPositions = new List<Vector3>(_pbMesh.positions);
        foreach (int index in _selectedVertexIndices)
        {
            Vector3 newWorldPos = _initialVertexWorldPositions[index] + finalMovement;
            newPositions[index] = _pbMesh.transform.InverseTransformPoint(newWorldPos);
        }
        _pbMesh.positions = newPositions;
        
        _pbMesh.ToMesh();
        _pbMesh.Refresh();

        totalDraggedDistance = finalMovement.magnitude;
    }
    
    // Called when the right index trigger is pressed to begin extrusion.
    public void CallExtrution()
    {
        List<Face> facesToExtrude = new List<Face>();
        
        // Always start by finding the face closest to the controller.
        Face controllerFace = GetClosestFace();
        if (controllerFace != null)
        {
            facesToExtrude.Add(controllerFace);
        }
        
        // First, try to get multiple selected faces from your MultiSelectedList.
        MultiSelectedList facesSelectedList = _pbMesh.transform.GetComponent<MultiSelectedList>();
        if (facesSelectedList != null && facesSelectedList.selectedFaces != null && facesSelectedList.selectedFaces.Count > 0)
        {
            foreach (var faceObj in facesSelectedList.selectedFaces)
            {
                Face extrudeFace = GetClosestFace(faceObj.transform.parent);
                if (extrudeFace != null && !facesToExtrude.Contains(extrudeFace))
                {
                    facesToExtrude.Add(extrudeFace);
                }
            }
        }
        
        if (facesToExtrude.Count > 0)
        {
            //Store current mesh state in undo Stack
            _pbMesh.GetComponent<UndoTracker>()?.SaveState();
            
            // Extrude all the selected faces simultaneously by a small initial amount.
            _pbMesh.Extrude(facesToExtrude, ExtrudeMethod.FaceNormal, .001f);
            _pbMesh.ToMesh();
            _pbMesh.Refresh();
            
            // Set the primary face (first in the list) and record the others.
            _selectedFace = facesToExtrude[0];
            _dragAlongFaces.Clear();
            for (int i = 1; i < facesToExtrude.Count; i++)
            {
                _dragAlongFaces.Add(facesToExtrude[i]);
            }
            
            // Now record the vertices for all extruded faces and begin dragging.
            StartDraggingFace();
        }
    }
    
    // Finds the closest face to the right controller.
    Face GetClosestFace()
    {
        if (_pbMesh == null || rightController == null) return null;
        
        _currControllerPos = rightController.transform.position;
        float minDistance = minExtrudeDistance;
        Face closestFace = null;
        
        foreach (Face face in _pbMesh.faces)
        {
            Vector3 faceCenter = GetFaceCenter(face);
            float distance = Vector3.Distance(_currControllerPos, faceCenter);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestFace = face;
            }
        }
        return closestFace;
    }
    
    // Overload: Finds the closest face to a given reference transform.
    Face GetClosestFace(Transform referenceTransform)
    {
        if (_pbMesh == null || referenceTransform == null) return null;
        
        Vector3 referencePos = referenceTransform.position;
        float minDistance = minExtrudeDistance;
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
    
    // Computes the center point of a face.
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