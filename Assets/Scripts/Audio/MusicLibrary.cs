using UnityEngine;

/// <summary>
/// Canonical ids for the project's music tracks. Keep in sync with
/// <see cref="MusicLibrary"/> and <c>_silas_design/music/tracklist-v2.md</c>.
///
/// Numeric values are intentionally stable (do not renumber) so Unity
/// serialization of <see cref="MusicTrack"/> fields in scenes / prefabs
/// does not silently remap. Declaration order below follows the creative
/// order in the tracklist; numeric values reflect historical insertion.
/// </summary>
public enum MusicTrack
{
    MainMenu = 0,
    NormalStation = 1,
    DamagedStationLowAnxiety = 2,
    GotAway = 5,
    MonsterAround = 3,
    MonsterNear = 4,
    MonsterAboutToKill = 10,
    GameOver = 9,
    Victory = 7,
    Credits = 8,
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

    [Header("04 — Got Away From Monster — Back to Calm (slightly busier)")]
    public AudioClip gotAway;

    [Header("05 — Monster Is Around — Light Fear")]
    public AudioClip monsterAround;

    [Header("06 — Monster Is Near — Fear / Suspense / Terror")]
    public AudioClip monsterNear;

    [Header("07 — Monster About To Kill You — Terror, High Intensity")]
    public AudioClip monsterAboutToKill;

    [Header("08 — Game Over — Monster Killed You")]
    public AudioClip gameOver;

    [Header("09 — Victory — Escape in Rescue Ship")]
    public AudioClip victory;

    [Header("10 — Credits — Epic Triumph")]
    public AudioClip credits;

    /// <summary>Returns the clip assigned to <paramref name="track"/>, or null if unassigned.</summary>
    public AudioClip Get(MusicTrack track)
    {
        switch (track)
        {
            case MusicTrack.MainMenu: return mainMenu;
            case MusicTrack.NormalStation: return normalStation;
            case MusicTrack.DamagedStationLowAnxiety: return damagedStationLowAnxiety;
            case MusicTrack.GotAway: return gotAway;
            case MusicTrack.MonsterAround: return monsterAround;
            case MusicTrack.MonsterNear: return monsterNear;
            case MusicTrack.MonsterAboutToKill: return monsterAboutToKill;
            case MusicTrack.GameOver: return gameOver;
            case MusicTrack.Victory: return victory;
            case MusicTrack.Credits: return credits;
            default: return null;
        }
    }
}
