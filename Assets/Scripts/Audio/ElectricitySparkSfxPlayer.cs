using UnityEngine;

/// <summary>
/// Drives ambient electricity audio for a single emitter (e.g. the <c>electricity.prefab</c>
/// particle effect). Attach to the same GameObject as (or a child of) the particle system.
///
/// Design goals (see ElectricitySparkSounds for the full rationale):
///   1) A softly looping 3D bed AudioSource provides the "there is electricity here" ambience
///      for ~70% of the audible surface at ~0% trigger cost.
///   2) A jittered timer fires discrete crackle one-shots at a human-friendly rate (sub-Hz up
///      to ~2 Hz), *decoupled from the particle emission rate*, with position scatter, pitch
///      jitter, and volume jitter -- so the pops feel like different sparks in different places
///      instead of a machine-gunning point source.
///   3) All audio is hard-gated behind <see cref="CutsceneManager.IntroComplete"/> so the
///      scripted intro sequence (powerdown / wake) plays in deliberate silence; the electricity
///      only starts being audible once the player is handed control. Matches the pattern used
///      by <c>MachineryAmbientDirector</c>. Additionally an optional <see cref="emissionGate"/>
///      ParticleSystem can be wired in to silence everything while it is not emitting.
/// </summary>
[DisallowMultipleComponent]
public class ElectricitySparkSfxPlayer : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Shared audio config asset. Multiple electricity emitters can reference the same " +
             "ScriptableObject to keep them sonically consistent.")]
    [SerializeField] private ElectricitySparkSounds sounds;

    [Header("Optional Gating")]
    [Tooltip("If assigned, crackle triggers and the bed loop are silenced whenever this " +
             "ParticleSystem is NOT emitting (e.g. via emission.enabled = false). Leave null to " +
             "have the audio simply follow this component's own enabled state.")]
    [SerializeField] private ParticleSystem emissionGate;

    private AudioSource _bedSource;
    private float _crackleTimer;

    private void OnEnable()
    {
        if (sounds == null)
        {
            enabled = false;
            return;
        }

        SpawnBedLoop();
        _crackleTimer = Random.Range(0f, Mathf.Max(0f, sounds.crackleStartupDelayMax));
    }

    private void OnDisable()
    {
        if (_bedSource != null)
        {
            Destroy(_bedSource.gameObject);
            _bedSource = null;
        }
    }

    private void Update()
    {
        if (sounds == null || AudioManager.Instance == null)
            return;

        // Hard gate: audio stays fully silent until the opening cutscene has finished. The intro
        // is a scripted silent sequence -- letting the sparks crackle under it would fight the
        // directed pacing. This mirrors the way MachineryAmbientDirector gates ambient triggers.
        bool introDone = CutsceneManager.IntroComplete;
        bool emitterActive = emissionGate == null || emissionGate.isEmitting;
        bool gateOpen = introDone && emitterActive;

        UpdateBedLoopVolume(gateOpen);

        if (!gateOpen || !sounds.HasAnyCrackle)
        {
            // Hold the timer at ~0 so the next crackle lands quickly after the gate reopens,
            // but don't let it accumulate into a burst of missed triggers while silenced.
            _crackleTimer = Mathf.Min(_crackleTimer, sounds.crackleMinInterval);
            return;
        }

        _crackleTimer -= Time.deltaTime;
        if (_crackleTimer > 0f)
            return;

        // Sample a scatter position in a small sphere around the emitter. Using insideUnitSphere
        // (not onUnitSphere) keeps some pops closer to center and some at the edge, which plays
        // nicer than ring-like placement.
        Vector3 scatter = Random.insideUnitSphere * Mathf.Max(0f, sounds.crackleScatterRadius);
        sounds.crackles.PlayAt(transform.position + scatter, sounds.crackleMinDistance, sounds.crackleMaxDistance);

        float minInt = Mathf.Max(0.01f, sounds.crackleMinInterval);
        float maxInt = Mathf.Max(minInt, sounds.crackleMaxInterval);
        _crackleTimer = Random.Range(minInt, maxInt);
    }

    // ---------- Bed loop plumbing ----------

    // Mirrors the pattern used by RadarAlignmentSounds.SpawnLoopSource: a dedicated child
    // AudioSource routed through the same mixer group as AudioManager.sfxSource so the master
    // volume / bus settings apply uniformly with the rest of the SFX stack.
    private void SpawnBedLoop()
    {
        if (sounds == null || !sounds.HasBedLoop)
            return;

        GameObject go = new GameObject($"ElectricityBedLoop_{sounds.bedLoop.name}");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = sounds.bedLoop;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = Mathf.Max(0.1f, sounds.bedMinDistance);
        src.maxDistance = Mathf.Max(src.minDistance + 0.1f, sounds.bedMaxDistance);
        src.pitch = 1f;
        src.volume = 0f;

        AudioSource template = AudioManager.Instance != null ? AudioManager.Instance.sfxSource : null;
        if (template != null)
        {
            src.outputAudioMixerGroup = template.outputAudioMixerGroup;
            src.bypassEffects = template.bypassEffects;
            src.bypassListenerEffects = template.bypassListenerEffects;
            src.bypassReverbZones = template.bypassReverbZones;
        }

        // Start playback at a random offset so multiple electricity emitters sharing the same
        // bedLoop clip don't phase-lock into a chorus-of-one effect.
        if (sounds.bedLoop.length > 0.1f)
            src.time = Random.Range(0f, sounds.bedLoop.length);

        src.Play();
        _bedSource = src;
    }

    private void UpdateBedLoopVolume(bool gateOpen)
    {
        if (_bedSource == null || sounds == null)
            return;

        AudioSource template = AudioManager.Instance != null ? AudioManager.Instance.sfxSource : null;
        float masterLinear = template != null ? template.volume : 1f;
        float bedLinear = AudioVolume.ToLinear(sounds.bedPerceivedVolume);
        _bedSource.volume = gateOpen ? masterLinear * bedLinear : 0f;
    }
}
