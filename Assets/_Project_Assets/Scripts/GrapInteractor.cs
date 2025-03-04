using UnityEngine;
using UnityEngine.ProBuilder;

public class GrapInteractor : MonoBehaviour
{
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    private float _controllerObjDistance;
    public float maxAllowedGrabDistance;
    private bool _isGrabbing;
    private Vector3 _relativeDistance;
    private Quaternion _relativeRotation;
    
    public GameObject rightController;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _objSelector = FindObjectOfType<ObjSelector>(); // Find the ObjSelector instance
    }

    // Update is called once per frame
    void Update()
    {
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        }
        
        if (_isGrabbing) FollowController();
    }

    public void AttachToController()
    {
        if (_pbMesh == null) return;

        _controllerObjDistance = Vector3.Distance(_pbMesh.transform.position, rightController.transform.position);
        if (_controllerObjDistance <= maxAllowedGrabDistance)
        {
            //_relativeDistance = rightController.transform.position - _pbMesh.transform.position;
            _relativeDistance = rightController.transform.InverseTransformPoint(_pbMesh.transform.position);
            _relativeRotation = Quaternion.Inverse(rightController.transform.rotation) * _pbMesh.transform.rotation;
            _isGrabbing = true;
        }
    }

    void FollowController()
    {
        //_pbMesh.transform.position = rightController.transform.position + _relativeDistance;
        _pbMesh.transform.position = rightController.transform.TransformPoint(_relativeDistance);
        _pbMesh.transform.rotation = rightController.transform.rotation * _relativeRotation;
    }
    
    public void DetachFromController()
    {
        if (_pbMesh == null) return;
        _isGrabbing = false;
    }
}