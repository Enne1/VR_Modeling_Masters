using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    private DragFace _dragScript;
    
    public GameObject vertexDragManager;
    private DragVertex _vertexDragScript;
    
    public GameObject mergeManager;
    private MergeFaces _mergeScript;
    
    //Private variables
    private string _currentSelection;
    private bool _rightTrigger;
    private List<GameObject> _selectedPadlocks;

    private void Start()
    {
        _selectedPadlocks = new List<GameObject>();
    }

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
        Debug.Log("Selected Object Tag: " + selectedObject.tag);
        
        _currentSelection = selectedObject.tag;
        
        switch (selectedObject.tag)
        {
            case "FaceHandle":
                Debug.Log("Selected Handle: " + selectedObject.name);
                //selectedObject.GetComponent<MeshRenderer>().material = sMat;
                if (_rightTrigger)
                {
                    Debug.Log("Should Start Extrution");
                    _extrudeScript = extrudeManager.GetComponent<ExtrudeFeature_V2>();
                    _extrudeScript.CallExtrution();
                }
                else
                {
                    Debug.Log("Should Start Dragging");
                    _dragScript = dragManager.GetComponent<DragFace>();
                    _dragScript.StartDraggingFace();
                }
                break;

            case "VertexMarker":
                Debug.Log("Selected Vertex: " + selectedObject.name);
                
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
                Debug.Log("Selected Padlock: " + selectedObject.name);
   
                // Check if the PadlockToggler component is attached
                MultiSelectionToggler padlockToggler = selectedObject.GetComponent<MultiSelectionToggler>();
                MultiSelectedList padlockSelectedList = selectedObject.transform.parent.parent.GetComponent<MultiSelectedList>();
                if (padlockToggler != null)
                {
                    padlockToggler.SwitchTogglePadlock();
                    Debug.Log("Padlock lock state: " + padlockToggler.isToggledOn);
        
                    if (padlockToggler.isToggledOn)
                    {
                        padlockSelectedList.AddToPadlockList(selectedObject);
                    }
                    else
                    {
                        // Remove the selected object from the list if it's there
                        padlockSelectedList.RemoveFromPadlockList(selectedObject);
                    }

                    // Debugging: Print out the list of selected padlocks
                    Debug.Log("Number of Selected Padlocks: " + padlockSelectedList.selectedPadlocks.Count);
                    Debug.Log("List of Selected Padlocks: " + string.Join(", ", padlockSelectedList.selectedPadlocks.ConvertAll(padlock => padlock.name).ToArray()));
                }
                else 
                {
                    Debug.LogWarning("PadlockToggle component not found on selectedObject.");
                }
                break;
            case "FaceLocker":
                Debug.Log("Selected Face: " + selectedObject.name);
   
                // Check if the fac lockToggler component is attached
                MultiSelectionToggler faceLockToggler = selectedObject.GetComponent<MultiSelectionToggler>();
                MultiSelectedList facesSelectedList = selectedObject.transform.parent.parent.GetComponent<MultiSelectedList>();
                if (faceLockToggler != null)
                {
                    faceLockToggler.SwitchToggleFacelock();
                    Debug.Log("face lock state: " + faceLockToggler.isToggledOn);
        
                    if (faceLockToggler.isToggledOn)
                    {
                        facesSelectedList.AddToFacesList(selectedObject);
                    }
                    else
                    {
                        // Remove the selected object from the list if it's there
                        facesSelectedList.RemoveFromFacesList(selectedObject);
                    }

                    // Debugging: Print out the list of selected padlocks
                    Debug.Log("Number of Selected faces: " + facesSelectedList.selectedFaces.Count);
                    Debug.Log("List of Selected faces: " + string.Join(", ", facesSelectedList.selectedFaces.ConvertAll(padlock => padlock.name).ToArray()));
                }
                else 
                {
                    Debug.LogWarning("FaceToggle component not found on selectedObject.");
                }
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
                break;
            case "PadlockMarker":
                break;
            case "FaceLocker":
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
