using UnityEngine;
using UnityEngine.ProBuilder;
using System.Linq;
using System.Net;

public class GrapInteractorHand : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    private float _initialDistance;
    private bool _isGrabbing;
    private bool _isScaling;
    
    private bool _isGrabbingL;
    private bool _isGrabbingR;
    
    private Vector3 _relativeDistance;
    private Vector3 _initialScale;
    private Quaternion _relativeRotation;
    
    public GameObject rightController;
    public GameObject leftController;
    public float maxAllowedGrabDistance;

    private GameObject _currController;
    
    public GameObject[] handModelsL;
    public GameObject[] handModelsR;
    
    void Start()
    {
        _objSelector = FindFirstObjectByType<ObjSelector>(); 
    }

    public void getMeshFromSelector(GameObject obj)
    {
        _pbMesh = obj.GetComponent<ProBuilderMesh>();
    }
    
    void Update()
    {
        if (_objSelector == null || _objSelector.ClosestObj == null || _pbMesh == null)
        {
            return;
        }
        
        if (_isGrabbingL ^ _isGrabbingR) FollowController();

        if (_isGrabbingL && _isGrabbingR)
        {
            float currentDistance = Vector3.Distance(leftController.transform.position, rightController.transform.position);
            float scaleMultiplier = currentDistance / _initialDistance;
            ScaleProBuilderMesh(_initialScale * scaleMultiplier);
        }
    }
    
    /// <summary>
    /// Attach the selected object to the controller when starting grab
    /// </summary>
    void AttachToController()
    {
        if (_pbMesh == null) return;
        
        if (IsControllerNearObject(_currController.transform.position))
        {
            _relativeDistance = _currController.transform.InverseTransformPoint(_pbMesh.transform.position);
            _relativeRotation = Quaternion.Inverse(_currController.transform.rotation) * _pbMesh.transform.rotation;
        }
    }
    
    /// <summary>
    /// Release the object from the controller
    /// </summary>
    public void DetachFromController()
    {
        if (_pbMesh == null) return;
        _isGrabbingL = false;
        _isGrabbingR = false;
    }

    /// <summary>
    /// Make the mesh follow the controller when the controller is moved
    /// If the meshs rotation is close to a 90° interval on any axis, snap to that axis
    /// </summary>
    void FollowController()
    {
        _pbMesh.transform.position = _currController.transform.TransformPoint(_relativeDistance);
        _pbMesh.transform.rotation = _currController.transform.rotation * _relativeRotation;
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
    void StopScaling()
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

    
    public void LeftHandDown()
    {
        _isGrabbingL = true;

        if (_isGrabbingR) StartScaling();
        else
        {
            _currController = leftController;
            AttachToController();
        }
        
        handModelsL[0].SetActive(false);
        handModelsL[1].SetActive(true);
        handModelsL[2].SetActive(false);
    }

    public void LeftHandUp()
    {
        _isGrabbingL = false;
        
        DetachFromController();
        
        handModelsL[0].SetActive(true);
        handModelsL[1].SetActive(false);
        handModelsL[2].SetActive(false);
    }
    
    public void RightHandDown()
    {
        _isGrabbingR = true;
        
        if(_isGrabbingL) StartScaling();
        else
        {
            _currController = rightController;
            AttachToController();
        }
        
        handModelsR[0].SetActive(false);
        handModelsR[1].SetActive(true);
        handModelsR[2].SetActive(false);
        
        _currController = rightController;
    }
    
    public void RightHandUp()
    {
        _isGrabbingR = false;

        DetachFromController();
        
        handModelsR[0].SetActive(true);
        handModelsR[1].SetActive(false);
        handModelsR[2].SetActive(false);
    }
}