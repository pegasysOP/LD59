using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BatterySlot : MonoBehaviour, IInteractable
{
    [SerializeField]
    private Battery.BatteryColour slotColour;

    [SerializeField]
    private Transform slotPosition;

    private Battery battery;

    private float intensityIncreaseOnFail = 0.25f;

    [SerializeField]
    private float ejectForce = 2.5f;

    // Minimum gap between plays of the same sound type on this slot.
    // Absorbs rapid-fire duplicate events (re-entering triggers, click + trigger races, etc.)
    // without relying on complex coroutine / state guards.
    private const float SoundCooldown = 1f;
    private readonly Dictionary<SfxBank, float> _lastPlayedAt = new Dictionary<SfxBank, float>();

    public void Interact()
    {
        //TODO: Maybe play a sound when trying to place nothing in the slot?
        //Set the light to green when the correct battery is placed in the slot?
        if (battery == null)
            return;

        battery.isHeld = false;

        //TODO: Set the batterys position to be in the middle of the slot
        battery.transform.parent = null;
        battery.transform.position = slotPosition.position;

        StartCoroutine(RotateBattery(battery.transform, Quaternion.Euler(0, 0, 0), 0.1f));
    }

    public bool IsInteractable()
    {
        //TODO: Maybe it should only be interactable if the player is holding a battery?
        return battery != null;
    }

    private IEnumerator RotateBattery(Transform target, Quaternion targetRotation, float duration)
    {
        Quaternion startRotation = target.rotation;
        float time = 0f;

        while (time < duration)
        {
            float t = time / duration;
            target.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            time += Time.deltaTime;
            yield return null;
        }

        target.rotation = targetRotation;

        yield return PostRotationCheck();
    }

    private IEnumerator PostRotationCheck()
    {
        yield return new WaitForSeconds(0.05f);

        if (battery == null)
            yield break;

        BatterySounds sounds = AudioManager.Instance != null ? AudioManager.Instance.batterySounds : null;
        Vector3 slotPos = slotPosition != null ? slotPosition.position : transform.position;

        if (!battery.isInCorrectSlot)
        {
            PlayWithCooldown(sounds?.rejectFeedback, slotPos);
            PlayWithCooldown(sounds?.rejectStaticZap, slotPos);
            IntensityManager.Instance.AddIntensity(intensityIncreaseOnFail);
            EjectBattery();
        }
        else
        {
            PlayWithCooldown(sounds?.acceptFeedback, slotPos);
            GameManager.Instance.CollectBattery(slotPos);
            StartCoroutine(AnimatePlacedBattery(battery));
            battery = null;
        }
    }

    private void EjectBattery()
    {
        Rigidbody rb = battery.GetComponent<Rigidbody>();

        battery.isHeld = false;
        battery.transform.parent = null;

        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 ejectDir = (battery.transform.position - transform.position).normalized + Vector3.up * 0.5f;

        rb.AddForce(ejectDir * ejectForce, ForceMode.Impulse);

        // Play the drop sound so the ejection feels physical.
        // Polish note: ideally the drop sound would play when the battery actually
        // hits the ground (via a collision callback on Battery), not the moment it
        // leaves the slot. Good enough for now.
        AudioManager.Instance?.batterySounds?.drop.PlayAt(battery.transform.position);

        battery = null;
    }

    private IEnumerator AnimatePlacedBattery(Battery placedBattery)
    {
        Transform batteryPosition = placedBattery.transform;

        Rigidbody rb = placedBattery.GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Vector3 startPos = batteryPosition.position;

        float bobHeight = 0.05f;     
        float bobSpeed = 2f;        
        float rotationSpeed = 60f; 

        while (true)
        {
            
            batteryPosition.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);

            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            batteryPosition.position = startPos + new Vector3(0f, bobOffset, 0f);

            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Battery otherBattery = other.GetComponent<Battery>();
        if (otherBattery == null)
            return;

        battery = otherBattery;

        Debug.Log("Battery placed in a slot!");

        Vector3 clunkPos = slotPosition != null ? slotPosition.position : transform.position;
        PlayWithCooldown(AudioManager.Instance?.batterySounds?.placeClunk, clunkPos);

        Interact();

        if (battery != null && battery.colour == slotColour)
        {
            Debug.Log("Battery placed in correct slot!");
            battery.isInCorrectSlot = true;
        }
    }

    private void PlayWithCooldown(SfxBank bank, Vector3 worldPosition)
    {
        if (bank == null)
            return;

        float now = Time.time;
        if (_lastPlayedAt.TryGetValue(bank, out float last) && now - last < SoundCooldown)
            return;

        _lastPlayedAt[bank] = now;
        bank.PlayAt(worldPosition);
    }
}
