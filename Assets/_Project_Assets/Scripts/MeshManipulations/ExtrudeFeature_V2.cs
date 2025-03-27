using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class ExtrudeFeature_V2 : MonoBehaviour
{
    // Private variables needed for extrution
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    private Face _selectedFace;
    private bool _isDragging;
    private Vector3 _initialControllerPos;
    private Vector3 _initialFaceCenter;
    private Vector3 _currControllerPos;
    
    // Public variables
    public GameObject rightController;
    public float minExtrudeDistance;
    
    void Start()
    {
        _objSelector = FindFirstObjectByType<ObjSelector>();
    }
    
    void Update()
    {
        // Continuously update the wireframe of the objects
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        }

        // Continuously update the shape when an extrution operation is happening
        if (_isDragging)
        {
            DragFace();
        }
    }
    
    // Initiate variables when starting an extrution
    public void StartDraggingFace()
    {
        if (_selectedFace == null) return;
        
        _initialControllerPos = rightController.transform.position;
        _initialFaceCenter = GetFaceCenter(_selectedFace);
        _isDragging = true;
    }

    // Reset variable when stopping an extrution
    public void StopDraggingFace()
    {
        _isDragging = false;
        _selectedFace = null;
    }

    // Function responsible for performing extrution
    void DragFace()
    {
        if (_selectedFace == null || _pbMesh == null) return;
        
        _currControllerPos = rightController.transform.position;
        
        // Calculate the movement delta (how much the controller moved)
        Vector3 movementDelta = _currControllerPos - _initialControllerPos;

        // Get the normal of the face
        Vector3 localNormal = Math.Normal(_pbMesh, _selectedFace);
        Vector3 faceNormal = _pbMesh.transform.TransformDirection(localNormal);
        
        // Project the movement delta onto the face normal
        float movementAlongNormal = Vector3.Dot(movementDelta, faceNormal);
        Vector3 constrainedMovement = faceNormal * movementAlongNormal;

        // Compute new face center position
        Vector3 newFaceCenter = _initialFaceCenter + constrainedMovement;

        // Calculate displacement and apply to vertices
        Vector3 displacement = newFaceCenter - GetFaceCenter(_selectedFace);
        _pbMesh.TranslateVerticesInWorldSpace(_selectedFace.distinctIndexes.ToArray(), displacement);

        _pbMesh.ToMesh();
        _pbMesh.Refresh();
    }
    
    // Start extrution when right index trigger is pressed
    public void CallExtrution()
    {
        Face closestFace = GetClosestFace();
        if (closestFace != null)
        {
            _selectedFace = closestFace;
            ExtrudeFace(closestFace);
            StartDraggingFace();
        }
    }
    
    // Calculate the closest face, to know which face to perform extrution on
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

    // Calculate the center coordinate of a face
    Vector3 GetFaceCenter(Face face)
    {
        Vector3 sum = Vector3.zero;
        int count = face.indexes.Count;
        
        foreach (int index in face.indexes)
        {
            sum += _pbMesh.transform.TransformPoint(_pbMesh.positions[index]); // Convert local space to world space
        }
    
        return sum / count;
    }

    // Perform extrution of face (Face gets extruded by a very small amount, and then afterward simple dragged)
    // This is because for a ProBuilder object extrution, you must give the extrution distance at the beginning.
    void ExtrudeFace(Face face)
    {
        List<Face> newFaces = new List<Face> { face };
        _pbMesh.Extrude(newFaces, ExtrudeMethod.IndividualFaces, .01f);
        _pbMesh.ToMesh();
        _pbMesh.Refresh();
    }
}