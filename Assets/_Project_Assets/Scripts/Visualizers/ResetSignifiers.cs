using UnityEngine;
using UnityEngine.ProBuilder;

public class ResetSignifiers : MonoBehaviour
{
    public void ReaddSignifiers()
    {
        // Dynamically fetch the mesh reference
        //ObjSelector objSelector = FindFirstObjectByType<ObjSelector>();
        //if (objSelector == null || objSelector.ClosestObj == null) return;

        //ProBuilderMesh pbMesh = objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        //if (pbMesh == null) return;

        // Destroy all children (even inactive/nested ones), except the root
        //Transform[] allChildren = pbMesh.transform.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in transform)
        {
            Debug.Log($"Destroyed signifier: {child.name}");
            Destroy(child.gameObject);
        }

        // Rebuild face handles
        HandleUpdater handleUpdater = transform.GetComponent<HandleUpdater>();
        if (handleUpdater != null)
        {
            Debug.Log("rebuilt Handle signifier");
            handleUpdater.RebuildHandles();
        }

        // Rebuild vertex visualizers
        VertexVisualizer vertexVisualizer = transform.GetComponent<VertexVisualizer>();
        if (vertexVisualizer != null)
        {
            Debug.Log("rebuilt vertex signifier");
            vertexVisualizer.RebuildVertices();
        }
    }
}
