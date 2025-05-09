using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using GAOS.DataStructure.Interfaces;
using GAOS.DataStructure.References;
using GAOS.Logger;

namespace GAOS.DataStructure
{
    /// <summary>
    /// A container for storing and retrieving data of various types.
    /// Supports serialization and deserialization to/from JSON.
    /// </summary>
    [Serializable]
    public class DataContainer : IDataContainer, IDataSerializable, ISerializationCallbackReceiver
    {
        // Internal storage - runtime dictionary (working copy)
        private Dictionary<string, object> _data = new Dictionary<string, object>();
        
        // Storage for Unity serialization - a single JSON string
        [SerializeField] private string _serializedJson = "{}";
        
        // Observer pattern support
        private event Action<string, object, object> _onValueChanged;

        #region Direct Access Methods

        /// <summary>
        /// Gets or sets a value for the specified key.
        /// </summary>
        /// <param name="key">The key to get or set.</param>
        /// <returns>The value associated with the key.</returns>
        public object this[string key]
        {
            get => Get<object>(key);
            set => Set(key, value);
        }

        /// <summary>
        /// Sets a value for the specified key.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key to set.</param>
        /// <param name="value">The value to set.</param>
        public void Set<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            object oldValue = null;
            bool hasOldValue = _data.TryGetValue(key, out oldValue);

            _data[key] = value;
            
            // Notify observers
            if (_onValueChanged != null)
            {
                _onValueChanged.Invoke(key, hasOldValue ? oldValue : null, value);
            }
        }

        /// <summary>
        /// Gets a value for the specified key.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="key">The key to get.</param>
        /// <param name="defaultValue">The default value to return if the key does not exist.</param>
        /// <returns>The value associated with the key, or the default value if the key does not exist.</returns>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            if (_data.TryGetValue(key, out object value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
                
                GLog.Warning<DataSystemLogger>($"Value for key '{key}' is not of type {typeof(T).Name}, but {value.GetType().Name}. Returning default value.");
            }

            return defaultValue;
        }

        /// <summary>
        /// Tries to get a value for the specified key.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="key">The key to get.</param>
        /// <param name="value">The value associated with the key, or the default value if the key does not exist.</param>
        /// <returns>True if the key exists and the value is of the expected type, false otherwise.</returns>
        public bool TryGet<T>(string key, out T value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            value = default;
            
            if (_data.TryGetValue(key, out object objValue) && objValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the container contains a value for the specified key.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        public bool Contains(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            return _data.ContainsKey(key);
        }

        /// <summary>
        /// Removes a value for the specified key.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        public void Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            if (_data.TryGetValue(key, out object oldValue))
            {
                _data.Remove(key);
                
                // Notify observers
                if (_onValueChanged != null)
                {
                    _onValueChanged.Invoke(key, oldValue, null);
                }
            }
        }

        /// <summary>
        /// Gets all keys in the container.
        /// </summary>
        /// <returns>An enumerable of all keys in the container.</returns>
        public IEnumerable<string> GetKeys()
        {
            return _data.Keys;
        }

        /// <summary>
        /// Gets the type of the value for the specified key.
        /// </summary>
        /// <param name="key">The key to get the type for.</param>
        /// <returns>The type of the value, or null if the key does not exist.</returns>
        public Type GetValueType(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            if (_data.TryGetValue(key, out object value))
            {
                return value?.GetType();
            }

            return null;
        }

        /// <summary>
        /// Clears all data in the container.
        /// </summary>
        public void Clear()
        {
            var keys = _data.Keys.ToList();
            foreach (var key in keys)
            {
                Remove(key);
            }
        }

        #endregion

        #region Container Creation Methods

        /// <summary>
        /// Gets or creates a nested container for the specified key.
        /// </summary>
        /// <param name="key">The key to get or create the container for.</param>
        /// <returns>The existing or new container.</returns>
        public DataContainer GetOrCreateContainer(string key)
        {
            if (TryGet<DataContainer>(key, out var container))
            {
                return container;
            }

            container = new DataContainer();
            Set(key, container);
            return container;
        }

        /// <summary>
        /// Gets or creates a list of containers for the specified key.
        /// </summary>
        /// <param name="key">The key to get or create the list for.</param>
        /// <returns>The existing or new list of containers.</returns>
        public List<DataContainer> GetOrCreateList(string key)
        {
            if (TryGet<List<DataContainer>>(key, out var list))
            {
                return list;
            }

            list = new List<DataContainer>();
            Set(key, list);
            return list;
        }

        /// <summary>
        /// Gets or creates a dictionary of containers for the specified key.
        /// </summary>
        /// <param name="key">The key to get or create the dictionary for.</param>
        /// <returns>The existing or new dictionary of containers.</returns>
        public OrderedDictionary<string, DataContainer> GetOrCreateDictionary(string key)
        {
            if (TryGet<OrderedDictionary<string, DataContainer>>(key, out var dictionary))
            {
                return dictionary;
            }

            // No need for legacy Dictionary conversion - we only support OrderedDictionary
            dictionary = new OrderedDictionary<string, DataContainer>();
            Set(key, dictionary);
            return dictionary;
        }

        #endregion

        #region Deep Copy Methods

        /// <summary>
        /// Creates a deep copy of the container.
        /// </summary>
        /// <returns>A new container with the same values.</returns>
        public IDataContainer DeepCopy()
        {
            var copy = new DataContainer();
            
            foreach (var kvp in _data)
            {
                if (kvp.Value is DataContainer nestedContainer)
                {
                    // Deep copy nested containers
                    copy.Set(kvp.Key, nestedContainer.DeepCopy());
                }
                else if (kvp.Value is List<DataContainer> containerList)
                {
                    // Deep copy container lists
                    var newList = new List<DataContainer>();
                    foreach (var item in containerList)
                    {
                        newList.Add((DataContainer)item.DeepCopy());
                    }
                    copy.Set(kvp.Key, newList);
                }
                else if (kvp.Value is OrderedDictionary<string, DataContainer> containerDict)
                {
                    // Deep copy ordered dictionary of containers
                    var newDict = new OrderedDictionary<string, DataContainer>();
                    foreach (var item in containerDict)
                    {
                        newDict[item.Key] = (DataContainer)item.Value.DeepCopy();
                    }
                    copy.Set(kvp.Key, newDict);
                }
                else
                {
                    // For other types, do a simple copy (reference types will still be references)
                    copy.Set(kvp.Key, kvp.Value);
                }
            }
            
            return copy;
        }

        #endregion

        #region Path Utility Methods

        /// <summary>
        /// Creates a path by combining a parent path and a key.
        /// </summary>
        /// <param name="parentPath">The parent path, can be empty for root level.</param>
        /// <param name="key">The key to append.</param>
        /// <returns>Combined path using dot notation.</returns>
        public static string CombinePath(string parentPath, string key)
        {
            if (string.IsNullOrEmpty(parentPath))
                return key;
            return $"{parentPath}.{key}";
        }
        
        /// <summary>
        /// Creates a path for a list item by combining a parent path and an index.
        /// </summary>
        /// <param name="parentPath">Path to the list, can be empty for root level.</param>
        /// <param name="index">The index in the list.</param>
        /// <returns>Combined path with list index notation.</returns>
        public static string CombineListItemPath(string parentPath, int index)
        {
            return $"{parentPath}[{index}]";
        }
        
        /// <summary>
        /// Creates a path for a dictionary item by combining a parent path and a key.
        /// </summary>
        /// <param name="parentPath">Path to the dictionary, can be empty for root level.</param>
        /// <param name="key">The dictionary key.</param>
        /// <returns>Combined path with dictionary key notation.</returns>
        public static string CombineDictionaryItemPath(string parentPath, string key)
        {
            return $"{parentPath}[\"{key}\"]";
        }
        
        /// <summary>
        /// Gets the parent path from a path.
        /// </summary>
        /// <param name="path">The path to get the parent from.</param>
        /// <returns>The parent path, or empty string if the path has no parent.</returns>
        public static string GetParentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
                
            // Handle bracket notation for lists and dictionaries
            int lastBracketIndex = path.LastIndexOf('[');
            if (lastBracketIndex >= 0)
                return path.Substring(0, lastBracketIndex);
                
            // Handle dot notation
            int lastDotIndex = path.LastIndexOf('.');
            if (lastDotIndex >= 0)
                return path.Substring(0, lastDotIndex);
                
            return string.Empty;
        }
        
        /// <summary>
        /// Gets the key or index from a path.
        /// </summary>
        /// <param name="path">The path to get the key from.</param>
        /// <returns>The key or index from the path.</returns>
        public static string GetPathKey(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
                
            // Handle bracket notation for lists and dictionaries
            if (path.EndsWith("]"))
            {
                int openBracket = path.LastIndexOf('[');
                if (openBracket >= 0)
                {
                    string bracketContent = path.Substring(openBracket + 1, path.Length - openBracket - 2);
                    // If it's a dictionary key (has quotes)
                    if (bracketContent.StartsWith("\"") && bracketContent.EndsWith("\""))
                        return bracketContent.Substring(1, bracketContent.Length - 2);
                    return bracketContent; // List index
                }
            }
            
            // Handle dot notation
            int lastDotIndex = path.LastIndexOf('.');
            if (lastDotIndex >= 0)
                return path.Substring(lastDotIndex + 1);
                
            return path;
        }
        
        /// <summary>
        /// Determines if a path refers to a list index.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path ends with a numeric index in brackets.</returns>
        public static bool IsListIndexPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.EndsWith("]"))
                return false;
                
            int openBracket = path.LastIndexOf('[');
            if (openBracket < 0)
                return false;
                
            string bracketContent = path.Substring(openBracket + 1, path.Length - openBracket - 2);
            return int.TryParse(bracketContent, out _);
        }
        
        /// <summary>
        /// Determines if a path refers to a dictionary key.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path ends with a quoted key in brackets.</returns>
        public static bool IsDictionaryKeyPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.EndsWith("]"))
                return false;
                
            int openBracket = path.LastIndexOf('[');
            if (openBracket < 0)
                return false;
                
            string bracketContent = path.Substring(openBracket + 1, path.Length - openBracket - 2);
            return bracketContent.StartsWith("\"") && bracketContent.EndsWith("\"");
        }

        /// <summary>
        /// Splits a path into segments.
        /// </summary>
        /// <param name="path">The path to split.</param>
        /// <returns>List of path segments.</returns>
        public static List<PathSegment> ParsePath(string path)
        {
            var segments = new List<PathSegment>();
            
            if (string.IsNullOrEmpty(path))
                return segments;
            
            // This regex handles property names, list indexes [0], and dictionary keys ["key"]
            var matches = Regex.Matches(path, @"([^.\[\]]+)(?:\[((?:[0-9]+)|(?:""[^""]+?""))\])?");
            
            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;
                
                var segment = new PathSegment { Raw = match.Value };
                
                // Extract the property name
                if (match.Groups[1].Success)
                    segment.PropertyName = match.Groups[1].Value;
                
                // Extract indexed access if present
                if (match.Groups[2].Success)
                {
                    string indexOrKey = match.Groups[2].Value;
                    if (indexOrKey.StartsWith("\"") && indexOrKey.EndsWith("\""))
                    {
                        // Dictionary key
                        segment.Type = PathSegmentType.DictionaryKey;
                        segment.DictionaryKey = indexOrKey.Substring(1, indexOrKey.Length - 2);
                    }
                    else if (int.TryParse(indexOrKey, out int index))
                    {
                        // List index
                        segment.Type = PathSegmentType.ListIndex;
                        segment.ListIndex = index;
                    }
                }
                else
                {
                    segment.Type = PathSegmentType.Property;
                }
                
                segments.Add(segment);
            }
            
            return segments;
        }

        #endregion

        #region Path Access Methods

        /// <summary>
        /// Internal helper method to get a container at the specified path.
        /// Use PathGet&lt;DataContainer&gt; for public access.
        /// </summary>
        /// <param name="path">The path to the container (dot-separated).</param>
        /// <returns>The container at the specified path, or null if it doesn't exist.</returns>
        [Obsolete("Use ParsePath and PathNavigate methods instead")]
        internal DataContainer GetContainerAtPathHelper(string path)
        {
            if (string.IsNullOrEmpty(path))
                return this;

            var parts = path.Split('.');
            var current = this;

            for (int i = 0; i < parts.Length; i++)
            {
                if (!current.TryGet<DataContainer>(parts[i], out var next))
                {
                    return null;
                }
                current = next;
            }

            return current;
        }

        /// <summary>
        /// Gets a container at the specified path.
        /// Maintained for backward compatibility.
        /// Consider using PathGet&lt;DataContainer&gt; instead.
        /// </summary>
        /// <param name="path">The path to the container (dot-separated).</param>
        /// <returns>The container at the specified path, or null if it doesn't exist.</returns>
        [System.Obsolete("Use PathGet<DataContainer> instead")]
        public DataContainer Path(string path)
        {
            return GetContainerAtPathHelper(path);
        }

        /// <summary>
        /// Gets a value at the specified path.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="path">The path to the value.</param>
        /// <param name="defaultValue">The default value to return if the path does not exist.</param>
        /// <returns>The value at the specified path, or the default value if the path does not exist.</returns>
        public T PathGet<T>(string path, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            // Parse the path into segments
            var segments = ParsePath(path);
            if (segments.Count == 0)
                return defaultValue;

            // Navigate to the final container
            return (T)PathNavigate(this, segments, 0, typeof(T), defaultValue);
        }

        /// <summary>
        /// Sets a value at the specified path.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="path">The path to the value.</param>
        /// <param name="value">The value to set.</param>
        public void PathSet<T>(string path, T value)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            // Parse the path into segments
            var segments = ParsePath(path);
            if (segments.Count == 0)
                return;
            
            // Navigate to the parent container, creating containers as needed
            var parentContainer = this;
            int lastIndex = segments.Count - 1;
            
            // Navigate through all but the last segment
            for (int i = 0; i < lastIndex; i++)
            {
                var segment = segments[i];
                
                // Handle property segment
                if (segment.Type == PathSegmentType.Property)
                {
                    // Try to get the existing container or create a new one
                    if (!parentContainer.TryGet<DataContainer>(segment.PropertyName, out var nextContainer))
                    {
                        nextContainer = new DataContainer();
                        parentContainer.Set(segment.PropertyName, nextContainer);
                    }
                    parentContainer = nextContainer;
                }
                // Handle list index segment
                else if (segment.Type == PathSegmentType.ListIndex)
                {
                    var list = parentContainer.GetOrCreateList(segment.PropertyName);
                    
                    // Ensure the list is big enough
                    while (list.Count <= segment.ListIndex)
                    {
                        list.Add(new DataContainer());
                    }
                    
                    parentContainer = list[segment.ListIndex];
                }
                // Handle dictionary key segment
                else if (segment.Type == PathSegmentType.DictionaryKey)
                {
                    var dict = parentContainer.GetOrCreateDictionary(segment.PropertyName);
                    
                    // Get or create the container for this key
                    if (!dict.TryGetValue(segment.DictionaryKey, out var nextContainer))
                    {
                        nextContainer = new DataContainer();
                        dict[segment.DictionaryKey] = nextContainer;
                    }
                    
                    parentContainer = nextContainer;
                }
            }
            
            // Handle the final segment to set the value
            var lastSegment = segments[lastIndex];
            
            if (lastSegment.Type == PathSegmentType.Property)
            {
                parentContainer.Set(lastSegment.PropertyName, value);
            }
            else if (lastSegment.Type == PathSegmentType.ListIndex)
            {
                var list = parentContainer.GetOrCreateList(lastSegment.PropertyName);
                
                // Ensure the list is big enough
                while (list.Count <= lastSegment.ListIndex)
                {
                    if (typeof(T) == typeof(DataContainer))
                        list.Add(new DataContainer());
                    else
                        list.Add(null);
                }
                
                if (typeof(T) == typeof(DataContainer))
                    list[lastSegment.ListIndex] = (DataContainer)(object)value;
                else
                    throw new InvalidOperationException($"Cannot set value of type {typeof(T).Name} at list index. Only DataContainer is supported.");
            }
            else if (lastSegment.Type == PathSegmentType.DictionaryKey)
            {
                var dict = parentContainer.GetOrCreateDictionary(lastSegment.PropertyName);
                
                if (typeof(T) == typeof(DataContainer))
                    dict[lastSegment.DictionaryKey] = (DataContainer)(object)value;
                else
                    throw new InvalidOperationException($"Cannot set value of type {typeof(T).Name} at dictionary key. Only DataContainer is supported.");
            }
        }

        /// <summary>
        /// Helper method to navigate a path and get the value.
        /// </summary>
        /// <param name="container">The starting container.</param>
        /// <param name="segments">The path segments to navigate.</param>
        /// <param name="currentIndex">The current segment index.</param>
        /// <param name="expectedType">The expected type of the final value.</param>
        /// <param name="defaultValue">The default value to return if navigation fails.</param>
        /// <returns>The value at the path, or the default value if navigation fails.</returns>
        private object PathNavigate(DataContainer container, List<PathSegment> segments, int currentIndex, Type expectedType, object defaultValue)
        {
            if (container == null || currentIndex >= segments.Count)
                return defaultValue;
            
            var segment = segments[currentIndex];
            bool isLastSegment = (currentIndex == segments.Count - 1);
            
            // Handle property segment
            if (segment.Type == PathSegmentType.Property)
            {
                // If this is the last segment, get the actual value
                if (isLastSegment)
                {
                    // Use reflection to get the value with the correct type
                    var getMethod = typeof(DataContainer).GetMethod("Get").MakeGenericMethod(expectedType);
                    return getMethod.Invoke(container, new[] { segment.PropertyName, defaultValue });
                }
                // Otherwise, navigate to the next container
                else
                {
                    if (!container.TryGet<DataContainer>(segment.PropertyName, out var nextContainer))
                        return defaultValue;
                    
                    return PathNavigate(nextContainer, segments, currentIndex + 1, expectedType, defaultValue);
                }
            }
            // Handle list index segment
            else if (segment.Type == PathSegmentType.ListIndex)
            {
                if (!container.TryGet<List<DataContainer>>(segment.PropertyName, out var list))
                    return defaultValue;
                
                if (segment.ListIndex < 0 || segment.ListIndex >= list.Count)
                    return defaultValue;
                
                var listItem = list[segment.ListIndex];
                
                // If this is the last segment and we expect DataContainer, return the item
                if (isLastSegment && expectedType == typeof(DataContainer))
                    return listItem;
                
                // If this is the last segment but we expect a different type, we can't access it
                if (isLastSegment)
                    return defaultValue;
                
                // Otherwise, navigate deeper
                return PathNavigate(listItem, segments, currentIndex + 1, expectedType, defaultValue);
            }
            // Handle dictionary key segment
            else if (segment.Type == PathSegmentType.DictionaryKey)
        {
                if (!container.TryGet<OrderedDictionary<string, DataContainer>>(segment.PropertyName, out var dict))
                    return defaultValue;
                
                if (!dict.TryGetValue(segment.DictionaryKey, out var dictItem))
                    return defaultValue;
                
                // If this is the last segment and we expect DataContainer, return the item
                if (isLastSegment && expectedType == typeof(DataContainer))
                    return dictItem;
                
                // If this is the last segment but we expect a different type, we can't access it
                if (isLastSegment)
                    return defaultValue;
                
                // Otherwise, navigate deeper
                return PathNavigate(dictItem, segments, currentIndex + 1, expectedType, defaultValue);
        }
            
            return defaultValue;
        }

        #endregion

        #region Unity Object Methods

        /// <summary>
        /// Sets a Unity object reference in the container.
        /// </summary>
        /// <typeparam name="T">The type of Unity object</typeparam>
        /// <param name="key">The key to store the reference under</param>
        /// <param name="obj">The Unity object to reference</param>
        /// <param name="storageType">How the reference should be stored</param>
        /// <param name="referenceKey">Optional custom key for the reference (defaults to object name)</param>
        public void SetUnityObject<T>(string key, T obj, ReferenceStorageType storageType = ReferenceStorageType.Registry, string referenceKey = null) where T : UnityEngine.Object
        {
            if (obj == null)
            {
                Remove(key);
                return;
            }
            
            // Use object name as key if not specified
            if (string.IsNullOrEmpty(referenceKey))
            {
                referenceKey = obj.name;
            }
            
            // Create and store the reference
            var reference = new UnityObjectReference(obj, storageType, referenceKey);
            Set(key, reference);
        }
        
        /// <summary>
        /// Gets a Unity object reference from the container.
        /// </summary>
        /// <typeparam name="T">The type of Unity object to get</typeparam>
        /// <param name="key">The key the reference is stored under</param>
        /// <param name="defaultValue">Default value if reference is not found or invalid</param>
        /// <returns>The referenced Unity object, or defaultValue if not found</returns>
        public T GetUnityObject<T>(string key, T defaultValue = null) where T : UnityEngine.Object
        {
            if (TryGet<UnityObjectReference>(key, out var reference))
            {
                var obj = reference.GetObject<T>();
                return obj != null ? obj : defaultValue;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Sets a Unity object reference at the specified path.
        /// </summary>
        /// <typeparam name="T">The type of Unity object</typeparam>
        /// <param name="path">The path to set the reference at</param>
        /// <param name="obj">The Unity object to reference</param>
        /// <param name="storageType">How the reference should be stored</param>
        /// <param name="referenceKey">Optional custom key for the reference</param>
        public void PathSetUnityObject<T>(string path, T obj, ReferenceStorageType storageType = ReferenceStorageType.Registry, string referenceKey = null) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            if (obj == null)
            {
                PathSet<UnityObjectReference>(path, null);
                return;
            }

            // Use object name as key if not specified
            if (string.IsNullOrEmpty(referenceKey))
            {
                referenceKey = obj.name;
            }
            
            // Create the reference
            var reference = new UnityObjectReference(obj, storageType, referenceKey);
            
            // Store using existing path setter
            PathSet(path, reference);
        }
        
        /// <summary>
        /// Gets a Unity object reference from the specified path.
        /// </summary>
        /// <typeparam name="T">The type of Unity object to get</typeparam>
        /// <param name="path">The path to get the reference from</param>
        /// <param name="defaultValue">Default value if reference is not found or invalid</param>
        /// <returns>The referenced Unity object, or defaultValue if not found</returns>
        public T PathGetUnityObject<T>(string path, T defaultValue = null) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
                
            var reference = PathGet<UnityObjectReference>(path);
            if (reference != null)
            {
                var obj = reference.GetObject<T>();
                return obj != null ? obj : defaultValue;
        }
            return defaultValue;
        }

        #endregion

        #region Serialization Methods

        /// <summary>
        /// Called before Unity serializes this object.
        /// </summary>
        public void OnBeforeSerialize()
        {
            // Convert the in-memory dictionary to JSON string for serialization
            _serializedJson = ToJson();
        }

        /// <summary>
        /// Called after Unity deserializes this object.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // Initialize the in-memory dictionary from the serialized JSON
            _data = new Dictionary<string, object>();
            FromJson(_serializedJson);
        }

        /// <summary>
        /// Adds an observer for value changes.
        /// </summary>
        /// <param name="observer">The action to call when a value changes.</param>
        public void AddObserver(Action<string, object, object> observer)
        {
            _onValueChanged += observer;
        }

        /// <summary>
        /// Removes an observer for value changes.
        /// </summary>
        /// <param name="observer">The action to remove.</param>
        public void RemoveObserver(Action<string, object, object> observer)
        {
            _onValueChanged -= observer;
        }

        /// <summary>
        /// Converts the container to a JSON string.
        /// </summary>
        /// <returns>A JSON string representing the container.</returns>
        public string ToJson()
        {
            var data = new Dictionary<string, string>();
            var typeInfo = new Dictionary<string, string>();
            
            foreach (var kvp in _data)
            {
                // Special handling for string type to prevent type loss
                if (kvp.Value is string)
                {
                    typeInfo[kvp.Key] = typeof(string).AssemblyQualifiedName;
                }
                else
                {
                    typeInfo[kvp.Key] = kvp.Value?.GetType().AssemblyQualifiedName ?? "null";
                }
                
                // All values are serialized to string, including nested containers
                data[kvp.Key] = SerializationHelpers.SerializeValue(kvp.Value);
            }
            
            var wrapper = new JsonWrapper 
            { 
                Data = data, 
                TypeInfo = typeInfo 
            };
            
            return UnityEngine.JsonUtility.ToJson(wrapper);
        }

        /// <summary>
        /// Populates the container from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        public void FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                var wrapper = UnityEngine.JsonUtility.FromJson<JsonWrapper>(json);
                if (wrapper == null)
                    return;

                Clear();
                
                foreach (var kvp in wrapper.Data)
                {
                    var key = kvp.Key;
                    var serializedValue = kvp.Value;
                    var typeString = wrapper.TypeInfo[key];
                    
                    if (typeString == "null")
                    {
                        _data[key] = null;
                        continue;
                    }
                    
                    var type = Type.GetType(typeString);
                    if (type == null)
                    {
                        GLog.Warning<DataSystemLogger>($"Could not find type {typeString} for key {key}. Skipping.");
                        continue;
                    }
                    
                    // Special handling for string type with empty values
                    if (type == typeof(string) && string.IsNullOrEmpty(serializedValue))
                    {
                        _data[key] = string.Empty; // Ensure we store an empty string, not null
                        continue;
                    }
                    
                    // Special handling for DataContainer
                    if (type == typeof(DataContainer))
                    {
                        var container = new DataContainer();
                        container.FromJson(serializedValue);
                        _data[key] = container;
                        continue;
                    }
                    
                    var value = SerializationHelpers.DeserializeValue(serializedValue, type);
                    _data[key] = value;
                }
            }
            catch (Exception ex)
            {
                GLog.Error<DataSystemLogger>($"Error deserializing container: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper class for JSON serialization and deserialization.
        /// </summary>
        [Serializable]
        private class JsonWrapper
        {
            [Serializable]
            public class SerializableDictionaryEntry
            {
                public string Key;
                public string Value;
            }
            
            [SerializeField] private List<SerializableDictionaryEntry> _data = new List<SerializableDictionaryEntry>();
            [SerializeField] private List<SerializableDictionaryEntry> _typeInfo = new List<SerializableDictionaryEntry>();
            
            public Dictionary<string, string> Data
            {
                get
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var entry in _data)
                    {
                        dict[entry.Key] = entry.Value;
                    }
                    return dict;
                }
                set
                {
                    _data.Clear();
                    foreach (var kvp in value)
                    {
                        _data.Add(new SerializableDictionaryEntry { Key = kvp.Key, Value = kvp.Value });
                    }
                }
            }
            
            public Dictionary<string, string> TypeInfo
            {
                get
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var entry in _typeInfo)
                    {
                        dict[entry.Key] = entry.Value;
                    }
                    return dict;
                }
                set
                {
                    _typeInfo.Clear();
                    foreach (var kvp in value)
                    {
                        _typeInfo.Add(new SerializableDictionaryEntry { Key = kvp.Key, Value = kvp.Value });
                    }
                }
            }
        }

        #endregion
    }

    #region Path Support Classes
        
        /// <summary>
    /// Enum representing the type of a path segment.
        /// </summary>
    public enum PathSegmentType
    {
        /// <summary>Simple property</summary>
        Property,
        /// <summary>List index</summary>
        ListIndex,
        /// <summary>Dictionary key</summary>
        DictionaryKey
        }
        
        /// <summary>
    /// Class representing a segment in a path.
        /// </summary>
    public class PathSegment
    {
        /// <summary>Raw path segment text</summary>
        public string Raw;
        /// <summary>Property name (always present)</summary>
        public string PropertyName;
        /// <summary>Type of path segment</summary>
        public PathSegmentType Type = PathSegmentType.Property;
        /// <summary>List index (only for ListIndex type)</summary>
        public int ListIndex;
        /// <summary>Dictionary key (only for DictionaryKey type)</summary>
        public string DictionaryKey;
    }

    #endregion
} 