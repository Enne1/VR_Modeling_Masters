using UnityEngine;
using UnityEngine.ProBuilder;

public class ShapeSpawning : MonoBehaviour
{
    // Public variables
    public GameObject proBuilderShape;
    public float spawnRadius;
    public LayerMask detectionLayer; 

    // Spawn an initial shape on the spawner pedestal
    private void Start()
    {
        if(proBuilderShape == null)
            return;
        
        Instantiate(proBuilderShape, transform.position, Quaternion.identity);
    }

    // When the shape is removed from the pedestal, generate a new one
    // If a shape exists within the "Physics.CheckSphere" don't add a new one
    private void Update()
    {
        // Check if the area is empty before spawning a new cube
        if(proBuilderShape == null)
            return;
        
        if (!Physics.CheckSphere(transform.position, spawnRadius, detectionLayer, QueryTriggerInteraction.Collide))
        {
            Instantiate(proBuilderShape, transform.position, Quaternion.identity);
        }
    }
}