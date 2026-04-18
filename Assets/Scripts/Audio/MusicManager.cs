using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Single-track music player with two playback modes:
///
/// <list type="number">
///   <item>
///     <description>
///     <see cref="PlayGameMusic(AudioClip)"/> - in-game flow. Crossfades from
///     the currently playing track to the new clip over
///     <see cref="crossfadeDuration"/> (fading IN to the peak multiplier), holds
///     at peak for <see cref="peakHoldDuration"/>, then decays down to
///     <see cref="backgroundMultiplier"/> over <see cref="decayDuration"/>.
///     The three phases together are tuned so the music lands in the background
///     roughly 10 seconds after a change, mirroring the swell/decay behaviour
///     of <see cref="HeartbeatSoundPlayer"/>.
///     </description>
///   </item>
///   <item>
///     <description>
///     <see cref="PlayMusic(AudioClip)"/> - non-in-game flow (main menu, win,
///     credits). Snaps to the new clip at full volume with no envelope or
///     ongoing volume manipulation.
///     </description>
///   </item>
/// </list>
///
/// Re-requesting the clip that is already playing is a no-op in both modes.
/// All interpolation happens in perceived-loudness space via
/// <see cref="AudioVolume"/>, matching the rest of the audio stack.
/// </summary>
[DisallowMultipleComponent]
public class MusicManager : MonoBehaviour
{
    /// <summary>Most recently enabled instance.</summary>
    public static MusicManager Instance { get; private set; }

    private enum Mode { None, Game, Menu }

    [Header("Sources")]
    [Tooltip("Primary music source. Auto-created as a child if left null.")]
    public AudioSource sourceA;
    [Tooltip("Secondary music source used for crossfading. Auto-created as a child if left null.")]
    public AudioSource sourceB;
    [Tooltip("Optional mixer group output applied to both sources on Awake.")]
    public AudioMixerGroup mixerGroup;

    [Header("In-Game Envelope")]
    [Tooltip("Crossfade length when switching tracks in-game. Also acts as the ramp-up " +
             "from silence to peak the first time a track starts.")]
    [Min(0.01f)] public float crossfadeDuration = 3f;
    [Tooltip("Seconds the new track stays at peak before decaying to the background.")]
    [Min(0f)] public float peakHoldDuration = 3f;
    [Tooltip("Decay length from peak down to backgroundMultiplier. " +
             "crossfade + peakHold + decay should land around ~10s total.")]
    [Min(0.01f)] public float decayDuration = 4f;
    [Tooltip("Perceived-loudness multiplier at the peak of the envelope (0-1). " +
             "1.0 = full music loudness.")]
    [Range(0f, 1f)] public float peakMultiplier = 1f;
    [Tooltip("Perceived-loudness multiplier during the background / resting phase (0-1).")]
    [Range(0f, 1f)] public float backgroundMultiplier = 0.4f;
    [Tooltip("Ease ramp-in and decay with SmoothStep instead of a straight linear lerp.")]
    public bool smoothEnvelope = true;

    [Header("Non-Game Playback")]
    [Tooltip("Perceived-loudness multiplier for menu/victory/credits music. 1.0 = full loudness.")]
    [Range(0f, 1f)] public float fullVolumeMultiplier = 1f;

    [Header("Debug")]
    [SerializeField] private AudioClip currentClipDebug;
    [SerializeField, Range(0f, 1f)] private float currentPerceivedMultiplierDebug;
    [SerializeField] private Mode currentModeDebug = Mode.None;
    [SerializeField] private string phaseDebug = "Idle";

    private AudioSource activeSource;
    private Coroutine routine;

    private void Awake()
    {
        if (Instance == null || !Instance.isActiveAndEnabled)
            Instance = this;

        EnsureSource(ref sourceA, "MusicSourceA");
        EnsureSource(ref sourceB, "MusicSourceB");
        activeSource = sourceA;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void EnsureSource(ref AudioSource src, string sourceName)
    {
        if (src == null)
        {
            GameObject go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            src = go.AddComponent<AudioSource>();
        }

        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = 0f;
        if (mixerGroup != null)
            src.outputAudioMixerGroup = mixerGroup;
    }

    //==================== Public API ====================

    /// <summary>
    /// Play <paramref name="clip"/> using the in-game envelope (crossfade in,
    /// hold at peak, then fade to the background multiplier). No-op when the
    /// clip is already the actively-playing track. Pass a positive
    /// <paramref name="crossfadeOverride"/> to force a specific crossfade
    /// length for this call (useful for panic stingers that need to swap
    /// tracks near-instantly); leave it negative to use
    /// <see cref="crossfadeDuration"/>.
    /// </summary>
    public void PlayGameMusic(AudioClip clip, float crossfadeOverride = -1f)
    {
        if (clip == null) return;
        if (IsAlreadyPlaying(clip)) return;

        StopRoutine();
        currentClipDebug = clip;
        currentModeDebug = Mode.Game;
        float fade = crossfadeOverride >= 0f ? crossfadeOverride : crossfadeDuration;
        routine = StartCoroutine(GameMusicRoutine(clip, fade));
    }

    /// <summary>
    /// Play <paramref name="clip"/> at full volume with no envelope or ongoing
    /// manipulation. Intended for menu / victory / credits screens. No-op when
    /// the clip is already the actively-playing track.
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (IsAlreadyPlaying(clip)) return;

        StopRoutine();

        AudioSource other = OtherSource(activeSource);
        if (other != null)
        {
            other.Stop();
            other.volume = 0f;
            other.clip = null;
        }

        activeSource.clip = clip;
        activeSource.volume = TargetLinear(fullVolumeMultiplier);
        activeSource.Play();

        currentClipDebug = clip;
        currentModeDebug = Mode.Menu;
        currentPerceivedMultiplierDebug = fullVolumeMultiplier;
        phaseDebug = "FullVolume";
    }

    /// <summary>
    /// Stop all music. When <paramref name="fadeOutDuration"/> is greater than
    /// zero, ramps the active source down in perceived space first.
    /// </summary>
    public void StopMusic(float fadeOutDuration = 0f)
    {
        StopRoutine();

        if (fadeOutDuration <= 0f)
        {
            SilenceAll();
            ResetDebug();
            return;
        }

        routine = StartCoroutine(FadeOutRoutine(fadeOutDuration));
    }

    //==================== Internals ====================

    private bool IsAlreadyPlaying(AudioClip clip)
    {
        return activeSource != null
            && activeSource.clip == clip
            && activeSource.isPlaying;
    }

    private AudioSource OtherSource(AudioSource src)
    {
        return src == sourceA ? sourceB : sourceA;
    }

    private void StopRoutine()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private void SilenceAll()
    {
        if (sourceA != null) { sourceA.Stop(); sourceA.volume = 0f; sourceA.clip = null; }
        if (sourceB != null) { sourceB.Stop(); sourceB.volume = 0f; sourceB.clip = null; }
    }

    private void ResetDebug()
    {
        currentClipDebug = null;
        currentModeDebug = Mode.None;
        currentPerceivedMultiplierDebug = 0f;
        phaseDebug = "Idle";
    }

    // Crossfade in to peak, hold, then decay to background.
    private IEnumerator GameMusicRoutine(AudioClip clip, float crossfadeSeconds)
    {
        AudioSource from = activeSource;
        AudioSource to = OtherSource(from);

        float fromStartLinear = (from != null && from.isPlaying) ? from.volume : 0f;
        float peakLinear = TargetLinear(peakMultiplier);

        to.clip = clip;
        to.volume = 0f;
        to.Play();

        // Phase 1: crossfade (previous track fades out as new track rises to peak)
        phaseDebug = "Crossfade";
        float fade = Mathf.Max(0.01f, crossfadeSeconds);
        float t = 0f;
        while (t < fade)
        {
            t += Time.deltaTime;
            float n = Ease(t / fade);
            to.volume = AudioVolume.LerpAmplitudePerceived(0f, peakLinear, n);
            if (from != null && from != to)
                from.volume = AudioVolume.LerpAmplitudePerceived(fromStartLinear, 0f, n);
            currentPerceivedMultiplierDebug = Mathf.Lerp(0f, peakMultiplier, n);
            yield return null;
        }

        to.volume = peakLinear;
        if (from != null && from != to)
        {
            from.Stop();
            from.volume = 0f;
            from.clip = null;
        }
        activeSource = to;
        currentPerceivedMultiplierDebug = peakMultiplier;

        // Phase 2: hold at peak
        phaseDebug = "PeakHold";
        float hold = Mathf.Max(0f, peakHoldDuration);
        float held = 0f;
        while (held < hold)
        {
            held += Time.deltaTime;
            // Re-evaluate each frame so master-volume changes mid-hold still track.
            to.volume = TargetLinear(peakMultiplier);
            yield return null;
        }

        // Phase 3: decay to background
        phaseDebug = "Decay";
        float decay = Mathf.Max(0.01f, decayDuration);
        float decayStart = to.volume;
        float decayEnd = TargetLinear(backgroundMultiplier);
        float td = 0f;
        while (td < decay)
        {
            td += Time.deltaTime;
            float n = Ease(td / decay);
            // Recompute end each frame so master-volume changes still track.
            decayEnd = TargetLinear(backgroundMultiplier);
            to.volume = AudioVolume.LerpAmplitudePerceived(decayStart, decayEnd, n);
            currentPerceivedMultiplierDebug = Mathf.Lerp(peakMultiplier, backgroundMultiplier, n);
            yield return null;
        }

        to.volume = TargetLinear(backgroundMultiplier);
        currentPerceivedMultiplierDebug = backgroundMultiplier;
        phaseDebug = "Background";
        routine = null;
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        AudioSource src = activeSource;
        if (src == null || !src.isPlaying)
        {
            SilenceAll();
            ResetDebug();
            routine = null;
            yield break;
        }

        phaseDebug = "FadeOut";
        float startVol = src.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            src.volume = AudioVolume.LerpAmplitudePerceived(startVol, 0f, t / duration);
            yield return null;
        }

        SilenceAll();
        ResetDebug();
        routine = null;
    }

    // Convert a perceived 0-1 multiplier into a linear AudioSource.volume, scaled
    // to the project's "full music loudness" baseline (master/3, matching AudioManager).
    private float TargetLinear(float perceivedMultiplier)
    {
        float masterLinear = Mathf.Clamp01(SettingsUtils.GetMasterVolume()) / 3f;
        float masterPerceived = AudioVolume.ToPerceived(masterLinear);
        float perceived = masterPerceived * Mathf.Clamp01(perceivedMultiplier);
        return AudioVolume.ToLinear(perceived);
    }

    private float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        return smoothEnvelope ? Mathf.SmoothStep(0f, 1f, t) : t;
    }
}
