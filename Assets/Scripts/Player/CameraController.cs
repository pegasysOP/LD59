using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Camera playerCamera;
    public Camera viewModelCamera;
    public float yaw;
    public float pitch;
    public float mouseSensitivity;
    public float maxLookAngle;

    private Vector3 cameraBaseLocalPos;
    private bool cameraBaseCached;
    private bool shaking;

    [Header("Minigame aim")]
    public Transform lookAtTarget;
    public float lookAtFollowSpeed = 12f;
    [Tooltip("World-space vertical offset added to lookAtTarget.position when aiming. Lets the camera target the head of a floor-anchored object.")]
    public float lookAtYOffset = 0f;

    private float sensitivitySetting = 1f;
    private InputAction lookAction;

    private void Awake()
    {
        sensitivitySetting = SettingsUtils.GetSensitivity();
    }

    private void Start()
    {
        lookAction = InputSystem.actions.FindAction("Look");
    }

    private void Update()
    {
        if (GameManager.Instance.LOCKED)
        {
            if (GameManager.Instance.MinigameActive && lookAtTarget != null)
            {
                AimAt(lookAtTarget.position + Vector3.up * lookAtYOffset);
            }
            else if (GameManager.Instance.MinigameActive)
            {
                pitch = 0f;
                playerCamera.transform.localEulerAngles = Vector3.zero;
                viewModelCamera.transform.localEulerAngles = Vector3.zero;
            }
            return;
        }

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

    public IEnumerator Shake(float duration, float magnitude)
    {
        if (playerCamera == null || duration <= 0f) yield break;

        if (!cameraBaseCached)
        {
            cameraBaseLocalPos = playerCamera.transform.localPosition;
            cameraBaseCached = true;
        }
        Vector3 basePos = cameraBaseLocalPos;

        shaking = true;
        float time = 0f;
        while (time < duration)
        {
            float decay = 1f - (time / duration);
            playerCamera.transform.localPosition = basePos + Random.insideUnitSphere * magnitude * decay;
            time += Time.deltaTime;
            yield return null;
        }
        playerCamera.transform.localPosition = basePos;
        shaking = false;
    }

    private void AimAt(Vector3 worldPoint)
    {
        Vector3 toTarget = worldPoint - playerCamera.transform.position;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        float desiredYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        float flatDist = new Vector2(toTarget.x, toTarget.z).magnitude;
        float desiredPitch = -Mathf.Atan2(toTarget.y, flatDist) * Mathf.Rad2Deg;
        desiredPitch = Mathf.Clamp(desiredPitch, -maxLookAngle, maxLookAngle);

        float t = 1f - Mathf.Exp(-lookAtFollowSpeed * Time.deltaTime);
        yaw = Mathf.LerpAngle(yaw, desiredYaw, t);
        pitch = Mathf.Lerp(pitch, desiredPitch, t);

        transform.localEulerAngles = new Vector3(0, yaw, 0);
        playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
    }
}
