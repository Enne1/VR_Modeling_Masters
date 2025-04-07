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
    private Dictionary<int, Vector3> _lastVertexPositions = new Dictionary<int, Vector3>();

    private HashSet<Vector3> _uniqueVertexPositions = new HashSet<Vector3>(); // Set to track unique positions

    public GameObject spherePrefab;  // Reference to the small sphere prefab
    public GameObject padlockPrefab; // Reference to the padlock prefab
    public float padlockOffset = 0.1f; // Distance to place the padlock outside the vertex
    
    void Start()
    {
        _pbMesh = GetComponent<ProBuilderMesh>();
        
        if (_pbMesh != null)
        {
            UpdateVertexSpheresAndPadlocks();
            
            foreach( Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (_pbMesh == null) return;

        List<int> modifiedVertices = GetModifiedVertices();
        if (modifiedVertices.Count > 0)
        {
            UpdateVertexSpheresAndPadlocks(modifiedVertices);
        }
    }

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
        
        foreach (int vertexIndex in modifiedVertices)
        {
            Vector3 vertexPosition = _pbMesh.transform.TransformPoint(_pbMesh.positions[vertexIndex]);

            // Only create a sphere if this position hasn't been used yet
            if (!_uniqueVertexPositions.Contains(vertexPosition))
            {
                // Update or create sphere
                if (_vertexSpheres.ContainsKey(vertexIndex))
                {
                    _vertexSpheres[vertexIndex].transform.position = vertexPosition;
                }
                else
                {
                    GameObject sphere = Instantiate(spherePrefab, vertexPosition, Quaternion.identity);
                    sphere.transform.SetParent(_pbMesh.transform, true);
                    _vertexSpheres[vertexIndex] = sphere;
                }

                // Add this position to the set of unique positions
                _uniqueVertexPositions.Add(vertexPosition);
            }

            // Now handle the padlock as a child of the sphere
            if (_vertexSpheres.ContainsKey(vertexIndex))
            {
                Vector3 padlockPosition = FindPadlockPosition(transform.InverseTransformPoint(vertexPosition), vertexIndex);
                
                if(!_vertexPadlocks.ContainsKey(vertexIndex))
                {
                    GameObject padlock = Instantiate(padlockPrefab, padlockPosition, Quaternion.identity);
                    padlock.transform.SetParent(_vertexSpheres[vertexIndex].transform, false);
                    _vertexPadlocks[vertexIndex] = padlock;
                }
            }
            _lastVertexPositions[vertexIndex] = vertexPosition;
        }
    }

    Vector3 FindPadlockPosition(Vector3 vertexPosition, int vertexIndex)
    {
        Vector3 optimalDirection = GetFurthestDirection(vertexIndex);
        Vector3 worldVertexPosition = transform.TransformPoint(vertexPosition);
        // Vector3 worldPadlockPosition = (worldVertexPosition + optimalDirection) * padlockOffset;
        // return transform.InverseTransformPoint(worldPadlockPosition);
        Vector3 worldPadlockPosition = (vertexPosition + optimalDirection) * -padlockOffset;
        return worldPadlockPosition;
    }

    Vector3 GetFurthestDirection(int vertexIndex)
    {
        List<Vector3> faceNormals = GetConnectedFaceNormals(vertexIndex);
        if (faceNormals.Count == 0)
            return Vector3.up; // Default fallback

        Vector3 sumNormals = Vector3.zero;
        foreach (Vector3 normal in faceNormals)
        {
            sumNormals += normal;
        }

        if (sumNormals == Vector3.zero)
            return Vector3.up; // Fallback case

        Vector3 bestDirection = (-sumNormals).normalized; // Invert direction
        return bestDirection;
    }   

    List<Vector3> GetConnectedFaceNormals(int vertexIndex)
    {
        List<Vector3> normals = new List<Vector3>();

        // Get all shared vertex handles (groups of equivalent vertices)
        HashSet<int> sharedIndexes = new HashSet<int>();

        foreach (SharedVertex sharedVertex in _pbMesh.sharedVertices)
        {
            if (sharedVertex.Contains(vertexIndex))
            {
                sharedIndexes.UnionWith(sharedVertex);
                break; // Stop after finding the matching shared vertex group
            }
        }

        foreach (Face face in _pbMesh.faces)
        {
            // If any of the shared vertices exist in this face, consider it
            if (face.indexes.Any(sharedIndexes.Contains))  
            {
                Vector3 localNormal = Math.Normal(_pbMesh, face); // Local space normal
                Vector3 worldNormal = _pbMesh.transform.TransformDirection(localNormal); // Convert to world space
            
                normals.Add(worldNormal);
            }
        }
        return normals;
    }
    
    List<int> GetModifiedVertices()
    {
        List<int> modifiedVertices = new List<int>();
        for (int i = 0; i < _pbMesh.positions.Count; i++)
        {
            Vector3 vertexPosition = _pbMesh.transform.TransformPoint(_pbMesh.positions[i]);
            if (!_lastVertexPositions.ContainsKey(i) || _lastVertexPositions[i] != vertexPosition)
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
    }
}