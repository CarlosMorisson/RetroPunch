// Author: Gabriel Barberiz - https://gabrielbarberiz.notion.site/
// Created: 2024/09/20

using UnityEngine;

namespace NEO.Utils
{
    /// <summary>
    /// ScriptableObject to manage audio configurations.
    /// Contains a dictionary mapping keys to audio groups.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioConfiguration", menuName = "Configurations/AudioConfig")]
    public class SO_AudioConfig : ScriptableObject
    {
        [Header("Audio Configuration")]
        [Tooltip("Dictionary to store all audios with keys and values.")]
        public SerializableDictionary<string, AudioGroup> Audios = new();
    }
}
