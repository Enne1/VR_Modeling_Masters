using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class SelectionManager : MonoBehaviour
{
    [Header("Selection Settings")]
    public Transform rightController; // Assign in Inspector (e.g., XR Controller)
    public Transform leftController;  // Assign in Inspector
    public float selectionRadius = 0.1f;  // Sphere radius for detection
    public float maxSelectionDistance = 0.1f; // Max raycast distance
    public LayerMask selectionMask;
    
    [Header("Managers")]
    public GameObject extrudeManager;
    private ExtrudeFeature _extrudeScript;
    
    public GameObject dragManager;
    private DragFace _dragScript;
    
    public GameObject vertexDragManager;
    private DragVertex _vertexDragScript;
    
    public GameObject mergeManager;
    private MergeFaces _mergeScript;
    
    public GameObject proximityScaler;
    private ProximityScaler _proximityScript;
    
    //Private variables
    private string _currentSelection;
    private bool _rightTrigger;
    private GameObject _closestObject;
    
    private void SelectObject(Transform controller)
    {
        Collider[] hits = Physics.OverlapSphere(
            controller.position, selectionRadius, selectionMask
        );
    
        _closestObject = null;
        float closestDistance = maxSelectionDistance;

        foreach (Collider hit in hits)
        {
            float distance = Vector3.Distance(controller.position, hit.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                _closestObject = hit.transform.gameObject;
            }
        }
        
        if (_closestObject != null)
        {
            HandleSelection(_closestObject);
        }
    }

    private void HandleSelection(GameObject selectedObject)
    {
        _currentSelection = selectedObject.tag;
        
        switch (selectedObject.tag)
        {
            case "FaceHandle":
                //selectedObject.GetComponent<MeshRenderer>().material = sMat;
                if (_rightTrigger)
                {
                    _extrudeScript = extrudeManager.GetComponent<ExtrudeFeature>();
                    _extrudeScript.CallExtrusion();
                }
                else
                {
                    _dragScript = dragManager.GetComponent<DragFace>();
                    _dragScript.StartDraggingFace();
                }
                break;

            case "VertexMarker":
                if (_rightTrigger)
                {
                    _vertexDragScript = vertexDragManager.GetComponent<DragVertex>();
                    _vertexDragScript.StartDraggingVertex(rightController);
                }
                else
                {
                    _vertexDragScript = vertexDragManager.GetComponent<DragVertex>();
                    _vertexDragScript.StartDraggingVertex(leftController);
                }
                break;

            case "PadlockMarker":
                // Check if the PadlockToggler component is attached
                MultiSelectionToggler padlockToggler = selectedObject.GetComponent<MultiSelectionToggler>();
                MultiSelectedList padlockSelectedList = selectedObject.transform.parent.parent.GetComponent<MultiSelectedList>();
                if (padlockToggler != null)
                {
                    padlockToggler.SwitchTogglePadlock();
                    
                    if (padlockToggler.isToggledOn)
                    {
                        padlockSelectedList.AddToPadlockList(selectedObject);
                    }
                    else
                    {
                        // Remove the selected object from the list if it's there
                        padlockSelectedList.RemoveFromPadlockList(selectedObject);
                    }
                }
                break;
            case "FaceLocker":
                // Check if the fac lockToggler component is attached
                MultiSelectionToggler faceLockToggler = selectedObject.GetComponent<MultiSelectionToggler>();
                MultiSelectedList facesSelectedList = selectedObject.transform.parent.parent.GetComponent<MultiSelectedList>();
                if (faceLockToggler != null)
                {
                    faceLockToggler.SwitchToggleFacelock();
        
                    if (faceLockToggler.isToggledOn)
                    {
                        facesSelectedList.AddToFacesList(selectedObject);
                    }
                    else
                    {
                        // Remove the selected object from the list if it's there
                        facesSelectedList.RemoveFromFacesList(selectedObject);
                    }
                }
                break;
            case "LoopCutMarker":
                var data = selectedObject.GetComponent<EdgeMarkerData>();
                Edge seed = new Edge(data.edge.Item1, data.edge.Item2);
                
                var pbMesh = selectedObject.transform.root.GetComponent<ProBuilderMesh>();
                
                var ring = selectedObject.transform.parent.GetComponent<LoopCuts>()?.GetRing(pbMesh, seed);
                
                //Store current mesh state in undo Stack
                pbMesh.GetComponent<UndoTracker>()?.SaveState();
                
                pbMesh.Connect(ring);
                pbMesh.ToMesh();
                pbMesh.Refresh();
                
                selectedObject.transform.parent.GetComponent<EdgeVisualizer>()?.ReBuildEdges();
                
                break;
        }
    }

    void StopSelection()
    {
        switch (_currentSelection)
        {
            case "FaceHandle":
                if (_rightTrigger)
                {
                    _extrudeScript.StopDraggingFace();
                    
                    _mergeScript = mergeManager.GetComponent<MergeFaces>();
                    _mergeScript.MergeCloseFaces();
                }
                else
                {
                    _dragScript.StopDraggingFace();
                    
                    _mergeScript = mergeManager.GetComponent<MergeFaces>();
                    _mergeScript.MergeCloseFaces();
                }
                break;
            case "VertexMarker":
                _vertexDragScript.StopDraggingVertex();
                _mergeScript.MergeCloseFaces();
                break;
            case "PadlockMarker":
                break;
            case "FaceLocker":
                break;
            case "LoopCutMarker":
                break;
            default:
                break;
        }
    }


    private bool _rightIsActive;
    private bool _leftIsActive;
    
    public void RightIndexTriggerDown()
    {
        if (!_leftIsActive)
        {
            _rightTrigger = true;
            _rightIsActive = true;
            SelectObject(rightController);
        }
    }

    public void RightIndexTriggerUp()
    {
        if (!_leftIsActive)
        {
            _rightTrigger = true;
            _rightIsActive = false;
            StopSelection();
        
            // Update list of scalables for the proximity Scaler
            _proximityScript = proximityScaler.GetComponent<ProximityScaler>();
            _proximityScript.ResetScales();
        }
    }

    public void LeftIndexTriggerDown()
    {
        if (!_rightIsActive)
        {
            _rightTrigger = false;
            _leftIsActive = true;
            SelectObject(leftController);
        }
    }

    public void LeftIndexTriggerUp()
    {
        if (!_rightIsActive)
        {
            _rightTrigger = false;
            _leftIsActive = false;
            StopSelection();
        
            // Update list of scalables for the proximity Scaler
            _proximityScript = proximityScaler.GetComponent<ProximityScaler>();
            _proximityScript.ResetScales();
        }
    }
}
