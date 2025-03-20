using UnityEngine;
using UnityEngine.ProBuilder;
using System.Linq;

public class GrapInteractor : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    private float _initialDistance;
    private bool _isGrabbing;
    private bool _isScaling;
    private Vector3 _relativeDistance;
    private Vector3 _initialScale;
    private Quaternion _relativeRotation;

    private WireframeWithVertices _wireframeScript;

    public GameObject rightController;
    public GameObject leftController;
    public float maxAllowedGrabDistance;
    
    void Start()
    {
        _objSelector = FindFirstObjectByType<ObjSelector>(); 
    }

    void Update()
    {
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
            _wireframeScript = _pbMesh.GetComponent<WireframeWithVertices>();
        }
        
        if (_isGrabbing && !_isScaling) FollowController();

        if (_isGrabbing && _isScaling)
        {
            float currentDistance = Vector3.Distance(leftController.transform.position, rightController.transform.position);
            float scaleMultiplier = currentDistance / _initialDistance;
            ScaleProBuilderMesh(_initialScale * scaleMultiplier);
        }
    }

    // Use OverlapSphere instead of distance-to-center check
    public void AttachToController()
    {
        if (_pbMesh == null) return;
        
        _wireframeScript.updateWireframe = true;

        if (IsControllerNearObject(rightController.transform.position))
        {
            _relativeDistance = rightController.transform.InverseTransformPoint(_pbMesh.transform.position);
            _relativeRotation = Quaternion.Inverse(rightController.transform.rotation) * _pbMesh.transform.rotation;
            _isGrabbing = true;
        }
    }
    
    public void DetachFromController()
    {
        _wireframeScript.updateWireframe = false;
        if (_pbMesh == null) return;
        _isGrabbing = false;
    }

    void FollowController()
    {
        _pbMesh.transform.position = rightController.transform.TransformPoint(_relativeDistance);
        _pbMesh.transform.rotation = rightController.transform.rotation * _relativeRotation;
    }

    void ScaleProBuilderMesh(Vector3 targetScale)
    {
        Vector3[] vertices = _pbMesh.positions.ToArray();
        
        Vector3 center = Vector3.zero;
        foreach (var vertex in vertices)
        {
            center += vertex;
        }
        center /= vertices.Length; 

        Vector3 scaleRatio = new Vector3(
            targetScale.x / transform.localScale.x,
            targetScale.y / transform.localScale.y,
            targetScale.z / transform.localScale.z
        );
        
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = center + Vector3.Scale(vertices[i] - center, scaleRatio);
        }

        transform.localScale = targetScale;
        _pbMesh.positions = vertices;
        _pbMesh.ToMesh();
        _pbMesh.Refresh();
    }
    
    public void StartScaling()
    {
        _isScaling = true;
        _initialDistance = Vector3.Distance(leftController.transform.position, rightController.transform.position);
        _initialScale = transform.localScale;
    }

    public void StopScaling()
    {
        _isScaling = false;
    }

    /// <summary>
    /// Uses a spherical overlap check to see if the controller is close to any part of the object.
    /// </summary>
    private bool IsControllerNearObject(Vector3 controllerPosition)
    {
        Collider[] hitColliders = Physics.OverlapSphere(controllerPosition, maxAllowedGrabDistance);
        foreach (var collider in hitColliders)
        {
            if (collider.gameObject == _pbMesh.gameObject)
            {
                return true; // Controller is near the object
            }
        }
        return false;
    }
}

