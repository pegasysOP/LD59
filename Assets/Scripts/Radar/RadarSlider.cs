using UnityEngine;
using UnityEngine.InputSystem;

public enum RadarSliderInputAxis
{
    Auto,
    MouseX,
    MouseY,
}

public class RadarSlider : MonoBehaviour, IInteractable
{
    [Header("Handle")]
    [SerializeField] private Transform handle;
    [SerializeField] private Vector3 localAxis = Vector3.up;
    [SerializeField] private float travelDistance = 0.4f;

    [Header("Input")]
    [SerializeField] private RadarSliderInputAxis inputAxis = RadarSliderInputAxis.Auto;
    [SerializeField] private float mouseSensitivity = 0.005f;

    [Header("Initial")]
    [Range(0f, 1f)]
    [SerializeField] private float startValue = 0.5f;

    public float Value { get; private set; }

    private bool grabbed;
    private bool locked;
    private Vector3 handleRestLocal;
    private InputAction interactAction;
    private Camera cam;

    void Start()
    {
        Value = startValue;
        handleRestLocal = handle.localPosition;
        interactAction = InputSystem.actions.FindAction("Interact");
        cam = Camera.main;
        ApplyHandle();
    }

    public void Interact()
    {
        if (locked)
            return;

        grabbed = true;
    }

    public bool IsInteractable()
    {
        return !locked;
    }

    public void Lock()
    {
        locked = true;
        grabbed = false;
    }

    void Update()
    {
        if (!grabbed)
            return;

        if (interactAction.WasReleasedThisFrame())
        {
            grabbed = false;
            return;
        }

        float delta = ReadInputDelta();
        float next = Mathf.Clamp01(Value + delta * mouseSensitivity);
        if (next == Value)
            return;

        Value = next;
        ApplyHandle();
    }

    private void ApplyHandle()
    {
        Vector3 axis = localAxis.normalized;
        float offset = Mathf.Lerp(-travelDistance * 0.5f, travelDistance * 0.5f, Value);
        handle.localPosition = handleRestLocal + axis * offset;
    }

    private float ReadInputDelta()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        switch (inputAxis)
        {
            case RadarSliderInputAxis.MouseX:
                return mouseDelta.x;
            case RadarSliderInputAxis.MouseY:
                return mouseDelta.y;
            default:
                return Vector2.Dot(mouseDelta, ScreenAxisDirection());
        }
    }

    private Vector2 ScreenAxisDirection()
    {
        Vector3 worldAxis = transform.TransformDirection(localAxis.normalized);
        Vector3 worldFrom = handle.position;
        Vector3 worldTo = worldFrom + worldAxis;
        Vector2 screenFrom = cam.WorldToScreenPoint(worldFrom);
        Vector2 screenTo = cam.WorldToScreenPoint(worldTo);
        Vector2 dir = screenTo - screenFrom;
        float mag = dir.magnitude;
        return mag > 0.0001f ? dir / mag : Vector2.up;
    }
}
