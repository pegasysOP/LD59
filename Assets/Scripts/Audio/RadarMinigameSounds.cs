using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Radar Minigame Sounds", fileName = "RadarMinigameSounds")]
public class RadarMinigameSounds : ScriptableObject
{
    [Header("Radar Signal Loop")]
    [Tooltip("Looping radar-signal clip. Driven by alignment accuracy: as the player line approaches the " +
             "target line the pitch and volume climb toward their authored (max) values; when fully " +
             "mis-aligned the loop drops to the min pitch/volume.")]
    public AudioClipVolume radarLoop;

    [Tooltip("Pitch at the worst possible alignment error (1.0). Typically well below 1 so the signal " +
             "sounds detuned/starved when the player line is far from the target.")]
    [Range(0.1f, 1f)] public float radarMinPitch = 0.5f;
    [Tooltip("Pitch at a perfect alignment match (error = 0). This is the 'original' pitch the signal " +
             "should reach when the player is spot-on — keep at 1.0 unless you want the peak to be " +
             "slightly sharp/flat.")]
    [Range(0.5f, 2f)] public float radarMaxPitch = 1f;

    [Tooltip("Perceived loudness (0-1) floor when fully mis-aligned. Keep audible so the player can still " +
             "hear it sweeping as they search.")]
    [Range(0f, 1f)] public float radarMinPerceivedVolume = 0.35f;
    [Tooltip("Perceived loudness (0-1) at perfect alignment.")]
    [Range(0f, 1f)] public float radarMaxPerceivedVolume = 1f;

    [Tooltip("How quickly the radar loop's pitch/volume chase the current alignment value. Higher = snappier.")]
    [Min(0.01f)] public float radarResponseSpeed = 8f;

    [Tooltip("Seconds to fade the radar loop out once the minigame is completed.")]
    [Min(0.01f)] public float radarCompleteFadeOut = 0.8f;

    [Tooltip("Seconds of no handle activity (no slider grabbed) after which the radar tone fades to " +
             "silence. Timer resets every frame a handle is held and on every grab. The loop is ALSO " +
             "silent before the player's very first handle grab — it only starts sounding once the " +
             "player first engages. Set to 0 or negative to disable the idle fade-out.")]
    [Min(0f)] public float radarIdleTimeoutSeconds = 8f;
    [Tooltip("Seconds to fade the radar tone from its current level down to 0 once the idle timeout " +
             "elapses. The subsequent grab brings it back instantly.")]
    [Min(0.01f)] public float radarIdleFadeOutDuration = 1.5f;

    [Header("Signal Send (completion one-shot)")]
    [Tooltip("Played once at the radar rig when the alignment completes and the signal is 'sent'. " +
             "Fires in parallel with the radar-loop fade-out so the send cue sits on top of the " +
             "decaying signal tone.")]
    public SfxBank signalSend = new SfxBank { pitchMin = 1f, pitchMax = 1f };

    [Header("Handle Grab / Release (one-shots)")]
    [Tooltip("Played at the slider's position when the player grabs the handle.")]
    public SfxBank handleGrab = new SfxBank { pitchMin = 0.97f, pitchMax = 1.03f };
    [Tooltip("Played at the slider's position when the handle is released.")]
    public SfxBank handleRelease = new SfxBank { pitchMin = 0.97f, pitchMax = 1.03f };

    [Header("Movement Loop")]
    [Tooltip("Continuous clip that fades in while a slider handle is moving and fades back out when it " +
             "stops. Same clip is shared between both sliders — whichever is moving faster drives the " +
             "current volume/boost.")]
    public AudioClipVolume moveLoop;

    [Tooltip("Perceived loudness (0-1) the move loop settles at during steady slow motion.")]
    [Range(0f, 1f)] public float moveBasePerceivedVolume = 0.55f;
    [Tooltip("Extra perceived loudness (0-1) layered on top of the base when movement is at the boost " +
             "speed (or faster). Lets fast scrubbing feel beefier without changing pitch.")]
    [Range(0f, 1f)] public float moveBoostExtraPerceivedVolume = 0.35f;
    [Tooltip("Slider value change rate (in normalized Value units per second) at which the boost layer " +
             "reaches its full extra volume. Lower values = boost kicks in with only modest motion.")]
    [Min(0.01f)] public float moveSpeedForFullBoost = 0.9f;

    [Tooltip("How quickly the move loop's volume ramps UP when motion starts (perceived-space units/sec).")]
    [Min(0.01f)] public float moveFadeInSpeed = 14f;
    [Tooltip("How quickly the move loop's volume ramps DOWN when motion stops (perceived-space units/sec).")]
    [Min(0.01f)] public float moveFadeOutSpeed = 8f;

    [Tooltip("Pitch for the movement loop. Usually 1.0 — vary slightly if you want the scrub sound to " +
             "sit a little above or below the radar loop.")]
    [Range(0.5f, 2f)] public float movePitch = 1f;
}
