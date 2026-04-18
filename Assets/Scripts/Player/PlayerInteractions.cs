using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractions : MonoBehaviour
{
    //TODO: Implement raycasting and calling objects interact methods, such as picking up batteries, interacting with doors, etc.
    public Keyboard keyboard = Keyboard.current;
    public Mouse mouse = Mouse.current;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (keyboard.eKey.wasPressedThisFrame)
        {
            //TODO: Shoot ray and check if it hit anything with an IInteractable
            //If so call it's interact method 
             Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
             if (Physics.Raycast(ray, out RaycastHit hit, 3f))
             {
                Debug.Log("Shot ray and hit: " + hit.collider.name);
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                 if (interactable != null && interactable.IsInteractable())
                 {
                     interactable.Interact();
                 }
            }
        }
    }
}
