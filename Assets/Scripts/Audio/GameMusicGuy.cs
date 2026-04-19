using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drives in-game music selection from a single place. On Start it primes
/// <see cref="MusicManager"/> with the configured base <see cref="track"/>
/// (or, when the active scene is the menu, snaps to <see cref="menuTrack"/>
/// using the non-envelope playback path and stops steering after that).
/// While the gameplay scene is running, each frame it chooses the desired
/// track based on a small priority ladder and asks <see cref="MusicManager"/>
/// to crossfade into it (respecting the in-game envelope). Track ids follow
/// the creative ordering in <c>_silas_design/music/tracklist-v2.md</c>.
///
/// <list type="number">
///   <item><description>Monster Minigame active with at least
///   <see cref="panicStrikeThreshold"/> strikes accumulated -> <see cref="peakTrack"/>
///   (the about-to-kill-you cue).</description></item>
///   <item><description>Monster Minigame active below that strike threshold
///   -> <see cref="minigameTrack"/>.</description></item>
///   <item><description>Just exited the Monster Minigame -> <c>GotAway</c>
///   (latched until the intensity zone changes or the minigame re-activates).
///   Falls through to the intensity-implied track if <c>GotAway</c> is
///   unassigned in the library.</description></item>
///   <item><description>Otherwise, mapped from <see cref="IntensityLevel"/>:
///   Calm -> base <see cref="track"/>, Elevated -> <see cref="mediumTrack"/>,
///   Intense -> <see cref="intenseTrack"/>, Overload -> <see cref="peakTrack"/>.
///   Each tier falls back to the next-lower tier if its clip is unassigned
///   in the library.</description></item>
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

    [Tooltip("Scene names for which this component should play menuTrack on Start " +
             "(via the non-envelope MusicManager.PlayMusic path) and skip all " +
             "gameplay-driven track selection afterwards.")]
    public string[] menuSceneNames = new[] { SceneUtils.MENU_SCENE };

    [Tooltip("Track played on Start when the active scene is in menuSceneNames. " +
             "Snapped in at full volume - no envelope.")]
    public MusicTrack menuTrack = MusicTrack.MainMenu;

    [Tooltip("Base / Calm-zone track played on Start and whenever intensity is below 0.25.")]
    public MusicTrack track = MusicTrack.NormalStation;

    [Tooltip("Track used while intensity is Elevated (0.25 - 0.50) and the Monster " +
             "Minigame is not active.")]
    public MusicTrack mediumTrack = MusicTrack.MonsterAround;

    [Tooltip("Track used while intensity is Intense (0.50 - 0.75) and the Monster " +
             "Minigame is not active. Falls back to mediumTrack if unassigned in the library.")]
    public MusicTrack intenseTrack = MusicTrack.MonsterNear;

    [Tooltip("Track used at peak intensity (Overload, 0.75+) when the Monster Minigame is " +
             "not active, and also during the Monster Minigame once the player has " +
             "accumulated panicStrikeThreshold strikes. Falls back to intenseTrack then " +
             "mediumTrack if unassigned in the library.")]
    public MusicTrack peakTrack = MusicTrack.MonsterAboutToKill;

    [Tooltip("Track used while the Monster Minigame is active and the player is still below " +
             "panicStrikeThreshold strikes.")]
    public MusicTrack minigameTrack = MusicTrack.MonsterNear;

    [Tooltip("Crossfade length (seconds) used when the Monster Minigame begins. " +
             "Intentionally short so the monster cue lands like a stinger.")]
    [Min(0.01f)] public float monsterEncounterCrossfade = 0.3f;

    [Tooltip("Strike count at which the Monster Minigame music escalates from " +
             "minigameTrack to peakTrack. Default 2 means the final (third) attempt " +
             "plays the about-to-kill-you cue.")]
    [Min(1)] public int panicStrikeThreshold = 2;

    [Tooltip("Track played immediately after the Monster Minigame ends. If the clip is " +
             "unassigned in the library, falls back to the intensity-implied track.")]
    public MusicTrack postEscapeTrack = MusicTrack.GotAway;

    private AudioClip lastRequestedClip;
    private bool wasMinigameActive;
    private bool wasMinigamePanicked;
    private bool postEscapeLatched;
    private IntensityLevel postEscapeLevel;
    private bool menuModeActive;
    private Minigame cachedMinigame;

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

        if (IsMenuScene())
        {
            menuModeActive = true;
            AudioClip menuClip = library.Get(menuTrack);
            if (menuClip != null)
            {
                lastRequestedClip = menuClip;
                MusicManager.Instance.PlayMusic(menuClip);
            }
            return;
        }

        wasMinigameActive = IsMinigameActive();
        RequestClip(library.Get(track));
    }

    private void Update()
    {
        if (menuModeActive) return;
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
            bool panicked = GetMinigameStrikes() >= panicStrikeThreshold;
            bool justEntered = !wasMinigameActive;
            bool justEscalated = wasMinigameActive && panicked && !wasMinigamePanicked;
            float fade = (justEntered || justEscalated) ? monsterEncounterCrossfade : -1f;
            RequestClip(GetMinigameClip(panicked), fade);
            wasMinigamePanicked = panicked;
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
        if (!inMinigame) wasMinigamePanicked = false;
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
        switch (level)
        {
            case IntensityLevel.Calm:
                return library.Get(track);

            case IntensityLevel.Elevated:
                return library.Get(mediumTrack);

            case IntensityLevel.Intense:
            {
                AudioClip intense = library.Get(intenseTrack);
                if (intense != null) return intense;
                return library.Get(mediumTrack);
            }

            case IntensityLevel.Overload:
            {
                AudioClip peak = library.Get(peakTrack);
                if (peak != null) return peak;
                AudioClip intense = library.Get(intenseTrack);
                if (intense != null) return intense;
                return library.Get(mediumTrack);
            }

            default:
                return library.Get(mediumTrack);
        }
    }

    private AudioClip GetMinigameClip(bool panicked)
    {
        if (panicked)
        {
            AudioClip peak = library.Get(peakTrack);
            if (peak != null) return peak;
        }
        return library.Get(minigameTrack);
    }

    private int GetMinigameStrikes()
    {
        if (cachedMinigame == null)
        {
#if UNITY_2023_1_OR_NEWER
            cachedMinigame = FindFirstObjectByType<Minigame>();
#else
            cachedMinigame = FindObjectOfType<Minigame>();
#endif
        }
        return cachedMinigame != null ? cachedMinigame.FailCount : 0;
    }

    private bool IsMenuScene()
    {
        if (menuSceneNames == null || menuSceneNames.Length == 0)
            return false;

        string active = SceneManager.GetActiveScene().name;
        for (int i = 0; i < menuSceneNames.Length; i++)
        {
            if (!string.IsNullOrEmpty(menuSceneNames[i]) && menuSceneNames[i] == active)
                return true;
        }
        return false;
    }

    private static bool IsMinigameActive()
    {
        return GameManager.Instance != null && GameManager.Instance.MinigameActive;
    }
}
