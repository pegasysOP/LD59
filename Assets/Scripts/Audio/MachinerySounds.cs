using UnityEngine;

/// <summary>
/// Random pool of one-shot clips for ambient machinery: distant crashes, hums, metal hits,
/// fans spinning up, etc. Wraps a single <see cref="SfxBank"/> so it picks up the same
/// perceived-loudness shaping, pitch variance, and volume jitter as the rest of the game's
/// SFX pipeline. Intended consumer: <see cref="MachineryAmbientPlayer"/>.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Machinery Sounds", fileName = "MachinerySounds")]
public class MachinerySounds : ScriptableObject
{
    [Tooltip("Pool of machinery one-shots. Widen pitch range a touch for organic variance.")]
    public SfxBank bank = new SfxBank { pitchMin = 0.9f, pitchMax = 1.1f, volumeJitter = 0.08f };

    public bool HasAnyClip => bank != null && bank.HasAnyClip;

    /// <summary>
    /// Plays a random clip on the caller's <see cref="AudioSource"/>. Returns the expected
    /// playback duration in seconds (clip length scaled by pitch) so the caller can manage
    /// concurrency bookkeeping without needing per-frame <c>isPlaying</c> polls.
    /// </summary>
    public float PlayOnSource(AudioSource src, float perceivedMultiplier = 1f)
    {
        if (bank == null || src == null) return 0f;
        return bank.PlayOnSource(src, perceivedMultiplier);
    }
}
