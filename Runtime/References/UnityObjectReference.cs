using System;
using System.Collections.Generic;
using UnityEngine;
using GAOS.ServiceLocator;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace GAOS.DataStructure.References
{
    /// <summary>
    /// Storage types for Unity object references.
    /// </summary>
    public enum ReferenceStorageType
    {
        /// <summary>
        /// Reference is stored in the UnityReferenceRegistry.
        /// </summary>
        Registry,
        
        /// <summary>
        /// Reference is loaded from Resources folder using path.
        /// </summary>
        Resources,
        
        /// <summary>
        /// Reference is loaded using Addressables system (if available).
        /// </summary>
        Addressable
    }
    
    /// <summary>
    /// A serializable wrapper for UnityEngine.Object references that can be stored in DataContainer.
    /// </summary>
    [Serializable]
    public class UnityObjectReference
    {
        [SerializeField] private ReferenceStorageType _storageType;
        [SerializeField] private string _key;
        [SerializeField] private string _typeName;
        
        [NonSerialized] private UnityEngine.Object _cachedObject;
        [NonSerialized] private Type _cachedType;
        [NonSerialized] private bool _hasLoadedObject;
        
        /// <summary>
        /// The storage type for this reference.
        /// </summary>
        public ReferenceStorageType StorageType => _storageType;
        
        /// <summary>
        /// The key used to retrieve the object (meaning depends on StorageType).
        /// </summary>
        public string Key => _key;
        
        /// <summary>
        /// The type name of the referenced object.
        /// </summary>
        public string TypeName => _typeName;
        
        /// <summary>
        /// Create a new UnityObjectReference.
        /// </summary>
        /// <param name="obj">The Unity object to reference</param>
        /// <param name="storageType">How the reference is stored</param>
        /// <param name="key">The key to use for retrieval</param>
        public UnityObjectReference(UnityEngine.Object obj, ReferenceStorageType storageType, string key)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
                
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
                
            _storageType = storageType;
            _key = key;
            _typeName = obj.GetType().AssemblyQualifiedName;
            _cachedObject = obj;
            _cachedType = obj.GetType();
            _hasLoadedObject = true;
            
            // Register with registry if needed
            if (storageType == ReferenceStorageType.Registry)
            {
                var registry = GAOS.ServiceLocator.ServiceLocator.GetService<IUnityReferenceRegistry>("Default");
                registry.RegisterObject(key, obj);
            }
        }
        
        /// <summary>
        /// Create a new UnityObjectReference without an initial object.
        /// </summary>
        /// <param name="storageType">How the reference is stored</param>
        /// <param name="key">The key to use for retrieval</param>
        /// <param name="typeName">The full type name of the referenced object</param>
        public UnityObjectReference(ReferenceStorageType storageType, string key, string typeName)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
                
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentException("TypeName cannot be null or empty", nameof(typeName));
                
            _storageType = storageType;
            _key = key;
            _typeName = typeName;
        }
        
        /// <summary>
        /// Gets the referenced object, loading it if necessary.
        /// </summary>
        /// <returns>The referenced object, or null if not found.</returns>
        public UnityEngine.Object GetObject()
        {
            if (_hasLoadedObject && _cachedObject != null)
                return _cachedObject;
                
            _cachedObject = LoadObject();
            _hasLoadedObject = true;
            return _cachedObject;
        }
        
        /// <summary>
        /// Gets the referenced object with type casting.
        /// </summary>
        /// <typeparam name="T">The type to cast to</typeparam>
        /// <returns>The referenced object cast to type T, or null if not found or wrong type</returns>
        public T GetObject<T>() where T : UnityEngine.Object
        {
            var obj = GetObject();
            if (obj == null)
                return null;
                
            return obj as T;
        }
        
        /// <summary>
        /// Gets the type of the referenced object.
        /// </summary>
        /// <returns>The type of the referenced object</returns>
        public Type GetObjectType()
        {
            if (_cachedType != null)
                return _cachedType;
                
            if (!string.IsNullOrEmpty(_typeName))
                _cachedType = Type.GetType(_typeName);
                
            return _cachedType;
        }
        
        /// <summary>
        /// Loads the object from its storage location.
        /// </summary>
        private UnityEngine.Object LoadObject()
        {
            switch (_storageType)
            {
                case ReferenceStorageType.Registry:
                    return LoadFromRegistry();
                    
                case ReferenceStorageType.Resources:
                    return LoadFromResources();
                    
                case ReferenceStorageType.Addressable:
                    return LoadFromAddressables();
                    
                default:
                    Debug.LogError($"Unknown storage type: {_storageType}");
                    return null;
            }
        }
        
        private UnityEngine.Object LoadFromRegistry()
        {
            try
            {
                // Check if service locator has the registry service
                if (!ServiceLocatorHasRegistry())
                {
                    // If not, check if we can load it from Resources as a fallback
                    var registry = UnityEngine.Resources.Load<UnityReferenceRegistry>("UnityReferenceRegistry");
                    if (registry != null)
                    {
                        return registry.GetObject(_key);
                    }
                    
                    Debug.LogWarning($"UnityReferenceRegistry service not available and no registry asset found in Resources. Cannot resolve reference: {_key}");
                    return null;
                }
                
                var registryService = GAOS.ServiceLocator.ServiceLocator.GetService<IUnityReferenceRegistry>("Default");
                return registryService.GetObject(_key);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading object from registry: {ex.Message}");
                return null;
            }
        }
        
        // Helper to check if the registry service is available
        private bool ServiceLocatorHasRegistry()
        {
            try
            {
                // Using reflection to check if the service is registered without throwing exceptions
                var serviceLocatorType = typeof(GAOS.ServiceLocator.ServiceLocator);
                var getServiceNamesMethod = serviceLocatorType.GetMethod("GetServiceNames", new[] { typeof(Type) });
                
                if (getServiceNamesMethod != null)
                {
                    var names = getServiceNamesMethod.Invoke(null, new object[] { typeof(IUnityReferenceRegistry) }) as IEnumerable<string>;
                    if (names != null)
                    {
                        foreach (var name in names)
                        {
                            if (name == "Default")
                                return true;
                        }
                    }
                    return false;
                }
                
                return false;
            }
            catch
            {
                // If anything goes wrong with reflection, assume service is not available
                return false;
            }
        }
        
        private UnityEngine.Object LoadFromResources()
        {
            try
            {
                return Resources.Load(_key, GetObjectType());
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading object from Resources: {ex.Message}");
                return null;
            }
        }
        
        private UnityEngine.Object LoadFromAddressables()
        {
#if UNITY_ADDRESSABLES
            try
            {
                // This is synchronous loading - in a real implementation,
                // you'd want to provide async loading options as well
                var operation = Addressables.LoadAssetAsync<UnityEngine.Object>(_key);
                operation.WaitForCompletion();
                return operation.Result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading object from Addressables: {ex.Message}");
                return null;
            }
#else
            Debug.LogError("Addressables package is not enabled. Cannot load reference.");
            return null;
#endif
        }
        
        /// <summary>
        /// Releases the cached reference.
        /// </summary>
        public void Release()
        {
            _cachedObject = null;
            _hasLoadedObject = false;
        }
    }
} 