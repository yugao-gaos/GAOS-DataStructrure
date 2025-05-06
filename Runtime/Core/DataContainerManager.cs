using System;
using System.Collections.Generic;
using GAOS.ServiceLocator;
using UnityEngine;

namespace GAOS.DataStructure
{
    /// <summary>
    /// Central manager for data containers.
    /// Provides methods for creating, accessing, and managing data containers.
    /// </summary>
    [Service(typeof(DataContainerManager), "Default", ServiceLifetime.Singleton, ServiceContext.Runtime)]
    public class DataContainerManager : IService
    {
        // Cache of loaded containers
        private Dictionary<string, DataContainer> _containers = new Dictionary<string, DataContainer>();
        
        // Cache of loaded instances
        private Dictionary<string, DataInstance> _instances = new Dictionary<string, DataInstance>();
        
        // Events
        public event Action<string, DataContainer> OnContainerCreated;
        public event Action<string, DataContainer> OnContainerRemoved;
        public event Action<string, DataInstance> OnInstanceCreated;
        public event Action<string, DataInstance> OnInstanceRemoved;

        /// <summary>
        /// Initializes the manager and registers it with the ServiceLocator.
        /// </summary>
        public void Initialize()
        {
            Debug.Log("DataContainerManager initialized");
        }

        /// <summary>
        /// Creates a new data container with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the container. If null, a random ID will be generated.</param>
        /// <returns>The created container.</returns>
        public DataContainer CreateContainer(string id = null)
        {
            id = id ?? Guid.NewGuid().ToString();
            
            if (_containers.ContainsKey(id))
            {
                throw new InvalidOperationException($"Container with ID '{id}' already exists");
            }
            
            var container = new DataContainer();
            _containers[id] = container;
            
            OnContainerCreated?.Invoke(id, container);
            
            return container;
        }

        /// <summary>
        /// Gets a container with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the container.</param>
        /// <returns>The container, or null if it doesn't exist.</returns>
        public DataContainer GetContainer(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Container ID cannot be null or empty", nameof(id));
                
            if (_containers.TryGetValue(id, out var container))
            {
                return container;
            }
            
            return null;
        }

        /// <summary>
        /// Tries to get a container with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the container.</param>
        /// <param name="container">The container, or null if it doesn't exist.</param>
        /// <returns>True if the container exists, false otherwise.</returns>
        public bool TryGetContainer(string id, out DataContainer container)
        {
            if (string.IsNullOrEmpty(id))
            {
                container = null;
                return false;
            }
            
            return _containers.TryGetValue(id, out container);
        }

        /// <summary>
        /// Removes a container with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the container.</param>
        public void RemoveContainer(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Container ID cannot be null or empty", nameof(id));
                
            if (_containers.TryGetValue(id, out var container))
            {
                _containers.Remove(id);
                OnContainerRemoved?.Invoke(id, container);
            }
        }

        /// <summary>
        /// Clears all containers.
        /// </summary>
        public void ClearAllContainers()
        {
            var containerIds = new List<string>(_containers.Keys);
            
            foreach (var id in containerIds)
            {
                RemoveContainer(id);
            }
        }

        /// <summary>
        /// Creates a container from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <param name="id">The ID of the container. If null, a random ID will be generated.</param>
        /// <returns>The created container.</returns>
        public DataContainer CreateFromJson(string json, string id = null)
        {
            var container = CreateContainer(id);
            container.FromJson(json);
            return container;
        }

        /// <summary>
        /// Creates a new instance from a data structure.
        /// </summary>
        /// <param name="structure">The data structure.</param>
        /// <param name="instanceId">The ID of the instance. If null, a random ID will be generated.</param>
        /// <returns>The created instance.</returns>
        public DataInstance CreateInstance(DataStructure structure, string instanceId = null)
        {
            if (structure == null)
                throw new ArgumentNullException(nameof(structure));
                
            instanceId = instanceId ?? Guid.NewGuid().ToString();
            
            if (_instances.ContainsKey(instanceId))
            {
                throw new InvalidOperationException($"Instance with ID '{instanceId}' already exists");
            }
            
            var instance = structure.CreateInstance();
            _instances[instanceId] = instance;
            
            OnInstanceCreated?.Invoke(instanceId, instance);
            
            return instance;
        }

        /// <summary>
        /// Gets an instance with the specified ID.
        /// </summary>
        /// <param name="instanceId">The ID of the instance.</param>
        /// <returns>The instance, or null if it doesn't exist.</returns>
        public DataInstance GetInstance(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
                
            if (_instances.TryGetValue(instanceId, out var instance))
            {
                return instance;
            }
            
            return null;
        }

        /// <summary>
        /// Tries to get an instance with the specified ID.
        /// </summary>
        /// <param name="instanceId">The ID of the instance.</param>
        /// <param name="instance">The instance, or null if it doesn't exist.</param>
        /// <returns>True if the instance exists, false otherwise.</returns>
        public bool TryGetInstance(string instanceId, out DataInstance instance)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                instance = null;
                return false;
            }
            
            return _instances.TryGetValue(instanceId, out instance);
        }

        /// <summary>
        /// Removes an instance with the specified ID.
        /// </summary>
        /// <param name="instanceId">The ID of the instance.</param>
        public void RemoveInstance(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
                
            if (_instances.TryGetValue(instanceId, out var instance))
            {
                _instances.Remove(instanceId);
                OnInstanceRemoved?.Invoke(instanceId, instance);
            }
        }

        /// <summary>
        /// Clears all instances.
        /// </summary>
        public void ClearAllInstances()
        {
            var instanceIds = new List<string>(_instances.Keys);
            
            foreach (var id in instanceIds)
            {
                RemoveInstance(id);
            }
        }
    }
} 