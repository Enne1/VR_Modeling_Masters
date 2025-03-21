using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class ObjSelector : MonoBehaviour
{
    // Private variables 
    private GameObject[] _pbObjectsInScene;
    
    // Scripts
    private HandleUpdater _handleUpdater;
    
    //Public variables
    public Material deselectedMat;
    public Material selectedMat;
    public GameObject leftController;
    public GameObject ClosestObj { get; private set; } // Public property to access closest object
    
    // Find and select the closest object to the controller when "A" is pressed
    public void SelectObj()
    {
        //_handleUpdater = FindFirstObjectByType<HandleUpdater>();
        
        //_handleUpdater.ClearHandles();
        
        _pbObjectsInScene = GameObject.FindGameObjectsWithTag("ProBuilderObj"); // Find all objects with the tag
        
        float distToController = float.MaxValue;
        float maxDist = 1f;
        ClosestObj = null;

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

            // Find the closest object
            float distance = Vector3.Distance(pb.transform.position, leftController.transform.position);
            if (distance < distToController && distance < maxDist)
            {
                distToController = distance;
                ClosestObj = pb;
            }
        }
        //_handleUpdater.HandleOnFace();
        
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