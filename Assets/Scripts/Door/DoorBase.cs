using System.Collections;
using UnityEngine;

public class DoorBase : MonoBehaviour, IInteractable
{
    [SerializeField]
    protected GameObject doorPanel;

    [Header("Door SFX")]
    [Tooltip("Played instantly when the player interacts with the door (button-thunk). " +
             "Phase 1 of the power-down arc, but owned by the door itself so it fires even " +
             "if no PowerDownSequence is wired up.")]
    [SerializeField]
    protected SfxBank buttonPress = new SfxBank { pitchMin = 0.97f, pitchMax = 1.03f };

    [Tooltip("Single baked door-opening clip (motor + slide + thunk all in one, ~2 seconds). " +
             "Fires once after doorOpenSoundDelay seconds. Phase 2 of the power-down arc, " +
             "owned by the door so each door can have its own clip.")]
    [SerializeField]
    protected SfxBank doorOpen = new SfxBank { pitchMin = 1f, pitchMax = 1f };

    [Tooltip("Seconds between the interact and the door-opening SFX firing. Lets the button-press " +
             "breathe before the motor/slide/thunk kicks in.")]
    [SerializeField, Min(0f)]
    protected float doorOpenSoundDelay = 0.5f;

    [Tooltip("Rejection beep played when the player interacts with the door before it's ready " +
             "(i.e. while it's still showing the red 'not interactable' state). Distinct from " +
             "buttonPress so the locked state reads unambiguously.")]
    [SerializeField]
    protected SfxBank rejectBeep = new SfxBank { pitchMin = 1f, pitchMax = 1f };

    protected bool isClosed = true;
    protected float initialTimer = 0f;

    [SerializeField]
    protected MeshRenderer meshRenderer;

    [SerializeField]
    protected Material greenMaterial;
    [SerializeField]
    protected Material redMaterial;

    protected const float OpenOffset = -1.45f;
    protected const float OpenDuration = 2f;

    public virtual void Interact()
    {
        throw new System.NotImplementedException();
    }

    public virtual bool IsInteractable()
    {
        if ((isClosed && initialTimer <= 0) == true)
        {
            meshRenderer.material = redMaterial;
        }
        return isClosed && initialTimer <= 0;
    }

    protected virtual void OnDoorOpened()
    {
        StateTracker.Instance?.NotifyStartingDoorOpened();
    }

    protected IEnumerator MoveDoor(float deltaZ, float duration)
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

        OnDoorOpened();
    }

    protected IEnumerator PlayDoorOpenAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        PlayAtDoor(doorOpen);
    }

    // Door sounds are diegetic world events; route them through the positional path
    // so they pan/attenuate relative to the player and pick up the room's reverb zone.
    protected void PlayAtDoor(SfxBank bank)
    {
        if (bank == null || !bank.HasAnyClip) return;
        bank.PlayAt(transform.position);
    }
}
