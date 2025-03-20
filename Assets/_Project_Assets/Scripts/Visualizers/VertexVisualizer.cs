using System.Collections.Generic;
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
    public float sphereRadius = 0.05f;
    public float padlockOffset = 0.1f; // Distance to place the padlock outside the vertex

    void Start()
    {
        _pbMesh = GetComponent<ProBuilderMesh>();

        if (_pbMesh != null)
        {
            UpdateVertexSpheresAndPadlocks();
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

        Vector3[] normals = _pbMesh.GetNormals(); // Get vertex normals

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
                    sphere.transform.localScale = Vector3.one * sphereRadius;
                    sphere.transform.SetParent(_pbMesh.transform, true);
                    _vertexSpheres[vertexIndex] = sphere;
                }

                // Add this position to the set of unique positions
                _uniqueVertexPositions.Add(vertexPosition);
            }

            // Now handle the padlock as a child of the sphere
            if (_vertexSpheres.ContainsKey(vertexIndex))
            {
                GameObject sphere = _vertexSpheres[vertexIndex];
                Vector3 padlockPosition = FindPadlockPosition(vertexPosition, normals[vertexIndex]);

                // Create or update the padlock
                if (_vertexPadlocks.ContainsKey(vertexIndex))
                {
                    _vertexPadlocks[vertexIndex].transform.position = padlockPosition;
                }
                else
                {
                    GameObject padlock = Instantiate(padlockPrefab, padlockPosition, Quaternion.identity);
                    padlock.transform.SetParent(sphere.transform, true); // Set the sphere as the parent
                    padlock.AddComponent<PadlockToggle>(); // Attach the toggle script
                    _vertexPadlocks[vertexIndex] = padlock;
                }
            }

            _lastVertexPositions[vertexIndex] = vertexPosition;
        }
    }

    Vector3 FindPadlockPosition(Vector3 vertexPosition, Vector3 normal)
    {
        return vertexPosition + normal.normalized * padlockOffset;
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
}

public class PadlockToggle : MonoBehaviour
{
    private bool isLocked = false;
    public void ToggleLock()
    {
        isLocked = !isLocked;
        GetComponent<Renderer>().material.color = isLocked ? Color.red : Color.green;
    }
}
