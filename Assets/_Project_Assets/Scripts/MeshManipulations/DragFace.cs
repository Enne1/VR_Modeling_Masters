using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class DragFace_V2 : MonoBehaviour
{
    // Private variables needed for dragging
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    private Face _selectedFace;
    private bool _isDragging;
    private Vector3 _initialControllerPos;
    private Vector3 _initialFaceCenter;
    private Vector3 _currControllerPos;

    private LineRenderer _normalAxisLineRenderer;
    public Material normalAxisMaterial;
    
    // Scripts
    //private WireframeWithVertices _wireframeScript;
    //private PadlockAndFacelockVisualizer _padlockScript;
  //  private HandleUpdater _handleUpdater;
    
    // Public variables
    public GameObject leftController; // Assign the Meta left-hand controller in the Inspector for Dragging
    public float minDragDistance;

    // Find other script instances in the scene
    void Start()
    {
        // Initiate the Normal Axis Line
        _normalAxisLineRenderer = gameObject.AddComponent<LineRenderer>();
        _normalAxisLineRenderer.startWidth = 0.005f;
        _normalAxisLineRenderer.endWidth = 0.005f;
        _normalAxisLineRenderer.positionCount = 2;
        _normalAxisLineRenderer.material = normalAxisMaterial;
        _normalAxisLineRenderer.enabled = false;
        
        _objSelector = FindFirstObjectByType<ObjSelector>(); // Find the ObjSelector instance
        //_handleUpdater = FindFirstObjectByType<HandleUpdater>();
    }

    void Update()
    {
        // Continuously update the wireframe of the objects
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
            //_wireframeScript = _pbMesh.GetComponent<WireframeWithVertices>();
            //_padlockScript = _pbMesh.GetComponent<PadlockAndFacelockVisualizer>();
        }

        // Continuously update the shape when a dragging operation is happening
        if (_isDragging)
        {
            DragFace();
            //_handleUpdater.HandleOnFace();
        }
    }
    
    // Initiate variables when starting a dragging
    public void StartDraggingFace()
    {
        _selectedFace = GetClosestFace();
        //_wireframeScript.updateWireframe = true;
        //_padlockScript.updatePadlocks = true;
        
        if (_selectedFace == null) return;
        
        _initialControllerPos = leftController.transform.position;
        _initialFaceCenter = GetFaceCenter(_selectedFace);
        _isDragging = true;
        
        Vector3 localNormal = Math.Normal(_pbMesh, _selectedFace);
        Vector3 faceNormal = _pbMesh.transform.TransformDirection(localNormal);
        _normalAxisLineRenderer.enabled = true;
        _normalAxisLineRenderer.SetPosition(0, faceNormal * 100 + _initialFaceCenter);
        _normalAxisLineRenderer.SetPosition(1, faceNormal * -100 + _initialFaceCenter); 
        Debug.Log(localNormal);
        Debug.Log(faceNormal);
    }
    
    // Reset variable when stopping a dragging
    public void StopDraggingFace()
    {
        _isDragging = false;
        _selectedFace = null;
        //_wireframeScript.updateWireframe = false;
        _normalAxisLineRenderer.enabled = false;
       // _padlockScript.updatePadlocks = false;
    }

    // Function responsible for performing dragging
    void DragFace()
    {
        if (_selectedFace == null || _pbMesh == null) return;
    
        _currControllerPos = leftController.transform.position;
    
        // Calculate the movement delta (how much the controller moved)
        Vector3 movementDelta = _currControllerPos - _initialControllerPos;
        
        // Get the normal of the face 
        Vector3 localNormal = Math.Normal(_pbMesh, _selectedFace);
        Vector3 faceNormal = _pbMesh.transform.TransformDirection(localNormal);
    
        // Project the movement delta onto the face normal
        float movementAlongNormal = Vector3.Dot(movementDelta, faceNormal);
        Vector3 constrainedMovement = faceNormal * movementAlongNormal;
    
        // Threshold for snapping (If the controller is moved away from the face normal,
        // The face can be dragged around freely, otherwise it is snapped to normal vector)
        float snapThreshold = 0.025f; // Adjust this value for sensitivity
        float deviation = (movementDelta - constrainedMovement).magnitude;
    
        Vector3 finalMovement;
        if (deviation < snapThreshold)
        {
            // Snap to normal movement
            finalMovement = constrainedMovement;
        }
        else
        {
            // Allow free movement
            finalMovement = movementDelta;
        }
    
        // Compute new face center position
        Vector3 newFaceCenter = _initialFaceCenter + finalMovement;
    
        // Calculate displacement and apply to vertices
        Vector3 displacement = newFaceCenter - GetFaceCenter(_selectedFace);
        _pbMesh.TranslateVerticesInWorldSpace(_selectedFace.distinctIndexes.ToArray(), displacement);

        _pbMesh.ToMesh();
        _pbMesh.Refresh();
    }

    // Calculate the closest face, to know which face to perform dragging on
    Face GetClosestFace()
    {
        if (_pbMesh == null || leftController == null) return null;
        
        _currControllerPos = leftController.transform.position;
        
        float minDistance = minDragDistance;
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
}
