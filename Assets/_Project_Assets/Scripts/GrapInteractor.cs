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
           // _wireframeScript = _pbMesh.GetComponent<WireframeWithVertices>();
        }
        
        if (_isGrabbing && !_isScaling) FollowController();

        if (_isGrabbing && _isScaling)
        {
            float currentDistance = Vector3.Distance(leftController.transform.position, rightController.transform.position);
            float scaleMultiplier = currentDistance / _initialDistance;
            ScaleProBuilderMesh(_initialScale * scaleMultiplier);
        }
    }
    
    /// <summary>
    /// Attach the selected object to the controller when starting grab
    /// </summary>
    public void AttachToController()
    {
        if (_pbMesh == null) return;
        
        if (IsControllerNearObject(rightController.transform.position))
        {
            _relativeDistance = rightController.transform.InverseTransformPoint(_pbMesh.transform.position);
            _relativeRotation = Quaternion.Inverse(rightController.transform.rotation) * _pbMesh.transform.rotation;
            _isGrabbing = true;
        }
    }
    
    /// <summary>
    /// Release the object from the controller
    /// </summary>
    public void DetachFromController()
    {
        //_wireframeScript.updateWireframe = false;
        if (_pbMesh == null) return;
        _isGrabbing = false;
    }

    /// <summary>
    /// Make the mesh follow the controller when the controller is moved
    /// If the meshs rotation is close to a 90° interval on any axis, snap to that axis
    /// </summary>
    void FollowController()
    {
        _pbMesh.transform.position = rightController.transform.TransformPoint(_relativeDistance);

        // compute the desired rotation from the controller
        Quaternion desired = rightController.transform.rotation * _relativeRotation;

        // Snap the mesh to an axis (x, y, z), if it is within 5° of a 90° rotation
        Quaternion snapped = SnapTo90(desired, 5f);
        _pbMesh.transform.rotation = snapped;

        
       // snaps each Euler component if it’s within 'threshold' degrees of 0°, 90°, 180°, 270°, etc.
        Quaternion SnapTo90(Quaternion q, float threshold)
        {
            Vector3 e = q.eulerAngles;
            e.x = SnapAxis(e.x, threshold);
            e.y = SnapAxis(e.y, threshold);
            e.z = SnapAxis(e.z, threshold);
            return Quaternion.Euler(e);
        }

        // Find out if the mesh needs to snap to an axit or not 
        float SnapAxis(float angle, float threshold)
        {
            // find the nearest multiple of 90°
            float nearest = Mathf.Round(angle / 90f) * 90f;
            // Mathf.DeltaAngle handles wrapping correctly (e.g. comparing 359° to 0°)
            if (Mathf.Abs(Mathf.DeltaAngle(angle, nearest)) <= threshold)
                return nearest;
            return angle;
        }
        
    }

    /// <summary>
    /// If both hand trigger buttons are pressed down, switch from grabbing to scaling
    /// Scale mesh based on how far apart the user moved the controllers from eachother
    /// </summary>
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
    
    /// <summary>
    /// Prepare the mesh for scaling
    /// </summary>
    public void StartScaling()
    {
        _isScaling = true;
        _initialDistance = Vector3.Distance(leftController.transform.position, rightController.transform.position);
        _initialScale = transform.localScale;
    }

    /// <summary>
    /// stop the mesh from scaling when one of the hand trigger buttons are released
    /// </summary>
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
                return true;
            }
        }
        return false;
    }
}