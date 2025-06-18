using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using System.Linq;

public class HandleUpdater : MonoBehaviour
{
    private ProBuilderMesh _pbMesh;
    private Dictionary<int, GameObject> _faceHandles = new();
    private Dictionary<int, Vector3> _lastFaceCenters = new();
    private Dictionary<int, Quaternion> _lastFaceRotations = new();
    private Dictionary<int, Edge> _faceEdges = new();

    public GameObject handlePrefab;

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

        List<Face> modifiedFaces = new List<Face>();

        // Loop through each face to check for changes
        foreach (Face face in _pbMesh.faces)
        {
            if (face == null || face.indexes == null || face.indexes.Count == 0) continue;

            int faceId = face.indexes.Min();
            Vector3 currentCenter = GetFaceCenter(face);
            Vector3 faceNormal = _pbMesh.transform.TransformDirection(Math.Normal(_pbMesh, face));

            // Cache the longest edge for the face, used for the handles rotation
            if (!_faceEdges.ContainsKey(faceId))
            {
                _faceEdges[faceId] = GetLongestEdge(face);
            }

            Edge longestEdge = _faceEdges[faceId];

            if (longestEdge.a < 0 || longestEdge.b < 0 ||
                longestEdge.a >= _pbMesh.positions.Count || longestEdge.b >= _pbMesh.positions.Count)
                continue;
            
            // Compute the world-space direction of the longest edge
            Vector3 pointA = _pbMesh.transform.TransformPoint(_pbMesh.positions[longestEdge.a]);
            Vector3 pointB = _pbMesh.transform.TransformPoint(_pbMesh.positions[longestEdge.b]);
            Vector3 edgeDir = (pointB - pointA).normalized;

            Quaternion currentRotation;

            if (faceNormal.sqrMagnitude > 0.0001f)
            {
                currentRotation = Quaternion.LookRotation(faceNormal.normalized, edgeDir.normalized);
                // ... rest of your code
            }
            else
            {
                // Fallback rotation if faceNormal is zero
                currentRotation = Quaternion.identity;
                // Or some other default rotation that makes sense in your context
            }
            
            bool centerChanged = !_lastFaceCenters.ContainsKey(faceId) || Vector3.Distance(_lastFaceCenters[faceId], currentCenter) > 0.001f;
            bool rotationChanged = !_lastFaceRotations.ContainsKey(faceId) || Quaternion.Angle(_lastFaceRotations[faceId], currentRotation) > 0.1f;

            if (centerChanged || rotationChanged)
            {
                modifiedFaces.Add(face);
                _lastFaceCenters[faceId] = currentCenter;
                _lastFaceRotations[faceId] = currentRotation;
            }
        }
        
        // Update only the handles for faces that changed
        if (modifiedFaces.Count > 0)
        {
            UpdateHandles(modifiedFaces);
        }
    }

    /// <summary>
    /// Creates or updates handle GameObjects for a list of faces.
    /// </summary>
    public void UpdateHandles(List<Face> modifiedFaces = null)
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

            int faceId = face.indexes.Min();
            
            // Compute face center and normal
            Vector3 faceCenter = GetFaceCenter(face);
            Vector3 faceNormal = _pbMesh.transform.TransformDirection(Math.Normal(_pbMesh, face));

            if (!_faceEdges.ContainsKey(faceId))
            {
                _faceEdges[faceId] = GetLongestEdge(face);
            }

            Edge longestEdge = _faceEdges[faceId];

            if (longestEdge.a < 0 || longestEdge.b < 0 ||
                longestEdge.a >= _pbMesh.positions.Count || longestEdge.b >= _pbMesh.positions.Count)
                continue;

            Vector3 pointA = _pbMesh.transform.TransformPoint(_pbMesh.positions[longestEdge.a]);
            Vector3 pointB = _pbMesh.transform.TransformPoint(_pbMesh.positions[longestEdge.b]);
            Vector3 longestEdgeDir = (pointB - pointA).normalized;

            Quaternion faceRotation = Quaternion.LookRotation(
                Vector3.Normalize(faceNormal),
                Vector3.Normalize(longestEdgeDir)
            );

            // Update handle position/rotation if handle already exists
            if (_faceHandles.ContainsKey(faceId))
            {
                GameObject handle = _faceHandles[faceId];
                if (handle != null)
                {
                    handle.transform.position = faceCenter;
                    handle.transform.rotation = faceRotation;
                }
            }
            //If handle does not exist, make a new handle
            else
            {
                GameObject handle = Instantiate(handlePrefab, faceCenter, faceRotation);
                handle.name = $"FaceHandle_{faceId}";
                handle.transform.SetParent(_pbMesh.transform, true);
                _faceHandles[faceId] = handle;
            }

            _lastFaceCenters[faceId] = faceCenter;
            _lastFaceRotations[faceId] = faceRotation;
        }
    }

    /// <summary>
    /// Calculate the center of the face
    /// </summary>
    Vector3 GetFaceCenter(Face face)
    {
        Vector3 sum = Vector3.zero;
        foreach (int index in face.indexes)
        {
            sum += _pbMesh.transform.TransformPoint(_pbMesh.positions[index]);
        }
        return sum / face.indexes.Count;
    }

    /// <summary>
    /// Find the longest edge of a face, used for determining the rotation of the handles
    /// </summary>
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

    /// <summary>
    /// Remove all handle signifiers when they need to be rebuild
    /// </summary>
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

    /// <summary>
    /// Clear and readd the signifiers. Fx when making an undo/redo
    /// </summary>
    public void RebuildHandles()
    {
        if (_pbMesh == null) _pbMesh = GetComponent<ProBuilderMesh>();
        if (_pbMesh != null)
        {
            ClearAll();
            UpdateHandles();
        }
    }
}
