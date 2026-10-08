using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RandomAudioClip : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<AudioClip> audioClips = new List<AudioClip>();
    [SerializeField] private List<AudioClip> usedClips = new List<AudioClip>();
    [SerializeField] private bool playOnStart = true;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        SetRandomClip();

        if (playOnStart)
            audioSource.Play();
    }

    // Escolhe um clip aleatório que ainda não foi usado e coloca no AudioSource
    public AudioClip SetRandomClip()
    {
        if (audioClips.Count == 0)
            return null;

        List<AudioClip> available = new List<AudioClip>();
        foreach (AudioClip clip in audioClips)
        {
            if (clip != null && !usedClips.Contains(clip))
                available.Add(clip);
        }

        // Todos já foram usados: esvazia a lista de usados e recomeça
        if (available.Count == 0)
        {
            usedClips.Clear();
            foreach (AudioClip clip in audioClips)
            {
                if (clip != null)
                    available.Add(clip);
            }

            if (available.Count == 0)
                return null;
        }

        AudioClip chosen = available[Random.Range(0, available.Count)];
        usedClips.Add(chosen);
        audioSource.clip = chosen;
        return chosen;
    }

    // Troca para um novo clip aleatório e toca
    public void PlayRandom()
    {
        if (SetRandomClip() != null)
            audioSource.Play();
    }
}
