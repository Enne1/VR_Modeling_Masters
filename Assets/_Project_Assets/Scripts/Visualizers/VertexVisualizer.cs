using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class VertexVisualizer : MonoBehaviour
{
    private ProBuilderMesh _pbMesh;
    private Dictionary<int, GameObject> _vertexSpheres = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> _vertexPadlocks = new Dictionary<int, GameObject>();
    // Store local positions for change detection.
    private Dictionary<int, Vector3> _lastVertexPositions = new Dictionary<int, Vector3>();
    // Keep track of unique local vertex positions.
    private HashSet<Vector3> _uniqueVertexPositions = new HashSet<Vector3>();

    public GameObject spherePrefab;
    public GameObject padlockPrefab;
    public float padlockOffset = 0.1f;

    void Start()
    {
        _pbMesh = GetComponent<ProBuilderMesh>();
        if (_pbMesh != null)
        {
            // Instantiate signifiers for all current vertices.
            UpdateVertexSpheresAndPadlocks();

            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (_pbMesh == null)
            return;

        // Get vertices that are new or whose local positions have changed.
        List<int> modifiedVertices = GetModifiedVertices();
        if (modifiedVertices.Count > 0)
        {
            UpdateVertexSpheresAndPadlocks(modifiedVertices);
        }
    }
    
    /// <summary>
    /// Updates or creates the vertex spheres and padlocks.
    /// Only processes vertices given in modifiedVertices.
    /// If modifiedVertices is null, all vertices are updated.
    /// </summary>
    void UpdateVertexSpheresAndPadlocks(List<int> modifiedVertices = null)
    {
        if (modifiedVertices == null)
        {
            modifiedVertices = new List<int>();
            for (int i = 0; i < _pbMesh.positions.Count; i++)
            {
                modifiedVertices.Add(i);
            }
        }
        //Debug.Log("Modified vertices: " + modifiedVertices.Count);

        foreach (int vertexIndex in modifiedVertices)
        {
            // Get vertex position in local space and convert to world space.
            Vector3 localVertexPos = _pbMesh.positions[vertexIndex];
            Vector3 worldVertexPos = _pbMesh.transform.TransformPoint(localVertexPos);

            // Update or create the vertex sphere.
            if (!_uniqueVertexPositions.Contains(localVertexPos))
            {
                if (_vertexSpheres.ContainsKey(vertexIndex))
                {
                    _vertexSpheres[vertexIndex].transform.position = worldVertexPos;
                }
                else
                {
                    Debug.Log("Adding vertex sphere for index " + vertexIndex);
                    GameObject sphere = Instantiate(spherePrefab, worldVertexPos, Quaternion.identity);
                    sphere.transform.SetParent(_pbMesh.transform, true);
                    _vertexSpheres[vertexIndex] = sphere;
                }
                _uniqueVertexPositions.Add(localVertexPos);
            }
            else
            {
                if (_vertexSpheres.ContainsKey(vertexIndex))
                {
                    _vertexSpheres[vertexIndex].transform.position = worldVertexPos;
                }
            }

            // Update or create padlock as child of the sphere.
            if (_vertexSpheres.ContainsKey(vertexIndex))
            {
                if (!_vertexPadlocks.ContainsKey(vertexIndex))
                {
                    Debug.Log("Adding padlock for vertex index " + vertexIndex);
                    // Instantiate padlock as a child so that we can easily set its local position.
                    GameObject padlock = Instantiate(padlockPrefab, Vector3.zero, Quaternion.identity, _vertexSpheres[vertexIndex].transform);
                    // Set its local position based on the computed optimal direction.
                    padlock.transform.localPosition = FindPadlockLocalPosition(vertexIndex, _vertexSpheres[vertexIndex].transform);
                    _vertexPadlocks[vertexIndex] = padlock;
                }
                else
                {
                    // Update the padlock's local position.
                    _vertexPadlocks[vertexIndex].transform.localPosition = FindPadlockLocalPosition(vertexIndex, _vertexSpheres[vertexIndex].transform);
                }
            }
            _lastVertexPositions[vertexIndex] = localVertexPos;
        }
    }

    /// <summary>
    /// Computes the padlock's position in the sphere's local space.
    /// </summary>
    Vector3 FindPadlockLocalPosition(int vertexIndex, Transform sphereTransform)
    {
        // Get the optimal (outward) direction in world space.
        Vector3 optimalDirectionWorld = GetFurthestDirection(vertexIndex);
        // Convert this optimal direction to the sphere's local space.
        Vector3 optimalDirectionLocal = sphereTransform.InverseTransformDirection(optimalDirectionWorld);
        // Invert the vector to ensure the padlock is placed outward, not inward.
        optimalDirectionLocal = -optimalDirectionLocal;
        // Scale by offset; this gives the padlock's local offset.
        return optimalDirectionLocal * padlockOffset;
    }

    /// <summary>
    /// Computes an optimal outward direction based on connected face normals.
    /// </summary>
    Vector3 GetFurthestDirection(int vertexIndex)
    {
        List<Vector3> faceNormals = GetConnectedFaceNormals(vertexIndex);
        if (faceNormals.Count == 0)
            return Vector3.up;

        Vector3 sumNormals = Vector3.zero;
        foreach (Vector3 normal in faceNormals)
        {
            sumNormals += normal;
        }
        // Return the normalized, negated sum (or default to up if zero).
        return sumNormals == Vector3.zero ? Vector3.up : (-sumNormals).normalized;
    }

    /// <summary>
    /// Retrieves world-space normals of all faces connected to the given vertex.
    /// </summary>
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

    /// <summary>
    /// Determines which vertices have changed in their local positions
    /// or are newly added.
    /// </summary>
    List<int> GetModifiedVertices()
    {
        List<int> modifiedVertices = new List<int>();
        for (int i = 0; i < _pbMesh.positions.Count; i++)
        {
            Vector3 localVertexPos = _pbMesh.positions[i];
            if (!_lastVertexPositions.ContainsKey(i) || _lastVertexPositions[i] != localVertexPos)
            {
                modifiedVertices.Add(i);
            }
        }
        return modifiedVertices;
    }

    public void ClearAll()
    {
        _vertexSpheres.Clear();
        _vertexPadlocks.Clear();
        _lastVertexPositions.Clear();
        _uniqueVertexPositions.Clear();

        // Destroy all instantiated signifier objects.
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
