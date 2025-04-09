using System.Collections.Generic;
using System.Linq;
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

    void Start()
    {
        _pbMesh = GetComponent<ProBuilderMesh>();

        if (_pbMesh != null)
        {
            UpdateHandles();
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (_pbMesh == null) return;

        // Clear if there's any stale face reference
        if (_faceHandles.Keys.Any(f => !_pbMesh.faces.Contains(f)))
        {
            ClearAll();
            UpdateHandles();
            return;
        }

        List<Face> modifiedFaces = GetModifiedFaces();
        if (modifiedFaces.Count > 0)
        {
            UpdateHandles(modifiedFaces);
        }
    }

    void UpdateHandles(List<Face> modifiedFaces = null)
    {
        if (_pbMesh == null) return;

        if (modifiedFaces == null)
        {
            modifiedFaces = new List<Face>(_pbMesh.faces);
        }

        foreach (Face face in modifiedFaces)
        {
            if (face == null || face.indexes == null || face.indexes.Count == 0)
                continue;

            Vector3 faceCenter = GetFaceCenter(face);
            Vector3 faceNormal = _pbMesh.transform.TransformDirection(Math.Normal(_pbMesh, face));

            if (!_faceEdges.ContainsKey(face))
            {
                _faceEdges[face] = GetLongestEdge(face);
            }

            Edge longestEdge = _faceEdges[face];

            if (longestEdge.a < 0 || longestEdge.b < 0 ||
                longestEdge.a >= _pbMesh.positions.Count || longestEdge.b >= _pbMesh.positions.Count)
                continue;

            Vector3 pointA = _pbMesh.transform.TransformPoint(_pbMesh.positions[longestEdge.a]);
            Vector3 pointB = _pbMesh.transform.TransformPoint(_pbMesh.positions[longestEdge.b]);
            Vector3 longestEdgeDir = (pointB - pointA).normalized;

            Quaternion faceRotation = Quaternion.LookRotation(faceNormal, longestEdgeDir);

            if (_lastFaceCenters.ContainsKey(face) && _lastFaceCenters[face] == faceCenter &&
                _lastFaceRotations.ContainsKey(face) && _lastFaceRotations[face] == faceRotation)
            {
                continue;
            }

            if (_faceHandles.ContainsKey(face))
            {
                GameObject handle = _faceHandles[face];
                if (handle != null)
                {
                    handle.transform.position = faceCenter;
                    handle.transform.rotation = faceRotation;
                }
            }
            else
            {
                GameObject handle = Instantiate(handlePrefab, faceCenter, faceRotation);
                handle.name = $"FaceHandle_{face.indexes[0]}";
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
            if (face == null || face.indexes == null || face.indexes.Count == 0) continue;

            Vector3 faceCenter = GetFaceCenter(face);
            Vector3 faceNormal = _pbMesh.transform.TransformDirection(Math.Normal(_pbMesh, face));
            Quaternion faceRotation = Quaternion.LookRotation(faceNormal);

            bool centerChanged = !_lastFaceCenters.ContainsKey(face) || _lastFaceCenters[face] != faceCenter;
            bool rotationChanged = !_lastFaceRotations.ContainsKey(face) || _lastFaceRotations[face] != faceRotation;

            if (centerChanged || rotationChanged)
            {
                modifiedFaces.Add(face);
            }
        }
        return modifiedFaces;
    }

    Edge GetLongestEdge(Face face)
    {
        if (face == null || face.edges == null) return default;

        float maxLength = 0f;
        Edge bestEdge = default;
        int minVertexIndex = int.MaxValue;

        foreach (Edge edge in face.edges)
        {
            if (edge.a < 0 || edge.b < 0 || edge.a >= _pbMesh.positions.Count || edge.b >= _pbMesh.positions.Count)
                continue;

            Vector3 pointA = _pbMesh.transform.TransformPoint(_pbMesh.positions[edge.a]);
            Vector3 pointB = _pbMesh.transform.TransformPoint(_pbMesh.positions[edge.b]);

            float length = Vector3.Distance(pointA, pointB);
            int edgeMinVertex = Mathf.Min(edge.a, edge.b);

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

        List<Transform> childrenToDestroy = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("FaceHandle_"))
            {
                childrenToDestroy.Add(child);
            }
        }

        foreach (var child in childrenToDestroy)
        {
            Destroy(child.gameObject);
        }
    }

    public void RebuildHandles()
    {
        if (_pbMesh == null) _pbMesh = GetComponent<ProBuilderMesh>();
        if (_pbMesh != null)
        {
            ClearAll();
            UpdateHandles(); // Rebuild from scratch
        }
    }
}
