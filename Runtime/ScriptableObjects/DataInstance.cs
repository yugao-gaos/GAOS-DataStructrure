using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAOS.DataStructure
{
    /// <summary>
    /// ScriptableObject that contains a data instance based on a DataStructure template.
    /// Instead of storing a full copy of the data container, it only stores overridden values.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDataInstance", menuName = "GAOS/Data/Data Instance")]
    public abstract class DataInstance : ScriptableObject
    {
        [SerializeField] private DataStructure _parentStructure;
        [SerializeField] private string _instanceId;
        
        // Store only overrides as path-value pairs
        [SerializeField] private List<OverrideEntry> _overrides = new List<OverrideEntry>();
        
        // Runtime-only container (not serialized)
        [NonSerialized] private DataContainer _runtimeContainer;
        
        // Dictionary to store original metadata values for fields changed at runtime
        [NonSerialized] private Dictionary<string, object> _originalMetadataValues = new Dictionary<string, object>();
        
        /// <summary>
        /// Serializable entry for storing path-value overrides.
        /// </summary>
        [Serializable]
        private class OverrideEntry
        {
            public string Path;
            public string TypeName;
            public string SerializedValue;
        }
        
        /// <summary>
        /// The unique identifier for this instance.
        /// </summary>
        public string InstanceId => string.IsNullOrEmpty(_instanceId) ? name : _instanceId;
        
        /// <summary>
        /// The parent structure that defines the structure of this instance.
        /// </summary>
        public DataStructure ParentStructure => _parentStructure;
        
        /// <summary>
        /// The runtime data container for this instance.
        /// Initialized on demand with the parent structure and all overrides applied.
        /// </summary>
        protected internal DataContainer Container 
        {
            get 
            {
                if (_runtimeContainer == null)
                {
                    InitializeRuntimeContainer();
                }
                return _runtimeContainer;
            }
        }

        /// <summary>
        /// Initializes the instance with a parent structure and a container.
        /// </summary>
        /// <param name="parent">The parent structure.</param>
        /// <param name="container">The container with initial values.</param>
        internal void Initialize(DataStructure parent, DataContainer container)
        {
            _parentStructure = parent;
            
            if (string.IsNullOrEmpty(_instanceId))
            {
                _instanceId = Guid.NewGuid().ToString();
            }
            
            // Convert container to overrides
            if (container != null && parent != null)
            {
                CalculateOverrides(parent.Container, container);
            }
            
            _runtimeContainer = null;
        }
        
        /// <summary>
        /// Calculates overrides by comparing the provided container with the parent structure's container.
        /// </summary>
        /// <param name="parentContainer">The parent container.</param>
        /// <param name="instanceContainer">The instance container.</param>
        private void CalculateOverrides(DataContainer parentContainer, DataContainer instanceContainer)
        {
            _overrides.Clear();
            var overriddenValues = new Dictionary<string, object>();
            CompareContainers("", parentContainer, instanceContainer, overriddenValues);
            
            foreach (var kvp in overriddenValues)
            {
                var entry = new OverrideEntry
                {
                    Path = kvp.Key,
                    TypeName = kvp.Value.GetType().AssemblyQualifiedName,
                    SerializedValue = SerializationHelpers.SerializeValue(kvp.Value)
                };
                
                _overrides.Add(entry);
            }
        }

        /// <summary>
        /// Initializes the runtime container by creating a copy of the parent structure's container
        /// and applying all overrides.
        /// </summary>
        private void InitializeRuntimeContainer()
        {
            if (_parentStructure == null)
            {
                Debug.LogError($"DataInstance {name} has no parent structure assigned.");
                _runtimeContainer = new DataContainer();
                return;
            }
            
            // Create a copy of the parent structure's container
            _runtimeContainer = (DataContainer)_parentStructure.Container.DeepCopy();
            
            // Apply all overrides
            foreach (var entry in _overrides)
            {
                try
                {
                    var type = Type.GetType(entry.TypeName);
                    if (type == null)
                    {
                        Debug.LogWarning($"Could not resolve type {entry.TypeName} for path: {entry.Path}");
                        continue;
                    }
                    
                    object value = SerializationHelpers.DeserializeValue(entry.SerializedValue, type);
                    _runtimeContainer.PathSet(entry.Path, value);
                }
                catch (Exception ex)
                {
                    // Path no longer exists or type incompatible - just skip it
                    Debug.LogWarning($"Could not apply override for path: {entry.Path}. Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Resets the instance by clearing all overrides.
        /// </summary>
        protected internal void Reset()
        {
            _overrides.Clear();
            _runtimeContainer = null; // Force recreation next time it's accessed
        }

        /// <summary>
        /// Gets a value from the container for the specified path.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="path">The path to get.</param>
        /// <param name="defaultValue">The default value to return if the path does not exist.</param>
        /// <returns>The current runtime value at the specified path.</returns>
        public T GetValue<T>(string path, T defaultValue = default)
        {
            return Container.PathGet(path, defaultValue);
        }
        
        /// <summary>
        /// Gets the original metadata value (template + overrides without runtime changes) for the specified path.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="path">The path to get.</param>
        /// <param name="defaultValue">The default value to return if the path does not exist.</param>
        /// <returns>The original metadata value at the specified path.</returns>
        public T GetMetadataValue<T>(string path, T defaultValue = default)
        {
            // If we have a stored original value, return it
            if (_originalMetadataValues.TryGetValue(path, out var originalValue))
            {
                return (T)originalValue;
            }
            
            // If not modified at runtime, current value is the original
            return Container.PathGet(path, defaultValue);
        }
        
        /// <summary>
        /// Sets a value for the specified path. In editor, this creates an override; at runtime, it only changes the runtime value.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="path">The path to set.</param>
        /// <param name="value">The value to set.</param>
        public void SetValue<T>(string path, T value)
        {
            #if UNITY_EDITOR
            // Only create overrides in editor AND when not in play mode
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                SetOverrideInternal<T>(path, value);
                return;
            }
            #endif
            
            // In all other cases (play mode or builds), just use runtime values
            SetRuntimeValueInternal<T>(path, value);
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Internal method to set an override value in editor mode.
        /// </summary>
        private void SetOverrideInternal<T>(string path, T value)
        {
            // Set in runtime container
            Container.PathSet(path, value);
            
            // Record as an override
            UpdateOverride(path, value);
        }
        #endif
        
        /// <summary>
        /// Internal method to set a runtime value without creating an override.
        /// </summary>
        private void SetRuntimeValueInternal<T>(string path, T value)
        {
            // Store original value if this is the first modification
            if (!_originalMetadataValues.ContainsKey(path))
            {
                _originalMetadataValues[path] = Container.PathGet<object>(path);
            }
            
            // Update runtime container only
            Container.PathSet(path, value);
        }

        /// <summary>
        /// Removes an override for the specified path.
        /// </summary>
        /// <param name="path">The path to remove the override for.</param>
        /// <returns>True if an override was removed, false otherwise.</returns>
        protected internal bool RemoveOverride(string path)
        {
            int initialCount = _overrides.Count;
            _overrides.RemoveAll(o => o.Path == path);
            
            if (_overrides.Count != initialCount)
            {
                // If we had a runtime container, reset the value to the parent value
                if (_runtimeContainer != null)
                {
                    // Get the value from the parent
                    var parentValue = _parentStructure.Container.PathGet<object>(path);
                    // Set it in our runtime container
                    _runtimeContainer.PathSet(path, parentValue);
                }
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Updates or adds an override for the specified path.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="path">The path to update.</param>
        /// <param name="value">The value to set.</param>
        private void UpdateOverride<T>(string path, T value)
        {
            // Check if the value matches the parent structure's value
            if (_parentStructure != null)
            {
                if (_parentStructure.Container.PathGet<T>(path, default).Equals(value))
                {
                    // Value is the same as parent, so remove the override
                    RemoveOverride(path);
                    return;
                }
            }
            
            // Remove existing override with this path if it exists
            _overrides.RemoveAll(o => o.Path == path);
            
            // Add new override
            var entry = new OverrideEntry
            {
                Path = path,
                TypeName = typeof(T).AssemblyQualifiedName,
                SerializedValue = SerializationHelpers.SerializeValue(value)
            };
            
            _overrides.Add(entry);
        }

        /// <summary>
        /// Gets all override entries.
        /// </summary>
        /// <returns>Dictionary mapping paths to values.</returns>
        protected internal Dictionary<string, object> GetAllOverrides()
        {
            var result = new Dictionary<string, object>();
            
            foreach (var entry in _overrides)
            {
                try
                {
                    var type = Type.GetType(entry.TypeName);
                    if (type != null)
                    {
                        result[entry.Path] = SerializationHelpers.DeserializeValue(entry.SerializedValue, type);
                    }
                }
                catch (Exception)
                {
                    // Skip entries that can't be deserialized
                }
            }
            
            return result;
        }

        /// <summary>
        /// Checks if a path has an override.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path has an override, false otherwise.</returns>
        protected internal bool HasOverride(string path)
        {
            return _overrides.Exists(o => o.Path == path);
        }
        
        /// <summary>
        /// Checks if a path has been modified at runtime.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path has been modified at runtime, false otherwise.</returns>
        public bool IsRuntimeModified(string path)
        {
            return _originalMetadataValues.ContainsKey(path);
        }

        /// <summary>
        /// Exports the instance to a JSON string.
        /// </summary>
        /// <returns>A JSON string representing the instance overrides.</returns>
        public string ToJson()
        {
            var wrapper = new InstanceJsonWrapper
            {
                InstanceId = _instanceId,
                ParentStructureId = _parentStructure?.StructureId,
                Overrides = _overrides
            };
            
            return UnityEngine.JsonUtility.ToJson(wrapper);
        }

        /// <summary>
        /// Imports the instance from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to import.</param>
        public void FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;
                
            try
            {
                var wrapper = UnityEngine.JsonUtility.FromJson<InstanceJsonWrapper>(json);
                if (wrapper == null)
                    return;
                    
                _instanceId = wrapper.InstanceId;
                _overrides = wrapper.Overrides ?? new List<OverrideEntry>();
                
                // Clear runtime container so it will be recreated with new overrides
                _runtimeContainer = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error deserializing DataInstance: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Helper class for JSON serialization.
        /// </summary>
        [Serializable]
        private class InstanceJsonWrapper
        {
            public string InstanceId;
            public string ParentStructureId;
            public List<OverrideEntry> Overrides;
        }
        
        /// <summary>
        /// Compares containers to find overridden values.
        /// </summary>
        private void CompareContainers(string basePath, DataContainer parentContainer, DataContainer instanceContainer, Dictionary<string, object> result)
        {
            foreach (var key in instanceContainer.GetKeys())
            {
                var path = string.IsNullOrEmpty(basePath) ? key : $"{basePath}.{key}";
                
                if (!parentContainer.Contains(key))
                {
                    // Key exists in instance but not in parent - it's an override
                    result[path] = instanceContainer[key];
                    continue;
                }
                
                var parentType = parentContainer.GetValueType(key);
                var instanceType = instanceContainer.GetValueType(key);
                
                if (parentType != instanceType)
                {
                    // Types are different - it's an override
                    result[path] = instanceContainer[key];
                    continue;
                }
                
                if (instanceType == typeof(DataContainer))
                {
                    // Recursively compare nested containers
                    var parentNestedContainer = parentContainer.Get<DataContainer>(key);
                    var instanceNestedContainer = instanceContainer.Get<DataContainer>(key);
                    
                    CompareContainers(path, parentNestedContainer, instanceNestedContainer, result);
                }
                else
                {
                    // For simple types, compare directly
                    var parentValue = parentContainer[key];
                    var instanceValue = instanceContainer[key];
                    
                    if (!Equals(parentValue, instanceValue))
                    {
                        result[path] = instanceValue;
                    }
                }
            }
        }
        
        /// <summary>
        /// Creates a persistent DataInstance asset from this structure (Editor only).
        /// </summary>
        /// <param name="path">The path to save the asset.</param>
        /// <param name="instanceName">Optional name for the instance.</param>
        /// <returns>The created DataInstance asset.</returns>
        #if UNITY_EDITOR
        internal static DataInstance CreatePersistentInstance(DataStructure structure, string path, string instanceName = null)
        {
            if (structure == null)
                throw new ArgumentNullException(nameof(structure));
                
            var instance = CreateInstance<DataInstance>();
            var container = (DataContainer)structure.Container.DeepCopy(); // Temporarily for conversion
            instance.Initialize(structure, container);
            instanceName = instanceName ?? $"{structure.name}Instance";
            instance.name = instanceName;
            
            UnityEditor.AssetDatabase.CreateAsset(instance, $"{path}/{instanceName}.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            return instance;
        }
        #endif
    }
} 