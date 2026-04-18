using UnityEngine;

/// <summary>
/// Drops into a scene and kicks off a single in-game music track via
/// <see cref="MusicManager"/> on Start. Picks the clip out of a shared
/// <see cref="MusicLibrary"/> asset by <see cref="MusicTrack"/> id, so
/// track assignments live in one place. Uses the in-game envelope
/// (crossfade in -> peak hold -> decay to background).
/// </summary>
[DisallowMultipleComponent]
public class GameMusicGuy : MonoBehaviour
{
    [Tooltip("Shared library holding every music track in the project.")]
    public MusicLibrary library;

    [Tooltip("Which track from the library to play on Start.")]
    public MusicTrack track = MusicTrack.NormalStation;

    private void Start()
    {
        if (library == null)
        {
            Debug.LogError($"{nameof(GameMusicGuy)} on '{name}' has no {nameof(MusicLibrary)} assigned.");
            return;
        }

        AudioClip clip = library.Get(track);
        if (clip == null)
        {
            Debug.LogWarning($"{nameof(GameMusicGuy)} on '{name}': track '{track}' has no clip assigned in the library.");
            return;
        }

        if (MusicManager.Instance == null)
        {
            Debug.LogError($"{nameof(GameMusicGuy)} on '{name}' cannot start music: no {nameof(MusicManager)} in scene.");
            return;
        }

        MusicManager.Instance.PlayGameMusic(clip);
    }
}
