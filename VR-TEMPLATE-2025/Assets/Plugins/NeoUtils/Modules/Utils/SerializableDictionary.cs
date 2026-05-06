// Author: Gabriel Barberiz - https://gabrielbarberiz.notion.site/
// Created: 2024/09/20

using System;
using System.Collections.Generic;
using UnityEngine;

namespace NEO.Utils
{
    /// <summary>
    /// A serializable dictionary that can be used in Unity's inspector.
    /// Stores keys and values in a structured format for better Inspector organization.
    /// Suitable for small to medium dictionaries due to performance limitations with large datasets.
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        #region Private Variables
        [Serializable]
        private struct KeyValuePair
        {
            public TKey Key;
            public TValue Value;

            public KeyValuePair(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        [SerializeField] private List<KeyValuePair> _valueKeyPairList = new();
        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a key-value pair to the dictionary.
        /// </summary>
        public void Add(TKey key, TValue value)
        {
            if (ContainsKey(key))
            {
                throw new ArgumentException($"Key '{key}' already exists in the dictionary.");
            }

            _valueKeyPairList.Add(new KeyValuePair(key, value));
        }

        /// <summary>
        /// Removes the key-value pair associated with the specified key.
        /// </summary>
        public bool Remove(TKey key)
        {
            var pair = _valueKeyPairList.Find(kvp => EqualityComparer<TKey>.Default.Equals(kvp.Key, key));
            if (!EqualityComparer<KeyValuePair>.Default.Equals(pair, default))
            {
                _valueKeyPairList.Remove(pair);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears all key-value pairs from the dictionary.
        /// </summary>
        public void Clear()
        {
            _valueKeyPairList.Clear();
        }

        /// <summary>
        /// Attempts to retrieve the value associated with the specified key.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            var pair = _valueKeyPairList.Find(kvp => EqualityComparer<TKey>.Default.Equals(kvp.Key, key));
            if (!EqualityComparer<KeyValuePair>.Default.Equals(pair, default))
            {
                value = pair.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Checks if the dictionary contains the specified key.
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            return _valueKeyPairList.Exists(kvp => EqualityComparer<TKey>.Default.Equals(kvp.Key, key));
        }

        /// <summary>
        /// Checks if the dictionary contains the specified value.
        /// </summary>
        public bool ContainsValue(TValue value)
        {
            return _valueKeyPairList.Exists(kvp => EqualityComparer<TValue>.Default.Equals(kvp.Value, value));
        }

        /// <summary>
        /// Gets all keys in the dictionary.
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                foreach (var kvp in _valueKeyPairList)
                {
                    yield return kvp.Key;
                }
            }
        }

        /// <summary>
        /// Gets all values in the dictionary.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var kvp in _valueKeyPairList)
                {
                    yield return kvp.Value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                var pair = _valueKeyPairList.Find(kvp => EqualityComparer<TKey>.Default.Equals(kvp.Key, key));
                if (!EqualityComparer<KeyValuePair>.Default.Equals(pair, default))
                {
                    return pair.Value;
                }

                throw new KeyNotFoundException($"Key '{key}' not found in the dictionary.");
            }
            set
            {
                var pairIndex = _valueKeyPairList.FindIndex(kvp => EqualityComparer<TKey>.Default.Equals(kvp.Key, key));
                if (pairIndex >= 0)
                {
                    _valueKeyPairList[pairIndex] = new KeyValuePair(key, value);
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        /// <summary>
        /// Gets the number of key-value pairs in the dictionary.
        /// </summary>
        public int Count => _valueKeyPairList.Count;

        /// <summary>
        /// Converts the SerializableDictionary to a standard Dictionary<TKey, TValue>.
        /// </summary>
        public Dictionary<TKey, TValue> ToDictionary()
        {
            var result = new Dictionary<TKey, TValue>();
            foreach (var kvp in _valueKeyPairList)
            {
                result[kvp.Key] = kvp.Value;
            }
            return result;
        }
        #endregion

        #region IEnumerable Implementation

        /// <summary>
        /// Gets the enumerator for the SerializableDictionary.
        /// </summary>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var kvp in _valueKeyPairList)
            {
                yield return new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value);
            }
        }

        #endregion
    }
}
