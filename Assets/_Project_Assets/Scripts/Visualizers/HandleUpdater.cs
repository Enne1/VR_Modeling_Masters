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

    public GameObject handlePrefab;

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
            Quaternion faceRotation = Quaternion.LookRotation(faceNormal);

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
}
