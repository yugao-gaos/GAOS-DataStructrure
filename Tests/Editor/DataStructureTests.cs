using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using GAOS.DataStructure;

namespace GAOS.DataStructure.Editor.Tests
{
    public class DataStructureTests
    {
        private DataStructure _structure;

        [SetUp]
        public void Setup()
        {
            // Create a test structure
            _structure = ScriptableObject.CreateInstance<DataStructure>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up
            Object.DestroyImmediate(_structure);
        }

        [Test]
        public void NewDataStructure_HasEmptyContainer()
        {
            // Assert
            Assert.IsNotNull(_structure.Container);
            Assert.AreEqual(0, _structure.Container.GetKeys().Count());
        }

        [Test]
        public void CreateInstance_CreatesInstanceWithCorrectParentStructure()
        {
            // Act
            var instance = _structure.CreateInstance();
            
            // Assert
            Assert.IsNotNull(instance);
            Assert.AreEqual(_structure, instance.ParentStructure);
            
            // Clean up
            Object.DestroyImmediate(instance);
        }

        [Test]
        public void CreateInstance_CreatesInstanceWithCopiedData()
        {
            // Arrange - Set up the structure's container
            var container = _structure.Container;
            container.Set("string", "default");
            container.Set("int", 42);
            container.Set("float", 3.14f);
            container.Set("bool", true);
            
            // Add a nested container
            var nestedContainer = container.GetOrCreateContainer("nested");
            nestedContainer.Set("nestedValue", "default");
            
            // Add a list
            var list = container.GetOrCreateList("list");
            var listItem = new DataContainer();
            listItem.Set("itemValue", "default");
            list.Add(listItem);
            
            // Add a dictionary
            var dict = container.GetOrCreateDictionary("dict");
            var dictItem = new DataContainer();
            dictItem.Set("dictValue", "default");
            dict["key"] = dictItem;
            
            // Act
            var instance = _structure.CreateInstance();
            
            // Assert
            Assert.IsNotNull(instance.Container);
            
            // Check that values are copied
            Assert.AreEqual("default", instance.Container.Get<string>("string"));
            Assert.AreEqual(42, instance.Container.Get<int>("int"));
            Assert.AreEqual(3.14f, instance.Container.Get<float>("float"));
            Assert.AreEqual(true, instance.Container.Get<bool>("bool"));
            
            // Check nested container
            var instanceNestedContainer = instance.Container.Get<DataContainer>("nested");
            Assert.IsNotNull(instanceNestedContainer);
            Assert.AreEqual("default", instanceNestedContainer.Get<string>("nestedValue"));
            
            // Check list
            var instanceList = instance.Container.Get<List<DataContainer>>("list");
            Assert.IsNotNull(instanceList);
            Assert.AreEqual(1, instanceList.Count);
            Assert.AreEqual("default", instanceList[0].Get<string>("itemValue"));
            
            // Check dictionary
            var instanceDict = instance.Container.Get<Dictionary<string, DataContainer>>("dict");
            Assert.IsNotNull(instanceDict);
            Assert.AreEqual(1, instanceDict.Count);
            Assert.IsTrue(instanceDict.ContainsKey("key"));
            Assert.AreEqual("default", instanceDict["key"].Get<string>("dictValue"));
            
            // Clean up
            Object.DestroyImmediate(instance);
        }

     
        [Test]
        public void ToJson_FromJson_PreservesData()
        {
            // Arrange - Set up the structure's container
            var container = _structure.Container;
            container.Set("string", "default");
            container.Set("int", 42);
            container.Set("float", 3.14f);
            container.Set("bool", true);
            
            // Add a nested container
            var nestedContainer = container.GetOrCreateContainer("nested");
            nestedContainer.Set("nestedValue", "default");
            
            // Act
            string json = _structure.ToJson();
            var newStructure = ScriptableObject.CreateInstance<DataStructure>();
            newStructure.FromJson(json);
            
            // Assert
            Assert.AreEqual("default", newStructure.Container.Get<string>("string"));
            Assert.AreEqual(42, newStructure.Container.Get<int>("int"));
            Assert.AreEqual(3.14f, newStructure.Container.Get<float>("float"));
            Assert.AreEqual(true, newStructure.Container.Get<bool>("bool"));
            
            // Check nested container
            var newNestedContainer = newStructure.Container.Get<DataContainer>("nested");
            Assert.IsNotNull(newNestedContainer);
            Assert.AreEqual("default", newNestedContainer.Get<string>("nestedValue"));
            
            // Clean up
            Object.DestroyImmediate(newStructure);
        }

        [Test]
        public void DeepCopy_CreatesIndependentCopy()
        {
            // Arrange - Set up the structure's container
            var container = _structure.Container;
            container.Set("string", "default");
            container.Set("int", 42);
            
            // Add a nested container
            var nestedContainer = container.GetOrCreateContainer("nested");
            nestedContainer.Set("nestedValue", "default");
            
            // Act
            var copy = _structure.DeepCopy();
            
            // Modify the original
            _structure.Container.Set("string", "modified");
            _structure.Container.Get<DataContainer>("nested").Set("nestedValue", "modified");
            
            // Assert
            Assert.AreEqual("default", copy.Container.Get<string>("string"));
            Assert.AreEqual(42, copy.Container.Get<int>("int"));
            
            var copyNestedContainer = copy.Container.Get<DataContainer>("nested");
            Assert.IsNotNull(copyNestedContainer);
            Assert.AreEqual("default", copyNestedContainer.Get<string>("nestedValue"));
            
            // Clean up
            Object.DestroyImmediate(copy);
        }

        [Test]
        public void GetAllPaths_ReturnsCorrectPaths()
        {
            // Setup structure with nested data
            var container = _structure.Container;
            container.Set("string", "value");
            var nested = container.GetOrCreateContainer("nested");
            nested.Set("nestedValue", 42);
            
            // Add a list for more complex paths
            var list = container.GetOrCreateList("items");
            var item = new DataContainer();
            item.Set("listItemValue", "test");
            list.Add(item);
            
            // Add a dictionary
            var dict = container.GetOrCreateDictionary("dict");
            var dictItem = new DataContainer();
            dictItem.Set("dictValue", "dictTest");
            dict["key1"] = dictItem;
            
            // Act
            var paths = _structure.GetAllPaths().ToList();
            
            // Assert
            Assert.IsTrue(paths.Contains("string"));
            Assert.IsTrue(paths.Contains("nested"));
            Assert.IsTrue(paths.Contains("nested.nestedValue"));
            Assert.IsTrue(paths.Contains("items"));
            Assert.IsTrue(paths.Contains("items[0]"));
            Assert.IsTrue(paths.Contains("items[0].listItemValue"));
            Assert.IsTrue(paths.Contains("dict"));
            Assert.IsTrue(paths.Contains("dict[\"key1\"]"));
            Assert.IsTrue(paths.Contains("dict[\"key1\"].dictValue"));
        }
        
        [Test]
        public void GetPathType_ReturnsCorrectTypes()
        {
            // Setup structure with different types
            var container = _structure.Container;
            container.Set("string", "value");
            container.Set("int", 42);
            container.Set("float", 3.14f);
            container.Set("bool", true);
            container.Set("vector3", new Vector3(1, 2, 3));
            container.GetOrCreateContainer("nested");
            container.GetOrCreateList("list");
            container.GetOrCreateDictionary("dict");
            
            // Assert
            Assert.AreEqual(typeof(string), _structure.GetPathType("string"));
            Assert.AreEqual(typeof(int), _structure.GetPathType("int"));
            Assert.AreEqual(typeof(float), _structure.GetPathType("float"));
            Assert.AreEqual(typeof(bool), _structure.GetPathType("bool"));
            Assert.AreEqual(typeof(Vector3), _structure.GetPathType("vector3"));
            Assert.AreEqual(typeof(DataContainer), _structure.GetPathType("nested"));
            Assert.AreEqual(typeof(List<DataContainer>), _structure.GetPathType("list"));
            Assert.AreEqual(typeof(Dictionary<string, DataContainer>), _structure.GetPathType("dict"));
            
            // Test non-existent path
            Assert.IsNull(_structure.GetPathType("nonexistent"));
            
            // Test nested path
            var nested = container.Get<DataContainer>("nested");
            nested.Set("nestedInt", 123);
            Assert.AreEqual(typeof(int), _structure.GetPathType("nested.nestedInt"));
        }
        
        [Test]
        public void Validate_ReturnsTrueForValidStructure()
        {
            // A new structure should be valid
            Assert.IsTrue(_structure.Validate());
            
            // Add some data to make sure it's still valid
            _structure.Container.Set("test", "value");
            Assert.IsTrue(_structure.Validate());
        }
        
        [Test]
        public void CreateInstance_PreservesListsAndDictionaries()
        {
            // Arrange - Set up complex nested collections
            var container = _structure.Container;
            
            // Create nested list of primitives (we'll use Set for ints since GetOrCreateList only works for DataContainer)
            container.Set("intList", new List<int> { 1, 2, 3 });
            
            // Create nested list of containers
            var containerList = container.GetOrCreateList("containerList");
            for (int i = 0; i < 3; i++)
            {
                var item = new DataContainer();
                item.Set("index", i);
                item.Set("value", $"item{i}");
                containerList.Add(item);
            }
            
            // Create nested dictionary (we'll use Set for string dictionary)
            var stringDict = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            };
            container.Set("stringDict", stringDict);
            
            // Create nested dictionary of containers
            var containerDict = container.GetOrCreateDictionary("containerDict");
            for (int i = 0; i < 3; i++)
            {
                var dictItem = new DataContainer();
                dictItem.Set("dictIndex", i);
                dictItem.Set("dictValue", $"dictItem{i}");
                containerDict[$"dictKey{i}"] = dictItem;
            }
            
            // Act
            var instance = _structure.CreateInstance();
            
            // Assert - Check lists
            var instanceIntList = instance.Container.Get<List<int>>("intList");
            Assert.IsNotNull(instanceIntList);
            Assert.AreEqual(3, instanceIntList.Count);
            Assert.AreEqual(1, instanceIntList[0]);
            Assert.AreEqual(2, instanceIntList[1]);
            Assert.AreEqual(3, instanceIntList[2]);
            
            var instanceContainerList = instance.Container.Get<List<DataContainer>>("containerList");
            Assert.IsNotNull(instanceContainerList);
            Assert.AreEqual(3, instanceContainerList.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(i, instanceContainerList[i].Get<int>("index"));
                Assert.AreEqual($"item{i}", instanceContainerList[i].Get<string>("value"));
            }
            
            // Assert - Check dictionaries
            var instanceStringDict = instance.Container.Get<Dictionary<string, string>>("stringDict");
            Assert.IsNotNull(instanceStringDict);
            Assert.AreEqual(2, instanceStringDict.Count);
            Assert.AreEqual("value1", instanceStringDict["key1"]);
            Assert.AreEqual("value2", instanceStringDict["key2"]);
            
            var instanceContainerDict = instance.Container.Get<Dictionary<string, DataContainer>>("containerDict");
            Assert.IsNotNull(instanceContainerDict);
            Assert.AreEqual(3, instanceContainerDict.Count);
            for (int i = 0; i < 3; i++)
            {
                var key = $"dictKey{i}";
                Assert.IsTrue(instanceContainerDict.ContainsKey(key));
                Assert.AreEqual(i, instanceContainerDict[key].Get<int>("dictIndex"));
                Assert.AreEqual($"dictItem{i}", instanceContainerDict[key].Get<string>("dictValue"));
            }
            
            // Clean up
            Object.DestroyImmediate(instance);
        }
    }
} 