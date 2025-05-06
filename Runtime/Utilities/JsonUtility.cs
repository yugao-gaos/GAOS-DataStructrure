using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAOS.DataStructure
{
    /// <summary>
    /// Utility class for JSON serialization and deserialization.
    /// </summary>
    public static class DataContainerJsonUtility
    {
        /// <summary>
        /// Serializes an object to a JSON string.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="type">The type of the object.</param>
        /// <returns>A JSON string representing the object.</returns>
        public static string Serialize(object obj, Type type)
        {
            if (obj == null)
                return "null";

            // Handle primitive types
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                return obj.ToString();

            // Handle Unity types
            if (type == typeof(Vector2))
                return SerializeVector2((Vector2)obj);
            if (type == typeof(Vector3))
                return SerializeVector3((Vector3)obj);
            if (type == typeof(Vector4))
                return SerializeVector4((Vector4)obj);
            if (type == typeof(Quaternion))
                return SerializeQuaternion((Quaternion)obj);
            if (type == typeof(Color))
                return SerializeColor((Color)obj);
            if (type == typeof(Rect))
                return SerializeRect((Rect)obj);
            if (type == typeof(Bounds))
                return SerializeBounds((Bounds)obj);

            // Handle DataContainer
            if (obj is DataContainer container)
                return container.ToJson();

            // Handle arrays and lists
            if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
                return SerializeNestedStructure(obj);

            // Handle dictionaries
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                return SerializeNestedStructure(obj);

            // Fall back to Unity's JsonUtility
            try
            {
                return UnityEngine.JsonUtility.ToJson(obj);
            }
            catch (Exception)
            {
                Debug.LogWarning($"Failed to serialize object of type {type.Name} using Unity's JsonUtility");
                return "{}";
            }
        }

        /// <summary>
        /// Serializes an object to a JSON string.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>A JSON string representing the object.</returns>
        public static string Serialize<T>(T obj)
        {
            return Serialize(obj, typeof(T));
        }

        /// <summary>
        /// Deserializes a JSON string to an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="json">The JSON string.</param>
        /// <returns>The deserialized object.</returns>
        public static T Deserialize<T>(string json)
        {
            return (T)Deserialize(json, typeof(T));
        }

        /// <summary>
        /// Deserializes a JSON string to an object of the specified type.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <param name="type">The type to deserialize to.</param>
        /// <returns>The deserialized object.</returns>
        public static object Deserialize(string json, Type type)
        {
            if (string.IsNullOrEmpty(json) || json == "null")
                return null;

            // Handle primitive types
            if (type == typeof(string))
                return json;
            if (type == typeof(int) && int.TryParse(json, out int intValue))
                return intValue;
            if (type == typeof(float) && float.TryParse(json, out float floatValue))
                return floatValue;
            if (type == typeof(double) && double.TryParse(json, out double doubleValue))
                return doubleValue;
            if (type == typeof(bool) && bool.TryParse(json, out bool boolValue))
                return boolValue;

            // Handle Unity types
            if (type == typeof(Vector2))
                return DeserializeVector2(json);
            if (type == typeof(Vector3))
                return DeserializeVector3(json);
            if (type == typeof(Vector4))
                return DeserializeVector4(json);
            if (type == typeof(Quaternion))
                return DeserializeQuaternion(json);
            if (type == typeof(Color))
                return DeserializeColor(json);
            if (type == typeof(Rect))
                return DeserializeRect(json);
            if (type == typeof(Bounds))
                return DeserializeBounds(json);

            // Handle DataContainer
            if (type == typeof(DataContainer))
            {
                var container = new DataContainer();
                container.FromJson(json);
                return container;
            }

            // Handle arrays, lists, and dictionaries
            if (type.IsArray || 
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
            {
                return DeserializeNestedStructure(json, type);
            }

            // Fall back to Unity's JsonUtility
            try
            {
                return UnityEngine.JsonUtility.FromJson(json, type);
            }
            catch (Exception)
            {
                Debug.LogWarning($"Failed to deserialize JSON to type {type.Name} using Unity's JsonUtility");
                return Activator.CreateInstance(type);
            }
        }

        /// <summary>
        /// Serializes a Vector2 to a JSON string.
        /// </summary>
        public static string SerializeVector2(Vector2 vector)
        {
            return UnityEngine.JsonUtility.ToJson(vector);
        }

        /// <summary>
        /// Deserializes a JSON string to a Vector2.
        /// </summary>
        public static Vector2 DeserializeVector2(string json)
        {
            return UnityEngine.JsonUtility.FromJson<Vector2>(json);
        }

        /// <summary>
        /// Serializes a Vector3 to a JSON string.
        /// </summary>
        public static string SerializeVector3(Vector3 vector)
        {
            return UnityEngine.JsonUtility.ToJson(vector);
        }

        /// <summary>
        /// Deserializes a JSON string to a Vector3.
        /// </summary>
        public static Vector3 DeserializeVector3(string json)
        {
            return UnityEngine.JsonUtility.FromJson<Vector3>(json);
        }

        /// <summary>
        /// Serializes a Vector4 to a JSON string.
        /// </summary>
        public static string SerializeVector4(Vector4 vector)
        {
            return UnityEngine.JsonUtility.ToJson(vector);
        }

        /// <summary>
        /// Deserializes a JSON string to a Vector4.
        /// </summary>
        public static Vector4 DeserializeVector4(string json)
        {
            return UnityEngine.JsonUtility.FromJson<Vector4>(json);
        }

        /// <summary>
        /// Serializes a Quaternion to a JSON string.
        /// </summary>
        public static string SerializeQuaternion(Quaternion quaternion)
        {
            return UnityEngine.JsonUtility.ToJson(quaternion);
        }

        /// <summary>
        /// Deserializes a JSON string to a Quaternion.
        /// </summary>
        public static Quaternion DeserializeQuaternion(string json)
        {
            return UnityEngine.JsonUtility.FromJson<Quaternion>(json);
        }

        /// <summary>
        /// Serializes a Color to a JSON string.
        /// </summary>
        public static string SerializeColor(Color color)
        {
            return UnityEngine.JsonUtility.ToJson(color);
        }

        /// <summary>
        /// Deserializes a JSON string to a Color.
        /// </summary>
        public static Color DeserializeColor(string json)
        {
            return UnityEngine.JsonUtility.FromJson<Color>(json);
        }

        /// <summary>
        /// Serializes a Rect to a JSON string.
        /// </summary>
        public static string SerializeRect(Rect rect)
        {
            // Create a wrapper for more reliable serialization
            var wrapper = new RectWrapper
            {
                x = rect.x,
                y = rect.y,
                width = rect.width,
                height = rect.height
            };
            return UnityEngine.JsonUtility.ToJson(wrapper);
        }

        /// <summary>
        /// Deserializes a JSON string to a Rect.
        /// </summary>
        public static Rect DeserializeRect(string json)
        {
            // Try deserializing with our wrapper first
            try
            {
                var wrapper = UnityEngine.JsonUtility.FromJson<RectWrapper>(json);
                if (wrapper != null)
                {
                    return new Rect(wrapper.x, wrapper.y, wrapper.width, wrapper.height);
                }
            }
            catch (Exception)
            {
                // If wrapper deserialization fails, try direct deserialization
                try
                {
                    return UnityEngine.JsonUtility.FromJson<Rect>(json);
                }
                catch (Exception)
                {
                    Debug.LogWarning("Failed to deserialize Rect. Returning default Rect.");
                }
            }
            
            return new Rect();
        }

        /// <summary>
        /// Helper class for serializing Rect values.
        /// </summary>
        [Serializable]
        private class RectWrapper
        {
            public float x;
            public float y;
            public float width;
            public float height;
        }

        /// <summary>
        /// Helper class for serializing Bounds values.
        /// </summary>
        [Serializable]
        private class BoundsWrapper
        {
            public float centerX;
            public float centerY;
            public float centerZ;
            public float sizeX;
            public float sizeY;
            public float sizeZ;
        }

        /// <summary>
        /// Serializes a Bounds to a JSON string.
        /// </summary>
        public static string SerializeBounds(Bounds bounds)
        {
            // Create a wrapper for more reliable serialization
            var wrapper = new BoundsWrapper
            {
                centerX = bounds.center.x,
                centerY = bounds.center.y,
                centerZ = bounds.center.z,
                sizeX = bounds.size.x,
                sizeY = bounds.size.y,
                sizeZ = bounds.size.z
            };
            return UnityEngine.JsonUtility.ToJson(wrapper);
        }

        /// <summary>
        /// Deserializes a JSON string to a Bounds.
        /// </summary>
        public static Bounds DeserializeBounds(string json)
        {
            // Try deserializing with our wrapper first
            try
            {
                var wrapper = UnityEngine.JsonUtility.FromJson<BoundsWrapper>(json);
                if (wrapper != null)
                {
                    return new Bounds(
                        new Vector3(wrapper.centerX, wrapper.centerY, wrapper.centerZ),
                        new Vector3(wrapper.sizeX, wrapper.sizeY, wrapper.sizeZ)
                    );
                }
            }
            catch (Exception)
            {
                // If wrapper deserialization fails, try direct deserialization
                try
                {
                    return UnityEngine.JsonUtility.FromJson<Bounds>(json);
                }
                catch (Exception)
                {
                    Debug.LogWarning("Failed to deserialize Bounds. Returning default Bounds.");
                }
            }
            
            return new Bounds();
        }

        /// <summary>
        /// Serializes a nested structure to a JSON string.
        /// </summary>
        public static string SerializeNestedStructure(object obj)
        {
            if (obj == null)
                return "null";

            // Handle dictionaries
            Type objectType = obj.GetType();
            if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type[] genericArgs = objectType.GetGenericArguments();
                Type keyType = genericArgs[0];
                Type valueType = genericArgs[1];
                
                // Only support string keys for now
                if (keyType != typeof(string))
                {
                    Debug.LogWarning("Only dictionaries with string keys are fully supported for serialization.");
                }
                
                // Serialize each key-value pair
                var dictWrapper = new DictionaryWrapper();
                
                // Get the Keys and Values
                var keysProperty = objectType.GetProperty("Keys");
                var valuesProperty = objectType.GetProperty("Values");
                var getItemMethod = objectType.GetMethod("get_Item");
                
                var keys = keysProperty.GetValue(obj);
                var keysEnumerator = ((System.Collections.IEnumerable)keys).GetEnumerator();
                
                while (keysEnumerator.MoveNext())
                {
                    var key = keysEnumerator.Current.ToString();
                    var value = getItemMethod.Invoke(obj, new[] { keysEnumerator.Current });
                    
                    dictWrapper.Keys.Add(key);
                    dictWrapper.Values.Add(value != null ? Serialize(value, value.GetType()) : "null");
                    dictWrapper.ValueTypes.Add(value != null ? value.GetType().AssemblyQualifiedName : "null");
                }
                
                return UnityEngine.JsonUtility.ToJson(dictWrapper);
            }
            
            // Handle arrays and lists
            if (objectType.IsArray || (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(List<>)))
            {
                Type elementType;
                var listWrapper = new ListWrapper();
                
                if (objectType.IsArray)
                {
                    elementType = objectType.GetElementType();
                    var array = (Array)obj;
                    
                    for (int i = 0; i < array.Length; i++)
                    {
                        var item = array.GetValue(i);
                        listWrapper.Items.Add(item != null ? Serialize(item, item.GetType()) : "null");
                        listWrapper.ItemTypes.Add(item != null ? item.GetType().AssemblyQualifiedName : "null");
                    }
                }
                else // List<T>
                {
                    elementType = objectType.GetGenericArguments()[0];
                    var list = obj;
                    var countProperty = objectType.GetProperty("Count");
                    var indexer = objectType.GetProperty("Item");
                    int count = (int)countProperty.GetValue(list);
                    
                    for (int i = 0; i < count; i++)
                    {
                        var item = indexer.GetValue(list, new object[] { i });
                        listWrapper.Items.Add(item != null ? Serialize(item, item.GetType()) : "null");
                        listWrapper.ItemTypes.Add(item != null ? item.GetType().AssemblyQualifiedName : "null");
                    }
                }
                
                listWrapper.ElementType = elementType.AssemblyQualifiedName;
                return UnityEngine.JsonUtility.ToJson(listWrapper);
            }
            
            // Default behavior for other types
            var wrapper = new SerializationWrapper { Data = obj.ToString() };
            return UnityEngine.JsonUtility.ToJson(wrapper);
        }

        /// <summary>
        /// Deserializes a JSON string to a nested structure.
        /// </summary>
        public static object DeserializeNestedStructure(string json, Type expectedType)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            
            // Handle dictionaries
            if (expectedType.IsGenericType && expectedType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                try
                {
                    var dictWrapper = UnityEngine.JsonUtility.FromJson<DictionaryWrapper>(json);
                    if (dictWrapper != null && dictWrapper.Keys.Count > 0)
                    {
                        Type[] genericArgs = expectedType.GetGenericArguments();
                        Type keyType = genericArgs[0];
                        Type valueType = genericArgs[1];
                        
                        // Create a new dictionary instance
                        var dictionary = Activator.CreateInstance(expectedType);
                        var addMethod = expectedType.GetMethod("Add");
                        
                        for (int i = 0; i < dictWrapper.Keys.Count; i++)
                        {
                            string key = dictWrapper.Keys[i];
                            string valueJson = dictWrapper.Values[i];
                            string valueTypeStr = dictWrapper.ValueTypes[i];
                            
                            // Skip null values
                            if (valueTypeStr == "null")
                                continue;
                                
                            // Get the actual value type
                            Type actualValueType = Type.GetType(valueTypeStr);
                            if (actualValueType == null)
                            {
                                Debug.LogWarning($"Could not find type {valueTypeStr} for key {key}. Skipping.");
                                continue;
                            }
                            
                            // Deserialize the value
                            object value;
                            
                            // Handle primitive types specially
                            if (actualValueType == typeof(int))
                            {
                                if (int.TryParse(valueJson, out int intValue))
                                    value = intValue;
                                else
                                {
                                    Debug.LogWarning($"Failed to convert value '{valueJson}' to type {actualValueType.Name}. Skipping.");
                                    continue;
                                }
                            }
                            else if (actualValueType == typeof(float))
                            {
                                if (float.TryParse(valueJson, out float floatValue))
                                    value = floatValue;
                                else
                                {
                                    Debug.LogWarning($"Failed to convert value '{valueJson}' to type {actualValueType.Name}. Skipping.");
                                    continue;
                                }
                            }
                            else if (actualValueType == typeof(bool))
                            {
                                if (bool.TryParse(valueJson, out bool boolValue))
                                    value = boolValue;
                                else
                                {
                                    Debug.LogWarning($"Failed to convert value '{valueJson}' to type {actualValueType.Name}. Skipping.");
                                    continue;
                                }
                            }
                            else if (actualValueType == typeof(string))
                            {
                                value = valueJson;
                            }
                            else
                            {
                                // For other types, use general deserialization
                                value = Deserialize(valueJson, actualValueType);
                            }
                            
                            // Convert the key if needed
                            object keyObj = key;
                            if (keyType != typeof(string))
                            {
                                try
                                {
                                    keyObj = Convert.ChangeType(key, keyType);
                                }
                                catch
                                {
                                    Debug.LogWarning($"Failed to convert key '{key}' to type {keyType.Name}. Skipping.");
                                    continue;
                                }
                            }
                            
                            // Add to the dictionary
                            addMethod.Invoke(dictionary, new[] { keyObj, value });
                        }
                        
                        return dictionary;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error deserializing dictionary: {ex.Message}");
                }
            }
            
            // Handle lists and arrays
            if (expectedType.IsArray || (expectedType.IsGenericType && expectedType.GetGenericTypeDefinition() == typeof(List<>)))
            {
                try
                {
                    var listWrapper = UnityEngine.JsonUtility.FromJson<ListWrapper>(json);
                    if (listWrapper != null && listWrapper.Items.Count > 0)
                    {
                        Type elementType = Type.GetType(listWrapper.ElementType);
                        if (elementType == null)
                        {
                            Debug.LogWarning($"Could not find element type {listWrapper.ElementType}. Returning null.");
                            return null;
                        }
                        
                        if (expectedType.IsArray)
                        {
                            // Handle arrays
                            Array array = Array.CreateInstance(elementType, listWrapper.Items.Count);
                            
                            for (int i = 0; i < listWrapper.Items.Count; i++)
                            {
                                string itemJson = listWrapper.Items[i];
                                string itemTypeStr = listWrapper.ItemTypes[i];
                                
                                if (itemTypeStr == "null")
                                {
                                    array.SetValue(null, i);
                                    continue;
                                }
                                
                                Type itemType = Type.GetType(itemTypeStr);
                                if (itemType == null)
                                {
                                    Debug.LogWarning($"Could not find type {itemTypeStr} for item at index {i}. Skipping.");
                                    continue;
                                }
                                
                                object item = Deserialize(itemJson, itemType);
                                array.SetValue(item, i);
                            }
                            
                            return array;
                        }
                        else
                        {
                            // Handle lists
                            var listType = typeof(List<>).MakeGenericType(elementType);
                            var list = Activator.CreateInstance(listType);
                            var addMethod = listType.GetMethod("Add");
                            
                            for (int i = 0; i < listWrapper.Items.Count; i++)
                            {
                                string itemJson = listWrapper.Items[i];
                                string itemTypeStr = listWrapper.ItemTypes[i];
                                
                                if (itemTypeStr == "null")
                                {
                                    addMethod.Invoke(list, new object[] { null });
                                    continue;
                                }
                                
                                Type itemType = Type.GetType(itemTypeStr);
                                if (itemType == null)
                                {
                                    Debug.LogWarning($"Could not find type {itemTypeStr} for item at index {i}. Skipping.");
                                    continue;
                                }
                                
                                object item = Deserialize(itemJson, itemType);
                                addMethod.Invoke(list, new object[] { item });
                            }
                            
                            return list;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error deserializing list: {ex.Message}");
                }
            }
            
            // Default behavior for other types
            try
            {
                var wrapper = UnityEngine.JsonUtility.FromJson<SerializationWrapper>(json);
                return wrapper.Data;
            }
            catch
            {
                Debug.LogWarning($"Failed to deserialize JSON to type {expectedType.Name}");
                return null;
            }
        }

        /// <summary>
        /// Helper class for serializing objects that Unity's JsonUtility can't handle directly.
        /// </summary>
        [Serializable]
        private class SerializationWrapper
        {
            public string Data;
        }
        
        /// <summary>
        /// Helper class for serializing dictionaries.
        /// </summary>
        [Serializable]
        private class DictionaryWrapper
        {
            public List<string> Keys = new List<string>();
            public List<string> Values = new List<string>();
            public List<string> ValueTypes = new List<string>();
        }

        /// <summary>
        /// Helper class for serializing lists.
        /// </summary>
        [Serializable]
        private class ListWrapper
        {
            public List<string> Items = new List<string>();
            public List<string> ItemTypes = new List<string>();
            public string ElementType;
        }
    }
} 