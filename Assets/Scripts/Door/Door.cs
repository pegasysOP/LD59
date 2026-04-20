using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Door : DoorBase
{

    [SerializeField]
    private GameObject doorPanel;

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

    private const float OpenOffset = -1.45f;

    private const float OpenDuration = 2f;

    private float initialDelay = 10f;

    
    public override void Interact()
    {
        Debug.Log("Interacting with door");
        if (isClosed && doorPanel != null)
        {
            PlayAtDoor(buttonPress);
            CutsceneManager.Instance.PlayCutscene(CutsceneManager.CutsceneType.Powerdown);
            StartCoroutine(MoveDoor(OpenOffset, OpenDuration));
            StartCoroutine(PlayDoorOpenAfterDelay(doorOpenSoundDelay));
            if (triggerPowerDownOnInteract)
                StartCoroutine(TriggerPowerDownAfterDelay(powerDownStartDelay));
        }
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

        StateTracker.Instance?.NotifyStartingDoorOpened();
    }

    private void Update()
    {
        initialTimer -= Time.deltaTime;

        if (initialTimer <= 0 && isClosed)
        {
            meshRenderer.material = greenMaterial;
        }
    }
}
