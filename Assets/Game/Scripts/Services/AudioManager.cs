using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [SerializeField] private AudioClip[] musicClip;
    [SerializeField] private AudioClip[] sfxClip;

    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private bool musicEnabled = true;
    [SerializeField] private bool sfxEnabled = true;

    private readonly Dictionary<string, AudioClip> musicDict = new();
    private readonly Dictionary<string, AudioClip> sfxDict = new();

    private readonly Dictionary<string, float> sfxCooldowns = new();
    [SerializeField] private float defaultCooldowns = 0.1f;

    protected override void OnInit()
    {
        InitializeAudioSource();
        BuildDictionaries();
        LoadSettings();
    }

    private void InitializeAudioSource()
    {
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        UpdateVolumes();
    }

    private void BuildDictionaries()
    {
        musicDict.Clear();
        foreach (var clip in musicClip)
        {
            if (clip != null && !musicDict.ContainsKey(clip.name))
            {
                musicDict.Add(clip.name, clip);
            }
        }
        sfxDict.Clear();
        foreach (var clip in sfxClip)
        {
            if (clip != null && !sfxDict.ContainsKey(clip.name))
            {
                sfxDict.Add(clip.name, clip);
            }
        }
    }

    #region Music Methods
    public void PlayMusic(MusicCue cue, bool loop = true)
    {
        PlayMusic(AudioCueCatalog.GetClipName(cue), loop);
    }

    public void PlayMusic(string clipName, bool loop = true)
    {
        if (!musicEnabled) return;
        if (musicDict.TryGetValue(clipName, out AudioClip clip))
        {
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }
        else
        {
            Debug.LogWarning($"Music clip '{clipName}' not found!");
        }
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (!musicEnabled || clip == null) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PauseMusic()
    {
        musicSource.Pause();
    }

    public void ResumeMusic()
    {
        musicSource.UnPause();
    }

    public void FadeOutMusic(float duration = 1f)
    {
        StartCoroutine(FadeAudioSource(musicSource, musicSource.volume, 0f, duration, true));
    }

    public void FadeInMusic(float duration = 1f)
    {
        StartCoroutine(FadeAudioSource(musicSource, 0f, musicVolume, duration, false));
    }
    #endregion

    #region SFX Methods
    public void PlaySFX(SfxCue cue)
    {
        PlaySFX(AudioCueCatalog.GetClipName(cue));
    }

    public void PlaySFX(string clipName)
    {
        TryPlaySfx(clipName, 1f);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (!sfxEnabled || clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlaySFX(SfxCue cue, float volumeScale)
    {
        PlaySFX(AudioCueCatalog.GetClipName(cue), volumeScale);
    }

    public void PlaySFX(string clipName, float volume)
    {
        TryPlaySfx(clipName, volume);
    }

    public void PlaySFXAtPoint(SfxCue cue, Vector3 position)
    {
        PlaySFXAtPoint(AudioCueCatalog.GetClipName(cue), position);
    }

    public void PlaySFXAtPoint(string clipName, Vector3 position)
    {
        if (!sfxEnabled) return;

        if (sfxDict.TryGetValue(clipName, out AudioClip clip))
        {
            AudioSource.PlayClipAtPoint(clip, position, sfxVolume);
        }
        else
        {
            Debug.LogWarning($"SFX clip '{clipName}' not found!");
        }
    }
    #endregion

    #region Volume Control
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicEnabled ? musicVolume : 0f;
        SaveSettings();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
        SaveSettings();
    }

    private void UpdateVolumes()
    {
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
    }

    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        if (!enabled) StopMusic();
        SaveSettings();
    }

    public void SetSFXEnabled(bool enabled)
    {
        sfxEnabled = enabled;
        SaveSettings();
    }
    public bool IsMusicEnabled() => musicEnabled;
    public bool IsSFXEnabled() => sfxEnabled;
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
    #endregion

    #region Helper Methods
    private bool TryPlaySfx(string clipName, float volumeScale)
    {
        if (!sfxEnabled || string.IsNullOrEmpty(clipName)) return false;

        if (sfxCooldowns.TryGetValue(clipName, out float lastPlayedTime)
            && Time.time - lastPlayedTime < defaultCooldowns)
        {
            return false;
        }

        if (!sfxDict.TryGetValue(clipName, out AudioClip clip))
        {
            Debug.LogWarning($"SFX clip '{clipName}' not found!");
            return false;
        }

        sfxSource.PlayOneShot(clip, Mathf.Max(0f, volumeScale) * sfxVolume);
        sfxCooldowns[clipName] = Time.time;
        return true;
    }

    private IEnumerator FadeAudioSource(AudioSource source, float startVol, float endVol, float duration, bool stopOnComplete)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, endVol, elapsed / duration);
            yield return null;
        }

        source.volume = endVol;

        if (stopOnComplete)
            source.Stop();
    }
    #endregion

    #region Save/Load Settings
    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetInt("MusicEnabled", musicEnabled ? 1 : 0);
        PlayerPrefs.SetInt("SFXEnabled", sfxEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        musicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;
        UpdateVolumes();
    }
    #endregion
}
