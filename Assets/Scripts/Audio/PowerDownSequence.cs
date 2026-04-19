using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the Tier 0 ("skeleton") pass of the station power-down + creature-reveal
/// audio sequence described in <c>_silas_design/sound/power-down-sequence.md</c>.
///
/// This is the minimum-viable arc: one sound per phase, every phase independently
/// optional. On <see cref="Run"/> (or via <see cref="runOnStart"/> / the context-menu
/// item) a single coroutine walks a cursor across firing moments and one sustaining
/// loop. Every trigger is guarded so unassigned banks / clips are silently skipped -
/// assets can be dropped in progressively without breaking the sequence.
///
/// Phases owned by this component:
///   Stinger (parallel)           - <see cref="stinger"/> one-shot on the UI channel,
///                                  fired stingerDelay seconds after Run(). Does NOT
///                                  advance the timeline.
///   0. Pre-collapse ambience     - <see cref="preCollapseAmbienceLoop"/> plays continuously
///                                  (started on enable, faded out at phase 3)
///   3. Power collapse (2.0s)     - <see cref="powerDropout"/> hit + music suspended
///                                  + pre-collapse ambience fades out
///   4. Silence gap (3.5s)        - intentionally empty
///   5. Chaos / reveal (4.2s)     - <see cref="metalImpactLarge"/> + <see cref="creatureBurst"/>
///   6. Residual state (8.5s+)   - <see cref="lowPowerAmbienceLoop"/> fades in
///                                  + game music resumes
///
/// Phases 1 (button press) and 2 (door opening) are owned by <see cref="Door"/>
/// so each door can carry its own clip and still fire button/door audio even when
/// no <see cref="PowerDownSequence"/> is wired up. This component's internal clock
/// still starts at 0 = <see cref="Run"/>, which corresponds to ~0.5s after the
/// door-interact when launched via the door's powerDownStartDelay.
///
/// One-shots route through <see cref="AudioManager.Instance"/> via
/// <see cref="SfxBank"/>, so they inherit the project's master volume and
/// perceived-loudness curve. The residual loop is parented under this component
/// and routed through the same mixer group as <see cref="AudioManager.sfxSource"/>.
///
/// Future tiers (extra door detail, hum pitch-down, chaos waves B/C/D, intermittent
/// failures, etc.) are tracked in <c>_silas_design/sound/power-down-sound-assets-todo-order.md</c>.
/// </summary>
[DisallowMultipleComponent]
public class PowerDownSequence : MonoBehaviour
{
    // ---------- Stinger (UI channel, parallel to the timeline) ----------

    [Header("Stinger (UI channel, parallel to timeline)")]
    [Tooltip("One-shot stinger fired on Run() via the UI audio channel. Runs in parallel - it does " +
             "NOT advance the power-down timeline, so its duration is irrelevant to phase scheduling. " +
             "Leave the bank empty to disable.")]
    public SfxBank stinger = new SfxBank { pitchMin = 1f, pitchMax = 1f };
    [Tooltip("Seconds after Run() before the stinger plays. 0 = fire immediately with the sequence.")]
    [Min(0f)] public float stingerDelay = 0f;

    // ---------- Phase 0: Pre-Collapse Ambience ----------

    [Header("Phase 0 — Pre-Collapse Ambience (2D, looping)")]
    [Tooltip("Looping 2D stereo ambient bed that plays BEFORE the power goes down. " +
             "Starts when this component is enabled, fades out during Phase 3.")]
    public AudioClip preCollapseAmbienceLoop;
    [Tooltip("Perceived loudness (0-1) of the pre-collapse ambient bed. Keep low - this is a room-tone pad.")]
    [Range(0f, 1f)] public float preCollapseAmbienceVolume = 0.25f;
    [Tooltip("Seconds to fade the pre-collapse ambient bed up from silence when it first starts.")]
    [Min(0f)] public float preCollapseAmbienceFadeIn = 2.0f;
    [Tooltip("Seconds to fade the pre-collapse ambient bed out once Phase 3 fires.")]
    [Min(0.01f)] public float preCollapseAmbienceFadeOut = 1.2f;

    // ---------- Phase 3: Power Collapse ----------

    [Header("Phase 3 — Power Collapse (2.0s)")]
    [Tooltip("Absolute sequence time at which power dies.")]
    [Min(0f)] public float powerCollapseStartTime = 2.0f;
    [Tooltip("power_dropout_main_* - primary dropout hit. The hard transition moment.")]
    public SfxBank powerDropout = new SfxBank { pitchMin = 0.98f, pitchMax = 1.02f };
    [Tooltip("Seconds over which to fade the game music out when power collapses. Music stays " +
             "suspended until Phase 6 resumes it, so GameMusicGuy won't restart it mid-sequence.")]
    [Min(0.01f)] public float musicFadeOutDuration = 1.2f;

    // ---------- Phase 4: Silence Gap ----------

    [Header("Phase 4 — Silence Gap (3.5s)")]
    [Tooltip("Absolute sequence time at which the silence gap begins. " +
             "Intentionally empty in Tier 0 - silence IS the content here.")]
    [Min(0f)] public float silenceStartTime = 3.5f;

    // ---------- Phase 5: Chaos Wave A (reveal) ----------

    [Header("Phase 5 — Chaos Wave A (4.2s) — Reveal")]
    [Tooltip("Absolute sequence time at which the reveal fires.")]
    [Min(0f)] public float chaosStartTime = 4.2f;
    [Tooltip("metal_impact_large_* - the big hit that breaks the silence.")]
    public SfxBank metalImpactLarge = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };
    [Tooltip("multi_contact_burst_* - creature burst that rides on top of the impact. Must be audible.")]
    public SfxBank creatureBurst = new SfxBank { pitchMin = 0.95f, pitchMax = 1.08f };
    [Tooltip("Seconds between the metal impact and the creature burst (keep tight - they should feel coupled).")]
    [Min(0f)] public float creatureBurstDelay = 0.12f;

    [Header("Phase 5 — Spatial Placement (listener-followed)")]
    [Tooltip("If true, Phase 5 reveal sounds spawn as 3D sources parented to the camera (listener) at a random " +
             "offset, so they pan/attenuate as if 'over there' while still being clearly audible. Disable to " +
             "fall back to the 2D non-diegetic pipe.")]
    public bool chaosSpatialized = true;
    [Tooltip("Horizontal distance range (meters) from the listener at which the reveal sounds spawn.")]
    [Min(0f)] public float chaosMinDistance = 3f;
    [Min(0f)] public float chaosMaxDistance = 5f;
    [Tooltip("Height offset range relative to listener (meters). Negative = below ear height, positive = above.")]
    public Vector2 chaosHeightRange = new Vector2(-0.3f, 0.6f);
    [Tooltip("Half-width of the angular window for the metal impact (degrees off the forward axis, mirrored " +
             "left/right). 0 = straight ahead, 90 = hard sides, 180 = directly behind. 75° keeps the impact " +
             "front/side of the listener.")]
    [Range(0f, 180f)] public float metalImpactAngleMax = 75f;
    [Tooltip("Angular window (degrees off forward, mirrored) where the creature burst is allowed to spawn. " +
             "Bias behind the listener (e.g. 90-180) for maximum unease.")]
    [Range(0f, 180f)] public float creatureBurstAngleMin = 90f;
    [Range(0f, 180f)] public float creatureBurstAngleMax = 180f;

    // ---------- Phase 6: Residual ----------

    [Header("Phase 6 — Residual State (8.5s+)")]
    [Tooltip("Absolute sequence time at which the residual ambient bed fades in.")]
    [Min(0f)] public float residualStartTime = 8.5f;
    [Tooltip("low_power_ambience_loop - the new gameplay ambient bed. Loops until Stop() is called.")]
    public AudioClip lowPowerAmbienceLoop;
    [Tooltip("Perceived loudness (0-1) for the low-power ambience bed.")]
    [Range(0f, 1f)] public float lowPowerAmbienceVolume = 0.6f;
    [Tooltip("Fade-in duration for the residual ambient loop.")]
    [Min(0.01f)] public float residualFadeIn = 1.5f;

    // ---------- Debug / Triggering ----------

    [Header("Debug")]
    [Tooltip("If true, runs the sequence automatically in Start. Useful for iteration.")]
    public bool runOnStart = false;

    [Tooltip("If true (default), the sequence can only be triggered once per play session. " +
             "Subsequent Run() calls are ignored until ResetLockout() is called. This prevents " +
             "the dropout / reveal SFX from double-firing when multiple Doors (or the context-menu " +
             "item) trigger the sequence more than once.")]
    public bool singleFire = true;

    // ---------- Runtime State ----------

    private Coroutine _runningSequence;
    private Coroutine _runningStinger;
    private AudioSource _lowPowerAmbienceSource;
    private AudioSource _preCollapseAmbienceSource;
    private bool _hasFired;
    private Transform _listenerCached;

    // ---------- Public API ----------

    private void OnEnable()
    {
        StartPreCollapseAmbience();
    }

    private void Start()
    {
        if (runOnStart)
            Run();
    }

    private void OnDisable()
    {
        Stop();
        KillLoop(ref _preCollapseAmbienceSource);
    }

    /// <summary>
    /// Fires the power-down sequence from the top. When <see cref="singleFire"/> is true (default)
    /// subsequent calls are ignored until <see cref="ResetLockout"/> runs, so repeated triggers
    /// (e.g. several Doors with triggerPowerDownOnInteract = true) don't double-fire the one-shots.
    /// </summary>
    [ContextMenu("Run Sequence")]
    public void Run()
    {
        if (singleFire && _hasFired)
            return;

        Stop();
        _hasFired = true;
        _runningSequence = StartCoroutine(RunSequence());
        _runningStinger = StartCoroutine(RunStinger());
    }

    /// <summary>Cancels any in-flight sequence and silences the residual ambience loop.</summary>
    [ContextMenu("Stop Sequence")]
    public void Stop()
    {
        if (_runningSequence != null) { StopCoroutine(_runningSequence); _runningSequence = null; }
        if (_runningStinger != null) { StopCoroutine(_runningStinger); _runningStinger = null; }
        KillLoop(ref _lowPowerAmbienceSource);
    }

    /// <summary>
    /// Clears the single-fire lockout so <see cref="Run"/> will play the sequence again. Call this
    /// from a level-reset or new-game path if the same <see cref="PowerDownSequence"/> instance
    /// needs to fire more than once per session.
    /// </summary>
    [ContextMenu("Reset Lockout")]
    public void ResetLockout()
    {
        _hasFired = false;
    }

    // ---------- Sequence ----------

    private IEnumerator RunSequence()
    {
        float cursor = 0f;

        // --- Phase 3: Power Collapse ---
        yield return WaitTo(ref cursor, powerCollapseStartTime);
        TryPlay(powerDropout);
        StartFadeOutAndDestroy(ref _preCollapseAmbienceSource, preCollapseAmbienceFadeOut);
        if (MusicManager.Instance != null)
            MusicManager.Instance.SuspendGameMusic(musicFadeOutDuration);

        // --- Phase 4: Silence Gap (intentionally empty in Tier 0) ---
        yield return WaitTo(ref cursor, silenceStartTime);

        // --- Phase 5: Chaos Wave A ---
        yield return WaitTo(ref cursor, chaosStartTime);
        PlayChaosBank(metalImpactLarge, 0f, metalImpactAngleMax);
        if (creatureBurstDelay > 0f) StartCoroutine(DelayThenPlayChaos(creatureBurstDelay, creatureBurst, creatureBurstAngleMin, creatureBurstAngleMax));
        else PlayChaosBank(creatureBurst, creatureBurstAngleMin, creatureBurstAngleMax);

        // --- Phase 6: Residual State ---
        yield return WaitTo(ref cursor, residualStartTime);
        _lowPowerAmbienceSource = StartLoop(lowPowerAmbienceLoop, lowPowerAmbienceVolume, fadeInDuration: residualFadeIn, loop: true);
        // Re-open the gate on GameMusicGuy; it will crossfade the post-intro track
        // back in over MusicManager.crossfadeDuration.
        if (MusicManager.Instance != null)
            MusicManager.Instance.ResumeGameMusic();

        _runningSequence = null;
    }

    // ---------- Scheduling helpers ----------

    // Advances the cursor to `absoluteTime` and yields the wait in between.
    // If we're already past the target, this yields zero seconds.
    private static WaitForSeconds WaitTo(ref float cursor, float absoluteTime)
    {
        float wait = Mathf.Max(0f, absoluteTime - cursor);
        cursor = Mathf.Max(cursor, absoluteTime);
        return new WaitForSeconds(wait);
    }

    private IEnumerator DelayThenPlay(float delay, SfxBank bank)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));
        TryPlay(bank);
    }

    private IEnumerator DelayThenPlayChaos(float delay, SfxBank bank, float angleMin, float angleMax)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));
        PlayChaosBank(bank, angleMin, angleMax);
    }

    // Fires a Phase 5 reveal bank either as a listener-attached 3D one-shot (the spatial path,
    // so the impact/creature read as "over there" with real stereo + reverb) or as the 2D fallback
    // when the listener can't be resolved or chaosSpatialized is off.
    private void PlayChaosBank(SfxBank bank, float angleMin, float angleMax)
    {
        if (bank == null || !bank.HasAnyClip) return;

        if (!chaosSpatialized)
        {
            bank.Play();
            return;
        }

        Transform listener = GetListener();
        if (listener == null)
        {
            bank.Play();
            return;
        }

        Vector3 offset = RandomListenerOffset(angleMin, angleMax);
        bank.PlayAttached(listener, offset);
    }

    private Transform GetListener()
    {
        if (_listenerCached != null) return _listenerCached;
        Camera cam = Camera.main;
        _listenerCached = cam != null ? cam.transform : null;
        return _listenerCached;
    }

    // Builds a camera-local offset: a random angle within [min,max] degrees off forward (randomly
    // flipped left/right so it lands on either side), a random distance within the chaos distance
    // range, plus a random height jitter. Z = forward, X = right (Unity camera-local convention).
    private Vector3 RandomListenerOffset(float angleMin, float angleMax)
    {
        float a = Mathf.Min(angleMin, angleMax);
        float b = Mathf.Max(angleMin, angleMax);
        float angleDeg = Random.Range(a, b);
        float sign = Random.value < 0.5f ? -1f : 1f;
        float rad = angleDeg * Mathf.Deg2Rad * sign;

        float dMin = Mathf.Min(chaosMinDistance, chaosMaxDistance);
        float dMax = Mathf.Max(chaosMinDistance, chaosMaxDistance);
        float dist = Random.Range(dMin, dMax);

        float hMin = Mathf.Min(chaosHeightRange.x, chaosHeightRange.y);
        float hMax = Mathf.Max(chaosHeightRange.x, chaosHeightRange.y);
        float height = Random.Range(hMin, hMax);

        return new Vector3(Mathf.Sin(rad) * dist, height, Mathf.Cos(rad) * dist);
    }

    // Runs in parallel to RunSequence. Waits stingerDelay seconds then plays the stinger
    // bank on AudioManager.uiSfxSource so the one-shot goes through the UI channel rather
    // than the SFX channel. Has zero effect on the main sequence timeline.
    private IEnumerator RunStinger()
    {
        if (stingerDelay > 0f)
            yield return new WaitForSeconds(stingerDelay);
        PlayOnUiChannel(stinger);
        _runningStinger = null;
    }

    private static void TryPlay(SfxBank bank)
    {
        if (bank == null || !bank.HasAnyClip) return;
        bank.Play();
    }

    private static void PlayOnUiChannel(SfxBank bank)
    {
        if (bank == null || !bank.HasAnyClip) return;
        if (AudioManager.Instance == null) return;
        AudioSource uiSource = AudioManager.Instance.uiSfxSource;
        if (uiSource == null) return;
        bank.PlayOnSource(uiSource);
    }

    // ---------- Loop source helpers ----------

    // Spawns a dedicated AudioSource parented under this component, routed through the
    // same mixer group as AudioManager.sfxSource so master-volume changes apply. Returns
    // null when the clip is missing, so callers can still store the result unconditionally.
    private AudioSource StartLoop(AudioClip clip, float perceivedVolume, float fadeInDuration, bool loop)
    {
        if (clip == null) return null;

        GameObject go = new GameObject($"PowerDownLoop_{clip.name}");
        go.transform.SetParent(transform, false);
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = loop;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.pitch = 1f;

        AudioSource template = AudioManager.Instance != null ? AudioManager.Instance.sfxSource : null;
        if (template != null)
        {
            src.outputAudioMixerGroup = template.outputAudioMixerGroup;
            src.bypassEffects = template.bypassEffects;
            src.bypassListenerEffects = template.bypassListenerEffects;
            src.bypassReverbZones = template.bypassReverbZones;
        }

        float target = AudioVolume.ToLinear(Mathf.Clamp01(perceivedVolume));
        if (fadeInDuration <= 0f)
        {
            src.volume = target;
        }
        else
        {
            src.volume = 0f;
            StartCoroutine(FadeVolume(src, 0f, target, fadeInDuration));
        }
        src.Play();
        return src;
    }

    private static void KillLoop(ref AudioSource src)
    {
        if (src == null) return;
        if (src.gameObject != null) Destroy(src.gameObject);
        src = null;
    }

    private void StartPreCollapseAmbience()
    {
        if (_preCollapseAmbienceSource != null) return;
        if (preCollapseAmbienceLoop == null) return;
        _preCollapseAmbienceSource = StartLoop(
            preCollapseAmbienceLoop,
            preCollapseAmbienceVolume,
            fadeInDuration: preCollapseAmbienceFadeIn,
            loop: true);
    }

    // Detaches ownership of `src` (clears the ref) and starts a coroutine that fades it
    // out in perceived space then destroys its GameObject. Safe to call with a null src.
    private void StartFadeOutAndDestroy(ref AudioSource src, float duration)
    {
        if (src == null) return;
        AudioSource captured = src;
        src = null;
        StartCoroutine(FadeOutAndDestroy(captured, duration));
    }

    private IEnumerator FadeOutAndDestroy(AudioSource src, float duration)
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

    private IEnumerator FadeVolume(AudioSource src, float from, float to, float duration)
    {
        if (src == null) yield break;
        float t = 0f;
        while (t < duration && src != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            src.volume = AudioVolume.LerpAmplitudePerceived(from, to, k);
            yield return null;
        }
        if (src != null) src.volume = to;
    }
}
