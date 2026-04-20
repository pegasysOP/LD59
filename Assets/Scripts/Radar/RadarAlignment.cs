using System.Collections;
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

    /// <summary>
    /// Normalized alignment error: 0 = player line perfectly on target, 1 = maximum possible
    /// distance from target within the allowed ranges. Uses the worse of the position and angle
    /// axes so the audio layer reacts to whichever axis is still off. Updated every frame while
    /// the minigame is active; frozen at 0 once <see cref="IsComplete"/> is true.
    /// </summary>
    public float AlignmentError01 { get; private set; } = 1f;

    /// <summary>True once both sliders have been aligned within tolerance and the minigame is done.</summary>
    public bool IsComplete => complete;

    /// <summary>Position slider reference — exposed for sibling systems (e.g. audio) that need to
    /// subscribe to grab/release events without being re-wired through the inspector.</summary>
    public RadarSlider PositionSlider => positionSlider;
    /// <summary>Angle slider reference — see <see cref="PositionSlider"/>.</summary>
    public RadarSlider AngleSlider => angleSlider;

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

        AlignmentError01 = ComputeAlignmentError(playerPos, playerAng);

        if (Mathf.Abs(playerPos - targetPosition) < positionTolerance
            && Mathf.Abs(playerAng - targetAngle) < angleTolerance)
        {
            Complete();
        }
    }

    // Normalises the current player-vs-target offset into a single 0-1 error value where 1 is the
    // worst achievable distance along whichever axis the player is currently farthest off. Using
    // MAX (rather than an average) means nailing one axis while the other is way off still reads as
    // "bad" to the audio layer, which matches how completion works: both axes must be on target.
    private float ComputeAlignmentError(float playerPos, float playerAng)
    {
        float posHalfRange = Mathf.Max(
            Mathf.Abs(targetPosition - positionMin),
            Mathf.Abs(positionMax - targetPosition));
        float angHalfRange = Mathf.Max(
            Mathf.Abs(targetAngle - angleMin),
            Mathf.Abs(angleMax - targetAngle));

        float posErr = posHalfRange > 0.0001f ? Mathf.Abs(playerPos - targetPosition) / posHalfRange : 0f;
        float angErr = angHalfRange > 0.0001f ? Mathf.Abs(playerAng - targetAngle) / angHalfRange : 0f;
        return Mathf.Clamp01(Mathf.Max(posErr, angErr));
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
        AlignmentError01 = 0f;
        positionSlider.Lock();
        angleSlider.Lock();
        lightRenderer.material = lightOnMaterial;
        
        StateTracker.Instance?.CompleteTask(TaskType.RadarAlignment);

        StartCoroutine(DelayedIntensityIncrease());
    }

    private IEnumerator DelayedIntensityIncrease()
    {
        yield return new WaitForSeconds(1f);
        IntensityManager.Instance.SetIntensity(1.0f);
    }
}
