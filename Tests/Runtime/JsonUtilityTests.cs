using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GAOS.DataStructure;

namespace GAOS.DataStructure.Tests
{
    public class JsonUtilityTests
    {
        [Test]
        public void Serialize_Deserialize_PrimitiveTypes()
        {
            // Arrange
            string stringValue = "test string";
            int intValue = 42;
            float floatValue = 3.14f;
            bool boolValue = true;
            
            // Act & Assert - String
            string stringJson = DataContainerJsonUtility.Serialize(stringValue);
            string deserializedString = DataContainerJsonUtility.Deserialize<string>(stringJson);
            Assert.AreEqual(stringValue, deserializedString);
            
            // Act & Assert - Int
            string intJson = DataContainerJsonUtility.Serialize(intValue);
            int deserializedInt = DataContainerJsonUtility.Deserialize<int>(intJson);
            Assert.AreEqual(intValue, deserializedInt);
            
            // Act & Assert - Float
            string floatJson = DataContainerJsonUtility.Serialize(floatValue);
            float deserializedFloat = DataContainerJsonUtility.Deserialize<float>(floatJson);
            Assert.AreEqual(floatValue, deserializedFloat);
            
            // Act & Assert - Bool
            string boolJson = DataContainerJsonUtility.Serialize(boolValue);
            bool deserializedBool = DataContainerJsonUtility.Deserialize<bool>(boolJson);
            Assert.AreEqual(boolValue, deserializedBool);
        }
        
        [Test]
        public void Serialize_Deserialize_UnityTypes()
        {
            // Arrange
            Vector2 vector2Value = new Vector2(1.0f, 2.0f);
            Vector3 vector3Value = new Vector3(1.0f, 2.0f, 3.0f);
            Quaternion quaternionValue = Quaternion.Euler(30, 45, 60);
            Color colorValue = new Color(0.1f, 0.2f, 0.3f, 1.0f);
            Rect rectValue = new Rect(1, 2, 3, 4);
            Bounds boundsValue = new Bounds(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
            
            // Act & Assert - Vector2
            string vector2Json = DataContainerJsonUtility.Serialize(vector2Value);
            Vector2 deserializedVector2 = DataContainerJsonUtility.Deserialize<Vector2>(vector2Json);
            Assert.AreEqual(vector2Value, deserializedVector2);
            
            // Act & Assert - Vector3
            string vector3Json = DataContainerJsonUtility.Serialize(vector3Value);
            Vector3 deserializedVector3 = DataContainerJsonUtility.Deserialize<Vector3>(vector3Json);
            Assert.AreEqual(vector3Value, deserializedVector3);
            
            // Act & Assert - Quaternion
            string quaternionJson = DataContainerJsonUtility.Serialize(quaternionValue);
            Quaternion deserializedQuaternion = DataContainerJsonUtility.Deserialize<Quaternion>(quaternionJson);
            Assert.AreEqual(quaternionValue.x, deserializedQuaternion.x, 0.0001f);
            Assert.AreEqual(quaternionValue.y, deserializedQuaternion.y, 0.0001f);
            Assert.AreEqual(quaternionValue.z, deserializedQuaternion.z, 0.0001f);
            Assert.AreEqual(quaternionValue.w, deserializedQuaternion.w, 0.0001f);
            
            // Act & Assert - Color
            string colorJson = DataContainerJsonUtility.Serialize(colorValue);
            Color deserializedColor = DataContainerJsonUtility.Deserialize<Color>(colorJson);
            Assert.AreEqual(colorValue, deserializedColor);
            
            // Act & Assert - Rect
            string rectJson = DataContainerJsonUtility.Serialize(rectValue);
            Rect deserializedRect = DataContainerJsonUtility.Deserialize<Rect>(rectJson);
            Assert.AreEqual(rectValue, deserializedRect);
            
            // Act & Assert - Bounds
            string boundsJson = DataContainerJsonUtility.Serialize(boundsValue);
            Bounds deserializedBounds = DataContainerJsonUtility.Deserialize<Bounds>(boundsJson);
            Assert.AreEqual(boundsValue.center, deserializedBounds.center);
            Assert.AreEqual(boundsValue.size, deserializedBounds.size);
        }
        
        [Test]
        public void Serialize_Deserialize_DataContainer()
        {
            // Arrange
            var container = new DataContainer();
            container.Set("string", "test");
            container.Set("int", 42);
            container.Set("float", 3.14f);
            container.Set("bool", true);
            container.Set("vector2", new Vector2(1, 2));
            
            // Add a nested container
            var nestedContainer = container.GetOrCreateContainer("nested");
            nestedContainer.Set("nestedValue", "nested test");
            
            // Act
            string json = DataContainerJsonUtility.Serialize(container);
            var deserializedContainer = DataContainerJsonUtility.Deserialize<DataContainer>(json);
            
            // Assert
            Assert.IsNotNull(deserializedContainer);
            Assert.AreEqual("test", deserializedContainer.Get<string>("string"));
            Assert.AreEqual(42, deserializedContainer.Get<int>("int"));
            Assert.AreEqual(3.14f, deserializedContainer.Get<float>("float"));
            Assert.AreEqual(true, deserializedContainer.Get<bool>("bool"));
            Assert.AreEqual(new Vector2(1, 2), deserializedContainer.Get<Vector2>("vector2"));
            
            // Check nested container
            var deserializedNestedContainer = deserializedContainer.Get<DataContainer>("nested");
            Assert.IsNotNull(deserializedNestedContainer);
            Assert.AreEqual("nested test", deserializedNestedContainer.Get<string>("nestedValue"));
        }
        
        [Test]
        public void Serialize_Deserialize_List()
        {
            // Arrange
            var list = new List<string> { "item1", "item2", "item3" };
            
            // Act
            string json = DataContainerJsonUtility.Serialize(list);
            var deserializedList = DataContainerJsonUtility.Deserialize<List<string>>(json);
            
            // Assert
            Assert.IsNotNull(deserializedList);
            Assert.AreEqual(3, deserializedList.Count);
            Assert.AreEqual("item1", deserializedList[0]);
            Assert.AreEqual("item2", deserializedList[1]);
            Assert.AreEqual("item3", deserializedList[2]);
        }
        
        [Test]
        public void Serialize_Deserialize_Dictionary()
        {
            // Arrange
            var dict = new Dictionary<string, int>
            {
                { "key1", 1 },
                { "key2", 2 },
                { "key3", 3 }
            };
            
            // Act
            string json = DataContainerJsonUtility.Serialize(dict);
            var deserializedDict = DataContainerJsonUtility.Deserialize<Dictionary<string, int>>(json);
            
            // Assert
            Assert.IsNotNull(deserializedDict);
            Assert.AreEqual(3, deserializedDict.Count);
            Assert.AreEqual(1, deserializedDict["key1"]);
            Assert.AreEqual(2, deserializedDict["key2"]);
            Assert.AreEqual(3, deserializedDict["key3"]);
        }
        
        [Test]
        public void Serialize_Deserialize_ComplexNestedStructure()
        {
            // Arrange
            var container = new DataContainer();
            
            // Add primitive values
            container.Set("string", "test");
            container.Set("int", 42);
            
            // Add a nested container
            var nestedContainer = container.GetOrCreateContainer("nested");
            nestedContainer.Set("nestedString", "nested value");
            nestedContainer.Set("nestedVector", new Vector3(1, 2, 3));
            
            // Add a list of containers
            var list = container.GetOrCreateList("list");
            for (int i = 0; i < 3; i++)
            {
                var item = new DataContainer();
                item.Set("index", i);
                item.Set("name", $"item{i}");
                list.Add(item);
            }
            
            // Add a dictionary of containers
            var dict = container.GetOrCreateDictionary("dict");
            for (int i = 0; i < 3; i++)
            {
                var item = new DataContainer();
                item.Set("value", i * 10);
                dict[$"key{i}"] = item;
            }
            
            // Act
            string json = DataContainerJsonUtility.Serialize(container);
            var deserializedContainer = DataContainerJsonUtility.Deserialize<DataContainer>(json);
            
            // Assert - Basic values
            Assert.IsNotNull(deserializedContainer);
            Assert.AreEqual("test", deserializedContainer.Get<string>("string"));
            Assert.AreEqual(42, deserializedContainer.Get<int>("int"));
            
            // Assert - Nested container
            var deserializedNestedContainer = deserializedContainer.Get<DataContainer>("nested");
            Assert.IsNotNull(deserializedNestedContainer);
            Assert.AreEqual("nested value", deserializedNestedContainer.Get<string>("nestedString"));
            Assert.AreEqual(new Vector3(1, 2, 3), deserializedNestedContainer.Get<Vector3>("nestedVector"));
            
            // Assert - List
            var deserializedList = deserializedContainer.Get<List<DataContainer>>("list");
            Assert.IsNotNull(deserializedList);
            Assert.AreEqual(3, deserializedList.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(i, deserializedList[i].Get<int>("index"));
                Assert.AreEqual($"item{i}", deserializedList[i].Get<string>("name"));
            }
            
            // Assert - Dictionary
            var deserializedDict = deserializedContainer.Get<Dictionary<string, DataContainer>>("dict");
            Assert.IsNotNull(deserializedDict);
            Assert.AreEqual(3, deserializedDict.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(deserializedDict.ContainsKey($"key{i}"));
                Assert.AreEqual(i * 10, deserializedDict[$"key{i}"].Get<int>("value"));
            }
        }
    }
} 