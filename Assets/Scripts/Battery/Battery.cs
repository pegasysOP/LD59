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
    private float holdDistance = 2f;

    private bool isHeld = false;

    public bool isInCorrectSlot = false;

    public enum BatteryColour
    {
        Yellow,
        Green,
        Blue
    }

    [SerializeField]
    public BatteryColour colour;

    public void Interact()
    {
        if (isHeld)
        {
            this.transform.parent = null;
            rb.useGravity = true;
            rb.isKinematic = false;
            isHeld = false;
            return;
        }

        isHeld = true;

        Debug.Log("Interacted with battery.");
        if (playerController == null)
        {
            Debug.LogError("PlayerController reference is not set on Battery.");
        }

        //Disable physics 
        rb.useGravity = false;
        rb.isKinematic = true;

        this.transform.parent = playerController.transform;
    }

    public bool IsInteractable()
    {
        //TODO: If the battery is in the correct slot then disable interaction.
        return !isInCorrectSlot;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.parent == playerController.transform)
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
