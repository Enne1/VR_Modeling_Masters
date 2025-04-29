using UnityEngine;
using UnityEngine.UI;

public class EdgeMarkerData : MonoBehaviour
{
    public (int, int) edge;

    /// <summary>
    /// Store the two vertex values for which the edge signifier is attached to
    /// </summary>
    public void SetEdge(int a, int b)
    {
        edge = (a, b);
    }
}