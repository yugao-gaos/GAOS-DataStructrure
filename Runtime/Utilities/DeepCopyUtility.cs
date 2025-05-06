using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GAOS.DataStructure
{
    /// <summary>
    /// Utility class for creating deep copies of objects.
    /// </summary>
    public static class DeepCopyUtility
    {
        // Cache of already copied objects to handle circular references
        private static Dictionary<object, object> _copiedReferences = new Dictionary<object, object>();

        /// <summary>
        /// Creates a deep copy of an object.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="source">The object to copy.</param>
        /// <returns>A deep copy of the object.</returns>
        public static T DeepCopy<T>(T source)
        {
            return (T)DeepCopyObject(source);
        }

        /// <summary>
        /// Creates a deep copy of an object.
        /// </summary>
        /// <param name="source">The object to copy.</param>
        /// <returns>A deep copy of the object.</returns>
        public static object DeepCopyObject(object source)
        {
            // Reset the copied references cache
            _copiedReferences = new Dictionary<object, object>();
            
            return DeepCopyInternal(source);
        }

        /// <summary>
        /// Internal method for creating a deep copy of an object.
        /// </summary>
        /// <param name="source">The object to copy.</param>
        /// <returns>A deep copy of the object.</returns>
        private static object DeepCopyInternal(object source)
        {
            if (source == null)
                return null;

            // Get the type of the source object
            Type type = source.GetType();

            // Handle value types and immutable types
            if (IsImmutableType(type))
                return source;

            // Check if we've already copied this object (circular reference)
            if (_copiedReferences.TryGetValue(source, out object existingCopy))
                return existingCopy;

            // Handle arrays
            if (type.IsArray)
                return DeepCopyArray(source);

            // Handle lists
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return DeepCopyList(source);

            // Handle dictionaries
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                return DeepCopyDictionary(source);

            // Handle DataContainer
            if (source is DataContainer container)
                return container.DeepCopy();

            // Handle custom objects
            return DeepCopyCustomObject(source);
        }

        /// <summary>
        /// Checks if a type is immutable.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is immutable, false otherwise.</returns>
        private static bool IsImmutableType(Type type)
        {
            return type.IsPrimitive || 
                   type == typeof(string) || 
                   type == typeof(decimal) ||
                   type.IsEnum ||
                   type == typeof(DateTime) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Guid) ||
                   type == typeof(Vector2) ||
                   type == typeof(Vector3) ||
                   type == typeof(Vector4) ||
                   type == typeof(Quaternion) ||
                   type == typeof(Color) ||
                   type == typeof(Rect) ||
                   type == typeof(Bounds);
        }

        /// <summary>
        /// Creates a deep copy of an array.
        /// </summary>
        /// <param name="source">The array to copy.</param>
        /// <returns>A deep copy of the array.</returns>
        private static object DeepCopyArray(object source)
        {
            Type type = source.GetType();
            Type elementType = type.GetElementType();
            Array sourceArray = (Array)source;
            Array destinationArray = Array.CreateInstance(elementType, sourceArray.Length);
            
            // Add to copied references before copying elements to handle circular references
            _copiedReferences[source] = destinationArray;
            
            for (int i = 0; i < sourceArray.Length; i++)
            {
                object element = sourceArray.GetValue(i);
                object copiedElement = DeepCopyInternal(element);
                destinationArray.SetValue(copiedElement, i);
            }
            
            return destinationArray;
        }

        /// <summary>
        /// Creates a deep copy of a list.
        /// </summary>
        /// <param name="source">The list to copy.</param>
        /// <returns>A deep copy of the list.</returns>
        private static object DeepCopyList(object source)
        {
            Type type = source.GetType();
            Type elementType = type.GetGenericArguments()[0];
            Type listType = typeof(List<>).MakeGenericType(elementType);
            object destination = Activator.CreateInstance(listType);
            
            // Add to copied references before copying elements to handle circular references
            _copiedReferences[source] = destination;
            
            MethodInfo addMethod = listType.GetMethod("Add");
            PropertyInfo countProperty = type.GetProperty("Count");
            PropertyInfo indexerProperty = type.GetProperty("Item");
            
            int count = (int)countProperty.GetValue(source);
            
            for (int i = 0; i < count; i++)
            {
                object element = indexerProperty.GetValue(source, new object[] { i });
                object copiedElement = DeepCopyInternal(element);
                addMethod.Invoke(destination, new[] { copiedElement });
            }
            
            return destination;
        }

        /// <summary>
        /// Creates a deep copy of a dictionary.
        /// </summary>
        /// <param name="source">The dictionary to copy.</param>
        /// <returns>A deep copy of the dictionary.</returns>
        private static object DeepCopyDictionary(object source)
        {
            Type type = source.GetType();
            Type[] genericArgs = type.GetGenericArguments();
            Type keyType = genericArgs[0];
            Type valueType = genericArgs[1];
            Type dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            object destination = Activator.CreateInstance(dictType);
            
            // Add to copied references before copying elements to handle circular references
            _copiedReferences[source] = destination;
            
            MethodInfo addMethod = dictType.GetMethod("Add", new[] { keyType, valueType });
            PropertyInfo keysProperty = type.GetProperty("Keys");
            MethodInfo containsKeyMethod = dictType.GetMethod("ContainsKey", new[] { keyType });
            MethodInfo getItemMethod = type.GetMethod("get_Item", new[] { keyType });
            
            object keys = keysProperty.GetValue(source);
            
            foreach (object key in (System.Collections.IEnumerable)keys)
            {
                object value = getItemMethod.Invoke(source, new[] { key });
                object copiedKey = DeepCopyInternal(key);
                object copiedValue = DeepCopyInternal(value);
                
                // Only add if the key doesn't already exist
                bool containsKey = (bool)containsKeyMethod.Invoke(destination, new[] { copiedKey });
                if (!containsKey)
                {
                    addMethod.Invoke(destination, new[] { copiedKey, copiedValue });
                }
            }
            
            return destination;
        }

        /// <summary>
        /// Creates a deep copy of a custom object.
        /// </summary>
        /// <param name="source">The object to copy.</param>
        /// <returns>A deep copy of the object.</returns>
        private static object DeepCopyCustomObject(object source)
        {
            Type type = source.GetType();
            object destination = Activator.CreateInstance(type);
            
            // Add to copied references before copying fields to handle circular references
            _copiedReferences[source] = destination;
            
            // Copy all fields (public and private)
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (FieldInfo field in fields)
            {
                object fieldValue = field.GetValue(source);
                object copiedFieldValue = DeepCopyInternal(fieldValue);
                field.SetValue(destination, copiedFieldValue);
            }
            
            return destination;
        }
    }
} 