using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BatterySlot : MonoBehaviour, IInteractable
{
    [SerializeField]
    private Battery.BatteryColour slotColour;

    [SerializeField]
    private Transform slotPosition;

    private Battery battery;

    public void Interact()
    {
        //TODO: Maybe play a sound when trying to place nothing in the slot?
        //Set the light to green when the correct battery is placed in the slot?
        if(battery != null)
        {
            battery.isHeld = false;

            //TODO: Set the batterys position to be in the middle of the slot
            battery.transform.parent = null;
            Vector3 positon = slotPosition.position;
            battery.transform.position = positon;

            // capture and pass the specific battery so concurrent trigger events won't clobber the reference
            StartCoroutine(RotateBattery(battery, battery.transform, Quaternion.Euler(0, 0, 0), 0.1f));
        }
    }

    public bool IsInteractable()
    {
        //TODO: Maybe it should only be interactable if the player is holding a battery?
        return battery != null;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator RotateBattery(Battery rotatingBattery, Transform target, Quaternion targetRotation, float duration)
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

        //Battery is not null here

        yield return StartCoroutine(PostRotationCheck());
    }

    private IEnumerator PostRotationCheck()
    {
        Debug.Log("Post Rotation check started. Battery = " + battery);
        //yield return new WaitForSeconds(0.05f);

        //Battery is null here
        if (battery == null)
            yield break;

        Debug.Log("Battery is not null");
        if (!battery.isInCorrectSlot)
        {
            Debug.Log("Need to eject");
            EjectBattery();
        }
        else
        {
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

        rb.AddForce(ejectDir * 5f, ForceMode.Impulse);
    }

    private void OnTriggerEnter(Collider other)
    {
        battery = other.GetComponent<Battery>();
        if (battery != null)
        {
            Debug.Log("Battery placed in a slot!");
            Interact();

            //TODO: If picking up the battery again then set battery to null

            if(battery.colour == slotColour)
            {
                //TODO: Set the batterys position to be in the middle of the slot and make it so it can't be picked up again.
                Debug.Log("Battery placed in correct slot!");
                battery.isInCorrectSlot = true;
            }

        }
    }
}
