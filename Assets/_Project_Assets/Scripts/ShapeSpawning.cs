using UnityEngine;

public class ShapeSpawning : MonoBehaviour
{
    public GameObject proBuilderShape;
    public float spawnRadius; // Slightly larger radius for better detection
    public LayerMask detectionLayer; // Layer to detect only relevant objects

    private void Start()
    {
        Instantiate(proBuilderShape, transform.position, Quaternion.identity);
    }

    private void Update()
    {
        // Check if the area is empty before spawning a new cube
        if (!Physics.CheckSphere(transform.position, spawnRadius, detectionLayer, QueryTriggerInteraction.Collide))
        {
            Instantiate(proBuilderShape, transform.position, Quaternion.identity);
        }
    }
}