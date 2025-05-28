using UnityEngine;
using UnityEngine.ProBuilder;
using System.IO;

public class PerformanceTester : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    private string _filePath;
    private StreamWriter _writer;
    
    void Start()
    {
        _objSelector = FindFirstObjectByType<ObjSelector>();
        
        _filePath = Path.Combine(Application.persistentDataPath, "log.csv");

        // Create file or append if it already exists
        _writer = new StreamWriter(_filePath, false);
        _writer.AutoFlush = true;

        // Optionally write headers if starting fresh
        if (new FileInfo(_filePath).Length == 0)
        {
            _writer.WriteLine("FPS,SignifierCount,FaceCount");
        }
    }
    
    void Update()
    {
        // Continuously update the mesh reference
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        }
        
        //Debug.Log("FPS: " + (int)(1.0f / Time.deltaTime) + " Total Signifiers: " + _pbMesh.transform.childCount + " Total Faces: " + _pbMesh.faces.Count);
        _writer.WriteLine($"{(int)(1.0f / Time.deltaTime)},{_pbMesh.transform.childCount},{_pbMesh.faces.Count}");
    }
}
