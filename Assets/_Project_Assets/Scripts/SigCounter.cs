using UnityEngine;
using UnityEngine.ProBuilder;

public class SigCounter : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ProBuilderMesh _pbMesh = gameObject.GetComponent<ProBuilderMesh>();
        Debug.Log(gameObject.name + ": " + gameObject.transform.childCount);
        
        Debug.Log(gameObject.name + ": " + _pbMesh.faces.Count);
    }
}
