using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Player Movement Sounds", fileName = "PlayerMovementSounds")]
public class PlayerMovementSounds : ScriptableObject
{
    [Header("Footsteps")]
    public SfxBank footsteps = new SfxBank { pitchMin = 0.88f, pitchMax = 1.12f };
    [Tooltip("Seconds between footstep one-shots while moving on the ground.")]
    public float footstepInterval = 0.45f;

    [Header("Jump Takeoff")]
    [Tooltip("Body/whoosh layer played at the instant of takeoff. Plays together with Jump Step.")]
    public SfxBank jumpAir = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };
    [Tooltip("Foot pushing off the ground, played at the instant of takeoff. Plays together with Jump Air.")]
    public SfxBank jumpStep = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };

    [Header("Landing")]
    public SfxBank landing = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };
    [Tooltip("Only play a landing sound if the player was airborne at least this long.")]
    public float minAirTimeForLanding = 0.12f;
}
