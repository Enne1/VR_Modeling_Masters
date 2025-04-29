using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

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
    public GameObject proximityScalerManager;
    public float selectionRadius;
    public LayerMask selectionMask;
        
    /// <summary>
    /// Find and select the closest object to the controller when "X" on the left controller is pressed
    /// </summary>
    public void SelectObj()
    {
        _pbObjectsInScene = GameObject.FindGameObjectsWithTag("ProBuilderObj"); // Find all objects with the tag
        foreach (GameObject pb in _pbObjectsInScene)
        {
            // Set all objects to the deselected material
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
        
        Collider[] hits = Physics.OverlapSphere(
            leftController.transform.position, selectionRadius, selectionMask
        );

        ClosestObj = null;
        float closestDistance = selectionRadius;

        foreach (Collider hit in hits)
        {
            // Find the closest point on the collider's surface
            Vector3 closestPoint = hit.ClosestPoint(leftController.transform.position);

            // Measure the distance from the controller to the closest point on the collider
            float distance = Vector3.Distance(leftController.transform.position, closestPoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                ClosestObj = hit.transform.gameObject;
            }
        }
        
        _proximityScaler = proximityScalerManager.GetComponent<ProximityScaler>();
        _proximityScaler.SetScales(ClosestObj);
        
        // Set the closest object to the selected material
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
}