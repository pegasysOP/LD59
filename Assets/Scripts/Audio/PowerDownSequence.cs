using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the full station power-down + creature-reveal audio sequence
/// described in <c>_silas_design/sound/power-down-sequence.md</c>.
///
/// The sequence is timeline-based: on <see cref="Run"/> (or via
/// <see cref="runOnStart"/> / the context-menu item) a single coroutine walks
/// a cursor across six phases and fires <see cref="SfxBank"/> triggers and
/// looping <see cref="AudioSource"/>s at authored offsets. Every trigger is
/// guarded so unassigned or empty banks / clips are silently skipped - the
/// sequence plays whatever assets currently exist, which means assets can be
/// added progressively without breaking anything.
///
/// Phases (from the design doc):
///   1. Interaction (0.0s)  - button press + mechanical ack
///   2. Door opening (0.5s) - motor start, track rattle, slide loop, end thunk
///   3. Power collapse (2.0s) - dropout, flickers, relays, hum pitch-down
///   4. Silence gap (3.5s) - residual low hum + distant creak, nothing more
///   5. Chaos (4.2s) - Wave A impact+creature, B humans/alarms, C creature
///      escalation, D environment instability
///   6. Residual state (8.5s+) - low-power ambience + distant creature loops
///      with intermittent failure events
///
/// One-shots route through <see cref="AudioManager.Instance"/> via
/// <see cref="SfxBank"/>, so they inherit the project's master volume and
/// perceived-loudness curve. Loop sources are parented under this component
/// and routed through the same mixer group as <see cref="AudioManager.sfxSource"/>;
/// fades use <see cref="AudioVolume"/> to keep the loudness curve consistent
/// with the rest of the audio stack.
/// </summary>
[DisallowMultipleComponent]
public class PowerDownSequence : MonoBehaviour
{
    // ---------- Phase 1: Interaction ----------

    [Header("Phase 1 — Interaction (0.0s)")]
    [Tooltip("button_press_heavy_* - solid tactile button press from the design doc.")]
    public SfxBank buttonPress = new SfxBank { pitchMin = 0.97f, pitchMax = 1.03f };
    [Tooltip("system_ack_mechanical_* - mechanical acknowledgement that fires just after the press.")]
    public SfxBank systemAck = new SfxBank { pitchMin = 0.98f, pitchMax = 1.02f };
    [Tooltip("Seconds between the button press and the acknowledgement tick.")]
    [Min(0f)] public float systemAckDelay = 0.15f;

    // ---------- Phase 2: Door Opening ----------

    [Header("Phase 2 — Door Opening (0.5s → 2.0s)")]
    [Tooltip("Absolute sequence time at which the door motor kicks in.")]
    [Min(0f)] public float doorStartTime = 0.5f;
    [Tooltip("door_slide_start_* - motor spin-up / detent release.")]
    public SfxBank doorStart = new SfxBank { pitchMin = 0.97f, pitchMax = 1.03f };
    [Tooltip("door_track_rattle_* - mechanical chatter layered on top of the motor.")]
    public SfxBank doorTrackRattle = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };
    [Tooltip("door_slide_loop_* - sustained slide. Plays as a loop until doorEndTime.")]
    public AudioClip doorSlideLoop;
    [Tooltip("Perceived loudness (0-1) for the door slide loop.")]
    [Range(0f, 1f)] public float doorSlideLoopVolume = 0.7f;
    [Tooltip("Absolute sequence time at which the door thunk lands and the slide loop stops.")]
    [Min(0f)] public float doorEndTime = 2.0f;
    [Tooltip("door_slide_end_* - final clunk when the door finishes travelling.")]
    public SfxBank doorEnd = new SfxBank { pitchMin = 0.96f, pitchMax = 1.02f };

    // ---------- Phase 3: Power Collapse ----------

    [Header("Phase 3 — Power Collapse (2.0s → 3.5s)")]
    [Tooltip("Absolute sequence time at which power begins to collapse.")]
    [Min(0f)] public float powerCollapseStartTime = 2.0f;
    [Tooltip("power_dropout_main_* - primary dropout hit. The hard transition moment.")]
    public SfxBank powerDropout = new SfxBank { pitchMin = 0.98f, pitchMax = 1.02f };
    [Tooltip("electrical_flicker_* - fast flicker bursts scattered across the collapse window.")]
    public SfxBank flickerBursts = new SfxBank { pitchMin = 0.9f, pitchMax = 1.1f };
    [Tooltip("How many flicker bursts to fire across the collapse window.")]
    [Min(0)] public int flickerBurstCount = 4;
    [Tooltip("Offset window (seconds, relative to powerCollapseStartTime) over which flickers are scattered.")]
    public Vector2 flickerBurstWindow = new Vector2(0.0f, 1.2f);
    [Tooltip("relay_clicks_* - mechanical relay clicks peppered across the collapse.")]
    public SfxBank relayClicks = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };
    [Tooltip("How many relay clicks to fire across the collapse window.")]
    [Min(0)] public int relayClickCount = 5;
    [Tooltip("Offset window (seconds, relative to powerCollapseStartTime) over which relay clicks are scattered.")]
    public Vector2 relayClickWindow = new Vector2(0.1f, 1.3f);
    [Tooltip("system_hum_down_* - station hum that pitches down as power dies. Plays as a loop with pitch automation.")]
    public AudioClip systemHumDown;
    [Tooltip("Perceived loudness (0-1) for the hum at the start of the collapse.")]
    [Range(0f, 1f)] public float systemHumDownVolume = 0.6f;
    [Tooltip("Starting playback pitch for the collapsing hum.")]
    [Range(0.05f, 2f)] public float humDownStartPitch = 1.0f;
    [Tooltip("Final playback pitch for the hum just before the silence gap.")]
    [Range(0.05f, 2f)] public float humDownEndPitch = 0.25f;
    [Tooltip("Duration of the pitch-down ramp. The hum fades out during the silence gap.")]
    [Min(0.05f)] public float humDownDuration = 1.3f;

    // ---------- Phase 4: Silence Gap ----------

    [Header("Phase 4 — Silence Gap (3.5s → 4.2s)")]
    [Tooltip("Absolute sequence time at which the silence gap begins.")]
    [Min(0f)] public float silenceStartTime = 3.5f;
    [Tooltip("residual_hum_low_01 - optional, very quiet sustained hum.")]
    public AudioClip residualHumLow;
    [Tooltip("Perceived loudness (0-1) for the residual hum during the gap. Keep this LOW.")]
    [Range(0f, 1f)] public float residualHumVolume = 0.08f;
    [Tooltip("metal_creak_distant_* - single distant creak, fired late in the gap to sell tension.")]
    public SfxBank metalCreakDistant = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };
    [Tooltip("Seconds after the silence gap starts before the distant creak lands.")]
    [Min(0f)] public float metalCreakDelay = 0.35f;

    // ---------- Phase 5: Chaos ----------

    [Header("Phase 5 — Chaos: Wave A (4.2s) — Impact + first threat")]
    [Tooltip("Absolute sequence time at which the chaos begins (Wave A).")]
    [Min(0f)] public float chaosStartTime = 4.2f;
    [Tooltip("metal_impact_large_* - the big hit that breaks the silence.")]
    public SfxBank metalImpactLarge = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };
    [Tooltip("multi_contact_burst_* - creature burst that rides on top of the impact. Must be audible.")]
    public SfxBank creatureBurst = new SfxBank { pitchMin = 0.95f, pitchMax = 1.08f };
    [Tooltip("Seconds between the metal impact and the creature burst (keep tight - they should feel coupled).")]
    [Min(0f)] public float creatureBurstDelay = 0.12f;

    [Header("Phase 5 — Chaos: Wave B (5.0s) — World reacting")]
    [Tooltip("Absolute sequence time at which Wave B fires.")]
    [Min(0f)] public float waveBTime = 5.0f;
    [Tooltip("alarm_degraded_* - broken alarms spinning up.")]
    public SfxBank alarmDegraded = new SfxBank { pitchMin = 0.97f, pitchMax = 1.03f };
    [Tooltip("distant_scream_muffled_* - wide stereo, reverb-heavy human elements.")]
    public SfxBank distantScream = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };
    [Tooltip("comms_fragment_distorted_* - clipped radio fragments.")]
    public SfxBank commsFragment = new SfxBank { pitchMin = 0.97f, pitchMax = 1.03f };

    [Header("Phase 5 — Chaos: Wave C (6.0s) — Creature escalation")]
    [Tooltip("Absolute sequence time at which Wave C fires. Ducks other chaos layers.")]
    [Min(0f)] public float waveCTime = 6.0f;
    [Tooltip("vent_scramble_fast_* - frantic movement through vents. Strong L/R directional cue.")]
    public SfxBank ventScramble = new SfxBank { pitchMin = 0.95f, pitchMax = 1.08f };
    [Tooltip("wall_scrape_* - claws / hide on surfaces.")]
    public SfxBank wallScrape = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };
    [Tooltip("distant_movement_* - implied presence somewhere else in the ship.")]
    public SfxBank distantMovement = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };

    [Header("Phase 5 — Chaos: Wave D (7.0s) — Environment instability")]
    [Tooltip("Absolute sequence time at which Wave D fires.")]
    [Min(0f)] public float waveDTime = 7.0f;
    [Tooltip("air_leak_* - pressurised hiss.")]
    public SfxBank airLeak = new SfxBank { pitchMin = 0.97f, pitchMax = 1.03f };
    [Tooltip("loose_object_rattle_* - props reacting to the instability.")]
    public SfxBank looseObjectRattle = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };
    [Tooltip("electrical_arc_* - sparking electrical discharges.")]
    public SfxBank electricalArc = new SfxBank { pitchMin = 0.96f, pitchMax = 1.06f };

    // ---------- Phase 6: Residual ----------

    [Header("Phase 6 — Residual State (8.5s+)")]
    [Tooltip("Absolute sequence time at which the residual ambient bed fades in.")]
    [Min(0f)] public float residualStartTime = 8.5f;
    [Tooltip("low_power_ambience_loop - the new gameplay ambient bed.")]
    public AudioClip lowPowerAmbienceLoop;
    [Tooltip("Perceived loudness (0-1) for the low-power ambience bed.")]
    [Range(0f, 1f)] public float lowPowerAmbienceVolume = 0.6f;
    [Tooltip("distant_creature_loop - implied creature presence under the ambience.")]
    public AudioClip distantCreatureLoop;
    [Tooltip("Perceived loudness (0-1) for the distant creature loop.")]
    [Range(0f, 1f)] public float distantCreatureVolume = 0.35f;
    [Tooltip("Fade-in duration for the residual ambient loops.")]
    [Min(0.01f)] public float residualFadeIn = 1.5f;
    [Tooltip("intermittent_failure_events - one-shots sprinkled on top of the residual state " +
             "(creaks, sparks, distant thumps). Leave empty to disable.")]
    public SfxBank intermittentFailureEvents = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };
    [Tooltip("Random delay (seconds) between intermittent failure events. X = min, Y = max.")]
    public Vector2 intermittentFailureInterval = new Vector2(5f, 12f);

    // ---------- Debug / Triggering ----------

    [Header("Debug")]
    [Tooltip("If true, runs the sequence automatically in Start. Useful for iteration.")]
    public bool runOnStart = false;

    // ---------- Runtime State ----------

    private Coroutine _runningSequence;
    private Coroutine _intermittentFailureCoroutine;
    private AudioSource _humSource;
    private AudioSource _residualHumSource;
    private AudioSource _doorLoopSource;
    private AudioSource _lowPowerAmbienceSource;
    private AudioSource _distantCreatureSource;

    // ---------- Public API ----------

    private void Start()
    {
        if (runOnStart)
            Run();
    }

    private void OnDisable()
    {
        Stop();
    }

    /// <summary>Fires the full power-down sequence from the top. Safe to call even if another run is in flight.</summary>
    [ContextMenu("Run Sequence")]
    public void Run()
    {
        Stop();
        _runningSequence = StartCoroutine(RunSequence());
    }

    /// <summary>Cancels any in-flight sequence and silences every loop spawned by this component.</summary>
    [ContextMenu("Stop Sequence")]
    public void Stop()
    {
        if (_runningSequence != null) { StopCoroutine(_runningSequence); _runningSequence = null; }
        if (_intermittentFailureCoroutine != null) { StopCoroutine(_intermittentFailureCoroutine); _intermittentFailureCoroutine = null; }
        KillLoop(ref _humSource);
        KillLoop(ref _residualHumSource);
        KillLoop(ref _doorLoopSource);
        KillLoop(ref _lowPowerAmbienceSource);
        KillLoop(ref _distantCreatureSource);
    }

    // ---------- Sequence ----------

    private IEnumerator RunSequence()
    {
        float cursor = 0f;

        // --- Phase 1: Interaction ---
        TryPlay(buttonPress);
        if (systemAckDelay > 0f) StartCoroutine(DelayThenPlay(systemAckDelay, systemAck));
        else TryPlay(systemAck);

        // --- Phase 2: Door Opening ---
        yield return WaitTo(ref cursor, doorStartTime);
        TryPlay(doorStart);
        TryPlay(doorTrackRattle);
        _doorLoopSource = StartLoop(doorSlideLoop, doorSlideLoopVolume, fadeInDuration: 0.15f, loop: true);

        yield return WaitTo(ref cursor, doorEndTime);
        FadeOutAndKill(ref _doorLoopSource, 0.2f);
        TryPlay(doorEnd);

        // --- Phase 3: Power Collapse ---
        yield return WaitTo(ref cursor, powerCollapseStartTime);
        TryPlay(powerDropout);
        StartCoroutine(ScatterPlays(flickerBursts, flickerBurstCount, flickerBurstWindow));
        StartCoroutine(ScatterPlays(relayClicks, relayClickCount, relayClickWindow));
        _humSource = StartLoop(systemHumDown, systemHumDownVolume, fadeInDuration: 0.05f, loop: true);
        if (_humSource != null)
        {
            _humSource.pitch = humDownStartPitch;
            StartCoroutine(RampPitch(_humSource, humDownStartPitch, humDownEndPitch, humDownDuration));
        }

        // --- Phase 4: Silence Gap ---
        yield return WaitTo(ref cursor, silenceStartTime);
        FadeOutAndKill(ref _humSource, 0.3f);
        _residualHumSource = StartLoop(residualHumLow, residualHumVolume, fadeInDuration: 0.2f, loop: true);
        if (metalCreakDelay > 0f) StartCoroutine(DelayThenPlay(metalCreakDelay, metalCreakDistant));
        else TryPlay(metalCreakDistant);

        // --- Phase 5: Chaos ---
        // Wave A: impact + creature burst.
        yield return WaitTo(ref cursor, chaosStartTime);
        FadeOutAndKill(ref _residualHumSource, 0.15f);
        TryPlay(metalImpactLarge);
        if (creatureBurstDelay > 0f) StartCoroutine(DelayThenPlay(creatureBurstDelay, creatureBurst));
        else TryPlay(creatureBurst);

        // Wave B: world reacting.
        yield return WaitTo(ref cursor, waveBTime);
        TryPlay(alarmDegraded);
        TryPlay(distantScream);
        TryPlay(commsFragment);

        // Wave C: creature escalation.
        yield return WaitTo(ref cursor, waveCTime);
        TryPlay(ventScramble);
        TryPlay(wallScrape);
        TryPlay(distantMovement);

        // Wave D: environment instability.
        yield return WaitTo(ref cursor, waveDTime);
        TryPlay(airLeak);
        TryPlay(looseObjectRattle);
        TryPlay(electricalArc);

        // --- Phase 6: Residual State ---
        yield return WaitTo(ref cursor, residualStartTime);
        _lowPowerAmbienceSource = StartLoop(lowPowerAmbienceLoop, lowPowerAmbienceVolume, fadeInDuration: residualFadeIn, loop: true);
        _distantCreatureSource = StartLoop(distantCreatureLoop, distantCreatureVolume, fadeInDuration: residualFadeIn, loop: true);
        if (intermittentFailureEvents != null && intermittentFailureEvents.HasAnyClip)
            _intermittentFailureCoroutine = StartCoroutine(IntermittentFailureLoop());

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

    private IEnumerator ScatterPlays(SfxBank bank, int count, Vector2 window)
    {
        if (bank == null || !bank.HasAnyClip || count <= 0) yield break;
        float a = Mathf.Max(0f, Mathf.Min(window.x, window.y));
        float b = Mathf.Max(a, Mathf.Max(window.x, window.y));

        // Sample `count` absolute offsets in [a, b] and play them in chronological order.
        float[] offsets = new float[count];
        for (int i = 0; i < count; i++) offsets[i] = Random.Range(a, b);
        System.Array.Sort(offsets);

        float elapsed = 0f;
        for (int i = 0; i < count; i++)
        {
            float wait = Mathf.Max(0f, offsets[i] - elapsed);
            if (wait > 0f) yield return new WaitForSeconds(wait);
            elapsed = offsets[i];
            bank.Play();
        }
    }

    private IEnumerator IntermittentFailureLoop()
    {
        float a = Mathf.Max(0.25f, Mathf.Min(intermittentFailureInterval.x, intermittentFailureInterval.y));
        float b = Mathf.Max(a + 0.01f, Mathf.Max(intermittentFailureInterval.x, intermittentFailureInterval.y));
        while (intermittentFailureEvents != null && intermittentFailureEvents.HasAnyClip)
        {
            yield return new WaitForSeconds(Random.Range(a, b));
            intermittentFailureEvents.Play();
        }
        _intermittentFailureCoroutine = null;
    }

    private static void TryPlay(SfxBank bank)
    {
        if (bank == null || !bank.HasAnyClip) return;
        bank.Play();
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

    private void FadeOutAndKill(ref AudioSource src, float duration)
    {
        if (src == null) return;
        StartCoroutine(FadeOutAndDestroy(src, duration));
        src = null;
    }

    private static void KillLoop(ref AudioSource src)
    {
        if (src == null) return;
        if (src.gameObject != null) Destroy(src.gameObject);
        src = null;
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

    private IEnumerator FadeOutAndDestroy(AudioSource src, float duration)
    {
        if (src == null) yield break;
        float startVolume = src.volume;
        float t = 0f;
        while (t < duration && src != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            src.volume = AudioVolume.LerpAmplitudePerceived(startVolume, 0f, k);
            yield return null;
        }
        if (src != null && src.gameObject != null) Destroy(src.gameObject);
    }

    private IEnumerator RampPitch(AudioSource src, float fromPitch, float toPitch, float duration)
    {
        if (src == null) yield break;
        float t = 0f;
        while (t < duration && src != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            src.pitch = Mathf.Lerp(fromPitch, toPitch, k);
            yield return null;
        }
        if (src != null) src.pitch = toPitch;
    }
}
