using UnityEngine;
using UnityEngine.UI;

public class SpawningSelector : MonoBehaviour
{
    public GameObject menuPanel;
    
    public Transform controllerTransform;
    
    private GameObject _selectedObj;

    public Button[] selectionButtons;
    public GameObject[] objects;
    
    void Update()
    {
        if (menuPanel.activeSelf)
        {
            menuPanel.transform.position = controllerTransform.position + new Vector3(0, 0.1f, 0.015f);
            menuPanel.transform.rotation = controllerTransform.rotation;
        }
    }
    
    public void ToggleMenu()
    {
        menuPanel.transform.position = controllerTransform.position;
        menuPanel.transform.rotation = controllerTransform.rotation;
        menuPanel.SetActive(!menuPanel.activeSelf);
    }

    void HighlightSelection(int i)
    {
        selectionButtons[i].transition = Selectable.Transition.None; // Disable automatic transition
        selectionButtons[i].GetComponent<Image>().color = selectionButtons[i].colors.highlightedColor; // Apply highlighted color
        
        _selectedObj = objects[i];
    }

    public void DisableSelection()
    {
        for (int i = 0; i < selectionButtons.Length; i++)
        {
            selectionButtons[i].transition = Selectable.Transition.None; // Disable automatic transition
            selectionButtons[i].GetComponent<Image>().color = selectionButtons[i].colors.normalColor; // Apply highlighted color
        }
        _selectedObj = null;
    }

    public void SpawnObject()
    {
        if (_selectedObj != null)
        {
            Instantiate(_selectedObj, controllerTransform.position, Quaternion.identity);
        }
    }

    public void CubeHighlight()
    {
        if (menuPanel.activeSelf)
        {
            HighlightSelection(0);
        }
    }

    public void SphereHighlight()
    {
        if (menuPanel.activeSelf)
        {
            HighlightSelection(1);
        }
    }

    public void CylinderHighlight()
    {
        if (menuPanel.activeSelf)
        {
            HighlightSelection(2);
        }
    }

    public void ConeHighlight()
    {
        if (menuPanel.activeSelf)
        {
            HighlightSelection(3);
        }
    }
    
    
}
