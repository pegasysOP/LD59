using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EscapePodDoor : DoorBase
{
    [Header("Music")]
    [Tooltip("When the escape pod door successfully opens, fade all game music out over this many " +
             "seconds and latch the suspension flag so nothing restarts music until the scene ends. " +
             "Gives the end-of-run beat a clean audio bed for door SFX / cutscene.")]
    [SerializeField, Min(0f)]
    private float musicFadeOutDuration = 2.5f;

    public override void Interact()
    {
        Debug.Log("Interacting with escape pod door");
        if (!isClosed || doorPanel == null)
            return;

        // IsInteractable() stays true while the door is closed so the player's click lands here even
        // when tasks are incomplete; the "locked" gate is enforced inside Interact so we can play a
        // distinct reject beep instead of silently eating the input.
        if (StateTracker.Instance != null && !StateTracker.Instance.AllTasksComplete)
        {
            PlayAtDoor(rejectBeep);
            return;
        }

        PlayAtDoor(buttonPress);
        StartCoroutine(MoveDoor(OpenOffset, OpenDuration));
        StartCoroutine(PlayDoorOpenAfterDelay(doorOpenSoundDelay));

        // Fade all game music out and latch suspension so nothing pops back in while the
        // escape sequence plays out. ResumeGameMusic is intentionally never called here —
        // the door opening is a one-way narrative beat.
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SuspendGameMusic(musicFadeOutDuration);
        }
    }

    public override bool IsInteractable()
    {
        // Always interactable while closed so the reject beep can fire on early clicks.
        // The actual "can the door open?" gate is evaluated inside Interact().
        return isClosed;
    }

    protected override void OnDoorOpened()
    {
        // Do nothing — overrides base behavior
    }

    public void OpenDoorEndCutscene()
    {
        OpenDoorEndCutscene(OpenDuration);
    }

    public void OpenDoorEndCutscene(float duration)
    {
        StartCoroutine(MoveDoor(-OpenOffset, duration));
    }

    private void Update()
    {

        if (StateTracker.Instance.AllTasksComplete && isClosed)
        {
            meshRenderer.material = greenMaterial;
        }
    }

}
