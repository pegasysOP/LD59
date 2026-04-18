using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A named pool of clips with a shared pitch range; picks one at random and plays it through <see cref="AudioManager"/>.
/// </summary>
[System.Serializable]
public class SfxBank
{
    public List<AudioClipVolume> clips = new List<AudioClipVolume>();

    [Range(0.75f, 1.25f)] public float pitchMin = 1f;
    [Range(0.75f, 1.25f)] public float pitchMax = 1f;

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

            AudioManager.Instance.PlaySfxWithPitchShifting(entry, pMin, pMax);
            return;
        }
    }
}
