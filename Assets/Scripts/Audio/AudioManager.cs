using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Central manager for SFX / UI / positional one-shots and the master-volume
/// propagation for those sources. Music playback (crossfades, envelopes,
/// menu/game tracks) lives in <see cref="MusicManager"/>.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Internals")]
    public AudioSource sfxSource;
    public AudioSource uiSfxSource;
    public AudioMixer audioMixer;

    [Header("UI")]
    public AudioClip buttonPressClip;
    public AudioClip buttonHoverClip;
    public AudioClip pauseMenuOpenClip;
    public AudioClip pauseMenuClosedClip;

    [Header("Interaction")]
    public AudioClip selectClip;
    public AudioClip selectBlockedClip;

    [Header("Gameplay")]
    public BatterySounds batterySounds;

    public static AudioManager Instance;

    public void Init()
    {
        Instance = this;
        UpdateVolume(SettingsUtils.GetMasterVolume());
    }

    //==================== SFX ====================

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

    //==================== Positional 3D One-Shots ====================

    // Hardcoded rolloff envelope for positional one-shots. Tuned for interior-scale rooms.
    private const float PositionalMinDistance = 1.5f;
    private const float PositionalMaxDistance = 25f;

    public void PlaySfxAtPoint(AudioClipVolume clipVolume, float pitch, Vector3 worldPosition)
    {
        if (sfxSource == null || clipVolume == null || clipVolume.Clip == null)
            return;

        PlayPositionalOneShotInternal(clipVolume.Clip, clipVolume.Volume, pitch, clipVolume.Delay, worldPosition);
    }

    private void PlayPositionalOneShotInternal(AudioClip clip, float volume, float pitch, float delay, Vector3 worldPosition)
    {
        if (clip == null)
            return;

        if (delay > 0f)
        {
            StartCoroutine(PlayPositionalAfterDelay(clip, volume, pitch, delay, worldPosition));
        }
        else if (delay < 0f)
        {
            float startTime = Mathf.Clamp(-delay, 0f, Mathf.Max(0f, clip.length - 0.01f));
            SpawnPositionalOneShot(clip, volume, pitch, worldPosition, startTime);
        }
        else
        {
            SpawnPositionalOneShot(clip, volume, pitch, worldPosition, 0f);
        }
    }

    private IEnumerator PlayPositionalAfterDelay(AudioClip clip, float volume, float pitch, float delay, Vector3 worldPosition)
    {
        yield return new WaitForSeconds(delay);
        if (clip == null)
            yield break;
        SpawnPositionalOneShot(clip, volume, pitch, worldPosition, 0f);
    }

    private void SpawnPositionalOneShot(AudioClip clip, float volume, float pitch, Vector3 worldPosition, float startTime)
    {
        GameObject go = new GameObject($"OneShot3D_{clip.name}");
        go.transform.position = worldPosition;
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
        src.volume = sfxSource.volume * Mathf.Clamp01(volume);
        src.pitch = pitch;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = PositionalMinDistance;
        src.maxDistance = PositionalMaxDistance;
        src.bypassEffects = sfxSource.bypassEffects;
        src.bypassListenerEffects = sfxSource.bypassListenerEffects;
        src.bypassReverbZones = sfxSource.bypassReverbZones;
        src.playOnAwake = false;
        src.time = startTime;
        src.Play();

        float remaining = (clip.length - startTime) / Mathf.Max(0.01f, Mathf.Abs(pitch));
        Destroy(go, remaining + 0.1f);
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
        sfxSource.volume = value / 3;
        uiSfxSource.volume = value / 3;
    }

    public void UpdateSfxVolume(float value)
    {
        sfxSource.volume = value / 3;
        uiSfxSource.volume = value / 3;
    }
}
