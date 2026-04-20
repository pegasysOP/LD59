using TMPro;
using UnityEngine;

public class HudController : MonoBehaviour
{
    public PauseMenu pauseMenu;

    public GameObject centreDot;
    public GameObject interactIcon;
    public GameObject interactPrompt;

    public void ShowInteractPrompt(bool show)
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(show);
    }

    public void ShowInteractIcon(bool show)
    {
        if (centreDot != null)
            centreDot.SetActive(!show);
        if (interactIcon != null)
            interactIcon.SetActive(show);
    }
}
