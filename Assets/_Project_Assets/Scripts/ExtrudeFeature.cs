using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class ExtrudeFeature : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    
    private Face _selectedFace;
    private bool _isDragging;
    private Vector3 _initialControllerPos;
    private Vector3 _initialFaceCenter;
    private Vector3 _currControllerPos;

    private WireframeWithVertecies _wireframeScript;
    
    public GameObject rightController; // Assign the Meta right-hand controller in the Inspector for Extrution
    public GameObject handlePrefab;
    
    public float minExtrudeDistance;

    void Start()
    {
        _objSelector = FindObjectOfType<ObjSelector>(); // Find the ObjSelector instance
        HandleOnFace();
    }

    void Update()
    {
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
            _wireframeScript = _pbMesh.GetComponent<WireframeWithVertecies>();
        }

        if (_isDragging)
        {
            DragFace();
        }
    }
    
    public void StartDraggingFace()
    {
        _selectedFace = GetClosestFace();
        
        if (_selectedFace == null) return;
        
        _initialControllerPos = rightController.transform.position;
        _initialFaceCenter = GetFaceCenter(_selectedFace);
        _isDragging = true;
    }

    public void StopDraggingFace()
    {
        _isDragging = false;
        _selectedFace = null;
        
        HandleOnFace();
        _wireframeScript.UpdateVertexMarker();
    }

    void DragFace()
    {
        if (_selectedFace == null || _pbMesh == null) return;
        
        _currControllerPos = rightController.transform.position;
        
        // Calculate the movement delta (how much the controller moved)
        Vector3 movementDelta = _currControllerPos - _initialControllerPos;

        
        // Get the normal of the face
        //Vector3 faceNormal = Math.Normal(_pbMesh, _selectedFace).normalized;

        Vector3 localNormal = Math.Normal(_pbMesh, _selectedFace);
        Vector3 faceNormal = _pbMesh.transform.TransformDirection(localNormal);
        
        // Project the movement delta onto the face normal
        float movementAlongNormal = Vector3.Dot(movementDelta, faceNormal);
        Vector3 constrainedMovement = faceNormal * movementAlongNormal;

        // Compute new face center position
        Vector3 newFaceCenter = _initialFaceCenter + constrainedMovement;

        // Calculate displacement and apply to vertices
        Vector3 displacement = newFaceCenter - GetFaceCenter(_selectedFace);
        _pbMesh.TranslateVerticesInWorldSpace(_selectedFace.distinctIndexes.ToArray(), displacement);

        _pbMesh.ToMesh();
        _pbMesh.Refresh();
    }
    
    public void RightIndexTriggerDown()
    {
        Face closestFace = GetClosestFace();
        if (closestFace != null)
        {
            ExtrudeFace(closestFace);
            StartDraggingFace();
        }
    }
    
    Face GetClosestFace()
    {
        if (_pbMesh == null || rightController == null) return null;

        //Vector3 controllerPos = rightController.transform.position;
        _currControllerPos = rightController.transform.position;
        
        // float minDistance = float.MaxValue;
        float minDistance = minExtrudeDistance;
        Face closestFace = null;

        foreach (Face face in _pbMesh.faces)
        {
            Vector3 faceCenter = GetFaceCenter(face);
            float distance = Vector3.Distance(_currControllerPos, faceCenter);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestFace = face;
            }
        }
        return closestFace;
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

    void ExtrudeFace(Face face)
    {
        //Vector3 localNormal = Math.Normal(_pbMesh, face);
        //Vector3 worldNormal = _pbMesh.transform.TransformDirection(localNormal);
        List<Face> newFaces = new List<Face> { face };
        _pbMesh.Extrude(newFaces, ExtrudeMethod.IndividualFaces, .05f);
        _pbMesh.ToMesh();
        _pbMesh.Refresh();
    }

    void HandleOnFace()
    {
        GameObject[] handles = GameObject.FindGameObjectsWithTag("FaceHandle");

        foreach (GameObject handle in handles)
        {
            //Debug.Log("Destroying handle: " + handle.name);
            Destroy(handle);
        }

        foreach (Face face in _pbMesh.faces)
        {
            Vector3 faceCenter = GetFaceCenter(face);

            //Vector3 faceNormal = Math.Normal(_pbMesh, face);
            Vector3 faceNormal = _pbMesh.transform.TransformDirection(Math.Normal(_pbMesh, face)); //Convert to worldspace
            Quaternion rotation = Quaternion.LookRotation(faceNormal); // define normal rotation based on worldspace

            GameObject handle = Instantiate(handlePrefab, faceCenter, rotation);
            handle.transform.SetParent(_pbMesh.transform, true); // Attach handle to the cube
        }
    }
}
