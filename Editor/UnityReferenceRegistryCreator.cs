using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GAOS.DataStructure.References;
using GAOS.ServiceLocator;
using GAOS.Logger;
using GAOS.DataStructure.Editor;

namespace GAOS.DataStructure.Editor
{
    [InitializeOnLoad]
    public static class UnityReferenceRegistryCreator
    {
        private const string RegistryPath = "Assets/Resources/UnityReferenceRegistry.asset";
        private const string ResourcesFolder = "Assets/Resources";
        private const int MaxRetries = 5;
        private static int _retryCount = 0;
        private static bool _isInitialized = false;
        
        static UnityReferenceRegistryCreator()
        {
            // Delay initialization to ensure TypeCache is ready
            // First delayed call: wait for Unity to fully initialize
            EditorApplication.delayCall += () => 
            {
                // Delay again to ensure all other initializers have a chance to run
                EditorApplication.delayCall += ScheduleInitialization;
            };
            
            // Subscribe to domain reload events
            AssemblyReloadEvents.afterAssemblyReload += () => 
            {
                // Reset state after domain reload
                _isInitialized = false;
                _retryCount = 0;
                
                // Schedule initialization again
                EditorApplication.delayCall += ScheduleInitialization;
            };
        }
        
        private static void ScheduleInitialization()
        {
            GLog.Info<DataSystemEditorLogger>("Scheduling UnityReferenceRegistry initialization");
            EditorApplication.update += TryInitialize;
        }
        
        private static void TryInitialize()
        {
            // Only run once
            if (_isInitialized)
            {
                EditorApplication.update -= TryInitialize;
                return;
            }
            
            // First check if TypeCache is ready
            if (!IsTypeCacheReady())
            {
                _retryCount++;
                
                // Log every other attempt to avoid spamming the console
                if (_retryCount % 2 == 0)
                {
                    GLog.Info<DataSystemEditorLogger>($"Waiting for ServiceLocator TypeCache to be ready... (Attempt {_retryCount}/{MaxRetries})");
                }
                
                // If we've tried too many times, proceed anyway
                if (_retryCount >= MaxRetries)
                {
                    GLog.Warning<DataSystemEditorLogger>("ServiceLocator TypeCache might not be fully initialized, proceeding anyway");
                    EnsureRegistryExists();
                    _isInitialized = true;
                    EditorApplication.update -= TryInitialize;
                }
                
                // Otherwise continue waiting
                return;
            }
            
            // TypeCache is ready, proceed with initialization
            EnsureRegistryExists();
            _isInitialized = true;
            EditorApplication.update -= TryInitialize;
        }
        
        private static bool IsTypeCacheReady()
        {
            try
            {
                // Use reflection to check if TypeCache is initialized
                var serviceLocatorType = typeof(GAOS.ServiceLocator.ServiceLocator);
                var typeCacheProperty = serviceLocatorType.GetProperty("TypeCache", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                    
                if (typeCacheProperty == null)
                    return false;
                    
                var typeCache = typeCacheProperty.GetValue(null);
                if (typeCache == null)
                    return false;
                    
                // Check if ServiceTypes property exists and has items
                var serviceTypesProperty = typeCache.GetType().GetProperty("ServiceTypes");
                if (serviceTypesProperty == null)
                    return false;
                    
                var serviceTypes = serviceTypesProperty.GetValue(typeCache) as IEnumerable;
                if (serviceTypes == null)
                    return false;
                    
                // Try to get at least one service type to confirm cache is populated
                IEnumerator enumerator = serviceTypes.GetEnumerator();
                bool hasItems = enumerator.MoveNext();
                
                // Manually dispose of the enumerator if it's IDisposable
                if (enumerator is System.IDisposable disposable)
                    disposable.Dispose();
                    
                return hasItems;
            }
            catch (System.Exception ex)
            {
                GLog.Error<DataSystemEditorLogger>($"Error checking TypeCache status: {ex.Message}");
                return false;
            }
        }
        
        private static void EnsureRegistryExists()
        {
            GLog.Info<DataSystemEditorLogger>("Ensuring UnityReferenceRegistry exists");
            
            // Check if the registry asset already exists
            var registry = AssetDatabase.LoadAssetAtPath<UnityReferenceRegistry>(RegistryPath);
            
            if (registry == null)
            {
                GLog.Info<DataSystemEditorLogger>("Creating UnityReferenceRegistry asset");
                
                // Make sure the Resources folder exists
                if (!Directory.Exists(ResourcesFolder))
                {
                    Directory.CreateDirectory(ResourcesFolder);
                }
                
                // Create the registry asset
                registry = ScriptableObject.CreateInstance<UnityReferenceRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                GLog.Info<DataSystemEditorLogger>("UnityReferenceRegistry asset created at: " + RegistryPath);
            }
            
            // Register the service manually
            RegisterService(registry);
        }
        
        private static void RegisterService(UnityReferenceRegistry registry)
        {
            // First check if service is already registered
            if (IsServiceRegistered())
            {
                GLog.Info<DataSystemEditorLogger>("UnityReferenceRegistry service is already registered");
                return;
            }
            
            // Use reflection to get the internal Register method
            var registerMethod = typeof(GAOS.ServiceLocator.ServiceLocator).GetMethod(
                "Register", 
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(System.Type), typeof(System.Type), typeof(string), 
                      typeof(ServiceLifetime), typeof(ServiceContext) },
                null);
                
            if (registerMethod != null)
            {
                try
                {
                    registerMethod.Invoke(null, new object[] { 
                        typeof(IUnityReferenceRegistry), 
                        typeof(UnityReferenceRegistry), 
                        "Default", 
                        ServiceLifetime.Singleton, 
                        ServiceContext.RuntimeAndEditor
                    });
                    
                    GLog.Info<DataSystemEditorLogger>("Manually registered UnityReferenceRegistry service");
                }
                catch (System.Exception ex)
                {
                    GLog.Error<DataSystemEditorLogger>($"Failed to register UnityReferenceRegistry service: {ex.Message}");
                }
            }
            else
            {
                GLog.Error<DataSystemEditorLogger>("Could not find Register method via reflection");
            }
        }
        
        private static bool IsServiceRegistered()
        {
            try
            {
                var getServiceNamesMethod = typeof(GAOS.ServiceLocator.ServiceLocator).GetMethod(
                    "GetServiceNames", 
                    new[] { typeof(System.Type) });
                    
                if (getServiceNamesMethod == null)
                    return false;
                    
                var names = getServiceNamesMethod.Invoke(null, new object[] { typeof(IUnityReferenceRegistry) }) 
                    as IEnumerable<string>;
                    
                if (names == null)
                    return false;
                    
                foreach (var name in names)
                {
                    if (name == "Default")
                        return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
} 