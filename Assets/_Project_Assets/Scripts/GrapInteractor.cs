using UnityEngine;
using UnityEngine.ProBuilder;
using System.Linq;

public class GrapInteractor : MonoBehaviour
{
    //private variables
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    private float _controllerObjDistance;
    private float _initialDistance;
    private bool _isGrabbing;
    private bool _isScaling;
    private Vector3 _relativeDistance;
    private Vector3 _initialScale;
    private Quaternion _relativeRotation;
    
    // Scripts
    private WireframeWithVertices _wireframeScript;
    

    // Public variables
    public GameObject rightController;
    public GameObject leftController;
    public float maxAllowedGrabDistance;
    
    // Find the selected object
    void Start()
    {
        _objSelector = FindFirstObjectByType<ObjSelector>(); 
    }

    // Continuously update the grabbing, or scaling
    void Update()
    {
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
            _wireframeScript = _pbMesh.GetComponent<WireframeWithVertices>();
        }
        
        // If only right hand trigger is pressed, perform grabbing
        if (_isGrabbing && !_isScaling) FollowController();

        // If both right and left trigger is pressed, perform scaling
        if (_isGrabbing && _isScaling)
        {
            float currentDistance = Vector3.Distance(leftController.transform.position, rightController.transform.position);
            float scaleMultiplier = currentDistance / _initialDistance;

            ScaleProBuilderMesh(_initialScale * scaleMultiplier);
        }
    }

    
    // Calculate the distance and rotation difference between the right controller and shape, before starting to grab
    public void AttachToController()
    {
        if (_pbMesh == null) return;
        
        _wireframeScript.updateWireframe = true;
        
        _controllerObjDistance = Vector3.Distance(_pbMesh.transform.position, rightController.transform.position);
        if (_controllerObjDistance <= maxAllowedGrabDistance)
        {
            _relativeDistance = rightController.transform.InverseTransformPoint(_pbMesh.transform.position);
            _relativeRotation = Quaternion.Inverse(rightController.transform.rotation) * _pbMesh.transform.rotation;
            _isGrabbing = true;
        }
    }
    
    // Reset variables when releasing the object
    public void DetachFromController()
    {
        _wireframeScript.updateWireframe = false;
        if (_pbMesh == null) return;
        _isGrabbing = false;
    }

    // Stick the object to the right controller, when holding down right hand trigger
    void FollowController()
    {
        _pbMesh.transform.position = rightController.transform.TransformPoint(_relativeDistance);
        _pbMesh.transform.rotation = rightController.transform.rotation * _relativeRotation;
    }

    // Scale the object around its center when both hand triggers are held down
    // Scaling is dependent on the distance between the controllers
    void ScaleProBuilderMesh(Vector3 targetScale)
    {
        Vector3[] vertices = _pbMesh.positions.ToArray();
        
        // Calculate the geometric center of the ProBuilder mesh
        Vector3 center = Vector3.zero;
        foreach (var vertex in vertices)
        {
            center += vertex;
        }
        center /= vertices.Length; // Average position of all vertices

        Vector3 scaleRatio = new Vector3(
            targetScale.x / transform.localScale.x,
            targetScale.y / transform.localScale.y,
            targetScale.z / transform.localScale.z
        );
        
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = center + Vector3.Scale(vertices[i] - center, scaleRatio);
        }

        transform.localScale = targetScale;  // Update GameObject scale
        _pbMesh.positions = vertices;         // Apply ProBuilder scaling
        _pbMesh.ToMesh();
        _pbMesh.Refresh();
    }
    
    // Set variables to begin scaling
    public void StartScaling()
    {
        _isScaling = true;
        _initialDistance = Vector3.Distance(leftController.transform.position, rightController.transform.position);
        _initialScale = transform.localScale;
    }

    // Reset variables when stopping to scale
    public void StopScaling()
    {
        _isScaling = false;
    }
}