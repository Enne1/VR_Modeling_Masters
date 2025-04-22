using UnityEngine;

public class ShapeCopying : MonoBehaviour
{
// Public variables
    public GameObject proBuilderShape;
    public GameObject copyPlayform;
    public float spawnRadius;
    public LayerMask detectionLayer; 

    // Spawn an initial shape on the spawner petistal
    private void Start()
    {
        if(proBuilderShape == null)
            return;
        
        Instantiate(proBuilderShape, transform.position, Quaternion.identity);
    }

    // When the shape is removed from the petistal, generate a new one
    // If a shape exists within the "Physics.CheckSphere" don't add a new one
    private void Update()
    {
        // Check if the area is empty before spawning a new cube
        if(proBuilderShape == null)
            return;
        
        if (!Physics.CheckSphere(transform.position, spawnRadius, detectionLayer, QueryTriggerInteraction.Collide))
        {
            var copiedMesh = copyPlayform.GetComponent<CopySaveMesh>();
            copiedMesh.copiedMesh = Instantiate(proBuilderShape, transform.position, Quaternion.identity);
            copiedMesh.LoadData();
        }
    }
}