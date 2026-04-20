using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Repeat Minigame Sounds", fileName = "RepeatMinigameSounds")]
public class RepeatMinigameSounds : ScriptableObject
{
    [Header("Start")]
    [Tooltip("Played at the StartMinigameButton on first interact when the Simon session begins.")]
    public AudioClipVolume startMinigame;

    [Header("Button feedback")]
    [Tooltip("One entry per Simon button, in the same order as RepeatMinigame.buttons (0 = first in the list). " +
             "Machine playback and player presses both trigger the clip for that button index at the button's position.")]
    public List<AudioClipVolume> buttonPressByButtonIndex = new List<AudioClipVolume>();

    [Header("Failure")]
    [Tooltip("Played at the wrongly pressed button when the player's input does not match the sequence.")]
    public AudioClipVolume sequenceFail;

    [Header("Progress")]
    [Tooltip("Played at the pressed button after each correct input EXCEPT the one that completes the sequence. " +
             "Gives a little positive click between steps; the final step is left to the round-complete feedback.")]
    public AudioClipVolume successfulEntry;

    [Header("Success")]
    [Tooltip("Pool of clips played when the full minigame completes successfully (e.g. at the approved object). " +
             "One clip is chosen at random each time — use two (or more) entries for variation.")]
    public SfxBank minigameSuccess = new SfxBank { pitchMin = 1f, pitchMax = 1f };

    /// <summary>3D one-shot at <paramref name="worldPosition"/> using <see cref="startMinigame"/>.</summary>
    public void PlayStartMinigameAt(Vector3 worldPosition)
    {
        if (startMinigame == null || startMinigame.Clip == null || AudioManager.Instance == null)
            return;

        float linear = AudioVolume.ToLinear(startMinigame.Volume);
        var shaped = new AudioClipVolume(startMinigame.Clip, linear, startMinigame.Delay);
        AudioManager.Instance.PlaySfxAtPoint(shaped, 1f, worldPosition);
    }
}
