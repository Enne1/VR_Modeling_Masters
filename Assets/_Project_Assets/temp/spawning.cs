using UnityEngine;
using UnityEngine.InputSystem; // Import the new Input System

public class Spawning : MonoBehaviour
{
    public GameObject prefab;

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame) // Correct Input System usage
        {
            Instantiate(prefab, transform.position, Quaternion.identity);
        }
    }
}