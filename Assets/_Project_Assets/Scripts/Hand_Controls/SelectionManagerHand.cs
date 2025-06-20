using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.Serialization;

public class SelectionManagerHand : MonoBehaviour
{
    [Header("Selection Settings")]
    public Transform rightController;
    public Transform leftController;
    
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
    
    public GameObject[] handModelsL;
    public GameObject[] handModelsR;
    
    //Private variables
    private string _currentSelection;
    private bool _rightTrigger;
    private GameObject _closestObject;
    
    /// <summary>
    /// Finds the closest signifier to the controller when either the left or right trigger is pressed
    /// </summary>
    private void SelectObject(Transform controller)
    {
        // Lists all signifiers within the shpere of selection
        Collider[] hits = Physics.OverlapSphere(
            controller.position, selectionRadius, selectionMask
        );
    
        _closestObject = null;
        float closestDistance = maxSelectionDistance;
        
        // Finds which signifier is closest to the controller
        foreach (Collider hit in hits)
        {
            float distance = Vector3.Distance(controller.position, hit.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                // store the closest signifier
                _closestObject = hit.transform.gameObject;
            }
        }
        
        // if a signifier is found, use that for further operations
        if (_closestObject != null)
        {
            HandleSelection(_closestObject);
        }
    }

    /// <summary>
    /// Figures out which type of signifier was selected, and applies appropriate operations with that signifier
    /// </summary>
    private void HandleSelection(GameObject selectedObject)
    {
        _currentSelection = selectedObject.tag;
        
        // look for which signifier type was selected
        switch (selectedObject.tag)
        {
            // Case: the closest signifier is a handle
            case "FaceHandle":
                // If right hand controller was used to select, run Extrusion
                if (_rightTrigger)
                {
                    handModelsR[0].SetActive(false);
                    handModelsR[1].SetActive(true);
                    handModelsR[2].SetActive(false);
                    
                    _extrudeScript = extrudeManager.GetComponent<ExtrudeFeature>();
                    _extrudeScript.CallExtrusion();
                }
                // If left hand controller was used to select, run Dragging
                else
                {
                    handModelsL[0].SetActive(false);
                    handModelsL[1].SetActive(true);
                    handModelsL[2].SetActive(false);
                    
                    _dragScript = dragManager.GetComponent<DragFace>();
                    _dragScript.StartDraggingFace();
                }
                break;

            // Case: the closest signifier is a Vertex
            case "VertexMarker":
                // Start vertex dragging based on which controller is used
                if (_rightTrigger)
                {
                    handModelsR[0].SetActive(false);
                    handModelsR[1].SetActive(true);
                    handModelsR[2].SetActive(false);
                    _vertexDragScript = vertexDragManager.GetComponent<DragVertex>();
                    _vertexDragScript.StartDraggingVertex(rightController);
                }
                else
                {
                    handModelsL[0].SetActive(false);
                    handModelsL[1].SetActive(true);
                    handModelsL[2].SetActive(false);
                    
                    _vertexDragScript = vertexDragManager.GetComponent<DragVertex>();
                    _vertexDragScript.StartDraggingVertex(leftController);
                }
                break;

            // Case: the closest signifier is a padlock
            case "PadlockMarker":
                if (_rightTrigger)
                {
                    handModelsR[0].SetActive(false);
                    handModelsR[1].SetActive(false);
                    handModelsR[2].SetActive(true);
                }
                else
                {
                    handModelsL[0].SetActive(false);
                    handModelsL[1].SetActive(false);
                    handModelsL[2].SetActive(true);
                }
                
                // toggle the padlock (select or unselect)
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
            
            // Case: the closest signifier is a face chain
            case "FaceLocker":
                if (_rightTrigger)
                {
                    handModelsR[0].SetActive(false);
                    handModelsR[1].SetActive(false);
                    handModelsR[2].SetActive(true);
                }
                else
                {
                    handModelsL[0].SetActive(false);
                    handModelsL[1].SetActive(false);
                    handModelsL[2].SetActive(true);
                }
                // toggle the chain (select or unselect)
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
            
            // Case: the closest signifier is an edge loopcut
            case "LoopCutMarker":
                if (_rightTrigger)
                {
                    handModelsR[0].SetActive(false);
                    handModelsR[1].SetActive(false);
                    handModelsR[2].SetActive(true);
                }
                else
                {
                    handModelsL[0].SetActive(false);
                    handModelsL[1].SetActive(false);
                    handModelsL[2].SetActive(true);
                }
                // get the edge value attached to the signifier
                var data = selectedObject.GetComponent<EdgeMarkerData>();
                Edge seed = new Edge(data.edge.Item1, data.edge.Item2);
                
                // get the selected ProBuilder mesh, and find a ring of edges, based of the seed edge
                var pbMesh = selectedObject.transform.root.GetComponent<ProBuilderMesh>();
                var ring = selectedObject.transform.parent.GetComponent<LoopCuts>()?.GetRing(pbMesh, seed);
                
                //Store current mesh state in undo Stack
                pbMesh.GetComponent<UndoTracker>()?.SaveState();

                // Connect the list of edges in the ring
                pbMesh.Connect(ring);
                pbMesh.ToMesh();
                pbMesh.Refresh();
                
                // Rebuild signifiers
                selectedObject.transform.parent.GetComponent<EdgeVisualizer>()?.ReBuildEdges();
                
                break;
        }
    }

    // Stop a signifier selection when trigger button is released
    void StopSelection()
    {
        handModelsR[0].SetActive(true);
        handModelsR[1].SetActive(false);
        handModelsR[2].SetActive(false);
        handModelsL[0].SetActive(true);
        handModelsL[1].SetActive(false);
        handModelsL[2].SetActive(false);
        
        switch (_currentSelection)
        {
            // Case: the closest signifier is a Handle
            case "FaceHandle":
                // If right hand controller was used to select, stop Extrusion
                if (_rightTrigger)
                {
                    _extrudeScript.StopDraggingFace();
                    
                    // Check if faces can be merged
                    _mergeScript = mergeManager.GetComponent<MergeFaces>();
                    _mergeScript.MergeCloseFaces();
                }
                // If left hand controller was used to select, stop Dragging
                else
                {
                    _dragScript.StopDraggingFace();
                    
                    // Check if faces can be merged
                    _mergeScript = mergeManager.GetComponent<MergeFaces>();
                    _mergeScript.MergeCloseFaces();
                }
                break;
            // Case: the closest signifier is a vertex
            case "VertexMarker":
                // Stop dragging the vertex
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

    // right trigger is pressed down
    public void RightIndexTriggerDownHand()
    {
        if (!_leftIsActive)
        {
            _rightTrigger = true;
            _rightIsActive = true;
            SelectObject(rightController);
        }
    }

    // right trigger is released
    public void RightIndexTriggerUpHand()
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

    // left trigger is pressed down
    public void LeftIndexTriggerDownHand()
    {
        if (!_rightIsActive)
        {
            _rightTrigger = false;
            _leftIsActive = true;
            SelectObject(leftController);
        }
    }

    // left trigger is released
    public void LeftIndexTriggerUpHand()
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