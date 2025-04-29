using UnityEngine;
using System.Collections.Generic;

public class MultiSelectedList : MonoBehaviour
{
    public List<GameObject> selectedPadlocks;
    public List<GameObject> selectedFaces;
    
    
    /// <summary>
    /// Adds the current selected padlock to a list of selected padlocks (multi-vertex selection system)
    /// Only works for previously non-selected padlocks
    /// </summary>
    public void AddToPadlockList(GameObject selectedObject)
    {
        selectedPadlocks.Add(selectedObject);
    }
    
    /// <summary>
    /// removes the current selected padlock from the list of selected padlocks (multi-vertex selection system)
    /// Only works for previously selected padlocks
    /// </summary>
    public void RemoveFromPadlockList(GameObject selectedObject)
    {
        selectedPadlocks.Remove(selectedObject);
    }
    
    
    /// <summary>
    /// Adds the current selected chain to a list of selected chains (multi-face selection system)
    /// Only works for previously non-selected chains
    /// </summary>
    public void AddToFacesList(GameObject selectedObject)
    {
        selectedFaces.Add(selectedObject);
    }

    /// <summary>
    /// removes the current selected chain from the list of selected chains (multi-face selection system)
    /// Only works for previously selected chains
    /// </summary>
    public void RemoveFromFacesList(GameObject selectedObject)
    {
        selectedFaces.Remove(selectedObject);
    }
}