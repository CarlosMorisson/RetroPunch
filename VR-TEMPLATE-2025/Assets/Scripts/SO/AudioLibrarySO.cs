using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Audio/Audio Library")]
public class AudioLibrarySO : ScriptableObject
{
    public List<AudioEntry> audios = new();
}

[System.Serializable]
public class AudioEntry
{
    [Header("ID")]
    public string audioName;

    [Header("Clip")]
    public AudioClip clip;

    [Header("Playback")]
    public bool loop;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float pitch = 1f;

    [Header("Routing")]
    public AudioMixerGroup mixerGroup;
}
