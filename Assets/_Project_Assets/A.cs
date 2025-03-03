using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject objectToSpawn;  // The prefab to instantiate
    public Transform spawnLocation;   // Optional: Location where the object will spawn, if not set it will spawn at the origin

    void Update()
    {
        // Check if the "A" key is pressed
        if (Input.GetKeyDown(KeyCode.A))
        {
            // If a spawn location is set, instantiate the object there, otherwise at the origin
            if (spawnLocation != null)
            {
                Instantiate(objectToSpawn, spawnLocation.position, spawnLocation.rotation);
            }
            else
            {
                Instantiate(objectToSpawn, Vector3.zero, Quaternion.identity);
            }
        }
    }
}