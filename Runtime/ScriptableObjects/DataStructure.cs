using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GAOS.DataStructure
{
    /// <summary>
    /// ScriptableObject that defines a data structure template.
    /// Used to create DataInstance objects with the same structure.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDataStructure", menuName = "GAOS/Data/Data Structure")]
    public class DataStructure : ScriptableObject
    {
        [SerializeField] private DataContainer _container = new DataContainer();
        [SerializeField] private string _structureId;
        [SerializeField] private string _description;
        
        /// <summary>
        /// The unique identifier for this structure.
        /// </summary>
        public string StructureId => string.IsNullOrEmpty(_structureId) ? name : _structureId;
        
        /// <summary>
        /// The description of this structure.
        /// </summary>
        public string Description => _description;
        
        /// <summary>
        /// The data container that defines the structure.
        /// </summary>
        public DataContainer Container => _container;

        /// <summary>
        /// Creates a new DataInstance from this structure.
        /// </summary>
        /// <param name="instanceName">Optional name for the instance.</param>
        /// <returns>A new DataInstance with this structure as parent.</returns>
        public DataInstance CreateInstance(string instanceName = null)
        {
            var instance = CreateInstance<DataInstance>();
            // Create a temporary container to convert to overrides
            var tempContainer = (DataContainer)_container.DeepCopy();
            instance.Initialize(this, tempContainer);
            instance.name = instanceName ?? $"{name}Instance";
            return instance;
        }

        /// <summary>
        /// Creates a deep copy of this DataStructure.
        /// </summary>
        /// <returns>A new DataStructure with a copy of this structure's data.</returns>
        public DataStructure DeepCopy()
        {
            var copy = CreateInstance<DataStructure>();
            copy._container = (DataContainer)_container.DeepCopy();
            copy._structureId = _structureId;
            copy._description = _description;
            copy.name = this.name;
            return copy;
        }

        /// <summary>
        /// Converts the structure to a JSON string.
        /// </summary>
        /// <returns>A JSON string representing the structure.</returns>
        public string ToJson()
        {
            var wrapper = new StructureJsonWrapper 
            { 
                StructureId = _structureId,
                Description = _description,
                ContainerJson = _container.ToJson()
            };
            
            return UnityEngine.JsonUtility.ToJson(wrapper);
        }
        
        /// <summary>
        /// Initializes the structure from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        public void FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;
                
            try
            {
                var wrapper = UnityEngine.JsonUtility.FromJson<StructureJsonWrapper>(json);
                if (wrapper == null)
                    return;
                    
                _structureId = wrapper.StructureId;
                _description = wrapper.Description;
                
                if (!string.IsNullOrEmpty(wrapper.ContainerJson))
                {
                    _container = new DataContainer();
                    _container.FromJson(wrapper.ContainerJson);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error deserializing DataStructure: {ex.Message}");
            }
        }
        
        [Serializable]
        private class StructureJsonWrapper
        {
            public string StructureId;
            public string Description;
            public string ContainerJson;
        }

        /// <summary>
        /// Creates a persistent DataInstance asset from this structure (Editor only).
        /// </summary>
        /// <param name="path">The path to save the asset.</param>
        /// <param name="instanceName">Optional name for the instance.</param>
        /// <returns>The created DataInstance asset.</returns>
        #if UNITY_EDITOR
        public DataInstance CreatePersistentInstance(string path, string instanceName = null)
        {
            var instance = CreateInstance();
            instanceName = instanceName ?? $"{name}Instance";
            UnityEditor.AssetDatabase.CreateAsset(instance, $"{path}/{instanceName}.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            return instance;
        }
        #endif

        /// <summary>
        /// Validates the structure to ensure it's valid.
        /// </summary>
        /// <returns>True if the structure is valid, false otherwise.</returns>
        public bool Validate()
        {
            // Basic validation - could be extended with more specific checks
            return _container != null;
        }

        /// <summary>
        /// Gets all paths in the structure.
        /// </summary>
        /// <returns>An enumerable of all paths in the structure.</returns>
        public IEnumerable<string> GetAllPaths()
        {
            return GetPathsRecursive("", _container);
        }

        /// <summary>
        /// Gets the type of the value at the specified path.
        /// </summary>
        /// <param name="path">The path to get the type for.</param>
        /// <returns>The type of the value, or null if the path doesn't exist.</returns>
        public Type GetPathType(string path)
        {
            if (string.IsNullOrEmpty(path))
                return typeof(DataContainer);

            var lastDotIndex = path.LastIndexOf('.');
            if (lastDotIndex < 0)
            {
                return _container.GetValueType(path);
            }

            var containerPath = path.Substring(0, lastDotIndex);
            var key = path.Substring(lastDotIndex + 1);

            var container = _container.PathGet<DataContainer>(containerPath);
            if (container == null)
            {
                return null;
            }

            return container.GetValueType(key);
        }

        /// <summary>
        /// Recursively gets all paths in a container.
        /// </summary>
        /// <param name="basePath">The base path.</param>
        /// <param name="container">The container to get paths from.</param>
        /// <returns>An enumerable of all paths in the container.</returns>
        private IEnumerable<string> GetPathsRecursive(string basePath, DataContainer container)
        {
            foreach (var key in container.GetKeys())
            {
                var path = string.IsNullOrEmpty(basePath) ? key : $"{basePath}.{key}";
                yield return path;

                var valueType = container.GetValueType(key);
                if (valueType == typeof(DataContainer))
                {
                    var nestedContainer = container.Get<DataContainer>(key);
                    foreach (var nestedPath in GetPathsRecursive(path, nestedContainer))
                    {
                        yield return nestedPath;
                    }
                }
                else if (valueType != null && valueType.IsGenericType)
                {
                    var genericType = valueType.GetGenericTypeDefinition();
                    
                    // Handle List<DataContainer>
                    if (genericType == typeof(List<>))
                    {
                        var elementType = valueType.GetGenericArguments()[0];
                        if (elementType == typeof(DataContainer))
                        {
                            var list = container.Get<List<DataContainer>>(key);
                            if (list != null)
                            {
                                for (int i = 0; i < list.Count; i++)
                                {
                                    var indexPath = $"{path}[{i}]";
                                    yield return indexPath;
                                    
                                    foreach (var nestedPath in GetPathsRecursive(indexPath, list[i]))
                                    {
                                        yield return nestedPath;
                                    }
                                }
                            }
                        }
                    }
                    // Handle OrderedDictionary<string, DataContainer>
                    else if (genericType == typeof(OrderedDictionary<,>))
                    {
                        var keyType = valueType.GetGenericArguments()[0];
                        var valueTypeArg = valueType.GetGenericArguments()[1];
                        
                        if (keyType == typeof(string) && valueTypeArg == typeof(DataContainer))
                        {
                            var dict = container.Get<OrderedDictionary<string, DataContainer>>(key);
                            if (dict != null)
                            {
                                foreach (var dictKey in dict.OrderedKeys)
                                {
                                    var dictPath = $"{path}[\"{dictKey}\"]";
                                    yield return dictPath;
                                    
                                    foreach (var nestedPath in GetPathsRecursive(dictPath, dict[dictKey]))
                                    {
                                        yield return nestedPath;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
} 