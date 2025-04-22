using UnityEngine;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using System.Linq;

public class PBMeshSave : MonoBehaviour
{
    
    private ProBuilderMesh _pbMesh;

    public GameObject targetGameObject;

    private void Start()
    {
        _pbMesh = GetComponent<ProBuilderMesh>();
    }

    private void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            ProBuilderMeshData2 data = new ProBuilderMeshData2
            {
                positions = new List<Vector3>(_pbMesh.positions),
                faces = _pbMesh.faces.Select(f => new SerializableFace2(f)).ToList(),
                sharedVertices = _pbMesh.sharedVertices.Select(sv => new SharedVertexData2(sv)).ToList()
            };

            string json = JsonUtility.ToJson(data);
            Debug.Log(json);
            
            
            
            ProBuilderMeshData2 loadedData = JsonUtility.FromJson<ProBuilderMeshData2>(json);

            ProBuilderMesh mesh = targetGameObject.GetComponent<ProBuilderMesh>();
            if (mesh == null) mesh = targetGameObject.AddComponent<ProBuilderMesh>();

            mesh.Clear();
            mesh.positions = new List<Vector3>(loadedData.positions);
            mesh.faces = loadedData.faces.Select(sf => sf.ToFace()).ToList();
            mesh.sharedVertices = loadedData.sharedVertices.Select(sv => sv.ToSharedVertex()).ToArray();
            mesh.ToMesh();
            mesh.Refresh();
        }
    }
}




[System.Serializable]
public class ProBuilderMeshData2
{
    public List<Vector3> positions;
    public List<SerializableFace2> faces;
    public List<SharedVertexData2> sharedVertices;
}

[System.Serializable]
public class SerializableFace2
{
    public List<int> indexes;

    public SerializableFace2(Face face)
    {
        indexes = new List<int>(face.indexes);
    }

    public Face ToFace()
    {
        return new Face(indexes.ToArray());
    }
}

[System.Serializable]
public class SharedVertexData2
{
    public List<int> indexes;

    public SharedVertexData2(SharedVertex sv)
    {
        indexes = new List<int>(sv);
    }

    public SharedVertex ToSharedVertex()
    {
        return new SharedVertex(indexes);
    }
}