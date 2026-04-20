using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global scheduler for ambient machinery one-shots. Instead of every emitter running its own
/// timer (which risks drift, accidental overlap, and a "louder world = more emitters" coupling),
/// a single director ticks one interval and picks a random registered <see cref="MachineryAmbientEmitter"/>
/// to play from. This keeps the world's ambient density constant no matter how many emitters are
/// scattered through the level.
///
/// Responsibilities:
///   1. One randomized inter-trigger timer for the whole scene.
///   2. Weighted random emitter selection (with optional no-repeat).
///   3. Global concurrency cap, so we never stack more than <see cref="maxConcurrent"/> clips.
///   4. Silence until the opening cutscene ends (<see cref="CutsceneManager.IntroComplete"/>).
///
/// The director auto-spawns itself on first registration if no instance exists, so the minimal
/// setup is "drop emitter prefabs in the scene". Place one manually if you want to author the
/// timing/cap in the inspector.
/// </summary>
[DisallowMultipleComponent]
public class MachineryAmbientDirector : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Minimum seconds between ambient machinery triggers (scene-wide).")]
    [SerializeField, Min(0f)] private float minTimeBetween = 6f;
    [Tooltip("Maximum seconds between ambient machinery triggers (scene-wide).")]
    [SerializeField, Min(0f)] private float maxTimeBetween = 18f;
    [Tooltip("Random delay (0..this) before the first possible trigger, so nothing fires " +
             "the instant the cutscene ends.")]
    [SerializeField, Min(0f)] private float startupDelay = 4f;

    [Header("Concurrency")]
    [Tooltip("Hard cap on simultaneously playing machinery clips across the whole scene.")]
    [SerializeField, Min(1)] private int maxConcurrent = 2;
    [Tooltip("When at the cap, skip this tick and reschedule instead of busy-waiting. " +
             "Keeps the world feeling sparse rather than bursting the moment a slot frees.")]
    [SerializeField] private bool skipWhenAtCap = true;

    [Header("Selection")]
    [Tooltip("When 2+ emitters are registered, avoid picking the same emitter twice in a row.")]
    [SerializeField] private bool avoidRepeats = true;

    [Header("Gating")]
    [Tooltip("Stay silent until CutsceneManager.IntroComplete flips true.")]
    [SerializeField] private bool waitForIntroCutscene = true;

    public static MachineryAmbientDirector Instance { get; private set; }

    private static readonly List<MachineryAmbientEmitter> s_pending = new List<MachineryAmbientEmitter>();
    private readonly List<MachineryAmbientEmitter> _emitters = new List<MachineryAmbientEmitter>();

    private float _nextTickTime;
    private int _activeCount;
    private MachineryAmbientEmitter _lastPicked;

    public static void Register(MachineryAmbientEmitter emitter)
    {
        if (emitter == null) return;
        if (Instance == null)
        {
            // Queue until a director instance exists in the scene (or one is auto-spawned).
            if (!s_pending.Contains(emitter)) s_pending.Add(emitter);
            EnsureInstance();
        }
        if (Instance != null && !Instance._emitters.Contains(emitter))
        {
            Instance._emitters.Add(emitter);
        }
    }

    public static void Unregister(MachineryAmbientEmitter emitter)
    {
        if (emitter == null) return;
        s_pending.Remove(emitter);
        if (Instance != null)
        {
            Instance._emitters.Remove(emitter);
            if (Instance._lastPicked == emitter) Instance._lastPicked = null;
        }
    }

    private static void EnsureInstance()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("MachineryAmbientDirector (auto)");
        go.AddComponent<MachineryAmbientDirector>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // Drain any emitters that registered before we came online.
        for (int i = 0; i < s_pending.Count; i++)
        {
            MachineryAmbientEmitter e = s_pending[i];
            if (e != null && !_emitters.Contains(e)) _emitters.Add(e);
        }
        s_pending.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        _nextTickTime = Time.time + Random.Range(0f, Mathf.Max(0f, startupDelay));
    }

    private void Update()
    {
        // Stay silent until the intro cutscene ends. Keep rescheduling during the cutscene
        // so the first post-intro trigger lands somewhere in [min, max] after that point,
        // rather than firing the instant the flag flips.
        if (waitForIntroCutscene && !CutsceneManager.IntroComplete)
        {
            ScheduleNext();
            return;
        }
        if (Time.time < _nextTickTime) return;

        if (_activeCount >= maxConcurrent)
        {
            if (skipWhenAtCap) ScheduleNext();
            else _nextTickTime = Time.time + 0.25f;
            return;
        }

        MachineryAmbientEmitter picked = PickWeightedEmitter();
        if (picked == null)
        {
            ScheduleNext();
            return;
        }

        float duration = picked.Sounds.PlayOnSource(picked.Source, picked.PerceivedVolume);
        if (duration > 0f)
        {
            _activeCount++;
            _lastPicked = picked;
            StartCoroutine(ReleaseSlotAfter(duration));
        }
        ScheduleNext();
    }

    private MachineryAmbientEmitter PickWeightedEmitter()
    {
        float total = 0f;
        int playableCount = 0;
        for (int i = 0; i < _emitters.Count; i++)
        {
            MachineryAmbientEmitter e = _emitters[i];
            if (e == null || !e.IsPlayable) continue;
            if (avoidRepeats && e == _lastPicked && _emitters.Count > 1) continue;
            total += Mathf.Max(0f, e.Weight);
            playableCount++;
        }

        // Fallback: only candidate was the last-picked one. Allow it rather than play nothing.
        if (playableCount == 0 && _lastPicked != null && _lastPicked.IsPlayable)
        {
            return _lastPicked;
        }
        if (total <= 0f) return null;

        float roll = Random.value * total;
        float acc = 0f;
        for (int i = 0; i < _emitters.Count; i++)
        {
            MachineryAmbientEmitter e = _emitters[i];
            if (e == null || !e.IsPlayable) continue;
            if (avoidRepeats && e == _lastPicked && _emitters.Count > 1) continue;
            acc += Mathf.Max(0f, e.Weight);
            if (roll <= acc) return e;
        }
        return null;
    }

    private void ScheduleNext()
    {
        float min = Mathf.Min(minTimeBetween, maxTimeBetween);
        float max = Mathf.Max(minTimeBetween, maxTimeBetween);
        _nextTickTime = Time.time + Random.Range(min, max);
    }

    private IEnumerator ReleaseSlotAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _activeCount = Mathf.Max(0, _activeCount - 1);
    }
}
