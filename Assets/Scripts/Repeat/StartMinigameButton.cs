using System.Collections;
using UnityEngine;

public class StartMinigameButton : BaseButton, IInteractable
{
    [SerializeField]
    private RepeatMinigame minigame;

    private bool hasStarted = false;

    [SerializeField]
    private float flashTime = 0.2f;

    [SerializeField]
    private float startDelay = 1.75f;

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

        StartCoroutine(StartMinigameAfterDelay());
    }

    private IEnumerator StartMinigameAfterDelay()
    {
        minigame?.SoundConfig?.PlayStartMinigameAt(transform.position);
        Flash(flashTime);

        yield return new WaitForSeconds(startDelay);

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
