using UnityEngine;

/// <summary>
/// A single machinery sound emitter in the world. Intentionally "dumb": it owns an
/// <see cref="AudioSource"/> and a <see cref="MachinerySounds"/> bank, and registers itself
/// with <see cref="MachineryAmbientDirector"/> when enabled. The director handles all timing,
/// selection, concurrency, and intro-cutscene gating globally, so scattering more emitters
/// through the level never makes the world louder or more frequent - it just gives the
/// director more places to pick from.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class MachineryAmbientEmitter : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Random clip bank this emitter pulls from when the director picks it.")]
    [SerializeField] private MachinerySounds sounds;
    [Tooltip("AudioSource the clips play through. Auto-filled from this GameObject.")]
    [SerializeField] private AudioSource source;

    [Header("Mix")]
    [Tooltip("Perceived-loudness multiplier applied on top of the clip+bank volume. " +
             "Use this to make a specific emitter quieter without editing the shared bank.")]
    [SerializeField, Range(0f, 1f)] private float perceivedVolume = 1f;
    [Tooltip("Relative pick weight among registered emitters. 2 = twice as likely as a 1.")]
    [SerializeField, Min(0f)] private float weight = 1f;

    public MachinerySounds Sounds => sounds;
    public AudioSource Source => source;
    public float PerceivedVolume => perceivedVolume;
    public float Weight => weight;
    public bool IsPlayable => sounds != null && sounds.HasAnyClip && source != null && isActiveAndEnabled;

    private void Reset()
    {
        source = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (source == null) source = GetComponent<AudioSource>();
        MachineryAmbientDirector.Register(this);
    }

    private void OnDisable()
    {
        MachineryAmbientDirector.Unregister(this);
    }
}
