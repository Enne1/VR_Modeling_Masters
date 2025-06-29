using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Linq;

public class ExtrudeFeature : MonoBehaviour
{
    // Private variables needed for extrusion
    private ObjSelector _objSelector;
    private ProBuilderMesh _pbMesh;
    private Face _selectedFace;
    private List<Face> _dragAlongFaces = new List<Face>(); 
    private bool _isDragging;
    private Vector3 _initialControllerPos;
    private Vector3 _initialFaceCenter;
    private Vector3 _currControllerPos;
    private float _totalDraggedDistance;
    
    // Multi-extrusion tracking: union of vertex indices from all extruded faces
    private HashSet<int> _selectedVertexIndices = new HashSet<int>();
    private Dictionary<int, Vector3> _initialVertexWorldPositions = new Dictionary<int, Vector3>();
    
    // Public variables
    public GameObject rightController;
    public GameObject leftController;
    public float minExtrudeDistance;
    public float angledSnapTolerance;
    
    //Ruler settings
    public GameObject tickPrefabSmall;
    public GameObject tickPrefabLarge;
    public float tickSpacing = 0.01f;
    private List<GameObject> _rulerTicks = new List<GameObject>();
    private Face[] _extrudedFaces;
    private HashSet<Face> _originalFaces;
    private HashSet<Edge> _sideWallEdges;
    
    
    void Start()
    {
        _objSelector = FindFirstObjectByType<ObjSelector>();
    }
    
    void Update()
    {
        // Continuously update the mesh reference
        if (_objSelector != null && _objSelector.ClosestObj != null)
        {
            _pbMesh = _objSelector.ClosestObj.GetComponent<ProBuilderMesh>();
        }
        
        // During an extrusion operation, update the dragged shape.
        if (_isDragging)
        {
            DragFace();
        }
    }
    
    /// <summary>
    /// Initiate dragging and record initial positions for all vertices in extruded faces.
    /// </summary>
    public void StartDraggingFace()
    {
        if (_selectedFace == null) return;
        
        _initialControllerPos = rightController.transform.position;
        //_initialFaceCenter = GetFaceCenter(_selectedFace);
        _isDragging = true;
        
        // Clear any previous vertex selections.
        _selectedVertexIndices.Clear();
        _initialVertexWorldPositions.Clear();
        
        // Build a union of vertex indices from the primary face
        foreach (int index in _selectedFace.distinctIndexes)
        {
            _selectedVertexIndices.Add(index);
        }
        // and from all additional extruded faces.
        foreach (Face face in _dragAlongFaces)
        {
            foreach (int index in face.distinctIndexes)
            {
                _selectedVertexIndices.Add(index);
            }
        }
        
        // Also include any vertices in the entire mesh that share the same world position.
        List<int> selectedIndicesCopy = new List<int>(_selectedVertexIndices);
        foreach (int selectedIndex in selectedIndicesCopy)
        {
            Vector3 selectedWorldPos = _pbMesh.transform.TransformPoint(_pbMesh.positions[selectedIndex]);
            for (int i = 0; i < _pbMesh.positions.Count; i++)
            {
                if (!_selectedVertexIndices.Contains(i))
                {
                    Vector3 worldPos = _pbMesh.transform.TransformPoint(_pbMesh.positions[i]);
                    // Using a small tolerance for floating point imprecision.
                    if (Vector3.Distance(worldPos, selectedWorldPos) < 0.0001f)
                    {
                        _selectedVertexIndices.Add(i);
                    }
                }
            }
        }
        
        // Record the initial world position for each vertex in our union.
        foreach (int index in _selectedVertexIndices)
        {
            Vector3 worldPos = _pbMesh.transform.TransformPoint(_pbMesh.positions[index]);
            _initialVertexWorldPositions[index] = worldPos;
        }
        
        Vector3 faceNormal = _pbMesh.transform.TransformDirection(Math.Normal(_pbMesh, _selectedFace));
    }
    
    /// <summary>
    /// Reset variables when stopping the extrusion.
    /// </summary>
    public void StopDraggingFace()
    {
        // Abort extrusion if it is too small
        if (_totalDraggedDistance < 0.01f)
        {
            _pbMesh.GetComponent<UndoTracker>()?.Undo(true);
        }
        
        // Reset parameters
        foreach (var t in _rulerTicks)
            Destroy(t);
        _rulerTicks.Clear();
        
        _isDragging = false;
        _selectedFace = null;
        _dragAlongFaces.Clear();
        _selectedVertexIndices.Clear();
        _initialVertexWorldPositions.Clear();
    }
    
    /// <summary>
    /// Update the extruded geometry based on controller movement.
    /// </summary>
    void DragFace()
    {
        if (_selectedFace == null || _pbMesh == null) return;
        
        _currControllerPos = rightController.transform.position;
        Vector3 movementDelta = _currControllerPos - _initialControllerPos;
        
        // Use the primary face normal as the extrusion direction.
        Vector3 localNormal = Math.Normal(_pbMesh, _selectedFace);
        Vector3 faceNormal = _pbMesh.transform.TransformDirection(localNormal);
        
        // Project the controller movement onto the face normal.
        float movementAlongNormal = Vector3.Dot(movementDelta, faceNormal);
        Vector3 constrainedMovement = faceNormal * movementAlongNormal;
        
        // For extrusion, use the constrained movement.
        Vector3 finalMovement = constrainedMovement;
        
        // Find face close to left controller for possible snapping based on that face' plane
        Face leftControllerFace = GetClosestFace(leftController.transform);
        if (leftControllerFace != null)
        {
            // Get left controller face' center point and normal vector
            Vector3 leftControllerFaceCenterpoint = GetFaceCenter(leftControllerFace);
            Vector3 leftcontrollerLocalNormal = Math.Normal(_pbMesh, leftControllerFace);

            // Create plane based on the center position and the facs' normal vector
            Vector3 snappingPlaneNormal = _pbMesh.transform.TransformDirection(leftcontrollerLocalNormal).normalized;
            Plane snappingPlane = new Plane(snappingPlaneNormal, leftControllerFaceCenterpoint);

            // Get world-space normal of the extruded face
            Vector3 rightFaceNormal = _pbMesh.transform.TransformDirection(Math.Normal(_pbMesh, _selectedFace)).normalized;

            float angleBetweenNormals = Vector3.Angle(rightFaceNormal, snappingPlaneNormal);

            // If the angle between the two faces is below threshold, snap
            if (angleBetweenNormals < angledSnapTolerance && leftControllerFace != _selectedFace)
            {
                Vector3 movedFaceCenter = _initialFaceCenter + finalMovement;
                float distanceToPlane = snappingPlane.GetDistanceToPoint(movedFaceCenter);

                if (Mathf.Abs(distanceToPlane) < 0.01f) 
                {
                    // Compute constrained correction along the extrusion normal
                    Vector3 snapCorrection = -snappingPlane.normal * distanceToPlane;
                    Vector3 constrainedCorrection = Vector3.Project(snapCorrection, rightFaceNormal);

                    // Apply snapped coordinate
                    finalMovement = movedFaceCenter + constrainedCorrection - _initialFaceCenter;
                }
            }
        }
        
        // Make a mutable copy of the mesh positions.
        List<Vector3> newPositions = new List<Vector3>(_pbMesh.positions);
        foreach (int index in _selectedVertexIndices)
        {
            Vector3 newWorldPos = _initialVertexWorldPositions[index] + finalMovement;
            newPositions[index] = _pbMesh.transform.InverseTransformPoint(newWorldPos);
        }
        _pbMesh.positions = newPositions;
        
        _pbMesh.ToMesh();
        _pbMesh.Refresh();

        _totalDraggedDistance = finalMovement.magnitude;
        
        // Make Tick Marks for ruler
        UpdateRulerTicks();
    }
    
    /// <summary>
    /// Called when the right index trigger is pressed down, to begin extrusion.
    /// </summary>
    public void CallExtrusion()
    {
        List<Face> facesToExtrude = new List<Face>();
        
        // start by finding the face closest to the controller.
        Face controllerFace = GetClosestFace();
        _initialFaceCenter = GetFaceCenter(controllerFace);
        if (controllerFace != null)
        {
            facesToExtrude.Add(controllerFace);
        }
        
        // Find all multiple selected faces from the MultiSelectedList, if any.
        MultiSelectedList facesSelectedList = _pbMesh.transform.GetComponent<MultiSelectedList>();
        if (facesSelectedList != null && facesSelectedList.selectedFaces != null && facesSelectedList.selectedFaces.Count > 0)
        {
            foreach (var faceObj in facesSelectedList.selectedFaces)
            {
                Face extrudeFace = GetClosestFace(faceObj.transform.parent);
                if (extrudeFace != null && !facesToExtrude.Contains(extrudeFace))
                {
                    facesToExtrude.Add(extrudeFace);
                }
            }
        }
        
        if (facesToExtrude.Count > 0)
        {
            //Store current mesh state in undo Stack
            _pbMesh.GetComponent<UndoTracker>()?.SaveState();
            
            // Store Edges before Extrusion (Used for finding edges for tick marks)
            _originalFaces = new HashSet<Face>();
            foreach (var face in _pbMesh.faces)
                _originalFaces.Add(face);
            
            // Extrude all the selected faces simultaneously by a small initial amount.
            _extrudedFaces = _pbMesh.Extrude(facesToExtrude, ExtrudeMethod.FaceNormal, .001f);
            _pbMesh.ToMesh();
            _pbMesh.Refresh();
            
            // Store edges of the newly created sidewalls of the extrusion
            _sideWallEdges = CalculateSidewallEdges();
            
            // Set the primary face (first in the list) and record the others.
            _selectedFace = facesToExtrude[0];
            _dragAlongFaces.Clear();
            for (int i = 1; i < facesToExtrude.Count; i++)
            {
                _dragAlongFaces.Add(facesToExtrude[i]);
            }
            
            // Record the vertices for all extruded faces and begin dragging.
            StartDraggingFace();
        }
    }
    
    /// <summary>
    /// Finds the closest face to the right controller.
    /// </summary>
    Face GetClosestFace()
    {
        if (_pbMesh == null || rightController == null) return null;
        
        _currControllerPos = rightController.transform.position;
        float minDistance = minExtrudeDistance;
        Face closestFace = null;
        
        // Loop over all faces to find the one closest to the controller
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
    
    /// <summary>
    /// Overload: Finds the closest face to a given reference transform.
    /// </summary>
    Face GetClosestFace(Transform referenceTransform)
    {
        if (_pbMesh == null || referenceTransform == null) return null;
        
        Vector3 referencePos = referenceTransform.position;
        float minDistance = minExtrudeDistance;
        Face closestFace = null;
        
        foreach (Face face in _pbMesh.faces)
        {
            Vector3 faceCenter = GetFaceCenter(face);
            float distance = Vector3.Distance(referencePos, faceCenter);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestFace = face;
            }
        }
        return closestFace;
    }
    
    /// <summary>
    /// Computes the center point of a face.
    /// </summary>
    Vector3 GetFaceCenter(Face face)
    {
        Vector3 sum = Vector3.zero;
        int count = face.indexes.Count;
        foreach (int index in face.indexes)
        {
            sum += _pbMesh.transform.TransformPoint(_pbMesh.positions[index]);
        }
        return sum / count;
    }
    
    /// <summary>
    /// Create small tick marks on all sidewall edges of extrudes faces
    /// tick-marks are added 0.01 units (1 cm) apart from each other
    /// </summary>
    void UpdateRulerTicks()
    {
        // Clear out the old ticks
        foreach (var t in _rulerTicks)
            Destroy(t);
        _rulerTicks.Clear();

        if (_extrudedFaces == null || _originalFaces == null || tickPrefabSmall == null || tickPrefabLarge == null)
            return;

        foreach (var edge in _sideWallEdges)
        {
            Vector3 pA = _pbMesh.transform.TransformPoint(_pbMesh.positions[edge.a]);
            Vector3 pB = _pbMesh.transform.TransformPoint(_pbMesh.positions[edge.b]);
            float length = Vector3.Distance(pA, pB);
            int count = Mathf.FloorToInt(length / tickSpacing);
            Vector3 edgeDir = (pB - pA).normalized;

            // Define an arbitrary forward direction that's not colinear with edgeDir
            Vector3 forward = Vector3.Cross(edgeDir, Vector3.forward);
            if (forward.sqrMagnitude < 0.001f) // edgeDir is parallel to Vector3.forward
            {
                forward = Vector3.Cross(edgeDir, Vector3.up);
            }

            // Create a rotation where the Y-axis points along edgeDir
            Quaternion tickRotation = Quaternion.LookRotation(forward, edgeDir);

            for (int i = 0; i <= count; i++)
            {
                float distanceAlongEdge = tickSpacing * i;
                float t = distanceAlongEdge / length;
                Vector3 pos = Vector3.Lerp(pA, pB, t);

                bool isMajorTick = i % 5 == 0;

                GameObject tick = Instantiate(
                    isMajorTick ? tickPrefabLarge : tickPrefabSmall,
                    pos,
                    tickRotation,
                    transform
                );

                _rulerTicks.Add(tick);
            }
        }

    }


    /// <summary>
    /// Make a list of only the 4 edges connecting sidewalls of a new extrusion 
    /// </summary>
    HashSet<Edge> CalculateSidewallEdges()
    {
        
        // gather all edges from the faces in the ProBuilder mesh
        var newFaces = new HashSet<Face>();
        foreach (var face in _extrudedFaces)
            newFaces.Add(face);

        // remove any edge that already existed in the mesh before extrusion
        newFaces.ExceptWith(_originalFaces);

        // Collect vertex indices from provided faces
        HashSet<int> faceVertexIndices = new HashSet<int>();
        foreach (var face in newFaces)
        {
            foreach (var idx in face.distinctIndexes)
                faceVertexIndices.Add(idx);
        }

        // Group vertices by position (within a small tolerance)
        Dictionary<Vector3, List<int>> positionToVertices = new Dictionary<Vector3, List<int>>();
        float threshold = 0.0001f;

        foreach (int idx in faceVertexIndices)
        {
            Vector3 pos = _pbMesh.positions[idx];
            bool found = false;

            // Find existing key within threshold
            foreach (var key in positionToVertices.Keys)
            {
                if (Vector3.Distance(key, pos) <= threshold)
                {
                    positionToVertices[key].Add(idx);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                positionToVertices[pos] = new List<int> { idx };
            }
        }

        // Create vertex-to-sharedVertex mapping
        Dictionary<int, int> vertexToShared = new Dictionary<int, int>();
        int sharedIdx = 0;
        foreach (var group in positionToVertices.Values)
        {
            foreach (int vertIdx in group)
            {
                vertexToShared[vertIdx] = sharedIdx;
            }
            sharedIdx++;
        }

        // Count occurrences of edges between faces using shared vertices
        Dictionary<(int, int), int> sharedEdgeCount = new Dictionary<(int, int), int>();

        foreach (Face face in newFaces)
        {
            foreach (Edge edge in face.edges)
            {
                int sharedA = vertexToShared[edge.a];
                int sharedB = vertexToShared[edge.b];

                var orderedEdge = sharedA < sharedB ? (sharedA, sharedB) : (sharedB, sharedA);

                if (sharedEdgeCount.ContainsKey(orderedEdge))
                    sharedEdgeCount[orderedEdge]++;
                else
                    sharedEdgeCount[orderedEdge] = 1;
            }
        }

        // Find edges appearing exactly twice (connecting two faces)
        HashSet<Edge> connectingEdges = new HashSet<Edge>();
        foreach (var kvp in sharedEdgeCount)
        {
            if (kvp.Value == 2)
            {
                // Get actual vertex indices for representation
                var verticesA = positionToVertices.Values.ElementAt(kvp.Key.Item1);
                var verticesB = positionToVertices.Values.ElementAt(kvp.Key.Item2);

                Edge actualEdge = new Edge(verticesA[0], verticesB[0]);
                connectingEdges.Add(actualEdge);
            }
        }
        return connectingEdges;
    }
}