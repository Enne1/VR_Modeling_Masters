using UnityEngine;
using System.Collections.Generic;

public class DeleteShape : MonoBehaviour
{
    private float spawnRadius = 0.2428563f;
    public LayerMask detectionLayer; 
    private Material originalMat;
    public Material deleteMat;
    public Material selectedMat;
    private HashSet<Collider> objectsInsideTrigger = new HashSet<Collider>();  // Track objects in the trigger
    
    /// <summary>
    /// Runs when a mesh is released of a grab, If the mesh is within the trashcan delete that mesh
    /// </summary>
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
    
    /// <summary>
    /// Called when another collider (A mesh) enters the trashcans trigger collider
    /// the mesh color will then turn red
    /// </summary>
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

    /// <summary>
    /// Called when another collider (A mesh) exits the trashcans trigger collider
    /// the mesh color will then turn back to its original color
    /// </summary>
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
}