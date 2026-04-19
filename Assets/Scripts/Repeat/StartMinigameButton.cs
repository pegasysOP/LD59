using UnityEngine;

public class StartMinigameButton : MonoBehaviour, IInteractable
{
    [SerializeField]
    private RepeatMinigame minigame;

    private bool hasStarted = false;
    public void Interact()
    {
        minigame.StartMinigame();
        hasStarted = true;
    }

    public bool IsInteractable()
    {
        return !hasStarted;
    }
}
