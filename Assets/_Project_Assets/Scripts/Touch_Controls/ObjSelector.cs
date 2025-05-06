using UnityEngine;

public class ObjSelector : MonoBehaviour
{
    // Private variables 
    private GameObject[] _pbObjectsInScene;
    private HandleUpdater _handleUpdater;
    private ProximityScaler _proximityScaler;
    
    //Public variables
    public GameObject ClosestObj { get; private set; }
    public Material deselectedMat;
    public Material selectedMat;
    public GameObject leftController;
    public GameObject rightController;
    public GameObject proximityScalerManager;
    public float selectionRadius;
    public LayerMask selectionMask;
    
    public GameObject grapManager;
    private GrapInteractorHand _grapScript;
        
    /// <summary>
    /// Find and select the closest object to the controller when "X" on the left controller is pressed
    /// </summary>
    void SelectObj(GameObject controller)
    {
        _pbObjectsInScene = GameObject.FindGameObjectsWithTag("ProBuilderObj");
        
        // make sure all ProBuilder objects in the scene are disabled
        foreach (GameObject pb in _pbObjectsInScene)
        {
            MeshRenderer renderer = pb.GetComponent<MeshRenderer>();
            
            if (renderer != null)
            {
                renderer.material = deselectedMat;
            }
            foreach( Transform child in pb.transform)
            {
                child.gameObject.SetActive(false);
            }
        }
        
        // find all ProBuilder objects within a certain radius of the controller, when selection button is pressed
        Collider[] hits = Physics.OverlapSphere(
            controller.transform.position, selectionRadius, selectionMask
        );

        ClosestObj = null;
        float closestDistance = selectionRadius;

        // Find the closest object to the controller within the selection sphere
        foreach (Collider hit in hits)
        {
            // Find the closest point on the collider's surface
            Vector3 closestPoint = hit.ClosestPoint(controller.transform.position);

            // Measure the distance from the controller to the closest point on the collider
            float distance = Vector3.Distance(controller.transform.position, closestPoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                //Store the closest object
                ClosestObj = hit.transform.gameObject;
            }
        }
        
        // Initiate ProximityScaler for the closest object
        _proximityScaler = proximityScalerManager.GetComponent<ProximityScaler>();
        _proximityScaler.SetScales(ClosestObj);
        
        _grapScript = grapManager.GetComponent<GrapInteractorHand>();
        _grapScript.getMeshFromSelector(ClosestObj);
        
        // Set the closest object to the selected material and enable its signifiers
        if (ClosestObj != null)
        {
            foreach( Transform child in ClosestObj.transform)
            {
                child.gameObject.SetActive(true);
            }
            
            MeshRenderer renderer = ClosestObj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = selectedMat;
            }
        }
    }

    public void SelectObjLeft()
    {
        SelectObj(leftController);
    }

    public void SelectObjRight()
    {
        SelectObj(rightController);
    }
}