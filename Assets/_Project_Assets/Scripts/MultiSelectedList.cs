using UnityEngine;
using System.Collections.Generic;

public class MultiSelectedList : MonoBehaviour
{
    public List<GameObject> selectedPadlocks = new List<GameObject>();
    public List<GameObject> selectedFaces = new List<GameObject>();
    
    public void AddToPadlockList(GameObject selectedObject)
    {
        selectedPadlocks.Add(selectedObject);
    }

    public void RemoveFromPadlockList(GameObject selectedObject)
    {
        selectedPadlocks.Remove(selectedObject);
    }
    
    
    public void AddToFacesList(GameObject selectedObject)
    {
        selectedFaces.Add(selectedObject);
    }

    public void RemoveFromFacesList(GameObject selectedObject)
    {
        selectedFaces.Remove(selectedObject);
    }
}
