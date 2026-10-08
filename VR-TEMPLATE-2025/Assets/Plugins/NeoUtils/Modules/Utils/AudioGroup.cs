// Author: Gabriel Barberiz - https://gabrielbarberiz.notion.site/
// Created: 2024/09/20

using UnityEngine;
using UnityEngine.Audio;

namespace NEO.Utils
{
    /// <summary>
    /// Represents a group of audio clips with an associated AudioMixerGroup.
    /// </summary>
    [System.Serializable]
    public class AudioGroup
    {
        [Tooltip("Mixer for the audio group")]
        public AudioMixerGroup AudioMixerGroup;
        [Tooltip("Number of max instances in the pool")]
        [Range(1,10)]
        public int PoolLenght = 5;
        [Tooltip("List of associated audio clips")]
        public SerializableDictionary<string, Sound> Sounds;
    }

    [System.Serializable]
    public class Sound
    {
        public AudioClip Clip;

        public bool Loop = false;

        [Header("General Settings")]
        [Range(0, 256)]
        public int Priority = 128;
        [Range(0f, 1f)]
        public float Volume = 1;
        [Range(0.1f, 2f)]
        public float Pitch = 1;
        [Range(-1f, 1f)]
        public float Pan = 0;

        [Header("3D Audio Settings")]
        [Tooltip("The distance within which the audio will be at full volume.")]
        public float MinDistance = 1f;

        [Tooltip("The distance beyond which the audio will not be heard.")]
        public float MaxDistance = 50f;
    }
}