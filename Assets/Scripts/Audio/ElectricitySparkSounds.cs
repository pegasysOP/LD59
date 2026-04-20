using UnityEngine;

/// <summary>
/// Audio config for an ambient electricity / sparking emitter. Shape matches the rest of the
/// <c>*Sounds.cs</c> family: a ScriptableObject asset that holds clips + tunables and is referenced
/// by a <see cref="ElectricitySparkSfxPlayer"/> component in the scene/prefab.
///
/// The design intentionally decouples audio rate from visual particle rate:
///   - <see cref="bedLoop"/>   -- a continuous 3D loop that carries the "there is electricity here"
///                                 sensation at low volume. Does most of the perceptual work.
///   - <see cref="crackles"/>  -- a small bank of short crackle transients triggered on an internal
///                                 jittered timer, NOT per particle. A handful of pops per second
///                                 max, with position scatter + pitch + volume jitter, sells the
///                                 detail without turning into a "machine gun".
/// </summary>
[CreateAssetMenu(menuName = "Audio/Electricity Spark Sounds", fileName = "ElectricitySparkSounds")]
public class ElectricitySparkSounds : ScriptableObject
{
    [Header("Bed Loop (ambient buzz)")]
    [Tooltip("Continuous hum/buzz/sizzle clip looped at low volume on a 3D source at the emitter's " +
             "position. This is the '70% of the job' layer -- it tells the player 'there is " +
             "electricity here' without any transient triggers at all.")]
    public AudioClip bedLoop;

    [Tooltip("Perceived loudness (0-1) of the bed loop. Keep low (~0.25-0.4) so it reads as ambience " +
             "rather than a foreground element. Converted to linear amplitude via AudioVolume.ToLinear.")]
    [Range(0f, 1f)] public float bedPerceivedVolume = 0.3f;

    [Tooltip("3D rolloff start distance for the bed loop (meters).")]
    [Min(0.1f)] public float bedMinDistance = 1.5f;

    [Tooltip("3D rolloff falloff-end distance for the bed loop (meters). Moderate range so the buzz " +
             "carries across the room but doesn't spill globally.")]
    [Min(1f)] public float bedMaxDistance = 12f;

    [Header("Crackle Transients (discrete pops)")]
    [Tooltip("Pool of short (~50-150 ms) crackle clips. Picked at random per tick with anti-repeat " +
             "built into SfxBank. Authored volumes are perceived-space (0-1); SfxBank applies the " +
             "perceived->linear curve and the per-trigger volume jitter on top.")]
    public SfxBank crackles = new SfxBank { pitchMin = 0.85f, pitchMax = 1.15f, volumeJitter = 0.2f };

    [Tooltip("Minimum seconds between consecutive crackle triggers. Floor here -- the actual interval " +
             "is uniformly randomized between min and max so the pops never feel metronomic.")]
    [Min(0.05f)] public float crackleMinInterval = 0.5f;

    [Tooltip("Maximum seconds between consecutive crackle triggers. Set noticeably higher than min " +
             "(e.g. 3x) so the rhythm feels organic.")]
    [Min(0.05f)] public float crackleMaxInterval = 1.5f;

    [Tooltip("Radius (meters) of the spherical region around the emitter's transform from which each " +
             "crackle's world position is sampled. Gives you cheap spatial variation so consecutive " +
             "pops feel like 'different sparks in different places' rather than one stuttering point.")]
    [Min(0f)] public float crackleScatterRadius = 0.75f;

    [Tooltip("3D rolloff start distance for each individual crackle (meters). Keep small -- the " +
             "crackles are short transients whose job is 'you're close to a spark', not 'there's " +
             "electricity over there' (that's what the bed loop is for).")]
    [Min(0.1f)] public float crackleMinDistance = 0.5f;

    [Tooltip("3D rolloff falloff-end distance for each crackle (meters). Set NOTICEABLY TIGHTER " +
             "than bedMaxDistance (typically ~half) so the bed carries the emitter's presence at " +
             "range and the pops only reveal themselves when the player is genuinely close. " +
             "Prevents distant arcs from machine-gunning audible transients across the level.")]
    [Min(1f)] public float crackleMaxDistance = 4f;

    [Tooltip("Initial random delay (seconds) before the FIRST crackle fires after the emitter " +
             "activates. Prevents a cluster of crackle triggers if many electricity emitters spawn on " +
             "the same frame. Second and subsequent triggers always use the min/max interval range.")]
    [Min(0f)] public float crackleStartupDelayMax = 1f;

    public bool HasBedLoop => bedLoop != null;
    public bool HasAnyCrackle => crackles != null && crackles.HasAnyClip;
}
