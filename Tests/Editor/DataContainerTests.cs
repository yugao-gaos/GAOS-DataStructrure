using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using GAOS.DataStructure;

namespace GAOS.DataStructure.Editor.Tests
{
    public class DataContainerTests
    {
        private DataContainer _container;

        [SetUp]
        public void Setup()
        {
            _container = new DataContainer();
        }

        [Test]
        public void SetAndGet_PrimitiveTypes_ReturnsCorrectValues()
        {
            // Arrange
            string stringValue = "test";
            int intValue = 42;
            float floatValue = 3.14f;
            bool boolValue = true;

            // Act
            _container.Set("string", stringValue);
            _container.Set("int", intValue);
            _container.Set("float", floatValue);
            _container.Set("bool", boolValue);

            // Assert
            Assert.AreEqual(stringValue, _container.Get<string>("string"));
            Assert.AreEqual(intValue, _container.Get<int>("int"));
            Assert.AreEqual(floatValue, _container.Get<float>("float"));
            Assert.AreEqual(boolValue, _container.Get<bool>("bool"));
        }

        [Test]
        public void SetAndGet_UnityTypes_ReturnsCorrectValues()
        {
            // Arrange
            Vector2 vector2Value = new Vector2(1, 2);
            Vector3 vector3Value = new Vector3(1, 2, 3);
            Color colorValue = Color.red;

            // Act
            _container.Set("vector2", vector2Value);
            _container.Set("vector3", vector3Value);
            _container.Set("color", colorValue);

            // Assert
            Assert.AreEqual(vector2Value, _container.Get<Vector2>("vector2"));
            Assert.AreEqual(vector3Value, _container.Get<Vector3>("vector3"));
            Assert.AreEqual(colorValue, _container.Get<Color>("color"));
        }

        [Test]
        public void SetAndGet_NestedContainer_ReturnsCorrectValues()
        {
            // Arrange
            var nestedContainer = new DataContainer();
            nestedContainer.Set("nestedValue", "nested");

            // Act
            _container.Set("nested", nestedContainer);

            // Assert
            var retrievedContainer = _container.Get<DataContainer>("nested");
            Assert.IsNotNull(retrievedContainer);
            Assert.AreEqual("nested", retrievedContainer.Get<string>("nestedValue"));
        }

        [Test]
        public void GetOrCreateContainer_CreatesNewContainer_WhenNotExists()
        {
            // Act
            var container = _container.GetOrCreateContainer("nested");

            // Assert
            Assert.IsNotNull(container);
            Assert.IsTrue(_container.Contains("nested"));
            Assert.AreEqual(typeof(DataContainer), _container.GetValueType("nested"));
        }

        [Test]
        public void GetOrCreateContainer_ReturnsExistingContainer_WhenExists()
        {
            // Arrange
            var existingContainer = new DataContainer();
            existingContainer.Set("test", "value");
            _container.Set("nested", existingContainer);

            // Act
            var container = _container.GetOrCreateContainer("nested");

            // Assert
            Assert.IsNotNull(container);
            Assert.AreEqual("value", container.Get<string>("test"));
        }

        [Test]
        public void GetOrCreateList_CreatesNewList_WhenNotExists()
        {
            // Act
            var list = _container.GetOrCreateList("list");

            // Assert
            Assert.IsNotNull(list);
            Assert.IsTrue(_container.Contains("list"));
            Assert.AreEqual(typeof(List<DataContainer>), _container.GetValueType("list"));
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void GetOrCreateDictionary_CreatesNewDictionary_WhenNotExists()
        {
            // Act
            var dict = _container.GetOrCreateDictionary("dict");

            // Assert
            Assert.IsNotNull(dict);
            Assert.IsTrue(_container.Contains("dict"));
            Assert.AreEqual(typeof(Dictionary<string, DataContainer>), _container.GetValueType("dict"));
            Assert.AreEqual(0, dict.Count);
        }

        [Test]
        public void DeepCopy_CreatesIndependentCopy()
        {
            // Arrange
            _container.Set("primitive", 42);
            var nestedContainer = _container.GetOrCreateContainer("nested");
            nestedContainer.Set("nestedValue", "test");
            var list = _container.GetOrCreateList("list");
            var listItem = new DataContainer();
            listItem.Set("itemValue", "item");
            list.Add(listItem);
            var dict = _container.GetOrCreateDictionary("dict");
            var dictItem = new DataContainer();
            dictItem.Set("dictValue", "dict");
            dict["key"] = dictItem;

            // Act
            var copy = (DataContainer)_container.DeepCopy();

            // Assert - Check that the copy has the same values
            Assert.AreEqual(42, copy.Get<int>("primitive"));
            Assert.AreEqual("test", copy.PathGet<DataContainer>("nested").Get<string>("nestedValue"));
            Assert.AreEqual("item", copy.Get<List<DataContainer>>("list")[0].Get<string>("itemValue"));
            Assert.AreEqual("dict", copy.Get<Dictionary<string, DataContainer>>("dict")["key"].Get<string>("dictValue"));

            // Assert - Check that the copy is independent
            _container.Set("primitive", 100);
            nestedContainer.Set("nestedValue", "changed");
            list[0].Set("itemValue", "changed");
            dict["key"].Set("dictValue", "changed");

            Assert.AreEqual(42, copy.Get<int>("primitive"));
            Assert.AreEqual("test", copy.PathGet<DataContainer>("nested").Get<string>("nestedValue"));
            Assert.AreEqual("item", copy.Get<List<DataContainer>>("list")[0].Get<string>("itemValue"));
            Assert.AreEqual("dict", copy.Get<Dictionary<string, DataContainer>>("dict")["key"].Get<string>("dictValue"));
        }

        [Test]
        public void Path_ReturnsCorrectContainer()
        {
            // Arrange
            var level1 = _container.GetOrCreateContainer("level1");
            var level2 = level1.GetOrCreateContainer("level2");
            var level3 = level2.GetOrCreateContainer("level3");
            level3.Set("value", "deep");

            // Act
            var container = _container.PathGet<DataContainer>("level1.level2.level3");

            // Assert
            Assert.IsNotNull(container);
            Assert.AreEqual("deep", container.Get<string>("value"));
        }

        [Test]
        public void PathGet_ReturnsCorrectValue()
        {
            // Arrange
            var level1 = _container.GetOrCreateContainer("level1");
            var level2 = level1.GetOrCreateContainer("level2");
            var level3 = level2.GetOrCreateContainer("level3");
            level3.Set("value", "deep");

            // Act
            var value = _container.PathGet<string>("level1.level2.level3.value");

            // Assert
            Assert.AreEqual("deep", value);
        }

        [Test]
        public void PathSet_SetsCorrectValue()
        {
            // Act
            _container.PathSet("level1.level2.level3.value", "deep");

            // Assert
            Assert.IsTrue(_container.Contains("level1"));
            var level1 = _container.Get<DataContainer>("level1");
            Assert.IsTrue(level1.Contains("level2"));
            var level2 = level1.Get<DataContainer>("level2");
            Assert.IsTrue(level2.Contains("level3"));
            var level3 = level2.Get<DataContainer>("level3");
            Assert.IsTrue(level3.Contains("value"));
            Assert.AreEqual("deep", level3.Get<string>("value"));
        }

        [Test]
        public void ToJson_FromJson_PreservesData()
        {
            // Arrange
            _container.Set("string", "test");
            _container.Set("int", 42);
            _container.Set("float", 3.14f);
            _container.Set("bool", true);
            _container.Set("vector2", new Vector2(1, 2));
            _container.Set("vector3", new Vector3(1, 2, 3));
            _container.Set("color", Color.red);
            var nestedContainer = _container.GetOrCreateContainer("nested");
            nestedContainer.Set("nestedValue", "nested");
            var list = _container.GetOrCreateList("list");
            var listItem = new DataContainer();
            listItem.Set("itemValue", "item");
            list.Add(listItem);
            var dict = _container.GetOrCreateDictionary("dict");
            var dictItem = new DataContainer();
            dictItem.Set("dictValue", "dict");
            dict["key"] = dictItem;

            // Act
            string json = _container.ToJson();
            var newContainer = new DataContainer();
            newContainer.FromJson(json);

            // Assert
            Assert.AreEqual("test", newContainer.Get<string>("string"));
            Assert.AreEqual(42, newContainer.Get<int>("int"));
            Assert.AreEqual(3.14f, newContainer.Get<float>("float"));
            Assert.AreEqual(true, newContainer.Get<bool>("bool"));
            Assert.AreEqual(new Vector2(1, 2), newContainer.Get<Vector2>("vector2"));
            Assert.AreEqual(new Vector3(1, 2, 3), newContainer.Get<Vector3>("vector3"));
            Assert.AreEqual(Color.red, newContainer.Get<Color>("color"));
            Assert.AreEqual("nested", newContainer.Path("nested").Get<string>("nestedValue"));
            Assert.AreEqual("item", newContainer.Get<List<DataContainer>>("list")[0].Get<string>("itemValue"));
            Assert.AreEqual("dict", newContainer.Get<Dictionary<string, DataContainer>>("dict")["key"].Get<string>("dictValue"));
        }
    }
} 