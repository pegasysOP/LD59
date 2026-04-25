using System.Collections;
using UnityEngine;

public class Door : DoorBase
{
    [Header("Power-Down Sequence")]
    [Tooltip("If true, opening this door fires the PowerDownSequence (lights-out / creature reveal SFX) " +
             "after powerDownStartDelay seconds. Leave enabled for the starting-room door; disable for " +
             "any other interactive doors that shouldn't re-trigger the event.")]
    [SerializeField]
    private bool triggerPowerDownOnInteract = true;

    [Tooltip("Optional explicit reference to the PowerDownSequence component. If left null, the door " +
             "looks one up in the scene the first time it fires.")]
    [SerializeField]
    private PowerDownSequence powerDownSequence;

    [Tooltip("Seconds between the interact and the PowerDownSequence kicking off. Gives the door " +
             "a beat to start moving before the lights-out audio begins.")]
    [SerializeField, Min(0f)]
    private float powerDownStartDelay = 0.5f;

    [Header("Door SFX")]
    [Tooltip("Played instantly when the player interacts with the door (button-thunk). " +
             "Phase 1 of the power-down arc, but owned by the door itself so it fires even " +
             "if no PowerDownSequence is wired up.")]
    [SerializeField]
    private SfxBank buttonPress = new SfxBank { pitchMin = 0.97f, pitchMax = 1.03f };

    [Tooltip("Single baked door-opening clip (motor + slide + thunk all in one, ~2 seconds). " +
             "Fires once after doorOpenSoundDelay seconds. Phase 2 of the power-down arc, " +
             "owned by the door so each door can have its own clip.")]
    [SerializeField]
    private SfxBank doorOpen = new SfxBank { pitchMin = 1f, pitchMax = 1f };

    [Tooltip("Seconds between the interact and the door-opening SFX firing. Lets the button-press " +
             "breathe before the motor/slide/thunk kicks in.")]
    [SerializeField, Min(0f)]
    private float doorOpenSoundDelay = 0.5f;

    [Tooltip("Rejection beep played when the player interacts with the door before it's ready " +
             "(i.e. while it's still showing the red 'not interactable' state). Distinct from " +
             "buttonPress so the locked state reads unambiguously.")]
    [SerializeField]
    private SfxBank rejectBeep = new SfxBank { pitchMin = 1f, pitchMax = 1f };

    private const float OpenDuration = 2f;

    public override void Interact()
    {
        Debug.Log("Interacting with door");
        if (!isClosed || doorPanel == null)
            return;

        // IsInteractable() stays true while the door is closed so the click always lands here;
        // the "is the door armed yet?" gate is enforced inside Interact so we can play the reject
        // beep during the red/initial-delay window instead of silently eating the input.
        if (initialTimer > 0f)
        {
            PlayAtDoor(rejectBeep);
            return;
        }

        PlayAtDoor(buttonPress);
        CutsceneManager.Instance.PlayCutscene(CutsceneManager.CutsceneType.Powerdown);
        StartCoroutine(MoveDoor(OpenOffset, OpenDuration));
        StartCoroutine(PlayDoorOpenAfterDelay(doorOpenSoundDelay));
        if (triggerPowerDownOnInteract)
            StartCoroutine(TriggerPowerDownAfterDelay(powerDownStartDelay));
    }

    public override bool IsInteractable()
    {
        // Always interactable while closed so the reject beep can fire during the red window.
        // The actual "can it open?" gate is evaluated inside Interact().
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

    private IEnumerator TriggerPowerDownAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (powerDownSequence == null)
        {
#if UNITY_2023_1_OR_NEWER
            powerDownSequence = FindFirstObjectByType<PowerDownSequence>();
#else
            powerDownSequence = FindObjectOfType<PowerDownSequence>();
#endif
        }

        if (powerDownSequence != null)
            powerDownSequence.Run();
        else
            Debug.LogWarning($"{nameof(Door)} on '{name}' wanted to fire the PowerDownSequence but none " +
                             $"was found in the scene. Assign one in the inspector or add the component.");
    }

    protected override void OnDoorOpened()
    {
        base.OnDoorOpened(); 
    }

    private void Start()
    {
        meshRenderer.material = greenMaterial;
    }
}
