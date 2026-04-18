using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A named pool of clips with a shared pitch range; picks one at random and plays it through <see cref="AudioManager"/>.
/// Authored clip volumes are treated as *perceived loudness* (0-1) and converted to linear amplitude
/// via <see cref="volumeExponent"/> before reaching the audio engine, so the ear-appropriate dynamic
/// range is preserved. Set the exponent to 1 to keep the old linear behavior.
/// </summary>
[System.Serializable]
public class SfxBank
{
    public List<AudioClipVolume> clips = new List<AudioClipVolume>();

    [Range(0.75f, 1.25f)] public float pitchMin = 1f;
    [Range(0.75f, 1.25f)] public float pitchMax = 1f;

    [Tooltip("Bank-wide perceived-loudness multiplier. 1.0 = use each clip's authored volume as-is.")]
    [Range(0f, 1f)] public float gain = 1f;

    [Tooltip("Exponent that maps authored perceived-loudness (0-1) to the linear amplitude passed to " +
             "AudioSource.PlayOneShot. 1 = linear (legacy). 2 = squared (-12 dB at 0.5, punchy default). " +
             "3 = cubed (-18 dB at 0.5, very wide dynamic range). Per-bank so you can tune, e.g., " +
             "2 for soft footsteps, 2.5 for landing thuds.")]
    [Range(1f, 4f)] public float volumeExponent = AudioVolume.DefaultExponent;

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

            float shapedAmp = AudioVolume.ToLinear(perceived, volumeExponent);

            AudioClipVolume shapedEntry = new AudioClipVolume(entry.Clip, shapedAmp, entry.Delay);
            AudioManager.Instance.PlaySfxWithPitchShifting(shapedEntry, pMin, pMax);
            return;
        }
    }
}
