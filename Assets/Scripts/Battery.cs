using UnityEngine;
using UnityEngine.InputSystem;

public class Battery : MonoBehaviour, IInteractable
{
    [SerializeField]
    private PlayerController playerController;
    [SerializeField]
    private Rigidbody rb;

    private Mouse mouse = Mouse.current;

    [SerializeField]
    private float holdDistance = 2f;

    public void Interact()
    {
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
        //TODO: If the player is close enough to the battery, return true. Otherwise, return false.
        return true;
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
