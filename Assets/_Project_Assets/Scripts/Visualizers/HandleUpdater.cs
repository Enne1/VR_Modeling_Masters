using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class HandleUpdater : MonoBehaviour
{
    private ProBuilderMesh _pbMesh;
    private Dictionary<Face, GameObject> _faceHandles = new Dictionary<Face, GameObject>();
    private Dictionary<Face, Vector3> _lastFaceCenters = new Dictionary<Face, Vector3>();
    private Dictionary<Face, Quaternion> _lastFaceRotations = new Dictionary<Face, Quaternion>();
    
    private Dictionary<Face, Edge> _faceEdges = new Dictionary<Face, Edge>();


    public GameObject handlePrefab;
    public GameObject placeholderSignifier;
    //public float handleSize = 0.02f;

    void Start()
    {
        _pbMesh = GetComponent<ProBuilderMesh>();

        if (_pbMesh != null)
        {
            UpdateHandles();
            
            foreach( Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (_pbMesh == null) return;

        List<Face> modifiedFaces = GetModifiedFaces();
        if (modifiedFaces.Count > 0)
        {
            UpdateHandles(modifiedFaces);
        }
    }

    void UpdateHandles(List<Face> modifiedFaces = null)
    {
        if (modifiedFaces == null)
        {
            modifiedFaces = new List<Face>(_pbMesh.faces);
        }

        foreach (Face face in modifiedFaces)
        {
            Vector3 faceCenter = GetFaceCenter(face);
            Vector3 faceNormal = _pbMesh.transform.TransformDirection(Math.Normal(_pbMesh, face));

            // Use stored edge if available, otherwise find and store the longest edge
            if (!_faceEdges.ContainsKey(face))
            {
                _faceEdges[face] = GetLongestEdge(face);
            }

            Edge longestEdge = _faceEdges[face];

            // Get the world positions of the longest edge
            Vector3 pointA = _pbMesh.transform.TransformPoint(_pbMesh.positions[longestEdge.a]);
            Vector3 pointB = _pbMesh.transform.TransformPoint(_pbMesh.positions[longestEdge.b]);
            Vector3 longestEdgeDir = (pointB - pointA).normalized;  // This is the edge direction

            // Make sure the rotation is aligned to face normal, and the longest edge for stability
            Quaternion faceRotation = Quaternion.LookRotation(faceNormal, longestEdgeDir);

            // Skip unnecessary updates to prevent flickering
            if (_lastFaceCenters.ContainsKey(face) && _lastFaceCenters[face] == faceCenter &&
                _lastFaceRotations.ContainsKey(face) && _lastFaceRotations[face] == faceRotation)
            {
                continue;
            }

            if (_faceHandles.ContainsKey(face))
            {
                GameObject handle = _faceHandles[face];
                handle.transform.position = faceCenter;
                handle.transform.rotation = faceRotation;
            }
            else
            {
                GameObject handle = Instantiate(handlePrefab, faceCenter, faceRotation);
                handle.transform.SetParent(_pbMesh.transform, true);
                _faceHandles[face] = handle;
            }

            _lastFaceCenters[face] = faceCenter;
            _lastFaceRotations[face] = faceRotation;
        }
    }

    Vector3 GetFaceCenter(Face face)
    {
        Vector3 sum = Vector3.zero;
        foreach (int index in face.indexes)
        {
            sum += _pbMesh.transform.TransformPoint(_pbMesh.positions[index]);
        }
        return sum / face.indexes.Count;
    }

    List<Face> GetModifiedFaces()
    {
        List<Face> modifiedFaces = new List<Face>();
        foreach (Face face in _pbMesh.faces)
        {
            Vector3 faceCenter = GetFaceCenter(face);
            Vector3 faceNormal = _pbMesh.transform.TransformDirection(Math.Normal(_pbMesh, face));
            Quaternion faceRotation = Quaternion.LookRotation(faceNormal);

            if (!_lastFaceCenters.ContainsKey(face) || _lastFaceCenters[face] != faceCenter ||
                !_lastFaceRotations.ContainsKey(face) || _lastFaceRotations[face] != faceRotation)
            {
                modifiedFaces.Add(face);
            }
        }
        return modifiedFaces;
    }
    
    Edge GetLongestEdge(Face face)
    {
        float maxLength = 0f;
        Edge bestEdge = default;
        int minVertexIndex = int.MaxValue;

        foreach (Edge edge in face.edges)
        {
            Vector3 pointA = _pbMesh.transform.TransformPoint(_pbMesh.positions[edge.a]);
            Vector3 pointB = _pbMesh.transform.TransformPoint(_pbMesh.positions[edge.b]);

            float length = Vector3.Distance(pointA, pointB);
            int edgeMinVertex = Mathf.Min(edge.a, edge.b); // Get the lowest vertex index

            if (length > maxLength || (Mathf.Approximately(length, maxLength) && edgeMinVertex < minVertexIndex))
            {
                maxLength = length;
                bestEdge = edge;
                minVertexIndex = edgeMinVertex;
            }
        }

        return bestEdge;
    }
    
    public void ClearAll()
    {
        _faceHandles.Clear();
        _lastFaceCenters.Clear();
        _lastFaceRotations.Clear();
        _faceEdges.Clear();
    }
}
