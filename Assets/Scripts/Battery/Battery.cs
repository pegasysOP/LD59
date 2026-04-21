using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

public class Battery : MonoBehaviour, IInteractable
{
    [SerializeField]
    private PlayerController playerController;
    [SerializeField]
    private Rigidbody rb;

    public bool hasTriggeredIntensityIncrease = false;

    //TODO: Replace with Actions
    private Mouse mouse = Mouse.current;

    [SerializeField]
    private float holdDistance = 1f;

    public bool isHeld = false;

    public bool isInCorrectSlot = false;

    public enum BatteryColour
    {
        Yellow,
        Green,
        Blue
    }

    [SerializeField]
    public BatteryColour colour;

    public void ReleaseBattery()
    {
        this.transform.parent = null;
        rb.useGravity = true;
        rb.isKinematic = false;
        isHeld = false;

        // Polish note: ideally the drop sound would play when the battery actually
        // hits the ground (via a collision callback), not the instant the player
        // lets go. Good enough for now.
        AudioManager.Instance?.batterySounds?.drop.PlayAt(transform.position);
    }
    public void Interact()
    {
        if (isHeld)
        {
            ReleaseBattery();
            return;
        }

        isHeld = true;
        this.transform.parent = playerController.transform;

        Debug.Log("Interacted with battery.");
        if (playerController == null)
        {
            Debug.LogError("PlayerController reference is not set on Battery.");
        }

        //Disable physics 
        rb.useGravity = false;
        rb.isKinematic = true;

        AudioManager.Instance?.batterySounds?.pickup.PlayAt(transform.position);

        FollowMouse();
    }

    public bool IsInteractable()
    {
        return !isInCorrectSlot;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isHeld)
        {
            FollowMouse();
        }
    }

    void FollowMouse()
    {
        Transform cam = Camera.main.transform;

        Vector3 targetPosition = cam.position + cam.forward * holdDistance;

        transform.position = targetPosition;
    }
}
