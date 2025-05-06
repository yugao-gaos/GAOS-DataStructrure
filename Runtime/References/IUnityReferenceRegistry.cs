using UnityEngine;

namespace GAOS.DataStructure.References
{
    /// <summary>
    /// Interface for the UnityReferenceRegistry service which manages references to Unity objects.
    /// </summary>
    public interface IUnityReferenceRegistry
    {
        /// <summary>
        /// Register an object with the registry
        /// </summary>
        /// <param name="key">The unique key to identify the object</param>
        /// <param name="obj">The Unity object to register</param>
        void RegisterObject(string key, Object obj);
        
        /// <summary>
        /// Get an object from the registry
        /// </summary>
        /// <param name="key">The unique key for the object</param>
        /// <returns>The Unity object, or null if not found</returns>
        Object GetObject(string key);
        
        /// <summary>
        /// Get an object from the registry with type casting
        /// </summary>
        /// <typeparam name="T">The type to cast the object to</typeparam>
        /// <param name="key">The unique key for the object</param>
        /// <returns>The Unity object cast to type T, or null if not found or wrong type</returns>
        T GetObject<T>(string key) where T : Object;
        
        /// <summary>
        /// Remove an object from the registry
        /// </summary>
        /// <param name="key">The unique key for the object to remove</param>
        /// <returns>True if the object was removed, false if not found</returns>
        bool RemoveObject(string key);
        
        /// <summary>
        /// Check if the registry contains an object with the given key
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if an object with the key exists, false otherwise</returns>
        bool ContainsKey(string key);
    }
} 