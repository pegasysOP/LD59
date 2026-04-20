using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drives in-game music selection from a single place. On Start it primes
/// <see cref="MusicManager"/> with the intro track (or, when the active scene
/// is the menu or credits scene, snaps to that scene's track using the
/// non-envelope playback path and stops steering after that). While the gameplay scene is running,
/// each frame it chooses the desired track based on a small priority ladder
/// and asks <see cref="MusicManager"/> to crossfade into it (respecting the
/// in-game envelope). Track ids follow the creative ordering in
/// <c>_silas_design/music/tracklist-v2.md</c>.
///
/// Everything here is hardcoded on purpose - there are no inspector fields.
/// Track mapping and tuning values are project-wide creative decisions; we
/// do NOT want them drifting across scenes / prefabs. The single shared
/// <see cref="MusicLibrary"/> is loaded from <c>Resources/MusicLibrary</c>.
///
/// Priority ladder (first rule that matches wins):
/// <list type="number">
///   <item><description>Monster Minigame active with at least
///   <see cref="PanicStrikeThreshold"/> strikes accumulated -> peak track
///   (the about-to-kill-you cue).</description></item>
///   <item><description>Monster Minigame active below that strike threshold
///   -> minigame track.</description></item>
///   <item><description>Just exited the Monster Minigame -> GotAway
///   (latched until the intensity zone changes or the minigame re-activates).
///   Falls through to the intensity-implied track if GotAway is unassigned
///   in the library.</description></item>
///   <item><description>Otherwise, mapped from <see cref="IntensityLevel"/>:
///   Calm -> NormalStation during the opening intro (before the starting
///   door is opened) or DamagedStationLowAnxiety once the intro has ended;
///   Elevated -> MonsterAround; Intense -> MonsterNear; Overload ->
///   MonsterAboutToKill. Each tier falls back to the next-lower tier if its
///   clip is unassigned in the library.</description></item>
/// </list>
///
/// Track swaps only fire when the desired clip actually changes, so we
/// don't keep restarting <c>MusicManager</c>'s crossfade coroutine while
/// a fade is already in flight.
/// </summary>
[DisallowMultipleComponent]
public class GameMusicGuy : MonoBehaviour
{
    // ---------- Track mapping (hardcoded, see class-level summary) ----------

    private const MusicTrack MenuTrack = MusicTrack.MainMenu;
    private const MusicTrack CreditsTrack = MusicTrack.Credits;
    private const MusicTrack IntroCalmTrack = MusicTrack.NormalStation;
    private const MusicTrack PostIntroCalmTrack = MusicTrack.DamagedStationLowAnxiety;
    private const MusicTrack ElevatedTrack = MusicTrack.MonsterAround;
    private const MusicTrack IntenseTrack = MusicTrack.MonsterNear;
    private const MusicTrack PeakTrack = MusicTrack.MonsterAboutToKill;
    private const MusicTrack MinigameTrack = MusicTrack.MonsterNear;
    private const MusicTrack PostEscapeTrack = MusicTrack.GotAway;

    // Name of the MusicLibrary ScriptableObject under any Resources folder.
    private const string MusicLibraryResourcePath = "MusicLibrary";

    // Crossfade length (seconds) used when the Monster Minigame begins.
    // Intentionally short so the monster cue lands like a stinger.
    private const float MonsterEncounterCrossfade = 0.3f;

    // Strike count at which the Monster Minigame music escalates from the
    // minigame track to the peak track. 2 means the final (third) attempt
    // plays the about-to-kill-you cue.
    private const int PanicStrikeThreshold = 2;

    // ---------- Runtime state ----------

    private MusicLibrary library;
    private AudioClip lastRequestedClip;
    private bool wasMinigameActive;
    private bool wasMinigamePanicked;
    private bool postEscapeLatched;
    private IntensityLevel postEscapeLevel;
    private bool menuModeActive;
    private Minigame cachedMinigame;
    private StateTracker subscribedStateTracker;

    private void Awake()
    {
        library = Resources.Load<MusicLibrary>(MusicLibraryResourcePath);
        if (library == null)
        {
            Debug.LogError($"{nameof(GameMusicGuy)} on '{name}' could not load " +
                           $"'{MusicLibraryResourcePath}' from any Resources folder.");
        }
    }

    private void Start()
    {
        if (library == null)
            return;

        if (MusicManager.Instance == null)
        {
            Debug.LogError($"{nameof(GameMusicGuy)} on '{name}' cannot start music: no {nameof(MusicManager)} in scene.");
            return;
        }

        if (IsMenuScene())
        {
            menuModeActive = true;
            AudioClip menuClip = library.Get(MenuTrack);
            if (menuClip != null)
            {
                lastRequestedClip = menuClip;
                MusicManager.Instance.PlayMusic(menuClip);
            }
            return;
        }

        if (IsCreditsScene())
        {
            menuModeActive = true;
            AudioClip creditsClip = library.Get(CreditsTrack);
            if (creditsClip != null)
            {
                lastRequestedClip = creditsClip;
                MusicManager.Instance.PlayMusic(creditsClip);
            }
            return;
        }

        wasMinigameActive = IsMinigameActive();
        RequestClip(GetCalmClip());
    }

    private void OnDestroy()
    {
        if (subscribedStateTracker != null)
        {
            subscribedStateTracker.OnStartingDoorOpened -= HandleStartingDoorOpened;
            subscribedStateTracker = null;
        }
    }

    private void HandleStartingDoorOpened()
    {
        // Forces a re-evaluation on the next Update; if we're currently in the
        // Calm zone the Calm clip will swap from the intro track to the
        // post-intro (damaged-station) track immediately instead of waiting for
        // the next intensity-level change.
        lastRequestedClip = null;
    }

    private void Update()
    {
        if (menuModeActive) return;
        if (library == null || MusicManager.Instance == null)
            return;

        // While the PowerDownSequence (or anything else) has game music suspended,
        // stay out of the way entirely. Clear the cached clip so when music resumes
        // we re-evaluate the priority ladder and crossfade the correct track in,
        // rather than short-circuiting because lastRequestedClip still matches the
        // track that was playing before the suspension fade-out.
        if (MusicManager.Instance.IsGameMusicSuspended)
        {
            lastRequestedClip = null;
            return;
        }

        IntensityManager intensityManager = IntensityManager.Instance;
        if (intensityManager == null)
            return;

        EnsureStateTrackerSubscription();

        bool inMinigame = IsMinigameActive();
        IntensityLevel level = intensityManager.CurrentLevel;

        if (inMinigame)
        {
            postEscapeLatched = false;
            bool panicked = GetMinigameStrikes() >= PanicStrikeThreshold;
            bool justEntered = !wasMinigameActive;
            bool justEscalated = wasMinigameActive && panicked && !wasMinigamePanicked;
            float fade = (justEntered || justEscalated) ? MonsterEncounterCrossfade : -1f;
            RequestClip(GetMinigameClip(panicked), fade);
            wasMinigamePanicked = panicked;
        }
        else if (wasMinigameActive)
        {
            AudioClip postEscapeClip = library.Get(PostEscapeTrack);
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
            case IntensityLevel.Anxiety:
                return GetCalmClip();

            case IntensityLevel.Elevated:
                return library.Get(ElevatedTrack);

            case IntensityLevel.Intense:
            {
                AudioClip intense = library.Get(IntenseTrack);
                if (intense != null) return intense;
                return library.Get(ElevatedTrack);
            }

            case IntensityLevel.Overload:
            {
                AudioClip peak = library.Get(PeakTrack);
                if (peak != null) return peak;
                AudioClip intense = library.Get(IntenseTrack);
                if (intense != null) return intense;
                return library.Get(ElevatedTrack);
            }

            default:
                return library.Get(ElevatedTrack);
        }
    }

    // Picks the Calm-zone clip. The NormalStation intro track is ONLY requested
    // while the intro is still running; once StateTracker.StartingDoorOpened
    // latches true we switch permanently to the post-intro (damaged-station)
    // track. Falls back to the intro track if the post-intro clip is
    // unassigned so nothing goes silent when the library is only partially
    // populated.
    private AudioClip GetCalmClip()
    {
        if (IsIntroComplete())
        {
            AudioClip postIntro = library.Get(PostIntroCalmTrack);
            if (postIntro != null) return postIntro;
        }
        return library.Get(IntroCalmTrack);
    }

    private static bool IsIntroComplete()
    {
        // The "intro" is a concept that only exists inside the main Game scene.
        // Anywhere else (non-menu / non-game scenes: victory screen, game-over,
        // credits, debug scenes...) we treat the intro as already finished so
        // the Calm bed defaults to the post-intro (damaged-station) track
        // instead of the NormalStation intro cue.
        if (SceneManager.GetActiveScene().name != SceneUtils.GAME_SCENE)
            return true;

        return StateTracker.Instance != null && StateTracker.Instance.StartingDoorOpened;
    }

    private void EnsureStateTrackerSubscription()
    {
        StateTracker tracker = StateTracker.Instance;
        if (tracker == subscribedStateTracker) return;

        if (subscribedStateTracker != null)
            subscribedStateTracker.OnStartingDoorOpened -= HandleStartingDoorOpened;

        subscribedStateTracker = tracker;
        if (subscribedStateTracker != null)
            subscribedStateTracker.OnStartingDoorOpened += HandleStartingDoorOpened;
    }

    private AudioClip GetMinigameClip(bool panicked)
    {
        if (panicked)
        {
            AudioClip peak = library.Get(PeakTrack);
            if (peak != null) return peak;
        }
        return library.Get(MinigameTrack);
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

    private static bool IsMenuScene()
    {
        return SceneManager.GetActiveScene().name == SceneUtils.MENU_SCENE;
    }

    private static bool IsCreditsScene()
    {
        return SceneManager.GetActiveScene().name == SceneUtils.CREDIT_SCENE;
    }

    private static bool IsMinigameActive()
    {
        return GameManager.Instance != null && GameManager.Instance.MinigameActive;
    }
}
