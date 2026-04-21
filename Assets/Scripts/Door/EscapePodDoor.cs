using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class EscapePodDoor : DoorBase
{

    private Keyboard keyboard = Keyboard.current;

    [SerializeField]
    private GameObject doorPanel;

    [Header("Door SFX")]
    [Tooltip("Played instantly when the player interacts with the door (button-thunk). " +
             "Matches the main Door's button-press layer so the two doors feel like siblings.")]
    [SerializeField]
    private SfxBank buttonPress = new SfxBank { pitchMin = 0.97f, pitchMax = 1.03f };

    [Tooltip("Single baked door-opening clip (motor + slide + thunk all in one, ~2 seconds). " +
             "Fires once after doorOpenSoundDelay seconds.")]
    [SerializeField]
    private SfxBank doorOpen = new SfxBank { pitchMin = 1f, pitchMax = 1f };

    [Tooltip("Seconds between the interact and the door-opening SFX firing. Lets the button-press " +
             "breathe before the motor/slide/thunk kicks in.")]
    [SerializeField, Min(0f)]
    private float doorOpenSoundDelay = 0.5f;

    [Tooltip("Rejection beep played when the player tries to open the escape pod before every task " +
             "is complete. Different from buttonPress so the locked state reads unambiguously.")]
    [SerializeField]
    private SfxBank rejectBeep = new SfxBank { pitchMin = 1f, pitchMax = 1f };

    [Header("Music")]
    [Tooltip("When the escape pod door successfully opens, fade all game music out over this many " +
             "seconds and latch the suspension flag so nothing restarts music until the scene ends. " +
             "Gives the end-of-run beat a clean audio bed for door SFX / cutscene.")]
    [SerializeField, Min(0f)]
    private float musicFadeOutDuration = 2.5f;

    private const float OpenOffset = -1.45f;

    // Matches Door.cs so the escape pod opens at the same pace as the starting room door.
    private const float OpenDuration = 2f;

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

    private IEnumerator PlayDoorOpenAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        PlayAtDoor(doorOpen);
    }

    // Door sounds are diegetic world events; route them through the positional path
    // so they pan/attenuate relative to the player and pick up the room's reverb zone.
    private void PlayAtDoor(SfxBank bank)
    {
        if (bank == null || !bank.HasAnyClip) return;
        bank.PlayAt(transform.position);
    }

    private IEnumerator MoveDoor(float deltaZ, float duration)
    {
        isClosed = false;

        Transform t = doorPanel.transform;
        Vector3 start = t.localPosition;
        Vector3 target = start + new Vector3(0f, 0f, deltaZ);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float frac = Mathf.Clamp01(elapsed / duration);
            t.localPosition = Vector3.Lerp(start, target, frac);
            yield return null;
        }

        t.localPosition = target;

        meshRenderer.material = redMaterial;

        StateTracker.Instance?.NotifyStartingDoorOpened();
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
        //FIXME: We may want to replace this but for now just open escape hatch door after all tasks complete
        //if (StateTracker.Instance.AllTasksComplete)
        //{
        //    StartCoroutine(MoveDoor(OpenOffset, OpenDuration));
        //}

        if (StateTracker.Instance.AllTasksComplete && isClosed)
        {
            meshRenderer.material = greenMaterial;
        }
    }

}
