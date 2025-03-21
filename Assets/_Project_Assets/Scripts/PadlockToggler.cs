using System;
using System.Collections.Generic;
using UnityEngine;

public class PadlockToggler : MonoBehaviour
{
    public bool isToggledOn = false;

    public void SwitchToggle()
    {
         isToggledOn = !isToggledOn;
    }
}
