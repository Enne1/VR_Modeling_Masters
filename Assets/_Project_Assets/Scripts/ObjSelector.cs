using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class ObjSelector : MonoBehaviour
{
    public Material deselectedMat;
    public Material selectedMat;
    public GameObject leftController;
    public GameObject ClosestObj { get; private set; } // Public property to access closest object
    
    
    private GameObject[] _pbObjectsInScene;
    
    public void SelectObj()
    {
        _pbObjectsInScene = GameObject.FindGameObjectsWithTag("ProBuilderObj"); // Find all objects with the tag
        
        float distToController = float.MaxValue;
        ClosestObj = null; // Reset the closest object

        foreach (GameObject pb in _pbObjectsInScene)
        {
            // Set all objects to the deselected material
            MeshRenderer renderer = pb.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = deselectedMat;
            }

            // Find the closest object
            float distance = Vector3.Distance(pb.transform.position, leftController.transform.position);
            if (distance < distToController)
            {
                distToController = distance;
                ClosestObj = pb;
            }
            
            ProBuilderMesh pbMesh = pb.GetComponent<ProBuilderMesh>();
        }
        
        // Set the closest object to the selected material
        if (ClosestObj != null)
        {
            MeshRenderer renderer = ClosestObj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = selectedMat;
            }
        }
    }
}