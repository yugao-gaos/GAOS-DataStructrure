using System;
using System.Collections.Generic;
using UnityEngine;
using GAOS.DataStructure.References;

namespace GAOS.DataStructure
{
    /// <summary>
    /// Helper methods for serializing and deserializing various types.
    /// </summary>
    internal static class SerializationHelpers
    {
        /// <summary>
        /// Serializes a value to a string based on its type.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialized value as a string.</returns>
        public static string SerializeValue(object value)
        {
            if (value == null)
                return string.Empty;

            Type type = value.GetType();

            // Handle primitive types
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                return value.ToString();

            // Handle Unity types
            if (type == typeof(Vector2))
                return UnityEngine.JsonUtility.ToJson((Vector2)value);
            if (type == typeof(Vector3))
                return UnityEngine.JsonUtility.ToJson((Vector3)value);
            if (type == typeof(Vector4))
                return UnityEngine.JsonUtility.ToJson((Vector4)value);
            if (type == typeof(Quaternion))
                return UnityEngine.JsonUtility.ToJson((Quaternion)value);
            if (type == typeof(Color))
                return UnityEngine.JsonUtility.ToJson((Color)value);
            if (type == typeof(Rect))
                return UnityEngine.JsonUtility.ToJson((Rect)value);
            if (type == typeof(Bounds))
                return UnityEngine.JsonUtility.ToJson((Bounds)value);

            // Handle UnityObjectReference
            if (value is UnityObjectReference unityRef)
            {
                var refWrapper = new UnityRefWrapper
                {
                    StorageType = (int)unityRef.StorageType,
                    Key = unityRef.Key,
                    TypeName = unityRef.TypeName
                };
                
                return UnityEngine.JsonUtility.ToJson(refWrapper);
            }

            // Handle DataContainer - directly use its ToJson method
            if (value is DataContainer container)
                return container.ToJson();

            // Handle arrays and lists
            if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
                return UnityEngine.JsonUtility.ToJson(new SerializableArray(value));

            // Handle OrderedDictionary
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(OrderedDictionary<,>))
                return UnityEngine.JsonUtility.ToJson(new SerializableDictionary(value));

            // For unknown types, fallback to string representation
            return UnityEngine.JsonUtility.ToJson(value);
        }

        /// <summary>
        /// Deserializes a string to a value of the specified type.
        /// </summary>
        /// <param name="serializedValue">The serialized value as a string.</param>
        /// <param name="targetType">The target type to deserialize to.</param>
        /// <returns>The deserialized value.</returns>
        public static object DeserializeValue(string serializedValue, Type targetType)
        {
            if (string.IsNullOrEmpty(serializedValue))
                return null;

            if (serializedValue == "null")
                return null;

            // Handle primitive types
            if (targetType == typeof(string))
                return serializedValue;

            if (targetType == typeof(int))
                return int.Parse(serializedValue);

            if (targetType == typeof(float))
                return float.Parse(serializedValue);

            if (targetType == typeof(bool))
                return bool.Parse(serializedValue);

            if (targetType == typeof(Vector2))
                return UnityEngine.JsonUtility.FromJson<Vector2>(serializedValue);

            if (targetType == typeof(Vector3))
                return UnityEngine.JsonUtility.FromJson<Vector3>(serializedValue);

            if (targetType == typeof(Vector4))
                return UnityEngine.JsonUtility.FromJson<Vector4>(serializedValue);

            if (targetType == typeof(Quaternion))
                return UnityEngine.JsonUtility.FromJson<Quaternion>(serializedValue);

            if (targetType == typeof(Color))
                return UnityEngine.JsonUtility.FromJson<Color>(serializedValue);

            if (targetType == typeof(Rect))
                return UnityEngine.JsonUtility.FromJson<Rect>(serializedValue);

            if (targetType == typeof(Bounds))
                return UnityEngine.JsonUtility.FromJson<Bounds>(serializedValue);

            if (targetType == typeof(UnityObjectReference))
            {
                var refWrapper = UnityEngine.JsonUtility.FromJson<UnityRefWrapper>(serializedValue);
                
                if (refWrapper == null)
                    return null;
                
                return new UnityObjectReference(
                    (ReferenceStorageType)refWrapper.StorageType,
                    refWrapper.Key,
                    refWrapper.TypeName
                );
            }

            // Handle DataContainer
            if (targetType == typeof(DataContainer))
            {
                var container = new DataContainer();
                container.FromJson(serializedValue);
                return container;
            }

            // Handle arrays and lists
            if (targetType.IsArray || (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>)))
            {
                var serializableArray = UnityEngine.JsonUtility.FromJson<SerializableArray>(serializedValue);
                if (serializableArray != null)
                    return serializableArray.ToObject(targetType);
            }

            // Handle OrderedDictionary
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(OrderedDictionary<,>))
            {
                var serializableDictionary = UnityEngine.JsonUtility.FromJson<SerializableDictionary>(serializedValue);
                if (serializableDictionary != null)
                    return serializableDictionary.ToObject(targetType);
            }

            // For unknown types, try Unity's JsonUtility
            try
            {
                return UnityEngine.JsonUtility.FromJson(serializedValue, targetType);
            }
            catch
            {
                Debug.LogWarning($"Failed to deserialize value to type {targetType.Name}. Returning default value.");
                return GetDefaultValue(targetType);
            }
        }

        /// <summary>
        /// Gets the default value for the specified type.
        /// </summary>
        /// <param name="type">The type to get the default value for.</param>
        /// <returns>The default value for the type.</returns>
        private static object GetDefaultValue(Type type)
        {
            if (type == null)
                return null;

            if (type.IsValueType)
                return Activator.CreateInstance(type);

            return null;
        }

        /// <summary>
        /// Helper class for serializing arrays and lists.
        /// </summary>
        [Serializable]
        private class SerializableArray
        {
            [SerializeField] private string[] _items;
            [SerializeField] private string _elementType;

            public SerializableArray(object array)
            {
                Type arrayType = array.GetType();
                Type elementType;

                if (arrayType.IsArray)
                {
                    elementType = arrayType.GetElementType();
                    var items = (Array)array;
                    _items = new string[items.Length];
                    for (int i = 0; i < items.Length; i++)
                    {
                        _items[i] = SerializeValue(items.GetValue(i));
                    }
                }
                else // List<T>
                {
                    elementType = arrayType.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var countProperty = listType.GetProperty("Count");
                    var indexer = listType.GetProperty("Item");
                    int count = (int)countProperty.GetValue(array);
                    _items = new string[count];
                    for (int i = 0; i < count; i++)
                    {
                        _items[i] = SerializeValue(indexer.GetValue(array, new object[] { i }));
                    }
                }

                _elementType = elementType.AssemblyQualifiedName;
            }

            public object ToObject(Type targetType)
            {
                Type elementType = Type.GetType(_elementType);
                if (elementType == null)
                {
                    Debug.LogError($"Could not find type {_elementType}");
                    return null;
                }

                if (targetType.IsArray)
                {
                    Array array = Array.CreateInstance(elementType, _items.Length);
                    for (int i = 0; i < _items.Length; i++)
                    {
                        array.SetValue(DeserializeValue(_items[i], elementType), i);
                    }
                    return array;
                }
                else // List<T>
                {
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var list = Activator.CreateInstance(listType);
                    var addMethod = listType.GetMethod("Add");
                    for (int i = 0; i < _items.Length; i++)
                    {
                        addMethod.Invoke(list, new[] { DeserializeValue(_items[i], elementType) });
                    }
                    return list;
                }
            }
        }

        /// <summary>
        /// Helper class for serializing dictionaries.
        /// </summary>
        [Serializable]
        private class SerializableDictionary
        {
            [SerializeField] private string[] _keys;
            [SerializeField] private string[] _values;

            public SerializableDictionary(object dictionary)
            {
                // Cast directly to known type
                var typedDictionary = (OrderedDictionary<string, DataContainer>)dictionary;
                
                var keysList = new List<string>();
                var valuesList = new List<string>();

                // Use direct access to OrderedKeys and indexer
                foreach (var key in typedDictionary.OrderedKeys)
                {
                    var value = typedDictionary[key];
                    keysList.Add(key); // Keys are already strings, no need to serialize
                    valuesList.Add(SerializeValue(value));
                }

                _keys = keysList.ToArray();
                _values = valuesList.ToArray();
            }

            public object ToObject(Type targetType)
            {
                // Create a new OrderedDictionary of the known type
                var dictionary = new OrderedDictionary<string, DataContainer>();
                
                for (int i = 0; i < _keys.Length; i++)
                {
                    string key = _keys[i]; // Keys are already strings
                    
                    // Deserialize the DataContainer value
                    var container = new DataContainer();
                    container.FromJson(_values[i]);
                    
                    // Add directly to dictionary
                    dictionary.Add(key, container);
                }

                return dictionary;
            }
        }

        /// <summary>
        /// Wrapper for serializing UnityObjectReference.
        /// </summary>
        [Serializable]
        private class UnityRefWrapper
        {
            public int StorageType;
            public string Key;
            public string TypeName;
        }
    }
} 