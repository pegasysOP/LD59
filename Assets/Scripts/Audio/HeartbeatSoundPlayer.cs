using UnityEngine;

/// <summary>
/// Plays the heartbeat loop for the current <see cref="IntensityLevel"/>.
/// When intensity escalates to a new level, the volume envelope ramps to peak,
/// holds, then slowly decays back to the subtle resting multiplier. Decreases
/// in intensity change the interval immediately but do not retrigger the ramp.
/// Reads the current level directly from <see cref="IntensityManager.Instance"/>.
/// </summary>
[DisallowMultipleComponent]
public class HeartbeatSoundPlayer : MonoBehaviour
{
    /// <summary>Most recently enabled instance. Intended for read-only debug/observer use.</summary>
    public static HeartbeatSoundPlayer Instance { get; private set; }

    [Header("References")]
    [Tooltip("Data asset with per-level banks, intervals and envelope values.")]
    public HeartbeatSoundConfig config;
    [Tooltip("Dedicated source used for heartbeat one-shots. If null, AudioManager.uiSfxSource " +
             "is used (dry, no-reverb \"in your head\" routing), falling back to sfxSource.")]
    public AudioSource audioSource;

    [Header("Debug")]
    [SerializeField] private IntensityLevel currentLevelDebug;
    [SerializeField, Range(0f, 1f)] private float currentVolumeMultiplier = 0.5f;
    [SerializeField] private float rampElapsedDebug = -1f;

    /// <summary>Live volume multiplier applied to each heartbeat one-shot (0-1).</summary>
    public float CurrentVolumeMultiplier => currentVolumeMultiplier;

    private float nextBeatTime;
    private float rampStartTime = -1f;
    private bool subscribed;
    private IntensityManager boundManager;

    private void OnEnable()
    {
        if (Instance == null || !Instance.isActiveAndEnabled)
            Instance = this;

        TrySubscribe();
        if (config != null)
            currentVolumeMultiplier = config.subtleVolumeMultiplier;
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!subscribed)
            TrySubscribe();

        if (config == null || boundManager == null)
            return;

        UpdateVolumeMultiplier();

        if (Time.time >= nextBeatTime)
        {
            PlayBeat();
            HeartbeatSoundConfig.LevelEntry entry = config.GetEntry(boundManager.CurrentLevel);
            float interval = entry != null ? Mathf.Max(0.05f, entry.secondsBetweenBeats) : 1f;
            nextBeatTime = Time.time + interval;
        }
    }

    private void TrySubscribe()
    {
        if (subscribed) return;

        IntensityManager manager = IntensityManager.Instance;
        if (manager == null) return;

        manager.OnLevelChanged += HandleLevelChanged;
        boundManager = manager;
        currentLevelDebug = manager.CurrentLevel;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (subscribed && boundManager != null)
            boundManager.OnLevelChanged -= HandleLevelChanged;
        subscribed = false;
        boundManager = null;
    }

    private void HandleLevelChanged(IntensityLevel previous, IntensityLevel next)
    {
        currentLevelDebug = next;

        // Ramp the volume envelope only when intensity escalates; downward
        // transitions change interval immediately but leave volume alone.
        if ((int)next > (int)previous)
        {
            rampStartTime = Time.time;
            // Restart the beat cadence so the first thump at the new level
            // lands promptly instead of waiting out the previous interval.
            nextBeatTime = Time.time;
        }
    }

    private void UpdateVolumeMultiplier()
    {
        float subtle = config.subtleVolumeMultiplier;
        float peak = config.peakVolumeMultiplier;

        if (rampStartTime < 0f)
        {
            currentVolumeMultiplier = subtle;
            rampElapsedDebug = -1f;
            return;
        }

        float elapsed = Time.time - rampStartTime;
        rampElapsedDebug = elapsed;

        float rampUp = Mathf.Max(0f, config.rampUpDuration);
        float hold = Mathf.Max(0f, config.peakHoldDuration);
        float decay = Mathf.Max(0.01f, config.decayDuration);

        if (elapsed < rampUp)
        {
            float n = rampUp <= 0f ? 1f : elapsed / rampUp;
            currentVolumeMultiplier = Mathf.Lerp(subtle, peak, Ease(n));
        }
        else if (elapsed < rampUp + hold)
        {
            currentVolumeMultiplier = peak;
        }
        else if (elapsed < rampUp + hold + decay)
        {
            float n = (elapsed - rampUp - hold) / decay;
            currentVolumeMultiplier = Mathf.Lerp(peak, subtle, Ease(n));
        }
        else
        {
            currentVolumeMultiplier = subtle;
            rampStartTime = -1f;
        }
    }

    private float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        return config != null && config.smoothEnvelope ? Mathf.SmoothStep(0f, 1f, t) : t;
    }

    private void PlayBeat()
    {
        HeartbeatSoundConfig.LevelEntry entry = config.GetEntry(boundManager.CurrentLevel);
        if (entry == null || entry.clips == null) return;

        AudioSource src = audioSource;
        if (src == null && AudioManager.Instance != null)
            src = AudioManager.Instance.uiSfxSource != null
                ? AudioManager.Instance.uiSfxSource
                : AudioManager.Instance.sfxSource;
        if (src == null) return;

        // SfxBank.PlayOnSource composes the envelope multiplier with the clip's authored
        // volume, the bank gain and the jitter in perceived space before a single ToLinear,
        // so the full inspector-authored loudness model is respected on the heartbeat path.
        entry.clips.PlayOnSource(src, currentVolumeMultiplier);
    }
}
