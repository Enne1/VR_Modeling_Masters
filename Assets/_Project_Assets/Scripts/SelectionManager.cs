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
    
        GameObject closestObject = null;
        string closestName = "";
        float closestDistance = maxSelectionDistance;

        foreach (Collider hit in hits)
        {
            float distance = Vector3.Distance(controller.position, hit.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestObject = hit.transform.gameObject;
                closestName = hit.name;
            }
        }
        
        if (closestObject != null)
        {
            HandleSelection(closestObject);
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
                    _extrudeScript.CallExtrution();
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
                
                var pbMesh = selectedObject.transform.parent.GetComponent<ProBuilderMesh>();
                
                var ring = selectedObject.transform.parent.GetComponent<LoopCuts>()?.GetRing(pbMesh, seed);
                
                Debug.Log("Selected Edge is: " + seed);
                
                foreach(var e in ring)
                    Debug.Log($"Ring edge: {e.a}→{e.b}");
                
                /*
                var edgesToConnect = new List<Edge>
                {
                    new Edge(2, 11),
                    new Edge(0, 9),
                    //new Edge(3, 7)
                };
                
                
                */
                pbMesh.Connect(ring);
                pbMesh.ToMesh();
                pbMesh.Refresh();
                
                //static IEnumerable<Edge> GetEdgeRing(ProBuilderMesh pb, IEnumerable<Edge> edges);

                
                //selectedObject.transform.parent.GetComponent<LoopCuts>().MakeLoopCut(pbMesh, seed);
                break;
        }
    }

    void StopSelection()
    {
        switch (_currentSelection)
        {
            case "FaceHandle":
                Debug.Log("Caused by face");
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
                Debug.Log("Caused by vertex");
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
