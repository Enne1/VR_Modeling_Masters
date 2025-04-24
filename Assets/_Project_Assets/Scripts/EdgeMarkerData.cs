using UnityEngine;
using UnityEngine.UI;

public class EdgeMarkerData : MonoBehaviour
{
    public (int, int) edge;

    public void setEdge(int a, int b)
    {
        edge = (a, b);
    }
}
