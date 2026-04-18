using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Battery Sounds", fileName = "BatterySounds")]
public class BatterySounds : ScriptableObject
{
    [Header("Handling")]
    [Tooltip("Played when the player grabs a battery off a surface.")]
    public SfxBank pickup = new SfxBank { pitchMin = 0.97f, pitchMax = 1.03f };
    [Tooltip("Played when the player releases a held battery into the air (not into a slot).")]
    public SfxBank drop = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };

    [Header("Slot Placement")]
    [Tooltip("Physical clunk when a battery enters any slot trigger.")]
    public SfxBank placeClunk = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };
    [Tooltip("Positive feedback when a battery's colour matches the slot.")]
    public SfxBank acceptFeedback = new SfxBank { pitchMin = 1.00f, pitchMax = 1.00f };
    [Tooltip("Negative feedback when a battery is placed in the wrong slot.")]
    public SfxBank rejectFeedback = new SfxBank { pitchMin = 1.00f, pitchMax = 1.00f };
    [Tooltip("Static/zap layered on top of reject feedback for a thicker cue.")]
    public SfxBank rejectStaticZap = new SfxBank { pitchMin = 0.98f, pitchMax = 1.02f };

    [Header("Power-Up Stingers")]
    [Tooltip("Stinger after the 1st correct battery is collected.")]
    public SfxBank powerUpFirst;
    [Tooltip("Stinger after the 2nd correct battery is collected.")]
    public SfxBank powerUpSecond;
    [Tooltip("Stinger after the 3rd (final) correct battery is collected.")]
    public SfxBank powerUpThird;

    public SfxBank GetPowerUpFor(int collectedCount)
    {
        return collectedCount switch
        {
            1 => powerUpFirst,
            2 => powerUpSecond,
            _ => powerUpThird,
        };
    }
}
