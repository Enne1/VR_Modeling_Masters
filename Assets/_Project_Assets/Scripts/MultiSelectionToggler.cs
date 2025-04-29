using UnityEngine;

public class MultiSelectionToggler : MonoBehaviour
{
    public bool isToggledOn;
    public GameObject selectionToggleLocked;
    public GameObject selctionToggleUnlocked;

    /// <summary>
    /// Switch the signifier of padlocks
    /// swithes between locked and unlocked object
    /// </summary>
    public void SwitchTogglePadlock()
    {
        isToggledOn = !isToggledOn;
        selctionToggleUnlocked.SetActive(!isToggledOn);
        selectionToggleLocked.SetActive(isToggledOn);
    }

    /// <summary>
    /// Switch the signifier of chians
    /// swithes between unbroken and broken object
    /// </summary>
    public void SwitchToggleFacelock()
    {
        isToggledOn = !isToggledOn;
        selctionToggleUnlocked.SetActive(!isToggledOn);
        selectionToggleLocked.SetActive(isToggledOn);
    }
}