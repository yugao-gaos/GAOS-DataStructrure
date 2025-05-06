using System;
using System.Collections.Generic;
using UnityEngine;
using GAOS.ServiceLocator;

namespace GAOS.DataStructure.References
{
    /// <summary>
    /// ScriptableObject service that manages references to Unity objects.
    /// </summary>
    [CreateAssetMenu(fileName = "UnityReferenceRegistry", menuName = "GAOS/Data Structure/Unity Reference Registry")]
    [Service(typeof(IUnityReferenceRegistry), "Default", ServiceLifetime.Singleton, ServiceContext.RuntimeAndEditor)]
    public class UnityReferenceRegistry : ScriptableObject, IUnityReferenceRegistry
    {
        [Serializable]
        private class ObjectEntry
        {
            public string Key;
            public UnityEngine.Object Value;
        }
        
        [SerializeField] private List<ObjectEntry> _objects = new List<ObjectEntry>();
        
        // Runtime dictionary for faster lookups
        private Dictionary<string, UnityEngine.Object> _objectDictionary;
        
        private void OnEnable()
        {
            InitializeDictionary();
        }
        
        private void InitializeDictionary()
        {
            _objectDictionary = new Dictionary<string, UnityEngine.Object>();
            foreach (var entry in _objects)
            {
                if (!string.IsNullOrEmpty(entry.Key) && entry.Value != null)
                {
                    _objectDictionary[entry.Key] = entry.Value;
                }
            }
        }
        
        /// <summary>
        /// Register an object with the registry
        /// </summary>
        public void RegisterObject(string key, UnityEngine.Object obj)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            
            // Update or add to dictionary
            _objectDictionary[key] = obj;
            
            // Update serialized list
            // First check if key exists
            bool keyExists = false;
            for (int i = 0; i < _objects.Count; i++)
            {
                if (_objects[i].Key == key)
                {
                    _objects[i].Value = obj;
                    keyExists = true;
                    break;
                }
            }
            
            // If key doesn't exist, add a new entry
            if (!keyExists)
            {
                _objects.Add(new ObjectEntry { Key = key, Value = obj });
            }
            
            // Mark the ScriptableObject as dirty
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
            #endif
        }
        
        /// <summary>
        /// Get an object from the registry
        /// </summary>
        public UnityEngine.Object GetObject(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            if (_objectDictionary == null)
                InitializeDictionary();
            
            if (_objectDictionary.TryGetValue(key, out var obj))
                return obj;
            
            return null;
        }
        
        /// <summary>
        /// Get an object from the registry with type casting
        /// </summary>
        public T GetObject<T>(string key) where T : UnityEngine.Object
        {
            var obj = GetObject(key);
            if (obj == null)
                return null;
            
            return obj as T;
        }
        
        /// <summary>
        /// Remove an object from the registry
        /// </summary>
        public bool RemoveObject(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            bool removed = _objectDictionary.Remove(key);
            
            // Also remove from serialized list
            for (int i = 0; i < _objects.Count; i++)
            {
                if (_objects[i].Key == key)
                {
                    _objects.RemoveAt(i);
                    break;
                }
            }
            
            // Mark the ScriptableObject as dirty
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
            #endif
            
            return removed;
        }
        
        /// <summary>
        /// Check if the registry contains an object with the given key
        /// </summary>
        public bool ContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            if (_objectDictionary == null)
                InitializeDictionary();
            
            return _objectDictionary.ContainsKey(key);
        }
    }
} 