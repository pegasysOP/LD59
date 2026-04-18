using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

public class Battery : MonoBehaviour, IInteractable
{
    [SerializeField]
    private PlayerController playerController;
    [SerializeField]
    private Rigidbody rb;

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
        Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        Vector3 targetPosition = ray.origin + ray.direction * holdDistance;

        transform.position = targetPosition;
    }
}
