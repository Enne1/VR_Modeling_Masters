using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class LoopCuts : MonoBehaviour
{
    // 1) Cache the internal GetEdgeRing method (non-public, static)
    static readonly MethodInfo s_GetEdgeRing = typeof(ElementSelection)
        .GetMethod("GetEdgeRing",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new Type[] { typeof(ProBuilderMesh), typeof(IEnumerable<Edge>) },
            null);

    /// <summary>
    /// Returns the “edge ring” for your seed edge (the same ring you’d get in the editor via Alt+R). 
    /// </summary>
    public IEnumerable<Edge> GetRing(ProBuilderMesh mesh, Edge seed)
    {
        if (mesh == null) throw new ArgumentNullException(nameof(mesh));
        if (s_GetEdgeRing == null)
            throw new InvalidOperationException("Could not locate ElementSelection.GetEdgeRing");

        // 2) Invoke it — note it returns an IEnumerable<Edge>
        var args = new object[] { mesh, new Edge[] { seed } };
        var result = s_GetEdgeRing.Invoke(null, args);

        return result as IEnumerable<Edge>;
    }
}
