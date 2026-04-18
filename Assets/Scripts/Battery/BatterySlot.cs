using UnityEngine;

public class BatterySlot : MonoBehaviour, IInteractable
{
    [SerializeField]
    private Battery.BatteryColour slotColour;

    public void Interact()
    { 
        //TODO: Maybe play a sound when trying to place nothing in the slot?
        //Set the light to green when the correct battery is placed in the slot?
        return;
    }

    public bool IsInteractable()
    {
        return true;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        Battery battery = other.GetComponent<Battery>();
        if (battery != null)
        {
            Debug.Log("Battery placed in a slot!");
            Interact();

            if(battery.colour == slotColour)
            {
                //TODO: Set the batterys position to be in the middle of the slot and make it so it can't be picked up again.
                Debug.Log("Battery placed in correct slot!");
                battery.isInCorrectSlot = true;

                
            }

        }
    }
}
