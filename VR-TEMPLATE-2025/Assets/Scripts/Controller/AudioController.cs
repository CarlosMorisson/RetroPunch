using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class AudioController : MonoBehaviour
{
    public static AudioController Instance;

    [Header("Library")]
    [SerializeField] private AudioLibrarySO library;

    private Dictionary<string, AudioSource> sources = new();

    private AudioSource musicASource;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildSources();
    }

    public AudioSource PlayMusic(string musicName)
    {
        if (sources.TryGetValue(musicName, out var source))
        {
            musicASource = source;
            source.Play();
            return source;
        }

        Debug.LogWarning($"Music '{musicName}' não encontrada na AudioLibrary.");
        return null;
    }

    private void BuildSources()
    {
        foreach (var entry in library.audios)
        {
            GameObject go = new GameObject($"Audio_{entry.audioName}");
            go.transform.SetParent(transform);

            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = entry.clip;
            source.loop = entry.loop;
            source.volume = entry.volume;
            source.pitch = entry.pitch;
            source.outputAudioMixerGroup = entry.mixerGroup;
            source.playOnAwake = false;

            sources.Add(entry.audioName, source);
        }
    }

    public AudioSource SetMusicAudioSource()
    {
        if (musicASource != null)
            return musicASource;

        musicASource = sources
            .Values
            .FirstOrDefault(s =>
                s.outputAudioMixerGroup != null &&
                s.outputAudioMixerGroup.name == "Music"
            );

        return musicASource;
    }

    #region Public API

    public void Play(string audioName)
    {
        if (!sources.TryGetValue(audioName, out var source))
        {
            Debug.LogWarning($"[AudioManager] Audio '{audioName}' não encontrado.");
            return;
        }

        source.Play();
    }

    public void Stop(string audioName)
    {
        if (sources.TryGetValue(audioName, out var source))
            source.Stop();
    }

    public void SetVolume(string audioName, float volume)
    {
        if (sources.TryGetValue(audioName, out var source))
            source.volume = volume;
    }

    public void SetPitch(string audioName, float pitch)
    {
        if (sources.TryGetValue(audioName, out var source))
            source.pitch = pitch;
    }
    public List<AudioSource> GetPlayingSources()
    {
        List<AudioSource> playing = new();

        foreach (var source in sources.Values)
        {
            if (source != null && source.isPlaying)
                playing.Add(source);
        }

        return playing;
    }
    #endregion
}
