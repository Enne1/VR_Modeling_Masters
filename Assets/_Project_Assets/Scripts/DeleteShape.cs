using UnityEngine;

public class DeleteShape : MonoBehaviour
{
    private float spawnRadius = 0.2f; 
    public LayerMask detectionLayer; 
    
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
}