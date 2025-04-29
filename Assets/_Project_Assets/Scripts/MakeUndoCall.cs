using UnityEngine;
using UnityEngine.ProBuilder;

public class MakeUndoCall : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProximityScaler _proximityScaler;
    private ProBuilderMesh _pbMesh;
    
    void Start()
    {
        _objSelector = FindFirstObjectByType<ObjSelector>();
        _proximityScaler = FindObjectOfType<ProximityScaler>(); 
    }
    
    void Update()
    {
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        }
    }

    /// <summary>
    /// Starts an Undo on the selected mesh, when undo button is pressed
    /// </summary>
    public void MakeUndo()
    {
        _pbMesh.GetComponent<UndoTracker>()?.Undo(false);
        _proximityScaler.ResetScales();
    }
    
    /// <summary>
    /// Starts a Redo on the selected mesh, when redo button is pressed
    /// </summary>
    public void MakeRedo()
    {
        _pbMesh.GetComponent<UndoTracker>()?.Redo();
        _proximityScaler.ResetScales();
    }
}