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
    [Tooltip("Dedicated source used for heartbeat one-shots. If null, AudioManager.sfxSource is used.")]
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

    /// <summary>
    /// Maps the perceived-loudness envelope value to a linear amplitude value
    /// suitable for <see cref="AudioSource.PlayOneShot(AudioClip, float)"/>, via the
    /// shared <see cref="AudioVolume"/> helper. Per-asset exponent allows
    /// heartbeat-specific punch independent of the project default.
    /// </summary>
    private float ShapeAmplitude(float perceivedMultiplier)
    {
        float exp = config != null ? config.volumeExponent : AudioVolume.DefaultExponent;
        return AudioVolume.ToLinear(perceivedMultiplier, exp);
    }

    private void PlayBeat()
    {
        HeartbeatSoundConfig.LevelEntry entry = config.GetEntry(boundManager.CurrentLevel);
        if (entry == null || entry.clips == null) return;
        PlayRandomFromBank(entry.clips, currentVolumeMultiplier);
    }

    private void PlayRandomFromBank(SfxBank bank, float volumeMultiplier)
    {
        if (bank == null || bank.clips == null || bank.clips.Count == 0)
            return;

        AudioSource src = audioSource;
        if (src == null && AudioManager.Instance != null)
            src = AudioManager.Instance.sfxSource;
        if (src == null) return;

        float pMin = Mathf.Min(bank.pitchMin, bank.pitchMax);
        float pMax = Mathf.Max(bank.pitchMin, bank.pitchMax);

        int start = Random.Range(0, bank.clips.Count);
        for (int i = 0; i < bank.clips.Count; i++)
        {
            int idx = (start + i) % bank.clips.Count;
            AudioClipVolume entry = bank.clips[idx];
            if (entry == null || entry.Clip == null)
                continue;

            float previousPitch = src.pitch;
            src.pitch = Random.Range(pMin, pMax);
            float shaped = ShapeAmplitude(volumeMultiplier);
            src.PlayOneShot(entry.Clip, Mathf.Clamp01(entry.Volume * shaped));
            src.pitch = previousPitch;
            return;
        }
    }
}
