using UnityEngine;

/// <summary>
/// Shared "perceptual loudness" helpers for the audio stack.
///
/// Convention used across the project:
///   - "Perceived" is a 0-1 value where a given delta corresponds to a consistent
///     perceived loudness change (roughly log/dB space).
///   - "Linear amplitude" is what Unity's <see cref="AudioSource.volume"/> and
///     <see cref="AudioSource.PlayOneShot(AudioClip, float)"/> actually consume.
///
/// All ramps, fades, ducks, crossfades and envelopes should INTERPOLATE in
/// perceived space and CONVERT to linear amplitude right before writing to the
/// audio engine, so the ear hears a smooth loudness curve instead of
/// "nothing, nothing, nothing, BLAM."
/// </summary>
public static class AudioVolume
{
    /// <summary>
    /// Default exponent mapping perceived 0-1 to linear amplitude 0-1.
    ///   1 = linear (~6 dB between 0.5 and 1.0)
    ///   2 = squared (~12 dB)       &lt;-- recommended default for most game audio
    ///   3 = cubed (~18 dB, very wide dynamic range)
    /// </summary>
    public const float DefaultExponent = 2f;

    /// <summary>Convert perceived loudness to linear amplitude using the project default exponent.</summary>
    public static float ToLinear(float perceived)
        => Mathf.Pow(Mathf.Clamp01(perceived), DefaultExponent);

    /// <summary>Convert perceived loudness to linear amplitude using a custom exponent.</summary>
    public static float ToLinear(float perceived, float exponent)
        => Mathf.Pow(Mathf.Clamp01(perceived), Mathf.Max(0.01f, exponent));

    /// <summary>Convert linear amplitude back to perceived loudness using the project default exponent.</summary>
    public static float ToPerceived(float linear)
        => Mathf.Pow(Mathf.Clamp01(linear), 1f / DefaultExponent);

    /// <summary>Convert linear amplitude back to perceived loudness using a custom exponent.</summary>
    public static float ToPerceived(float linear, float exponent)
        => Mathf.Pow(Mathf.Clamp01(linear), 1f / Mathf.Max(0.01f, exponent));

    /// <summary>
    /// Interpolate between two amplitude endpoints in PERCEIVED space and return
    /// the resulting linear amplitude. Use this in fade/duck/crossfade loops.
    /// </summary>
    public static float LerpAmplitudePerceived(float ampA, float ampB, float t)
    {
        float perA = ToPerceived(ampA);
        float perB = ToPerceived(ampB);
        float per = Mathf.Lerp(perA, perB, Mathf.Clamp01(t));
        return ToLinear(per);
    }

    /// <summary>SmoothStep-eased variant of <see cref="LerpAmplitudePerceived"/>.</summary>
    public static float SmoothStepAmplitudePerceived(float ampA, float ampB, float t)
    {
        return LerpAmplitudePerceived(ampA, ampB, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
    }
}
