using System;
using UnityEngine;

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
    private ExtrudeFeature_V2 _extrudeScript;
    
    public GameObject dragManager;
    private DragFace_V2 _dragScript;
    
    public GameObject vertexDragManager;
    private DragVertex _vertexDragScript;
    
    public GameObject lockManager;
    private VertexLocker _lockVertexScript;
    
    [Header("Testing Material")]
    public Material sMat;
    public Material dMat;

    
    //Private variables
    private string _currentSelection;
    private bool _rightTrigger;
    

    private void SelectObject(Transform controller)
    {
        Collider[] hits = Physics.OverlapSphere(
            controller.position, selectionRadius, selectionMask
        );
    
        Debug.Log("Hits Found: " + hits.Length);
        GameObject closestObject = null;
        string closestName = "";
        float closestDistance = maxSelectionDistance;

        foreach (Collider hit in hits)
        {
            float distance = Vector3.Distance(controller.position, hit.transform.position);

            if (distance < closestDistance)
            {
                Debug.Log("New closest object: " + hit.name);
                Debug.Log("Distance: " + distance);
                closestDistance = distance;
                closestObject = hit.transform.gameObject;
                closestName = hit.name;
                
            }
        }
        
        Debug.Log("Actual closest object: " + closestName);
        
        if (closestObject != null)
        {
            Debug.Log("Selected object: " + closestName);
            HandleSelection(closestObject);
        }
    }

    private void HandleSelection(GameObject selectedObject)
    {
        // Log the tag of the selected child object
        Debug.Log("Selected Object Tag: " + selectedObject.tag);
        
        _currentSelection = selectedObject.tag;
        
        switch (selectedObject.tag)
        {
            case "FaceHandle":
                Debug.Log("Selected Handle: " + selectedObject.name);
                selectedObject.GetComponent<MeshRenderer>().material = sMat;
                if (_rightTrigger)
                {
                    Debug.Log("Should Start Extrution");
                    _extrudeScript = extrudeManager.GetComponent<ExtrudeFeature_V2>();
                    _extrudeScript.CallExtrution();
                }
                else
                {
                    Debug.Log("Should Start Dragging");
                    _dragScript = dragManager.GetComponent<DragFace_V2>();
                    _dragScript.StartDraggingFace();
                }
                
                // Implement handle interaction (extrusion, dragging, etc.)
                break;

            case "VertexMarker":
                Debug.Log("Selected Vertex: " + selectedObject.name);
                //selectedObject.GetComponent<MeshRenderer>().material = sMat;
                
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
                
                // Implement vertex dragging logic
                break;

            case "PadlockMarker":
                //selectedObject.GetComponent<MeshRenderer>().material = sMat;
                Debug.Log("Selected Padlock: " + selectedObject.name);

                PadlockToggler _padlockToggle = selectedObject.GetComponent<PadlockToggler>();
                //PadlockToggler _padlockToggle = selectedObject.GetComponent<PadlockToggler>();
                if (_padlockToggle != null)
                {
                    _padlockToggle.SwitchToggle();
                    Debug.Log("Padlock lock state: " + _padlockToggle.isToggledOn);
                    if (_padlockToggle.isToggledOn)
                    {
                        selectedObject.GetComponent<MeshRenderer>().material = sMat;
                    }
                    else
                    {
                        selectedObject.GetComponent<MeshRenderer>().material = dMat;
                    }
                }
                else
                {
                    Debug.LogWarning("PadlockToggle component not found on selectedObject.");
                }

                
                // Implement multi-selection logic
                /*
                if (_rightTrigger)
                {
                    _lockVertexScript = lockManager.GetComponent<VertexLocker>();
                    _lockVertexScript.TogglePadlock(rightController.position);   
                }
                else
                {
                    //_lockScript = lockManager.GetComponent<PadlockAndFacelockVisualizer>();
                    //_lockScript.SelectPadlock(leftController.position);
                }
                */
                break;

            default:
                Debug.Log("Unknown Tag: " + selectedObject.name);  // If the tag doesn't match any of the expected ones
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
                }
                else
                {
                    _dragScript.StopDraggingFace();
                }
                break;
            case "VertexMarker":
                _vertexDragScript.StopDraggingVertex();
                break;
            case "PadlockMarker":
                break;
            default:
                Debug.Log("Unknown Tag: " + _currentSelection);
                break;
        }
    }


    public void RightIndexTriggerDown()
    {
        _rightTrigger = true;
        SelectObject(rightController);
    }

    public void RightIndexTriggerUp()
    {
        _rightTrigger = true;
        StopSelection();
    }

    public void LeftIndexTriggerDown()
    {
        _rightTrigger = false;
        SelectObject(leftController);
    }

    public void LeftIndexTriggerUp()
    {
        _rightTrigger = false;
        StopSelection();
    }
}
