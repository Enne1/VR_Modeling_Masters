using UnityEngine;
using System.Collections.Generic;

public class PadlockSelectedList : MonoBehaviour
{
    public List<GameObject> selectedPadlocks = new List<GameObject>();
    public void AddToPadlockList(GameObject selectedObject)
    {
        selectedPadlocks.Add(selectedObject);
        Debug.Log("Adding lock to list. Total selected: " + selectedPadlocks.Count);
    }

    public void RemoveFromPadlockList(GameObject selectedObject)
    {
        selectedPadlocks.Remove(selectedObject);
        Debug.Log("Removing lock from list. Total selected: " + selectedPadlocks.Count);
    }
}
