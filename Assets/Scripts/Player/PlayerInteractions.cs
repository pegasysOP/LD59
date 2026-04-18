using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractions : MonoBehaviour
{
    //TODO: Replace with Actions
    private Keyboard keyboard = Keyboard.current;
    private Mouse mouse = Mouse.current;

    [SerializeField]
    private float interactDistance = 3f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (keyboard.eKey.wasPressedThisFrame)
        {
             Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
             if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
             {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null && interactable.IsInteractable())
                    interactable.Interact();
            }
        }
    }
}
