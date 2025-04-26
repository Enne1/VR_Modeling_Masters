using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Linq;
using System.IO;

public class CopySaveMesh : MonoBehaviour
{
    public Transform snapPoint;
    public LayerMask draggableLayers;
    public GameObject defaultCube;
    public GameObject copiedMesh;
    public GameObject spawnPlatform;
    public GameObject grabManager;
    public GameObject[] saveCopyIcon;
    public string fileName;
    
    private string fullPath;
    private bool _isHooked;
    private string _json;
    private GameObject _candidate;

    
    void Start()
    {
        fullPath = Path.Combine(Application.persistentDataPath, fileName);
        
        // ensure trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        
        string content = File.ReadAllText(fullPath);
        if (content.Length > 0)
        {
            GameObject savedObj = Instantiate(defaultCube, snapPoint.transform.position, Quaternion.identity);
            saveCopyIcon[0].SetActive(false);
            saveCopyIcon[1].SetActive(true);
            
            ProBuilderMeshData loadedData = JsonUtility.FromJson<ProBuilderMeshData>(File.ReadAllText(fullPath));

            ProBuilderMesh mesh = savedObj.GetComponent<ProBuilderMesh>();
            if (mesh == null) mesh = savedObj.AddComponent<ProBuilderMesh>();

            mesh.Clear();
            mesh.positions = new List<Vector3>(loadedData.positions);
            mesh.faces = loadedData.faces.Select(sf => sf.ToFace()).ToList();
            mesh.sharedVertices = loadedData.sharedVertices.Select(sv => sv.ToSharedVertex()).ToArray();
            mesh.ToMesh();
            mesh.Refresh();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & draggableLayers) != 0 && !_isHooked)
        {
            
            _candidate = other.gameObject;
            
            ProBuilderMesh otherPb = other.gameObject.GetComponent<ProBuilderMesh>();
            otherPb.SetPivot(other.transform.GetComponent<Renderer>().bounds.center);
            
            grabManager.GetComponent<GrapInteractor>().DetachFromController();
            other.transform.position = snapPoint.position;
            
            PBUtils.NormalizeMeshSize(other.GetComponent<ProBuilderMesh>(), 0.1f);
            
            // Save Mesh data
            ProBuilderMeshData data = new ProBuilderMeshData
            {
                positions = new List<Vector3>(otherPb.positions),
                faces = otherPb.faces.Select(f => new SerializableFace(f)).ToList(),
                sharedVertices = otherPb.sharedVertices.Select(sv => new SharedVertexData(sv)).ToList()
            };

            _json = JsonUtility.ToJson(data, true);
            
            try
            {
                File.WriteAllText(fullPath, _json);
            }
            catch (IOException e)
            {
                Debug.LogError($"Failed to write mesh data to file: {e.Message}");
            }
            
            // Enable the copying
            var platformObj = spawnPlatform.GetComponent<ShapeCopying>();
            platformObj.proBuilderShape = defaultCube;
            saveCopyIcon[0].SetActive(false);
            saveCopyIcon[1].SetActive(true);
            
            _isHooked = true;
        }
    }
    
    void OnTriggerExit(Collider other)
        {
            if (other.gameObject == _candidate && _isHooked)
            {
                _candidate = null;
                _isHooked = false;

                var platformObj = spawnPlatform.GetComponent<ShapeCopying>();
                platformObj.proBuilderShape = _candidate;
                saveCopyIcon[0].SetActive(true);
                saveCopyIcon[1].SetActive(false);
                
                try
                {
                    File.WriteAllText(fullPath, string.Empty);
                }
                catch (IOException e)
                {
                    Debug.LogError($"Failed to clear mesh file: {e.Message}");
                }
            }
        }    
    
    // Load Mesh data
    public void LoadData()
    {
        ProBuilderMeshData loadedData = JsonUtility.FromJson<ProBuilderMeshData>(File.ReadAllText(fullPath));

        ProBuilderMesh mesh = copiedMesh.GetComponent<ProBuilderMesh>();
        if (mesh == null) mesh = copiedMesh.AddComponent<ProBuilderMesh>();

        mesh.Clear();
        mesh.positions = new List<Vector3>(loadedData.positions);
        mesh.faces = loadedData.faces.Select(sf => sf.ToFace()).ToList();
        mesh.sharedVertices = loadedData.sharedVertices.Select(sv => sv.ToSharedVertex()).ToArray();
        
        PBUtils.NormalizeMeshSize(mesh, 0.15f);
        
        mesh.ToMesh();
        mesh.Refresh();
    }
}

[System.Serializable]
public class ProBuilderMeshData
{
    public List<Vector3> positions;
    public List<SerializableFace> faces;
    public List<SharedVertexData> sharedVertices;
}

[System.Serializable]
public class SerializableFace
{
    public List<int> indexes;

    public SerializableFace(Face face)
    {
        indexes = new List<int>(face.indexes);
    }

    public Face ToFace()
    {
        return new Face(indexes.ToArray());
    }
}

[System.Serializable]
public class SharedVertexData
{
    public List<int> indexes;

    public SharedVertexData(SharedVertex sv)
    {
        indexes = new List<int>(sv);
    }

    public SharedVertex ToSharedVertex()
    {
        return new SharedVertex(indexes);
    }
}