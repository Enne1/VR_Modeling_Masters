using UnityEngine;
using UnityEngine.ProBuilder;
using System.Collections.Generic;

public static class PBUtils
{
    /// <summary>
    /// Normalize size of mesh for scaling when attached to the copy/save machine
    /// </summary>
    public static void NormalizeMeshSize(ProBuilderMesh pbMesh, float maxSize)
    {
        if (pbMesh == null) return;

        // Ensure mesh data is current
        pbMesh.ToMesh();
        pbMesh.Refresh();

        // Get current bounds in world space
        Bounds worldBounds = GetWorldBounds(pbMesh);

        float maxDimension = Mathf.Max(worldBounds.size.x, worldBounds.size.y, worldBounds.size.z);
        if (maxDimension <= 0.0001f) return;

        // Compute scale factor
        float scale = maxSize / maxDimension;

        // Make a mutable copy of the positions
        List<Vector3> newPositions = new List<Vector3>(pbMesh.positions);
        for (int i = 0; i < newPositions.Count; i++)
        {
            newPositions[i] *= scale;
        }
        
        pbMesh.positions = newPositions;
        pbMesh.ToMesh();
        pbMesh.Refresh();
    }
    
    private static Bounds GetWorldBounds(ProBuilderMesh pbMesh)
    {
        // Use the MeshFilter's mesh bounds
        MeshFilter meshFilter = pbMesh.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return new Bounds(pbMesh.transform.position, Vector3.zero);
        }

        Bounds localBounds = meshFilter.sharedMesh.bounds;
        Vector3 center = pbMesh.transform.TransformPoint(localBounds.center);
        Vector3 extents = Vector3.Scale(localBounds.extents, pbMesh.transform.lossyScale);

        return new Bounds(center, extents * 2f);
    }
}