using System;
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

    [Header("Motion readout (for audio)")]
    [Tooltip("How quickly the smoothed value-change rate chases the instantaneous rate. Higher = snappier " +
             "reaction to start/stop of motion (used to drive the radar-minigame movement SFX loop).")]
    [SerializeField] private float valueRateSmoothing = 25f;

    public float Value { get; private set; }

    /// <summary>Smoothed absolute change in <see cref="Value"/> per second (normalized 0-1 units/sec).
    /// Audio systems (see <c>RadarAlignmentSounds</c>) read this to fade a movement loop in/out
    /// and to boost its volume during fast scrubbing.</summary>
    public float ValueChangeRate { get; private set; }

    /// <summary>True while the handle is held by the player.</summary>
    public bool IsGrabbed => grabbed;

    /// <summary>True once the slider has been locked (minigame complete).</summary>
    public bool IsLocked => locked;

    /// <summary>Fired when the player grabs the handle. Argument is this slider.</summary>
    public event Action<RadarSlider> OnGrabbed;
    /// <summary>Fired when the player releases the handle (mouse up) OR when the slider is locked
    /// while still being grabbed. Argument is this slider.</summary>
    public event Action<RadarSlider> OnReleased;

    /// <summary>World position of the handle — useful for spawning 3D one-shots on grab/release.</summary>
    public Vector3 HandleWorldPosition => handle != null ? handle.position : transform.position;

    private bool grabbed;
    private bool locked;
    private Vector3 handleRestLocal;
    private InputAction interactAction;
    private Camera cam;
    private float lastValue;

    void Start()
    {
        Value = startValue;
        lastValue = Value;
        handleRestLocal = handle.localPosition;
        interactAction = InputSystem.actions.FindAction("Interact");
        cam = Camera.main;
        ApplyHandle();
    }

    public void Interact()
    {
        if (locked)
            return;

        if (!grabbed)
        {
            grabbed = true;
            OnGrabbed?.Invoke(this);
        }
    }

    public bool IsInteractable()
    {
        return !locked;
    }

    public void Lock()
    {
        locked = true;
        if (grabbed)
        {
            grabbed = false;
            OnReleased?.Invoke(this);
        }
    }

    void Update()
    {
        UpdateMotionRate();

        if (!grabbed)
            return;

        if (interactAction.WasReleasedThisFrame())
        {
            grabbed = false;
            OnReleased?.Invoke(this);
            return;
        }

        float delta = ReadInputDelta();
        float next = Mathf.Clamp01(Value + delta * mouseSensitivity);
        if (next == Value)
            return;

        Value = next;
        ApplyHandle();
    }

    // Smooths the instantaneous |dValue|/dt toward a displayable rate so the audio layer
    // can fade a "moving" loop in/out without chattering on single-frame deltas.
    private void UpdateMotionRate()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float instantaneous = Mathf.Abs(Value - lastValue) / dt;
        float lerpK = 1f - Mathf.Exp(-Mathf.Max(0.01f, valueRateSmoothing) * dt);
        ValueChangeRate = Mathf.Lerp(ValueChangeRate, instantaneous, lerpK);
        lastValue = Value;
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
