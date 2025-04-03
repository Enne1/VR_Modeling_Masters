using System;
using System.Collections.Generic;
using UnityEngine;

public class MultiSelectionToggler : MonoBehaviour
{
    public bool isToggledOn = false;
    
    public GameObject selectionToggleLocked;
    public GameObject selctionToggleUnlocked;

    public void SwitchTogglePadlock()
    {
        isToggledOn = !isToggledOn;
        selctionToggleUnlocked.SetActive(!isToggledOn);
        selectionToggleLocked.SetActive(isToggledOn);
    }

    public void SwitchToggleFacelock()
    {
        isToggledOn = !isToggledOn;
        selctionToggleUnlocked.SetActive(!isToggledOn);
        selectionToggleLocked.SetActive(isToggledOn);
    }
}
