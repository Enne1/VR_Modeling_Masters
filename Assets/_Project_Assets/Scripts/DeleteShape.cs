using System;
using UnityEngine;
using System.Collections.Generic;

public class DeleteShape : MonoBehaviour
{
    private float _overlapSphereSpawnRadius = 0.2f; 
    public LayerMask detectionLayer; 
    private Material _originalMat;
    public Material deleteMat;
    public Material selectedMat;
    private HashSet<Collider> _objectsInsideTrigger;
    
    /// <summary>
    /// Check if the currently grabbed object is within the trashcan
    /// If so when the grabbed object is released, delete the mesh
    /// </summary>
    public void Deletion()
    {
        // Overlap detection for objects to be deleted
        Collider[] objectsToDelete = Physics.OverlapSphere(transform.position, _overlapSphereSpawnRadius, detectionLayer);
        
        // Delete all objects within the trashcan objects
        foreach (Collider obj in objectsToDelete)
        {
            _objectsInsideTrigger.Remove(obj);
            Destroy(obj.gameObject);
        }
    }
    
    /// <summary>
    /// If an object enters the trashcan but is still being grabbed by the controller, turn the mesh red
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered the trigger is on the specified layer mask
        if ((detectionLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            // Change material to 'deleteMat' when entering
            other.gameObject.GetComponent<MeshRenderer>().material = deleteMat;
            _objectsInsideTrigger.Add(other); 
        }
    }

    /// <summary>
    /// Revert mesh color back to normal when exiting the trashcan trigger
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // Check if the object that exited the trigger is on the specified layer mask
        if ((detectionLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            // Only reset the material if it was previously inside the trigger
            if (_objectsInsideTrigger.Contains(other))
            {
                other.gameObject.GetComponent<MeshRenderer>().material = selectedMat;
                _objectsInsideTrigger.Remove(other);
            }
        }
    }
    
    private void Update()
    {
        // Check for all objects currently inside the trigger and make sure they are still within bounds.
        foreach (var obj in _objectsInsideTrigger)
        {
            if (!GetComponent<Collider>().bounds.Intersects(obj.bounds))
            {
                obj.gameObject.GetComponent<MeshRenderer>().material = selectedMat;
                _objectsInsideTrigger.Remove(obj);
            }
        }
    }
}