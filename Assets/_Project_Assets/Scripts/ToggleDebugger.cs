using UnityEngine;

public class ToggleDebugger : MonoBehaviour
{
    public GameObject canvas;

    public void CanvasToggle()
    {
        canvas.SetActive(!canvas.activeSelf);
    }
}
