using UnityEngine;

[System.Serializable]
public class AudioClipVolume
{
    [SerializeField] AudioClip _clip;
    [SerializeField, Range(0f, 1f)] float _volume = 1f;
    [SerializeField] float _delay = 0f;

    public AudioClip Clip => _clip;

    /// <summary>
    /// Authored loudness in 0-1 <b>perceived-loudness space</b>. Higher-level systems
    /// (e.g. <see cref="SfxBank"/>, <see cref="HeartbeatSoundPlayer"/>, fade routines)
    /// are expected to convert this to linear amplitude via <see cref="AudioVolume.ToLinear(float, float)"/>
    /// before it reaches the audio engine. See <see cref="AudioVolume"/> for the rationale.
    /// </summary>
    public float Volume => Mathf.Clamp01(_volume);

    /// <summary>
    /// Playback timing offset in seconds.
    /// Positive: delay playback by this many seconds after the trigger.
    /// Negative: trim this many seconds off the start of the clip (playback begins at <c>-Delay</c>).
    /// Zero: play immediately with no offset.
    /// </summary>
    public float Delay => _delay;

    public AudioClipVolume()
    {
        _clip = null;
        _volume = 1f;
        _delay = 0f;
    }

    public AudioClipVolume(AudioClip clip, float volume = 1f, float delay = 0f)
    {
        _clip = clip;
        _volume = Mathf.Clamp01(volume);
        _delay = delay;
    }
}
