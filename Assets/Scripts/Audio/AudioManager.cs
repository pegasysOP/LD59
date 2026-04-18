using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [Header("Internals")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource fadeSource;
    private Coroutine currentCoroutine;
    public AudioMixer audioMixer;
    private float currentSpeed = 1f;

    [Header("UI")]
    public AudioClip buttonPressClip;
    public AudioClip buttonHoverClip;
    public AudioClip pauseMenuOpenClip;
    public AudioClip pauseMenuClosedClip;

    [Header("SFX")]

    [Header("Interaction")]
    public AudioClip selectClip;
    public AudioClip selectBlockedClip;

    [Header("Music")]
    public AudioClip menuMusicClip;
    public AudioClip gameWonClip;
    public AudioClip gameLostClip;

    [Header("Playlist")]
    private List<AudioClip> playlist = new List<AudioClip>(){};
    private int playlistIndex = 0;

    public static AudioManager Instance;

    private float remainingDuckTime = 0f;

    public enum FadeType { None, FadeIn, CrossFade }

    public void Init()
    {
        Instance = this;
        UpdateVolume(SettingsUtils.GetMasterVolume());

        CreatePlaylist();

        WarmMusicCache();

        PlayCurrentSongInPlaylist();

        musicSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Master")[0];
    }

    void WarmMusicCache()
    {
        foreach (AudioClip clip in playlist)
        {
            if (clip != null && musicSource != null)
            {
                musicSource.clip = clip;
                musicSource.Play();
                musicSource.Stop();
            }
        }
    }

    //==================== Playlist ===================
    
    void CreatePlaylist()
    {
        playlist = new List<AudioClip>(){}; //TODO: Add music clips here 
    }

    public void AdvanceSong()
    {
        playlistIndex = (playlistIndex + 1) % playlist.Count;
        PlayCurrentSongInPlaylist();
    }

    void Shuffle()
    {
        if (playlist.Count == 0)
        {
            Debug.LogError("Error: playlist is empty. Cannot shuffle!");
            return;
        }
            

        if(playlist.Count == 1)
        {
            Debug.LogWarning("Warning: playlist only has one song. Shuffle will replay same song.");
            playlistIndex = 0;
            PlayCurrentSongInPlaylist();
        }

        int tempPlaylistIndex = playlistIndex;
        while(tempPlaylistIndex == playlistIndex)
        {
            tempPlaylistIndex = Random.Range(0, playlist.Count);
        }
        playlistIndex = tempPlaylistIndex;
        PlayCurrentSongInPlaylist();
    }

    void Skip(int songsToSkip)
    {
        playlistIndex = (playlistIndex + songsToSkip) % playlist.Count;
        PlayCurrentSongInPlaylist();
    }

    void PlayCurrentSongInPlaylist()
    {
        if(playlist == null || playlist.Count == 0)
        {
            Debug.LogWarning("Playlist empty!");
            return;
        }

        PlayMusic(playlist[playlistIndex], FadeType.FadeIn, 1f);
    }

    void StopPlaylist()
    {
        Stop(musicSource, true);
    }

    void PausePlaylist()
    {
        musicSource.Pause();
    }

    void UnpausePlaylist()
    {
        musicSource.UnPause();
    }

    //==================== Utility ====================
    public bool IsClipPlaying(AudioSource source, AudioClip clip)
    {
        return source.isPlaying && source.clip == clip;
    }

    public void PlayMusic(AudioClip clip, FadeType fadeType = FadeType.None, float fadeTime = 2f, bool isDucking = false)
    {
        Play(musicSource, clip, fadeType, fadeTime, isDucking);
    }

    public void PlaySfx(List<AudioClip> audioClips)
    {
        if (sfxSource == null || audioClips == null || audioClips.Count == 0)
            Debug.LogError("ERROR: there are no sounds to play!");

        int index = Random.Range(0, audioClips.Count);
        sfxSource.PlayOneShot(audioClips[index], 1f);
    }

    public void PlaySfx(List<AudioClipVolume> clips)
    {
        if (sfxSource == null || clips == null || clips.Count == 0)
            Debug.LogError("ERROR: there are no sounds to play!");

        int index = Random.Range(0, clips.Count);
        AudioClipVolume entry = clips[index];
        if (entry == null || entry.Clip == null)
            return;
        PlayOneShotInternal(entry.Clip, entry.Volume, 1f, entry.Delay);
    }

    public void Play(AudioSource source, AudioClip clip, FadeType fadeType = FadeType.None, float fadeTime = 2f, bool isDucking = false)
    {
        if (source == null || clip == null)
        {
            Debug.LogError("ERROR: You must provide an audio source and clip to play on it");
        }
        if (isDucking)
        {
            if(fadeType != FadeType.None)
            {
                Debug.LogError("ERROR: Simultaneously ducking and fading is not supported!");
            }
            StartDuckAudio(source);
        }
        else if (fadeType == FadeType.FadeIn)
        {
            StartFadeIn(source, clip, fadeTime);
        }
        else if (fadeType == FadeType.CrossFade)
        {
            StartCrossFade(clip, fadeTime);
        }
        else
        {
            source.clip = clip;
            source.Play();
        }
    }

    public void Update()
    {
        if (!musicSource.isPlaying && playlist.Count > 0)
        {
            AdvanceSong();
        }
    }

    public void Stop(AudioSource source, bool fadeOutEnabled = false, float fadeTime = 2f)
    {
        if(source == null)
        {
            Debug.LogError("ERROR: Must provide a source to stop playing");
        }
        if (fadeOutEnabled)
        {
            StartFadeOut(source, fadeTime);
        }
        else
        {
            source.Stop();
        }
    }

    public void SetPlaybackSpeed(float speed, AudioSource source)
    {
        currentSpeed = Mathf.Clamp(speed, 0.5f, 2f);

        source.pitch = currentSpeed;

        float semitoneOffset = Mathf.Log(currentSpeed, 2f) * 12f;

        audioMixer.SetFloat("Pitch", -semitoneOffset);
    }

    //==================== Interaction ====================

    public void PlaySfxWithPitchShifting(List<AudioClip> clips, float minPitch = 0.8f, float maxPitch = 1.2f)
    {
        if (sfxSource == null || clips == null || clips.Count == 0)
            Debug.LogError("ERROR: there are no sounds to pitch shift!");

        int index = Random.Range(0, clips.Count);
        sfxSource.pitch = Random.Range(minPitch, maxPitch);
        sfxSource.PlayOneShot(clips[index], 1f);
        sfxSource.pitch = 1f;
    }

    public void PlaySfxWithPitchShifting(List<AudioClipVolume> clips, float minPitch = 0.8f, float maxPitch = 1.2f)
    {
        if (sfxSource == null || clips == null || clips.Count == 0)
            Debug.LogError("ERROR: there are no sounds to pitch shift!");

        int index = Random.Range(0, clips.Count);
        AudioClipVolume entry = clips[index];
        if (entry == null || entry.Clip == null)
            return;
        PlayOneShotInternal(entry.Clip, entry.Volume, Random.Range(minPitch, maxPitch), entry.Delay);
    }

    public void PlaySfxWithPitchShifting(AudioClipVolume clipVolume, float minPitch = 0.8f, float maxPitch = 1.2f)
    {
        if (sfxSource == null || clipVolume == null || clipVolume.Clip == null)
            return;

        PlayOneShotInternal(clipVolume.Clip, clipVolume.Volume, Random.Range(minPitch, maxPitch), clipVolume.Delay);
    }

    private void PlayOneShotInternal(AudioClip clip, float volume, float pitch, float delay)
    {
        if (sfxSource == null || clip == null)
            return;

        if (delay > 0f)
        {
            StartCoroutine(PlayOneShotAfterDelay(clip, volume, pitch, delay));
        }
        else if (delay < 0f)
        {
            float startTime = Mathf.Clamp(-delay, 0f, Mathf.Max(0f, clip.length - 0.01f));
            PlayOneShotAtOffset(clip, volume, pitch, startTime);
        }
        else
        {
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume);
            sfxSource.pitch = 1f;
        }
    }

    private IEnumerator PlayOneShotAfterDelay(AudioClip clip, float volume, float pitch, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (sfxSource == null || clip == null)
            yield break;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
        sfxSource.pitch = 1f;
    }

    private void PlayOneShotAtOffset(AudioClip clip, float volume, float pitch, float startTime)
    {
        GameObject go = new GameObject($"OneShot_{clip.name}");
        go.transform.SetParent(transform, false);
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
        src.volume = sfxSource.volume * Mathf.Clamp01(volume);
        src.pitch = pitch;
        src.spatialBlend = sfxSource.spatialBlend;
        src.bypassEffects = sfxSource.bypassEffects;
        src.bypassListenerEffects = sfxSource.bypassListenerEffects;
        src.bypassReverbZones = sfxSource.bypassReverbZones;
        src.playOnAwake = false;
        src.time = startTime;
        src.Play();

        float remaining = (clip.length - startTime) / Mathf.Max(0.01f, Mathf.Abs(pitch));
        Destroy(go, remaining + 0.1f);
    }

    //==================== Volume ====================

    public void UpdateVolume(float value)
    {
        musicSource.volume = value / 3;
        fadeSource.volume = value / 3;
        sfxSource.volume = value / 3;
    }

    public void UpdateSfxVolume(float value)
    {
        sfxSource.volume = value / 3;
    }

    //==================== Ducking ====================

    public void StartDuckAudio(AudioSource sourceToDuck, float duckVolumePercent = 0.3f, float duckDuration = 2f, float fadeTime = 0.5f)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(DuckAudio(sourceToDuck, duckVolumePercent, duckDuration, fadeTime));
    }

    private IEnumerator DuckAudio(AudioSource source, float duckVolumePercent, float duckDuration, float fadeTime)
{
    if (!source.isPlaying)
    {
        yield break;
    }

    float originalVolume = SettingsUtils.GetMasterVolume() / 3;
    float duckVolume = originalVolume * duckVolumePercent;

    // Fade down
    for (float t = 0; t < fadeTime; t += Time.deltaTime)
    {
        float normalized = t / fadeTime;
        source.volume = Mathf.Lerp(source.volume, duckVolume, normalized);
        yield return null;
    }

    source.volume = duckVolume;

    remainingDuckTime = Mathf.Min(remainingDuckTime, duckDuration);

    while (remainingDuckTime > 0f)
    {
        remainingDuckTime -= Time.deltaTime;
        yield return null;
    }

    for (float t = 0; t < fadeTime; t += Time.deltaTime)
    {
        float normalized = t / fadeTime;
        source.volume = Mathf.Lerp(duckVolume, originalVolume, normalized);
        yield return null;
    }

    source.volume = originalVolume;

    currentCoroutine = null;
    remainingDuckTime = 0f;
}

    //==================== Fading ====================

    private void StartFadeIn(AudioSource source, AudioClip clip, float duration)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        currentCoroutine = StartCoroutine(FadeIn(source, clip, duration));
    }

    private IEnumerator FadeIn(AudioSource source, AudioClip clipToFadeIn, float fadeDuration)
    {
        source.Stop();

        float initialVolume = SettingsUtils.GetMasterVolume() / 3;
        source.volume = 0;

        source.clip = clipToFadeIn;
        source.Play();

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            source.volume = Mathf.Lerp(0f, initialVolume, t / fadeDuration);
            yield return null;
        }
        source.volume = initialVolume;
        //source.volume = Mathf.Lerp(0, SettingsUtils.GetMasterVolume(), fadeDuration);
    }

    private void StartFadeOut(AudioSource source, float duration)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        currentCoroutine = StartCoroutine(FadeOut(source, duration));
    }

    private IEnumerator FadeOut(AudioSource source, float fadeOutDuration)
    {
        for(float t = 0; t < fadeOutDuration; t+= Time.deltaTime)
        {
            source.volume = Mathf.Lerp(SettingsUtils.GetMasterVolume() / 3, 0f, t / fadeOutDuration);
            yield return null;
        }

        source.volume = 0;
        source.Stop();
    }

    private void StartCrossFade(AudioClip clipToFadeIn, float fadeOutDuration)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        currentCoroutine = StartCoroutine(CrossFade(clipToFadeIn, fadeOutDuration));
    }

    private IEnumerator CrossFade(AudioClip fadeInClip, float fadeDuration)
    {
        AudioSource fromSource = musicSource;
        AudioSource toSource = fadeSource;

        if (fromSource.clip == null)
        {
            Debug.LogError("CrossFade: No currently playing clip.");
            yield break;
        }

        // If the same clip is requested again, we can optionally skip
        if (fromSource.clip == fadeInClip)
        {
            Debug.LogWarning("WARNING: you are trying to switch to the same song as you are fading out ");
        }

        float masterVolume = SettingsUtils.GetMasterVolume();
        float fromStartVolume = fromSource.volume > 0f ? fromSource.volume : masterVolume / 3f;
        float toTargetVolume = masterVolume / 3f;

        // Setup fade in 
        toSource.clip = fadeInClip;
        toSource.volume = 0f;
        toSource.Play();

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / fadeDuration);

            fromSource.volume = Mathf.Lerp(fromStartVolume, 0f, normalized);
            toSource.volume = Mathf.Lerp(0f, toTargetVolume, normalized);

            yield return null;
        }

        fromSource.Stop();
        fromSource.volume = 0f;
        toSource.volume = toTargetVolume;

        // Swap roles after fade completes
        AudioSource temp = musicSource;
        musicSource = fadeSource;
        fadeSource = temp;

        currentCoroutine = null;

    }
}
