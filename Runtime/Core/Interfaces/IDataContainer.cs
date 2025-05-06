using System;
using System.Collections.Generic;

namespace GAOS.DataStructure.Interfaces
{
    /// <summary>
    /// Interface defining operations for a data container.
    /// </summary>
    public interface IDataContainer
    {
        /// <summary>
        /// Sets a value for the specified key.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key to set.</param>
        /// <param name="value">The value to set.</param>
        void Set<T>(string key, T value);

        /// <summary>
        /// Gets a value for the specified key.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="key">The key to get.</param>
        /// <param name="defaultValue">The default value to return if the key does not exist.</param>
        /// <returns>The value associated with the key, or the default value if the key does not exist.</returns>
        T Get<T>(string key, T defaultValue = default);

        /// <summary>
        /// Tries to get a value for the specified key.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="key">The key to get.</param>
        /// <param name="value">The value associated with the key, or the default value if the key does not exist.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        bool TryGet<T>(string key, out T value);

        /// <summary>
        /// Checks if the container contains a value for the specified key.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        bool Contains(string key);

        /// <summary>
        /// Removes a value for the specified key.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        void Remove(string key);

        /// <summary>
        /// Gets all keys in the container.
        /// </summary>
        /// <returns>An enumerable of all keys in the container.</returns>
        IEnumerable<string> GetKeys();

        /// <summary>
        /// Gets the type of the value for the specified key.
        /// </summary>
        /// <param name="key">The key to get the type for.</param>
        /// <returns>The type of the value, or null if the key does not exist.</returns>
        Type GetValueType(string key);

        /// <summary>
        /// Creates a deep copy of the container.
        /// </summary>
        /// <returns>A new container with the same values.</returns>
        IDataContainer DeepCopy();
    }
} 