using System;
using UnityEngine;
using System.Collections.Generic;

public class DeleteShape : MonoBehaviour
{
    private float spawnRadius = 0.2f; 
    public LayerMask detectionLayer; 
    private Material originalMat;
    public Material deleteMat;
    public Material selectedMat;
    private HashSet<Collider> objectsInsideTrigger = new HashSet<Collider>();  // Track objects in the trigger
    //private bool isObjectInside = false;  // A boolean to keep track of whether an object is inside the collider
    
    // Code Run whenever an object it released from being grabbed
    public void Deletion()
    {
        // Overlap detection for objects to be deleted
        Collider[] objectsToDelete = Physics.OverlapSphere(transform.position, spawnRadius, detectionLayer);
        
        // Delete all objects within the trashcan objects
        foreach (Collider obj in objectsToDelete)
        {
            objectsInsideTrigger.Remove(obj);
            Destroy(obj.gameObject);
        }
    }
   

    // Called when another collider enters the trigger collider
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered the trigger is on the specified layer mask
        if ((detectionLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            // Change material to 'deleteMat' when entering
            other.gameObject.GetComponent<MeshRenderer>().material = deleteMat;
            objectsInsideTrigger.Add(other);  // Add to the list of objects inside
        }
    }

    // Called when another collider exits the trigger collider
    private void OnTriggerExit(Collider other)
    {
        // Check if the object that exited the trigger is on the specified layer mask
        if ((detectionLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            // Only reset the material if it was previously inside the trigger
            if (objectsInsideTrigger.Contains(other))
            {
                other.gameObject.GetComponent<MeshRenderer>().material = selectedMat;
                objectsInsideTrigger.Remove(other);  // Remove from the list of objects inside
            }
        }
    }

    // Optional: This method is here just in case you want to handle things manually
    // for objects that scale down and don't trigger the OnTriggerExit properly.
    private void Update()
    {
        // Check for all objects currently inside the trigger and make sure they are still within bounds.
        foreach (var obj in objectsInsideTrigger)
        {
            if (!GetComponent<Collider>().bounds.Intersects(obj.bounds))
            {
                obj.gameObject.GetComponent<MeshRenderer>().material = selectedMat;
                objectsInsideTrigger.Remove(obj);
            }
        }
    }
}