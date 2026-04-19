using UnityEngine;

public class RadarAlignment : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private RadarSlider positionSlider;
    [SerializeField] private RadarSlider angleSlider;

    [Header("Player line")]
    [SerializeField] private Transform playerLinePivot;
    [SerializeField] private float positionMin = -0.4f;
    [SerializeField] private float positionMax = 0.4f;
    [SerializeField] private float angleMin = -40f;
    [SerializeField] private float angleMax = 40f;

    [Header("Target line")]
    [SerializeField] private Transform targetLine;
    [SerializeField] private float targetPositionPadding = 0.05f;
    [SerializeField] private float targetAnglePadding = 5f;
    [SerializeField] private float targetPositionMinFromCenter = 0.1f;
    [SerializeField] private float targetAngleMinFromCenter = 10f;

    [Header("Completion")]
    [SerializeField] private float positionTolerance = 0.04f;
    [SerializeField] private float angleTolerance = 3f;
    [SerializeField] private Renderer lightRenderer;
    [SerializeField] private Material lightOffMaterial;
    [SerializeField] private Material lightOnMaterial;

    private float targetPosition;
    private float targetAngle;
    private bool complete;

    void Start()
    {
        targetPosition = RandomAwayFromCenter(positionMin, positionMax, targetPositionPadding, targetPositionMinFromCenter);
        targetAngle = RandomAwayFromCenter(angleMin, angleMax, targetAnglePadding, targetAngleMinFromCenter);

        ApplyLine(targetLine, targetPosition, targetAngle);
        lightRenderer.material = lightOffMaterial;
    }

    private float RandomAwayFromCenter(float min, float max, float padding, float minFromCenter)
    {
        float center = (min + max) * 0.5f;
        float lowHigh = center - minFromCenter;
        float highLow = center + minFromCenter;
        float lowMin = min + padding;
        float highMax = max - padding;

        bool lowValid = lowHigh > lowMin;
        bool highValid = highMax > highLow;

        if (lowValid && highValid)
            return Random.value < 0.5f ? Random.Range(lowMin, lowHigh) : Random.Range(highLow, highMax);
        if (lowValid)
            return Random.Range(lowMin, lowHigh);
        if (highValid)
            return Random.Range(highLow, highMax);
        return Random.Range(lowMin, highMax);
    }

    void Update()
    {
        if (complete)
            return;

        float playerPos = Mathf.Lerp(positionMin, positionMax, positionSlider.Value);
        float playerAng = Mathf.Lerp(angleMin, angleMax, angleSlider.Value);

        ApplyLine(playerLinePivot, playerPos, playerAng);

        if (Mathf.Abs(playerPos - targetPosition) < positionTolerance
            && Mathf.Abs(playerAng - targetAngle) < angleTolerance)
        {
            Complete();
        }
    }

    private void ApplyLine(Transform pivot, float xPosition, float zAngle)
    {
        Vector3 local = pivot.localPosition;
        local.x = xPosition;
        pivot.localPosition = local;
        pivot.localRotation = Quaternion.Euler(0f, 0f, zAngle);
    }

    private void Complete()
    {
        complete = true;
        positionSlider.Lock();
        angleSlider.Lock();
        lightRenderer.material = lightOnMaterial;
        StateTracker.Instance?.CompleteTask(TaskType.RadarAlignment);
    }
}
