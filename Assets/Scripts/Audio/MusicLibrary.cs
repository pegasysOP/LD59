using UnityEngine;

/// <summary>
/// Canonical ids for the project's music tracks. Keep in sync with
/// <see cref="MusicLibrary"/> and <c>_silas_design/music/tracklist.md</c>.
/// </summary>
public enum MusicTrack
{
    MainMenu = 0,
    NormalStation = 1,
    DamagedStationLowAnxiety = 2,
    MonsterAround = 3,
    MonsterNear = 4,
    GotAway = 5,
    PointOfInterest = 6,
    Victory = 7,
    Credits = 8,
    GameOver = 9,
}

/// <summary>
/// Central ScriptableObject holding every music track in the project, keyed
/// by <see cref="MusicTrack"/>. Consumers (e.g. <c>GameMusicGuy</c>, menu /
/// victory / game-over controllers) look up the clip via <see cref="Get"/>
/// rather than holding raw <see cref="AudioClip"/> references directly.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Music Library", fileName = "MusicLibrary")]
public class MusicLibrary : ScriptableObject
{
    [Header("01 — Main Menu — Main Theme")]
    public AudioClip mainMenu;

    [Header("02 — Normal Space Station — Intro")]
    public AudioClip normalStation;

    [Header("03 — Damaged Station — Low Anxiety")]
    public AudioClip damagedStationLowAnxiety;

    [Header("04 — Monster Is Around — Light Fear")]
    public AudioClip monsterAround;

    [Header("05 — Monster Is Near — Fear / Suspense / Terror")]
    public AudioClip monsterNear;

    [Header("06 — Got Away From Monster — Back to Calm")]
    public AudioClip gotAway;

    [Header("07 — Point of Interest — Near a Ship System")]
    public AudioClip pointOfInterest;

    [Header("08 — Victory — Escape in Rescue Ship")]
    public AudioClip victory;

    [Header("09 — Credits — Epic Triumph")]
    public AudioClip credits;

    [Header("10 — Game Over — Monster Killed You")]
    public AudioClip gameOver;

    /// <summary>Returns the clip assigned to <paramref name="track"/>, or null if unassigned.</summary>
    public AudioClip Get(MusicTrack track)
    {
        switch (track)
        {
            case MusicTrack.MainMenu: return mainMenu;
            case MusicTrack.NormalStation: return normalStation;
            case MusicTrack.DamagedStationLowAnxiety: return damagedStationLowAnxiety;
            case MusicTrack.MonsterAround: return monsterAround;
            case MusicTrack.MonsterNear: return monsterNear;
            case MusicTrack.GotAway: return gotAway;
            case MusicTrack.PointOfInterest: return pointOfInterest;
            case MusicTrack.Victory: return victory;
            case MusicTrack.Credits: return credits;
            case MusicTrack.GameOver: return gameOver;
            default: return null;
        }
    }
}
