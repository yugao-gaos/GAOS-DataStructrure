using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

namespace GAOS.DataStructure
{
    /// <summary>
    /// A dictionary that preserves insertion order while maintaining O(1) lookup performance.
    /// Can be serialized to JSON while preserving order.
    /// </summary>
    [Serializable]
    public class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializable
    {
        // This will be serialized to JSON
        private List<KeyValuePair> _entries = new List<KeyValuePair>();
        
        // Runtime lookup (not serialized)
        [NonSerialized]
        private Dictionary<TKey, TValue> _dictionary;
        
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
        
        #region Unity Lifecycle
        
        // Called after domain reload in ScriptableObjects and MonoBehaviours
        public void OnEnable()
        {
            // Ensure dictionary is initialized after domain reload
            EnsureDictionaryInitialized();
            
            // Only log when we have entries to report
            if (_entries.Count > 0)
            {
                Debug.Log($"OrderedDictionary initialized with {_entries.Count} entries");
            }
        }
        
        #endregion
        
        #region Constructors
        
        public OrderedDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }
        
        public OrderedDictionary(int capacity)
        {
            _dictionary = new Dictionary<TKey, TValue>(capacity);
            _entries = new List<KeyValuePair>(capacity);
        }
        
        public OrderedDictionary(IEqualityComparer<TKey> comparer)
        {
            _dictionary = new Dictionary<TKey, TValue>(comparer);
        }
        
        public OrderedDictionary(IDictionary<TKey, TValue> dictionary)
        {
            _dictionary = new Dictionary<TKey, TValue>(dictionary);
            
            foreach (var kvp in dictionary)
            {
                _entries.Add(new KeyValuePair(kvp.Key, kvp.Value));
            }
        }
        
        protected OrderedDictionary(SerializationInfo info, StreamingContext context)
        {
            // Deserialize from JSON
            _entries = (List<KeyValuePair>)info.GetValue("entries", typeof(List<KeyValuePair>));
            _dictionary = new Dictionary<TKey, TValue>();
            
            // Rebuild dictionary
            foreach (var entry in _entries)
            {
                _dictionary[entry.Key] = entry.Value;
            }
        }
        
        #endregion
        
        #region IDictionary Implementation
        
        public TValue this[TKey key]
        {
            get 
            {
                EnsureDictionaryInitialized();
                return _dictionary[key];
            }
            set
            {
                EnsureDictionaryInitialized();
                
                // Update dictionary
                _dictionary[key] = value;
                
                // Update entries list
                int index = FindEntryIndex(key);
                if (index >= 0)
                {
                    // Update existing entry
                    var entry = _entries[index];
                    entry.Value = value;
                    _entries[index] = entry;
                }
                else
                {
                    // Add new entry at the end
                    _entries.Add(new KeyValuePair(key, value));
                }
            }
        }
        
        public ICollection<TKey> Keys 
        {
            get { return _entries.Select(e => e.Key).ToList(); }
        }
        
        public ICollection<TValue> Values
        {
            get { return _entries.Select(e => e.Value).ToList(); }
        }
        
        public int Count 
        {
            get { return _entries.Count; }
        }
        
        public bool IsReadOnly => false;
        
        public void Add(TKey key, TValue value)
        {
            EnsureDictionaryInitialized();
            
            if (_dictionary.ContainsKey(key))
                throw new ArgumentException($"Key '{key}' already exists in the dictionary");
            
            _dictionary.Add(key, value);
            _entries.Add(new KeyValuePair(key, value));
        }
        
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }
        
        public void Clear()
        {
            EnsureDictionaryInitialized();
            _dictionary.Clear();
            _entries.Clear();
        }
        
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            EnsureDictionaryInitialized();
            return _dictionary.TryGetValue(item.Key, out var value) && 
                   EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }
        
        public bool ContainsKey(TKey key)
        {
            EnsureDictionaryInitialized();
            return _dictionary.ContainsKey(key);
        }
        
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
                
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Destination array is not large enough");
                
            int i = arrayIndex;
            foreach (var entry in _entries)
            {
                array[i++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
            }
        }
        
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var entry in _entries)
            {
                yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
            }
        }
        
        public bool Remove(TKey key)
        {
            EnsureDictionaryInitialized();
            
            if (!_dictionary.Remove(key))
                return false;
                
            int index = FindEntryIndex(key);
            if (index >= 0)
            {
                _entries.RemoveAt(index);
                return true;
            }
            
            return false;
        }
        
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!Contains(item))
                return false;
                
            return Remove(item.Key);
        }
        
        public bool TryGetValue(TKey key, out TValue value)
        {
            EnsureDictionaryInitialized();
            return _dictionary.TryGetValue(key, out value);
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        #endregion
        
        #region ISerializable Implementation
        
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("entries", _entries, typeof(List<KeyValuePair>));
        }
        
        #endregion
        
        #region OrderedDictionary Specific Methods
        
        /// <summary>
        /// Returns the keys in their order.
        /// </summary>
        public IEnumerable<TKey> OrderedKeys => _entries.Select(e => e.Key);
        
        /// <summary>
        /// Returns the values in their keys' order.
        /// </summary>
        public IEnumerable<TValue> OrderedValues => _entries.Select(e => e.Value);
        
        /// <summary>
        /// Moves an entry to a specific index in the ordered collection.
        /// </summary>
        public void MoveEntry(TKey key, int newIndex)
        {
            if (newIndex < 0 || newIndex >= _entries.Count)
                throw new ArgumentOutOfRangeException(nameof(newIndex));
                
            int currentIndex = FindEntryIndex(key);
            if (currentIndex < 0)
                throw new KeyNotFoundException($"The key '{key}' was not found in the dictionary.");
                
            if (currentIndex == newIndex)
                return;
                
            var entry = _entries[currentIndex];
            _entries.RemoveAt(currentIndex);
            
            // Adjust index if removing from before the insert point
            if (newIndex > currentIndex)
                newIndex--;
                
            _entries.Insert(newIndex, entry);
        }
        
        /// <summary>
        /// Gets the index of a key in the ordered collection.
        /// </summary>
        public int IndexOf(TKey key)
        {
            return FindEntryIndex(key);
        }
        
        /// <summary>
        /// Gets a key at the specified index.
        /// </summary>
        public TKey GetKeyAtIndex(int index)
        {
            if (index < 0 || index >= _entries.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
                
            return _entries[index].Key;
        }
        
        /// <summary>
        /// Gets a value at the specified index.
        /// </summary>
        public TValue GetValueAtIndex(int index)
        {
            if (index < 0 || index >= _entries.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
                
            return _entries[index].Value;
        }
        
        #endregion
        
        #region Private Helpers
        
        private int FindEntryIndex(TKey key)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (EqualityComparer<TKey>.Default.Equals(_entries[i].Key, key))
                    return i;
            }
            return -1;
        }
        
        private void EnsureDictionaryInitialized()
        {
            if (_dictionary == null)
            {
                _dictionary = new Dictionary<TKey, TValue>();
                foreach (var entry in _entries)
                {
                    _dictionary[entry.Key] = entry.Value;
                }
            }
        }
        
        #endregion
    }
} 