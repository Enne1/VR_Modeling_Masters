using UnityEngine;
using System.Collections.Generic;

public class MultiSelectedList : MonoBehaviour
{
    public List<GameObject> selectedPadlocks = new List<GameObject>();
    public List<GameObject> selectedFaces = new List<GameObject>();
    public void AddToPadlockList(GameObject selectedObject)
    {
        selectedPadlocks.Add(selectedObject);
        Debug.Log("Adding padlock to list. Total selected: " + selectedPadlocks.Count);
    }

    public void RemoveFromPadlockList(GameObject selectedObject)
    {
        selectedPadlocks.Remove(selectedObject);
        Debug.Log("Removing padlock from list. Total selected: " + selectedPadlocks.Count);
    }
    
    
    public void AddToFacesList(GameObject selectedObject)
    {
        selectedFaces.Add(selectedObject);
        Debug.Log("Adding face lock to list. Total selected: " + selectedFaces.Count);
    }

    public void RemoveFromFacesList(GameObject selectedObject)
    {
        selectedFaces.Remove(selectedObject);
        Debug.Log("Removing face lock from list. Total selected: " + selectedFaces.Count);
    }
}
