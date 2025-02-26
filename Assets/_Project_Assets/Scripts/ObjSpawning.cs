using UnityEngine;
using UnityEngine.ProBuilder;

public class ObjSpawning : MonoBehaviour
{
    public GameObject pbCubePrefab;
    public GameObject pbSpherePrefab;
    public GameObject pbCylinderPrefab;
    public GameObject pbTorusPrefab;
    public GameObject handlePrefab;
    public GameObject rightController;
    
    
    private GameObject _pbObj;
    
    private Transform _rightControllerTransform;
    
    private ProBuilderMesh _pbMesh;
    
    
    public void SpawnCube()
    {
        Spawner(pbCubePrefab);
    }
    
    public void SpawnSphere()
    {
        Spawner(pbSpherePrefab);
    }
    
    public void SpawnTorus(){}
    
    public void SpawnCylinder(){}

    void Spawner(GameObject obj)
    {
        _rightControllerTransform = rightController.GetComponent<Transform>();
        _pbObj = Instantiate(obj, _rightControllerTransform.position, Quaternion.identity);
        _pbMesh = _pbObj.GetComponent<ProBuilderMesh>();
        HandleOnFace();
    }
    
    
    Vector3 GetFaceCenter(Face face)
    {
        Vector3 sum = Vector3.zero;
        int count = face.indexes.Count;
        
        foreach (int index in face.indexes)
        {
            sum += _pbMesh.transform.TransformPoint(_pbMesh.positions[index]); // Convert local space to world space
        }
    
        return sum / count;
    }

    void HandleOnFace()
    {
        foreach (Face face in _pbMesh.faces)
        {
            Vector3 faceCenter = GetFaceCenter(face);

            Vector3 faceNormal = Math.Normal(_pbMesh, face);
            Quaternion rotation = Quaternion.LookRotation(faceNormal);

            GameObject handle = Instantiate(handlePrefab, faceCenter, rotation);
            handle.transform.SetParent(_pbMesh.transform, true); // Attach handle to the cube
        }
    }
    
}
