using System;
using UnityEngine;

/// <summary>
/// Tracks a continuous 0-1 intensity value and exposes the current <see cref="IntensityLevel"/>
/// zone for UI, audio, and gameplay systems to query. Intensity slowly decays over time
/// at a configurable rate unless bumped by gameplay events.
/// </summary>
[DisallowMultipleComponent]
public class IntensityManager : MonoBehaviour
{
    private static IntensityManager _instance;

    /// <summary>
    /// Global accessor. Returns the existing instance if one has been placed in the scene,
    /// otherwise locates one, or as a last resort creates a hidden bootstrapping GameObject
    /// so gameplay systems can always rely on <c>IntensityManager.Instance</c> being non-null.
    /// </summary>
    public static IntensityManager Instance
    {
        get
        {
            if (_instance != null)
                return _instance;

#if UNITY_2023_1_OR_NEWER
            _instance = FindFirstObjectByType<IntensityManager>();
#else
            _instance = FindObjectOfType<IntensityManager>();
#endif
            if (_instance != null)
                return _instance;

            if (!Application.isPlaying)
                return null;

            GameObject go = new GameObject($"{nameof(IntensityManager)} (auto)");
            _instance = go.AddComponent<IntensityManager>();
            return _instance;
        }
    }

    [Header("State")]
    [SerializeField, Range(0f, 1f)] private float intensity = 0f;

    [Header("Decay")]
    [Tooltip("Intensity lost per second while idle. Default 0.0025 = 0.01 per 4 seconds.")]
    [SerializeField] private float decayPerSecond = 0.0025f;
    [Tooltip("Seconds of silence (no AddIntensity) required before decay begins.")]
    [SerializeField] private float decayStartDelay = 0f;

    [Header("Debug")]
    [SerializeField] private IntensityLevel currentLevelDebug;

    /// <summary>Raw intensity value in [0, 1].</summary>
    public float CurrentIntensity => intensity;

    /// <summary>Current discrete zone for the intensity value.</summary>
    public IntensityLevel CurrentLevel { get; private set; }

    /// <summary>Configurable decay rate. Units: intensity-per-second.</summary>
    public float DecayPerSecond
    {
        get => decayPerSecond;
        set => decayPerSecond = Mathf.Max(0f, value);
    }

    /// <summary>Fires with (previous, next) whenever the zone changes.</summary>
    public event Action<IntensityLevel, IntensityLevel> OnLevelChanged;

    /// <summary>Fires with the new intensity whenever the raw value changes.</summary>
    public event Action<float> OnIntensityChanged;

    private float lastAddTime = float.NegativeInfinity;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"Duplicate {nameof(IntensityManager)} detected on '{name}'. Destroying extra.");
            Destroy(this);
            return;
        }
        _instance = this;
        CurrentLevel = ComputeLevel(intensity);
        currentLevelDebug = CurrentLevel;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Update()
    {
        if (decayPerSecond <= 0f || intensity <= 0f)
            return;

        if (Time.time - lastAddTime < decayStartDelay)
            return;

        SetIntensity(intensity - decayPerSecond * Time.deltaTime);
    }

    /// <summary>Replaces the current intensity value (clamped to 0-1).</summary>
    public void SetIntensity(float value)
    {
        float clamped = Mathf.Clamp01(value);
        bool valueChanged = !Mathf.Approximately(intensity, clamped);
        intensity = clamped;

        IntensityLevel nextLevel = ComputeLevel(intensity);
        if (nextLevel != CurrentLevel)
        {
            IntensityLevel previous = CurrentLevel;
            CurrentLevel = nextLevel;
            currentLevelDebug = nextLevel;
            OnLevelChanged?.Invoke(previous, nextLevel);
        }

        if (valueChanged)
            OnIntensityChanged?.Invoke(intensity);
    }

    /// <summary>Adds to the current intensity value and resets the decay delay timer.</summary>
    public void AddIntensity(float amount)
    {
        if (amount > 0f)
            lastAddTime = Time.time;
        SetIntensity(intensity + amount);
    }

    /// <summary>Maps a 0-1 intensity value to the corresponding <see cref="IntensityLevel"/> zone.</summary>
    public static IntensityLevel ComputeLevel(float intensity01)
    {
        if (intensity01 >= 0.75f) return IntensityLevel.Overload;
        if (intensity01 >= 0.50f) return IntensityLevel.Intense;
        if (intensity01 >= 0.25f) return IntensityLevel.Elevated;
        return IntensityLevel.Anxiety;
    }
}
