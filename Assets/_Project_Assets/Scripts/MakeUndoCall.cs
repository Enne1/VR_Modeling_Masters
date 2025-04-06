using UnityEngine;
using UnityEngine.ProBuilder;

public class MakeUndoCall : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _objSelector = FindFirstObjectByType<ObjSelector>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        }
    }

    public void MakeUndo()
    {
        Debug.Log("making undo");
        _pbMesh.GetComponent<UndoTracker>()?.Undo();
    }
}
