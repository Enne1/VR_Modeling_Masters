using System;
using UnityEngine;

public class DeleteShape : MonoBehaviour
{
    private float spawnRadius = 0.2f; 
    public LayerMask detectionLayer; 
    private Material originalMat;
    public Material deleteMat;
    
    //private bool isObjectInside = false;  // A boolean to keep track of whether an object is inside the collider
    
    // Code Run whenever an object it released from being grabbed
    public void Deletion()
    {
        // Overlap detection for objects to be deleted
        Collider[] objectsToDelete = Physics.OverlapSphere(transform.position, spawnRadius, detectionLayer);
        
        // Delete all objects within the trashcan objects
        foreach (Collider obj in objectsToDelete)
        {
            Destroy(obj.gameObject);
        }
    }

    // Called when another collider enters the trigger collider
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered the trigger is on the specified layer mask
        if ((detectionLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            originalMat = other.gameObject.GetComponent<MeshRenderer>().material;
            //isObjectInside = true;  // Set the boolean to true when an object enters
            other.gameObject.GetComponent<MeshRenderer>().material = deleteMat;
        }
    }

    // Called when another collider exits the trigger collider
    private void OnTriggerExit(Collider other)
    {
        // Check if the object that exited the trigger is on the specified layer mask
        if ((detectionLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            //isObjectInside = false;  // Set the boolean to false when an object leaves
            other.gameObject.GetComponent<MeshRenderer>().material = originalMat;
        }
    }
}