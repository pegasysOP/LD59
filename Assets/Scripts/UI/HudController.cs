using TMPro;
using UnityEngine;

public class HudController : MonoBehaviour
{
    public PauseMenu pauseMenu;

    public GameObject centreDot;
    public GameObject interactPrompt;

    public void ShowInteractPrompt(bool show)
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(show);
    }
}
