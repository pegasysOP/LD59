using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractions : MonoBehaviour
{
    private Mouse mouse = Mouse.current;

    [SerializeField]
    private float interactDistance = 3f;

    private InputAction interactAction;

    private bool lookingAtInteractable;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        interactAction = InputSystem.actions.FindAction("Interact");
    }

    // Update is called once per frame
    void Update()
    {
        //Lock interaction if the game is locked (e.g. during cutscenes)
        if (GameManager.Instance != null && GameManager.Instance.LOCKED)
        {
            SetLookingAtInteractable(false);
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        IInteractable interactable = null;
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
            interactable = hit.collider.GetComponent<IInteractable>();

        bool canInteract = interactable != null && interactable.IsInteractable();
        SetLookingAtInteractable(canInteract);

        if (canInteract && interactAction != null && interactAction.WasPressedThisFrame())
            interactable.Interact();
    }

    private void SetLookingAtInteractable(bool value)
    {
        if (value == lookingAtInteractable)
            return;
        lookingAtInteractable = value;

        HudController hud = GameManager.Instance != null ? GameManager.Instance.hudController : null;
        if (hud != null)
            hud.ShowInteractIcon(value);
    }
}
