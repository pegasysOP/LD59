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
