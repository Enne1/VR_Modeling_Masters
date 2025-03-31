using System;
using System.Collections.Generic;
using UnityEngine;

public class PadlockToggler : MonoBehaviour
{
    public bool isToggledOn = false;
    
    public GameObject padlockToggleLocked;
    public GameObject padlockToggleUnlocked;

    public void SwitchToggle()
    {
        isToggledOn = !isToggledOn;
        padlockToggleUnlocked.SetActive(!isToggledOn);
        padlockToggleLocked.SetActive(isToggledOn);
    }
}
