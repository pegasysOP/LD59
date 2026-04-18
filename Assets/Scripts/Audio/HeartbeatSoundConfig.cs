using UnityEngine;

/// <summary>
/// Data-only definition for the heartbeat audio system. Pairs each
/// <see cref="IntensityLevel"/> with a clip bank and beat interval, and
/// holds the shared volume envelope used when intensity escalates.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Heartbeat Sound Config", fileName = "HeartbeatSoundConfig")]
public class HeartbeatSoundConfig : ScriptableObject
{
    [System.Serializable]
    public class LevelEntry
    {
        public SfxBank clips = new SfxBank { pitchMin = 0.97f, pitchMax = 1.03f };
        [Tooltip("Seconds between heartbeats while at this level.")]
        [Min(0.05f)] public float secondsBetweenBeats = 1.0f;
    }

    [Header("Per-Level Beats")]
    public LevelEntry calm = new LevelEntry { secondsBetweenBeats = 1.10f };
    public LevelEntry elevated = new LevelEntry { secondsBetweenBeats = 0.75f };
    public LevelEntry intense = new LevelEntry { secondsBetweenBeats = 0.55f };
    public LevelEntry overload = new LevelEntry { secondsBetweenBeats = 0.40f };

    [Header("Volume Envelope (triggered on intensity increase only)")]
    [Tooltip("Fast ramp from the current multiplier up to the peak multiplier.")]
    [Min(0f)] public float rampUpDuration = 0.35f;
    [Tooltip("Duration the heartbeat is held at the peak multiplier after an intensity increase.")]
    [Min(0f)] public float peakHoldDuration = 4f;
    [Tooltip("Duration of the slow decay from peak back down to the subtle multiplier.")]
    [Min(0.01f)] public float decayDuration = 8f;

    [Header("Volume Multipliers")]
    [Tooltip("Perceived-loudness multiplier at the peak of the envelope (0-1). 1.0 = full loudness.")]
    [Range(0f, 1f)] public float peakVolumeMultiplier = 1f;
    [Tooltip("Perceived-loudness multiplier during the resting / subtle phase (0-1).")]
    [Range(0f, 1f)] public float subtleVolumeMultiplier = 0.5f;
    [Tooltip("Exponent applied to the envelope before it's sent to PlayOneShot, to convert perceived " +
            "loudness into linear amplitude. 1 = linear (subtle=0.5 only -6 dB below peak). " +
            "2 = squared (-12 dB, project default). 3 = cubed (-18 dB). Use 2-3 for a punchy heartbeat.")]
    [Range(1f, 4f)] public float volumeExponent = AudioVolume.DefaultExponent;
    [Tooltip("If true the ramp-up and decay phases are eased with SmoothStep instead of a straight " +
            "linear lerp. Gives a more natural-feeling swell and ringdown.")]
    public bool smoothEnvelope = true;

    /// <summary>Returns the per-level bank + interval for the provided zone.</summary>
    public LevelEntry GetEntry(IntensityLevel level)
    {
        switch (level)
        {
            case IntensityLevel.Overload: return overload;
            case IntensityLevel.Intense: return intense;
            case IntensityLevel.Elevated: return elevated;
            case IntensityLevel.Calm:
            default: return calm;
        }
    }
}
