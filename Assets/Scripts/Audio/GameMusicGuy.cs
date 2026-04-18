using UnityEngine;

/// <summary>
/// Drives in-game music selection from a single place. On Start it primes
/// <see cref="MusicManager"/> with the configured base <see cref="track"/>.
/// After that, each frame it chooses the desired track based on a small
/// priority ladder and asks the <see cref="MusicManager"/> to crossfade
/// into it (respecting the in-game envelope):
///
/// <list type="number">
///   <item><description>Monster Minigame active -> <c>MonsterNear</c>.</description></item>
///   <item><description>Just exited the Monster Minigame -> <c>GotAway</c>
///   (latched until the intensity zone changes or the minigame re-activates).
///   Falls through to the intensity-implied track if <c>GotAway</c> is
///   unassigned in the library.</description></item>
///   <item><description>Otherwise, mapped from <see cref="IntensityLevel"/>:
///   Calm -> base <see cref="track"/>, Elevated / Intense / Overload ->
///   <see cref="mediumTrack"/>.</description></item>
/// </list>
///
/// Track swaps only fire when the desired clip actually changes, so we
/// don't keep restarting <c>MusicManager</c>'s crossfade coroutine while
/// a fade is already in flight.
/// </summary>
[DisallowMultipleComponent]
public class GameMusicGuy : MonoBehaviour
{
    [Tooltip("Shared library holding every music track in the project.")]
    public MusicLibrary library;

    [Tooltip("Base / Calm-zone track played on Start and whenever intensity is below 0.25.")]
    public MusicTrack track = MusicTrack.NormalStation;

    [Tooltip("Track used while intensity is at or above 0.25 (Elevated and higher) and " +
             "the Monster Minigame is not active.")]
    public MusicTrack mediumTrack = MusicTrack.MonsterAround;

    [Tooltip("Track used while the Monster Minigame is active.")]
    public MusicTrack minigameTrack = MusicTrack.MonsterNear;

    [Tooltip("Crossfade length (seconds) used when the Monster Minigame begins. " +
             "Intentionally short so the monster cue lands like a stinger.")]
    [Min(0.01f)] public float monsterEncounterCrossfade = 0.3f;

    [Tooltip("Track played immediately after the Monster Minigame ends. If the clip is " +
             "unassigned in the library, falls back to the intensity-implied track.")]
    public MusicTrack postEscapeTrack = MusicTrack.GotAway;

    private AudioClip lastRequestedClip;
    private bool wasMinigameActive;
    private bool postEscapeLatched;
    private IntensityLevel postEscapeLevel;

    private void Start()
    {
        if (library == null)
        {
            Debug.LogError($"{nameof(GameMusicGuy)} on '{name}' has no {nameof(MusicLibrary)} assigned.");
            return;
        }

        if (MusicManager.Instance == null)
        {
            Debug.LogError($"{nameof(GameMusicGuy)} on '{name}' cannot start music: no {nameof(MusicManager)} in scene.");
            return;
        }

        wasMinigameActive = IsMinigameActive();
        RequestClip(library.Get(track));
    }

    private void Update()
    {
        if (library == null || MusicManager.Instance == null)
            return;

        IntensityManager intensityManager = IntensityManager.Instance;
        if (intensityManager == null)
            return;

        bool inMinigame = IsMinigameActive();
        IntensityLevel level = intensityManager.CurrentLevel;

        if (inMinigame)
        {
            postEscapeLatched = false;
            float fade = wasMinigameActive ? -1f : monsterEncounterCrossfade;
            RequestClip(library.Get(minigameTrack), fade);
        }
        else if (wasMinigameActive)
        {
            AudioClip postEscapeClip = library.Get(postEscapeTrack);
            if (postEscapeClip != null)
            {
                postEscapeLatched = true;
                postEscapeLevel = level;
                RequestClip(postEscapeClip);
            }
            else
            {
                postEscapeLatched = false;
                RequestClip(GetIntensityClip(level));
            }
        }
        else if (postEscapeLatched)
        {
            if (level != postEscapeLevel)
            {
                postEscapeLatched = false;
                RequestClip(GetIntensityClip(level));
            }
        }
        else
        {
            RequestClip(GetIntensityClip(level));
        }

        wasMinigameActive = inMinigame;
    }

    private void RequestClip(AudioClip clip, float crossfadeOverride = -1f)
    {
        if (clip == null) return;
        if (clip == lastRequestedClip) return;
        lastRequestedClip = clip;
        MusicManager.Instance.PlayGameMusic(clip, crossfadeOverride);
    }

    private AudioClip GetIntensityClip(IntensityLevel level)
    {
        MusicTrack desired = level == IntensityLevel.Calm ? track : mediumTrack;
        return library.Get(desired);
    }

    private static bool IsMinigameActive()
    {
        return GameManager.Instance != null && GameManager.Instance.MinigameActive;
    }
}
