using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A named pool of clips with a shared pitch range; picks one at random and plays it through <see cref="AudioManager"/>.
/// Authored clip volumes are treated as *perceived loudness* (0-1) and converted to linear amplitude
/// via <see cref="AudioVolume.DefaultExponent"/> before reaching the audio engine, so the ear-appropriate
/// dynamic range is preserved. The exponent is intentionally global and non-overridable so every sound
/// in the game shares a single perceived-loudness curve.
/// </summary>
[System.Serializable]
public class SfxBank
{
    public List<AudioClipVolume> clips = new List<AudioClipVolume>();

    [Range(0.75f, 1.25f)] public float pitchMin = 1f;
    [Range(0.75f, 1.25f)] public float pitchMax = 1f;

    [Tooltip("Bank-wide perceived-loudness multiplier. 1.0 = use each clip's authored volume as-is.")]
    [Range(0f, 1f)] public float gain = 1f;

    [Tooltip("Random perceived-loudness variance applied per play so repeated sounds don't feel robotic. " +
             "0 = off. 0.10 = +/-10% perceived loudness variation per trigger.")]
    [Range(0f, 0.5f)] public float volumeJitter = 0f;

    public bool HasAnyClip
    {
        get
        {
            if (clips == null) return false;
            for (int i = 0; i < clips.Count; i++)
            {
                if (clips[i] != null && clips[i].Clip != null) return true;
            }
            return false;
        }
    }

    public void Play()
    {
        if (clips == null || clips.Count == 0 || AudioManager.Instance == null)
            return;

        float pMin = Mathf.Min(pitchMin, pitchMax);
        float pMax = Mathf.Max(pitchMin, pitchMax);

        int start = Random.Range(0, clips.Count);
        for (int i = 0; i < clips.Count; i++)
        {
            int idx = (start + i) % clips.Count;
            AudioClipVolume entry = clips[idx];
            if (entry == null || entry.Clip == null)
                continue;

            float perceived = entry.Volume * gain;
            if (volumeJitter > 0f)
                perceived *= 1f + Random.Range(-volumeJitter, volumeJitter);

            float shapedAmp = AudioVolume.ToLinear(perceived);

            AudioClipVolume shapedEntry = new AudioClipVolume(entry.Clip, shapedAmp, entry.Delay);
            AudioManager.Instance.PlaySfxWithPitchShifting(shapedEntry, pMin, pMax);
            return;
        }
    }

    /// <summary>
    /// Positional variant of <see cref="Play"/>: picks a clip, applies the same perceived-loudness
    /// shaping and jitter, and plays it from a temporary 3D AudioSource at <paramref name="worldPosition"/>.
    /// </summary>
    public void PlayAt(Vector3 worldPosition)
    {
        if (clips == null || clips.Count == 0 || AudioManager.Instance == null)
            return;

        float pMin = Mathf.Min(pitchMin, pitchMax);
        float pMax = Mathf.Max(pitchMin, pitchMax);

        int start = Random.Range(0, clips.Count);
        for (int i = 0; i < clips.Count; i++)
        {
            int idx = (start + i) % clips.Count;
            AudioClipVolume entry = clips[idx];
            if (entry == null || entry.Clip == null)
                continue;

            float perceived = entry.Volume * gain;
            if (volumeJitter > 0f)
                perceived *= 1f + Random.Range(-volumeJitter, volumeJitter);

            float shapedAmp = AudioVolume.ToLinear(perceived);

            AudioClipVolume shapedEntry = new AudioClipVolume(entry.Clip, shapedAmp, entry.Delay);
            AudioManager.Instance.PlaySfxAtPoint(shapedEntry, Random.Range(pMin, pMax), worldPosition);
            return;
        }
    }

    /// <summary>
    /// Plays a random clip from this bank on a caller-provided <see cref="AudioSource"/> (used by
    /// systems like the heartbeat that need a dedicated, persistent source rather than the shared
    /// SFX pool). Applies the identical loudness model as <see cref="Play"/>:
    ///   perceived = clip.volume * bank.gain * perceivedMultiplier * (1 +/- volumeJitter)
    ///   linear    = AudioVolume.ToLinear(perceived)
    /// so callers can pass an envelope value in perceived space and get correct amplitude shaping.
    /// Clip <c>Delay</c> is intentionally ignored on this path (the heartbeat is scheduled externally).
    /// </summary>
    public void PlayOnSource(AudioSource src, float perceivedMultiplier = 1f)
    {
        if (src == null || clips == null || clips.Count == 0)
            return;

        float pMin = Mathf.Min(pitchMin, pitchMax);
        float pMax = Mathf.Max(pitchMin, pitchMax);

        int start = Random.Range(0, clips.Count);
        for (int i = 0; i < clips.Count; i++)
        {
            int idx = (start + i) % clips.Count;
            AudioClipVolume entry = clips[idx];
            if (entry == null || entry.Clip == null)
                continue;

            float perceived = entry.Volume * gain * Mathf.Max(0f, perceivedMultiplier);
            if (volumeJitter > 0f)
                perceived *= 1f + Random.Range(-volumeJitter, volumeJitter);

            float shapedAmp = AudioVolume.ToLinear(perceived);

            src.pitch = Random.Range(pMin, pMax);
            src.PlayOneShot(entry.Clip, Mathf.Clamp01(shapedAmp));
            return;
        }
    }
}
