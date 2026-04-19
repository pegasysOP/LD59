using System.Collections;
using UnityEngine;

public class StartMinigameButton : BaseButton, IInteractable
{
    [SerializeField]
    private RepeatMinigame minigame;

    private bool hasStarted = false;

    [SerializeField]
    private float flashTime = 0.2f;

    public void Interact()
    {
        hasStarted = true;
        Flash(flashTime);
        minigame.StartMinigame();
    }

    public bool IsInteractable()
    {
        return !hasStarted;
    }
}
