using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Camera playerCamera;
    public float yaw;
    public float pitch;
    public float mouseSensitivity;
    public float maxLookAngle;

    private float sensitivitySetting = 1f;
    private InputAction lookAction;

    private void Awake()
    {
        sensitivitySetting = SettingsUtils.GetSensitivity();
    }

    private void Start()
    {
        if (GameManager.Instance.cameraController)
            Debug.LogError("WARNING: Duplicate camera controller instances in scene");

        GameManager.Instance.cameraController = this;
        lookAction = InputSystem.actions.FindAction("Look");
    }

    private void Update()
    {
        if (GameManager.Instance.LOCKED)
            return;

        Vector2 lookValue = lookAction.ReadValue<Vector2>() * 0.02f;

        yaw += lookValue.x * mouseSensitivity * sensitivitySetting;
        pitch -= lookValue.y * mouseSensitivity * sensitivitySetting;

        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

        transform.localEulerAngles = new Vector3(0, yaw, 0);
        playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
    }

    public void UpdateSensitivity(float value)
    {
        sensitivitySetting = value;
    }
}
