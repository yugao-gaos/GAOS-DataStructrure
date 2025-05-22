using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using GAOS.Logger;

namespace GAOS.DataStructure
{
    /// <summary>
    /// Registry for enum types that can be used in DataStructure.
    /// Supports both automatic discovery of [DataStructureEnum] marked enums
    /// and manual registration.
    /// </summary>
    public static class EnumRegistry
    {
        private static Dictionary<string, Type> _registeredEnumTypes = new Dictionary<string, Type>();
        // Dictionary mapping Type to converter delegates for optimized enum conversion
        private static Dictionary<Type, Func<int, object>> _enumConverters = new Dictionary<Type, Func<int, object>>();
        private static bool _initialized = false;

        /// <summary>
        /// Initialize the registry by scanning assemblies for enum types
        /// marked with [DataStructureEnum] attribute.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            
            try
            {
                // Scan all loaded assemblies for attribute-marked enum types
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        // Skip system and Unity engine assemblies to reduce overhead
                        if (assembly.GetName().Name.StartsWith("System") ||
                            assembly.GetName().Name.StartsWith("mscorlib") ||
                            assembly.GetName().Name.StartsWith("UnityEngine.CoreModule"))
                            continue;

                        foreach (var type in assembly.GetTypes())
                        {
                            if (type.IsEnum && type.GetCustomAttribute<DataStructureEnumAttribute>() != null)
                            {
                                RegisterEnum(type);
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // Log but continue if an assembly can't be fully loaded
                        Debug.LogWarning($"Could not load some types from assembly {assembly.FullName}: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Error scanning assembly {assembly.FullName}: {ex.Message}");
                    }
                }
                
                _initialized = true;
                Debug.Log($"EnumRegistry initialized with {_registeredEnumTypes.Count} enum types");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize EnumRegistry: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Register an enum type for use in DataStructure.
        /// </summary>
        /// <param name="enumType">The enum type to register</param>
        /// <returns>True if registration was successful, false otherwise</returns>
        public static bool RegisterEnum(Type enumType)
        {
            if (enumType == null || !enumType.IsEnum)
            {
                Debug.LogWarning($"Cannot register {enumType?.Name ?? "null"} as it is not an enum type");
                return false;
            }
            
            string key = FormatEnumTypeName(enumType);
            
            if (!_registeredEnumTypes.ContainsKey(key))
            {
                _registeredEnumTypes[key] = enumType;
                Debug.Log($"Registered enum type: {key}");
                
                // Register a converter for the enum type to avoid runtime reflection
                RegisterConverterForType(enumType);
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Register an enum type for use in DataStructure.
        /// </summary>
        /// <typeparam name="T">The enum type to register</typeparam>
        /// <returns>True if registration was successful, false otherwise</returns>
        public static bool RegisterEnum<T>() where T : Enum
        {
            bool result = RegisterEnum(typeof(T));
            
            // Also directly register a strongly-typed converter
            if (result)
            {
                RegisterConverter<T>();
            }
            
            return result;
        }
        
        /// <summary>
        /// Register a converter for a specific enum type.
        /// </summary>
        /// <typeparam name="TEnum">The enum type</typeparam>
        public static void RegisterConverter<TEnum>() where TEnum : Enum
        {
            Type enumType = typeof(TEnum);
            if (!_enumConverters.ContainsKey(enumType))
            {
                // Create a strongly-typed converter that doesn't use reflection at runtime
                _enumConverters[enumType] = (int value) => (TEnum)Enum.ToObject(enumType, value);
                
                if (Debug.isDebugBuild)
                    Debug.Log($"Registered optimized converter for enum: {enumType.Name}");
            }
        }
        
        /// <summary>
        /// Get a converter for an enum type.
        /// </summary>
        /// <param name="enumType">The enum type</param>
        /// <returns>A converter function, or null if not registered</returns>
        public static Func<int, object> GetConverter(Type enumType)
        {
            if (enumType == null || !enumType.IsEnum)
                return null;

            if (_enumConverters.TryGetValue(enumType, out var converter))
                return converter;
                
            return null;
        }

        /// <summary>
        /// Get all registered enum types.
        /// </summary>
        /// <returns>Dictionary mapping display names to enum types</returns>
        public static Dictionary<string, Type> GetRegisteredEnumTypes()
        {
            if (!_initialized) Initialize();
            return _registeredEnumTypes;
        }
        
        /// <summary>
        /// Check if an enum type is registered.
        /// </summary>
        /// <param name="enumType">The enum type to check</param>
        /// <returns>True if the enum type is registered, false otherwise</returns>
        public static bool IsEnumRegistered(Type enumType)
        {
            if (!_initialized) Initialize();
            
            if (enumType == null || !enumType.IsEnum)
                return false;
                
            string key = FormatEnumTypeName(enumType);
            return _registeredEnumTypes.ContainsKey(key);
        }
        
        /// <summary>
        /// Get a registered enum type by its formatted name.
        /// </summary>
        /// <param name="formattedName">The formatted name of the enum type</param>
        /// <returns>The enum type, or null if not found</returns>
        public static Type GetEnumTypeByName(string formattedName)
        {
            if (!_initialized) Initialize();
            
            if (string.IsNullOrEmpty(formattedName))
                return null;
                
            if (_registeredEnumTypes.TryGetValue(formattedName, out Type type))
                return type;
                
            return null;
        }
        
        /// <summary>
        /// Format an enum type name for display and lookup.
        /// </summary>
        /// <param name="enumType">The enum type</param>
        /// <returns>Formatted name string</returns>
        private static string FormatEnumTypeName(Type enumType)
        {
            if (string.IsNullOrEmpty(enumType.Namespace))
                return enumType.Name;
                
            return $"{enumType.Name} ({enumType.Namespace})";
        }
        
        /// <summary>
        /// Register a converter for an enum type using reflection-based registration.
        /// </summary>
        /// <param name="enumType">The enum type to register a converter for</param>
        private static void RegisterConverterForType(Type enumType)
        {
            try
            {
                // We need to use reflection here because we don't know the enum type at compile time,
                // but this is just done once during registration, not during runtime deserialization
                var registerMethod = typeof(EnumRegistry)
                    .GetMethod("RegisterConverter")
                    .MakeGenericMethod(enumType);
                    
                registerMethod.Invoke(null, null);
            }
            catch (Exception ex)
            {
                // Don't let converter registration failures prevent enum registration
                Debug.LogWarning($"Failed to register converter for enum type {enumType.Name}: {ex.Message}");
            }
        }
    }
} 