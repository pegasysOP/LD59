using System.Collections;
using UnityEngine;

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

            StartCoroutine(RotateBattery(battery.transform, Quaternion.Euler(0, 0, 0), 0.1f));
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
        battery = null;
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
