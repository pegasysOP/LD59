using UnityEngine;

/// <summary>
/// Audio driver for the radar-alignment minigame. Owns two dedicated looping <see cref="AudioSource"/>s
/// (the signal loop and the handle-movement loop) and fires one-shots for handle grab / release.
///
///   Radar signal loop — pitch and perceived volume ramp between the min (fully mis-aligned) and max
///                       (perfect match) values from <see cref="RadarMinigameSounds"/>. Value is
///                       smoothed with <c>radarResponseSpeed</c> so scrubbing the sliders produces
///                       a continuous slide rather than stepping. Fades out when the minigame
///                       completes.
///
///   Move loop         — shared between both sliders. Fades in while either handle is moving
///                       (driven by <see cref="RadarSlider.ValueChangeRate"/>) and fades back out
///                       when motion stops. Fast scrubbing adds an extra perceived-volume boost on
///                       top of the base level — see <c>moveBoostExtraPerceivedVolume</c>.
///
///   Grab / release    — one-shots from <see cref="RadarMinigameSounds.handleGrab"/> /
///                       <see cref="RadarMinigameSounds.handleRelease"/>, spatialised at the
///                       grabbed handle's world position so the cue reads left/right correctly.
///
/// Both loop sources are auto-spawned as children of this component on enable (same pattern as
/// <see cref="PowerDownSequence"/>'s ambient loops): clip/loop/mixer-group/spatial blend are set
/// up here so all that's required in the scene is dropping this component on the radar rig and
/// authoring a <see cref="RadarMinigameSounds"/> asset. No scene wiring for AudioSources needed.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RadarAlignment))]
public class RadarAlignmentSounds : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private RadarMinigameSounds sounds;

    [Header("Optional")]
    [Tooltip("Explicit RadarAlignment ref. Defaults to the component on this GameObject.")]
    [SerializeField] private RadarAlignment alignment;
    [Tooltip("Parent transform for the looping AudioSources spawned at runtime. Defaults to this " +
             "component's transform so the sources live with the radar rig in the hierarchy.")]
    [SerializeField] private Transform loopSourcesParent;

    [Header("Spatialisation")]
    [Tooltip("0 = 2D (non-diegetic), 1 = fully 3D positional. The radar and move loops share this " +
             "value. 3D is recommended when the radar table is a physical object in the world so " +
             "the sound attenuates as the player walks away.")]
    [Range(0f, 1f)] public float loopSpatialBlend = 1f;

    [Tooltip("Rolloff min distance for the loop sources when spatialised. Inside this radius the " +
             "source is at full volume.")]
    [Min(0f)] public float loopMinDistance = 1.5f;
    [Tooltip("Rolloff max distance for the loop sources when spatialised. Beyond this the source is " +
             "inaudible.")]
    [Min(0.1f)] public float loopMaxDistance = 20f;

    private AudioSource _radarSource;
    private AudioSource _moveSource;

    // Current perceived-space volume of the move loop (we smooth in perceived space and convert on
    // write, matching the rest of the audio stack — see AudioVolume for the rationale).
    private float _movePerceivedCurrent;

    // Smoothed outputs of the radar loop envelope in perceived / pitch space (they chase the target
    // every frame so rapid slider scrubs don't step).
    private float _radarPitchCurrent;
    private float _radarPerceivedCurrent;

    private bool _radarFadingOut;
    private bool _signalSendFired;

    // Engagement tracking for the "silent until first touch + fade to 0 after idle" behaviour.
    // _anyHandleEverGrabbed keeps the loop silent for the whole opening beat of the minigame.
    // _lastEngagedTime is updated every frame a handle is held AND on grab events, so any activity
    // (even brief re-grabs) restarts the idle countdown.
    private bool _anyHandleEverGrabbed;
    private float _lastEngagedTime = float.NegativeInfinity;

    private void Reset()
    {
        alignment = GetComponent<RadarAlignment>();
    }

    private void OnEnable()
    {
        if (alignment == null) alignment = GetComponent<RadarAlignment>();
        if (loopSourcesParent == null) loopSourcesParent = transform;

        SubscribeSlider(alignment != null ? alignment.PositionSlider : null);
        SubscribeSlider(alignment != null ? alignment.AngleSlider : null);

        SpawnRadarLoop();
        SpawnMoveLoop();
    }

    private void OnDisable()
    {
        UnsubscribeSlider(alignment != null ? alignment.PositionSlider : null);
        UnsubscribeSlider(alignment != null ? alignment.AngleSlider : null);

        KillLoop(ref _radarSource);
        KillLoop(ref _moveSource);
    }

    private void Update()
    {
        if (sounds == null)
            return;

        UpdateRadarLoop();
        UpdateMoveLoop();
        MaybeFireSignalSend();
    }

    // Detects the !IsComplete → IsComplete transition and fires the signal-send one-shot exactly
    // once. Runs independently of the radar-loop fade-out so the send cue can ride on top of the
    // decaying signal tone for a satisfying "locked on, transmitting" beat.
    private void MaybeFireSignalSend()
    {
        if (_signalSendFired) return;
        if (alignment == null || !alignment.IsComplete) return;

        _signalSendFired = true;
        if (sounds.signalSend != null && sounds.signalSend.HasAnyClip)
            sounds.signalSend.PlayAt(transform.position);
    }

    // ---------- Radar signal loop ----------

    private void SpawnRadarLoop()
    {
        if (sounds == null || sounds.radarLoop == null || sounds.radarLoop.Clip == null)
            return;

        _radarSource = SpawnLoopSource("RadarAlignment_RadarLoop", sounds.radarLoop.Clip);
        if (_radarSource == null) return;

        // Silent until the player's first handle grab (see _anyHandleEverGrabbed gate in
        // UpdateRadarLoop). Pitch starts at the "fully mis-aligned" floor so when audio does
        // eventually kick in the first sample isn't a spike of the max pitch.
        _radarPitchCurrent = sounds.radarMinPitch;
        _radarPerceivedCurrent = 0f;
        ApplyRadarToSource();
        _radarSource.Play();
    }

    private void UpdateRadarLoop()
    {
        if (_radarSource == null || sounds.radarLoop == null || sounds.radarLoop.Clip == null)
            return;

        if (_radarFadingOut)
            return; // completion fade-out routine owns the volume until destroy.

        // Alignment-driven targets: error=0 → max (original) pitch and max perceived volume;
        // error=1 → min pitch and min perceived volume.
        float error = alignment != null ? alignment.AlignmentError01 : 1f;
        float accuracy = 1f - Mathf.Clamp01(error);

        float alignmentTargetPitch = Mathf.Lerp(sounds.radarMinPitch, sounds.radarMaxPitch, accuracy);
        float alignmentTargetPerceived = Mathf.Lerp(sounds.radarMinPerceivedVolume, sounds.radarMaxPerceivedVolume, accuracy);

        // Engagement gate: silent until first grab, and fades to 0 once the player has been
        // idle past the configured timeout. Holding a handle keeps the timer pinned to now.
        bool anyHeldNow = IsAnyHandleGrabbed();
        if (anyHeldNow)
            _lastEngagedTime = Time.time;

        float timeout = Mathf.Max(0f, sounds.radarIdleTimeoutSeconds);
        bool withinIdleWindow = timeout <= 0f || (Time.time - _lastEngagedTime) < timeout;
        bool active = _anyHandleEverGrabbed && withinIdleWindow;

        // Exponential chase for pitch at all times — when volume is 0 it's inaudible anyway, and
        // chasing keeps it synced for the instant re-entry on the next grab.
        float k = 1f - Mathf.Exp(-sounds.radarResponseSpeed * Time.deltaTime);
        _radarPitchCurrent = Mathf.Lerp(_radarPitchCurrent, alignmentTargetPitch, k);

        if (active)
        {
            // Normal alignment-driven volume, smoothly chased.
            _radarPerceivedCurrent = Mathf.Lerp(_radarPerceivedCurrent, alignmentTargetPerceived, k);
        }
        else
        {
            // Either pre-first-touch (hard 0) or idle-fade-out (linear ramp to 0 over
            // radarIdleFadeOutDuration). Using MoveTowards in perceived space gives a
            // predictable fade length independent of the current level.
            float fadeDuration = Mathf.Max(0.01f, sounds.radarIdleFadeOutDuration);
            float step = (1f / fadeDuration) * Time.deltaTime;
            if (!_anyHandleEverGrabbed)
                _radarPerceivedCurrent = 0f;
            else
                _radarPerceivedCurrent = Mathf.MoveTowards(_radarPerceivedCurrent, 0f, step);
        }

        ApplyRadarToSource();

        if (alignment != null && alignment.IsComplete)
            StartRadarFadeOut();
    }

    private bool IsAnyHandleGrabbed()
    {
        if (alignment == null) return false;
        RadarSlider pos = alignment.PositionSlider;
        RadarSlider ang = alignment.AngleSlider;
        return (pos != null && pos.IsGrabbed) || (ang != null && ang.IsGrabbed);
    }

    private void ApplyRadarToSource()
    {
        if (_radarSource == null) return;
        _radarSource.pitch = _radarPitchCurrent;
        _radarSource.volume = AudioVolume.ToLinear(
            Mathf.Clamp01(_radarPerceivedCurrent * sounds.radarLoop.Volume));
    }

    private void StartRadarFadeOut()
    {
        if (_radarFadingOut || _radarSource == null) return;
        _radarFadingOut = true;
        StartCoroutine(FadeOutAndKill(_radarSource, sounds.radarCompleteFadeOut));
        _radarSource = null;
    }

    // ---------- Handle movement loop ----------

    private void SpawnMoveLoop()
    {
        if (sounds == null || sounds.moveLoop == null || sounds.moveLoop.Clip == null)
            return;

        _moveSource = SpawnLoopSource("RadarAlignment_MoveLoop", sounds.moveLoop.Clip);
        if (_moveSource == null) return;

        _moveSource.pitch = sounds.movePitch;
        _moveSource.volume = 0f;
        _movePerceivedCurrent = 0f;
        _moveSource.Play();
    }

    private void UpdateMoveLoop()
    {
        if (_moveSource == null || sounds.moveLoop == null || sounds.moveLoop.Clip == null)
            return;

        // Whichever slider is moving faster wins — this avoids sudden drops when the player hands
        // off between handles. Completion silences the loop so the success moment is clean.
        float rate = 0f;
        if (alignment != null && !alignment.IsComplete)
        {
            RadarSlider pos = alignment.PositionSlider;
            RadarSlider ang = alignment.AngleSlider;
            if (pos != null) rate = Mathf.Max(rate, pos.ValueChangeRate);
            if (ang != null) rate = Mathf.Max(rate, ang.ValueChangeRate);
        }

        // Any movement at all triggers the base volume; the boost layer only activates once the
        // scrub speed approaches moveSpeedForFullBoost, so a gentle drag sits at base level while a
        // fast fling punches in with the extra boost.
        bool moving = rate > 0.0005f;
        float boostT = Mathf.Clamp01(rate / Mathf.Max(0.0001f, sounds.moveSpeedForFullBoost));
        float targetPerceived = moving
            ? Mathf.Clamp01(sounds.moveBasePerceivedVolume + sounds.moveBoostExtraPerceivedVolume * boostT)
            : 0f;

        float speed = targetPerceived > _movePerceivedCurrent ? sounds.moveFadeInSpeed : sounds.moveFadeOutSpeed;
        _movePerceivedCurrent = Mathf.MoveTowards(_movePerceivedCurrent, targetPerceived, speed * Time.deltaTime);

        _moveSource.pitch = sounds.movePitch;
        _moveSource.volume = AudioVolume.ToLinear(
            Mathf.Clamp01(_movePerceivedCurrent * sounds.moveLoop.Volume));
    }

    // ---------- Grab / release one-shots ----------

    private void SubscribeSlider(RadarSlider slider)
    {
        if (slider == null) return;
        slider.OnGrabbed += HandleSliderGrabbed;
        slider.OnReleased += HandleSliderReleased;
    }

    private void UnsubscribeSlider(RadarSlider slider)
    {
        if (slider == null) return;
        slider.OnGrabbed -= HandleSliderGrabbed;
        slider.OnReleased -= HandleSliderReleased;
    }

    private void HandleSliderGrabbed(RadarSlider slider)
    {
        if (sounds == null || slider == null) return;

        // Arm the engagement gate. First ever grab unmutes the radar loop; every subsequent grab
        // resets the idle timer. We also SNAP the current envelope to the alignment-driven target
        // so the next frame reads as an instant bring-back (the user-facing promise is "touch a
        // handle → radar is back right now", not "touch a handle → radar fades up").
        _anyHandleEverGrabbed = true;
        _lastEngagedTime = Time.time;
        SnapRadarEnvelopeToAlignmentTarget();

        if (sounds.handleGrab != null && sounds.handleGrab.HasAnyClip)
            sounds.handleGrab.PlayAt(slider.HandleWorldPosition);
    }

    private void SnapRadarEnvelopeToAlignmentTarget()
    {
        if (_radarSource == null || sounds == null) return;
        if (_radarFadingOut) return;

        float error = alignment != null ? alignment.AlignmentError01 : 1f;
        float accuracy = 1f - Mathf.Clamp01(error);
        _radarPitchCurrent = Mathf.Lerp(sounds.radarMinPitch, sounds.radarMaxPitch, accuracy);
        _radarPerceivedCurrent = Mathf.Lerp(sounds.radarMinPerceivedVolume, sounds.radarMaxPerceivedVolume, accuracy);
        ApplyRadarToSource();
    }

    private void HandleSliderReleased(RadarSlider slider)
    {
        if (sounds == null || slider == null) return;
        if (sounds.handleRelease != null && sounds.handleRelease.HasAnyClip)
            sounds.handleRelease.PlayAt(slider.HandleWorldPosition);
    }

    // ---------- Loop source plumbing ----------

    // Builds a dedicated looping AudioSource parented under loopSourcesParent. Routes through the
    // same mixer group as AudioManager.sfxSource so master-volume / bus settings apply — matches
    // the pattern used by PowerDownSequence for its ambient loops.
    private AudioSource SpawnLoopSource(string name, AudioClip clip)
    {
        if (clip == null) return null;

        GameObject go = new GameObject(name);
        go.transform.SetParent(loopSourcesParent != null ? loopSourcesParent : transform, false);
        go.transform.localPosition = Vector3.zero;

        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = loopSpatialBlend;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = loopMinDistance;
        src.maxDistance = loopMaxDistance;
        src.volume = 0f;
        src.pitch = 1f;

        AudioSource template = AudioManager.Instance != null ? AudioManager.Instance.sfxSource : null;
        if (template != null)
        {
            src.outputAudioMixerGroup = template.outputAudioMixerGroup;
            src.bypassEffects = template.bypassEffects;
            src.bypassListenerEffects = template.bypassListenerEffects;
            src.bypassReverbZones = template.bypassReverbZones;
        }

        return src;
    }

    private static void KillLoop(ref AudioSource src)
    {
        if (src == null) return;
        if (src.gameObject != null) Destroy(src.gameObject);
        src = null;
    }

    private System.Collections.IEnumerator FadeOutAndKill(AudioSource src, float duration)
    {
        if (src == null) yield break;
        float start = src.volume;
        float safeDuration = Mathf.Max(0.01f, duration);
        float t = 0f;
        while (t < safeDuration && src != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / safeDuration);
            src.volume = AudioVolume.LerpAmplitudePerceived(start, 0f, k);
            yield return null;
        }
        if (src != null && src.gameObject != null)
            Destroy(src.gameObject);
    }
}
