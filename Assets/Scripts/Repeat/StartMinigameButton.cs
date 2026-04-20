using System.Collections;
using UnityEngine;

public class StartMinigameButton : BaseButton, IInteractable
{
    [SerializeField]
    private RepeatMinigame minigame;

    private bool hasStarted = false;

    [SerializeField]
    private float flashTime = 0.2f;

    private Coroutine idleFlashRoutine;

    public void Start()
    {
        if (IsInteractable())
            idleFlashRoutine = StartCoroutine(IdleFlashLoop());
    }

    public void Interact()
    {
        hasStarted = true;

        if (idleFlashRoutine != null)
            StopCoroutine(idleFlashRoutine);

        minigame?.SoundConfig?.PlayStartMinigameAt(transform.position);
        Flash(flashTime);
        minigame.StartMinigame();
    }

    public bool IsInteractable()
    {
        return !hasStarted;
    }

    private IEnumerator IdleFlashLoop()
    {
        while (!hasStarted)
        {
            Flash(flashTime);
            yield return new WaitForSeconds(Random.Range(1.7f, 2.2f));
        }
    }
}
