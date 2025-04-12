using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

public class VertexVisualizer : MonoBehaviour
{
    private ProBuilderMesh _pbMesh;
    private Dictionary<int, GameObject> _vertexSpheres = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> _vertexPadlocks = new Dictionary<int, GameObject>();
    private Dictionary<int, Vector3> _lastGroupPositions = new Dictionary<int, Vector3>();

    public GameObject spherePrefab;
    public GameObject padlockPrefab;
    public float padlockOffset = 0.1f;

    void Start()
    {
        _pbMesh = GetComponent<ProBuilderMesh>();
        if (_pbMesh != null)
        {
            UpdateVertexSpheresAndPadlocks(); // Init all
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (_pbMesh == null) return;

        List<int> modifiedGroups = GetModifiedSharedVertexGroups();
        if (modifiedGroups.Count > 0)
        {
            UpdateVertexSpheresAndPadlocks(modifiedGroups);
        }
    }

    void UpdateVertexSpheresAndPadlocks(List<int> modifiedGroups = null)
    {
        if (_pbMesh == null) return;

        var sharedVertices = _pbMesh.sharedVertices;
        if (modifiedGroups == null)
        {
            modifiedGroups = new List<int>();
            for (int i = 0; i < sharedVertices.Count; i++)
            {
                modifiedGroups.Add(i);
            }
        }

        foreach (int groupIndex in modifiedGroups)
        {
            int representative = sharedVertices[groupIndex][0];
            Vector3 localPos = _pbMesh.positions[representative];
            Vector3 worldPos = _pbMesh.transform.TransformPoint(localPos);

            // Update or create sphere
            if (!_vertexSpheres.ContainsKey(groupIndex))
            {
                GameObject sphere = Instantiate(spherePrefab, worldPos, Quaternion.identity);
                sphere.name = $"Sphere_{groupIndex}";
                sphere.transform.SetParent(_pbMesh.transform, true);
                _vertexSpheres[groupIndex] = sphere;
            }
            else
            {
                _vertexSpheres[groupIndex].transform.position = worldPos;
            }

            // Update or create padlock
            if (!_vertexPadlocks.ContainsKey(groupIndex))
            {
                GameObject padlock = Instantiate(padlockPrefab, Vector3.zero, Quaternion.identity, _vertexSpheres[groupIndex].transform);
                padlock.transform.localPosition = FindPadlockLocalPosition(representative, _vertexSpheres[groupIndex].transform);
                _vertexPadlocks[groupIndex] = padlock;
            }
            else
            {
                _vertexPadlocks[groupIndex].transform.localPosition = FindPadlockLocalPosition(representative, _vertexSpheres[groupIndex].transform);
            }

            _lastGroupPositions[groupIndex] = localPos;
        }
    }

    List<int> GetModifiedSharedVertexGroups()
    {
        List<int> modified = new List<int>();
        var sharedVertices = _pbMesh.sharedVertices;

        for (int i = 0; i < sharedVertices.Count; i++)
        {
            int representative = sharedVertices[i][0];
            Vector3 localPos = _pbMesh.positions[representative];

            if (!_lastGroupPositions.ContainsKey(i) || _lastGroupPositions[i] != localPos)
            {
                modified.Add(i);
            }
        }
        return modified;
    }

    Vector3 FindPadlockLocalPosition(int vertexIndex, Transform sphereTransform)
    {
        Vector3 optimalDirectionWorld = GetFurthestDirection(vertexIndex);
        Vector3 optimalDirectionLocal = sphereTransform.InverseTransformDirection(optimalDirectionWorld);
        return -optimalDirectionLocal.normalized * padlockOffset;
    }

    Vector3 GetFurthestDirection(int vertexIndex)
    {
        List<Vector3> normals = GetConnectedFaceNormals(vertexIndex);
        if (normals.Count == 0)
            return Vector3.up;

        Vector3 sum = Vector3.zero;
        foreach (var normal in normals)
        {
            sum += normal;
        }

        return sum == Vector3.zero ? Vector3.up : (-sum).normalized;
    }

    List<Vector3> GetConnectedFaceNormals(int vertexIndex)
    {
        List<Vector3> normals = new List<Vector3>();
        HashSet<int> sharedIndexes = new HashSet<int>();

        foreach (SharedVertex sharedVertex in _pbMesh.sharedVertices)
        {
            if (sharedVertex.Contains(vertexIndex))
            {
                sharedIndexes.UnionWith(sharedVertex);
                break;
            }
        }

        foreach (Face face in _pbMesh.faces)
        {
            if (face.indexes.Any(sharedIndexes.Contains))
            {
                Vector3 localNormal = Math.Normal(_pbMesh, face);
                Vector3 worldNormal = _pbMesh.transform.TransformDirection(localNormal);
                normals.Add(worldNormal);
            }
        }

        return normals;
    }

    public void ClearAll()
    {
        _vertexSpheres.Clear();
        _vertexPadlocks.Clear();
        _lastGroupPositions.Clear();

        List<Transform> childrenToDestroy = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Sphere_") || child.name.StartsWith("Padlock_"))
            {
                childrenToDestroy.Add(child);
            }
        }

        foreach (var child in childrenToDestroy)
        {
            Destroy(child.gameObject);
        }
    }

    public void RebuildVertices()
    {
        if (_pbMesh == null)
            _pbMesh = GetComponent<ProBuilderMesh>();

        ClearAll();
        UpdateVertexSpheresAndPadlocks();
    }
}
