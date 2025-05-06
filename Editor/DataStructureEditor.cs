using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using GAOS.DataStructure;

namespace GAOS.DataStructure.Editor
{
    [CustomEditor(typeof(DataStructure))]
    public class DataStructureEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            
            // Add stylesheet
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.gaos.datacontainer/Editor/UIToolkit/DataStructureEditor.uss");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }
            
            var structure = (DataStructure)target;
            
            // Add header with structure info
            var header = new VisualElement();
            header.AddToClassList("header");
            
            var nameLabel = new Label($"Data Structure: {structure.name}");
            nameLabel.AddToClassList("header-label");
            header.Add(nameLabel);
            
            root.Add(header);
            
            // Properties section
            var propertiesContainer = new VisualElement();
            propertiesContainer.AddToClassList("properties-container");
            
            // Structure ID field
            var idProperty = serializedObject.FindProperty("_structureId");
            var idField = new PropertyField(idProperty, "Structure ID");
            propertiesContainer.Add(idField);
            
            // Description field
            var descProperty = serializedObject.FindProperty("_description");
            var descField = new PropertyField(descProperty, "Description");
            propertiesContainer.Add(descField);
            
            root.Add(propertiesContainer);
            
            // Buttons section
            var buttonsContainer = new VisualElement();
            buttonsContainer.AddToClassList("buttons-container");
            
            // Open in full editor button
            var openButton = new Button(() => {
                DataStructureEditorWindow.ShowWindow();
                var window = EditorWindow.GetWindow<DataStructureEditorWindow>();
                window.SetStructure(structure);
            });
            openButton.text = "Open in Editor";
            buttonsContainer.Add(openButton);
            
            // Create Instance button
            var createInstanceButton = new Button(() => {
                CreateInstanceFromStructure(structure);
            });
            createInstanceButton.text = "Create Instance";
            buttonsContainer.Add(createInstanceButton);
            
            root.Add(buttonsContainer);
            
            // Structure info
            var infoContainer = new VisualElement();
            infoContainer.AddToClassList("info-container");
            
            var containerInfo = new Label($"Keys: {CountKeys(structure.Container)}");
            containerInfo.AddToClassList("container-info");
            infoContainer.Add(containerInfo);
            
            root.Add(infoContainer);
            
            // Add note about editing
            var noteContainer = new VisualElement();
            noteContainer.AddToClassList("note-container");
            
            var noteLabel = new Label("Note: Use the dedicated editor window for full editing capabilities.");
            noteLabel.AddToClassList("note-label");
            noteContainer.Add(noteLabel);
            
            root.Add(noteContainer);
            
            return root;
        }
        
        private int CountKeys(DataContainer container)
        {
            int count = 0;
            
            // Count top-level keys
            count += container.GetKeys().Count();
            
            // Count nested keys
            foreach (var key in container.GetKeys())
            {
                if (container.GetValueType(key) == typeof(DataContainer))
                {
                    var nestedContainer = container.Get<DataContainer>(key);
                    count += CountKeys(nestedContainer);
                }
            }
            
            return count;
        }
        
        private void CreateInstanceFromStructure(DataStructure structure)
        {
            if (structure == null)
                return;
                
            // Create save dialog
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Data Instance", 
                structure.name + "Instance", 
                "asset", 
                "Select location to save the Data Instance asset");
                
            if (string.IsNullOrEmpty(path))
                return;
                
            // Extract filename without extension
            var filename = System.IO.Path.GetFileNameWithoutExtension(path);
            var directory = System.IO.Path.GetDirectoryName(path);
                
            // Create the instance
            #if UNITY_EDITOR
            var instance = structure.CreatePersistentInstance(directory, filename);
            if (instance != null)
            {
                // Select the new asset
                Selection.activeObject = instance;
                EditorUtility.DisplayDialog("Instance Created", $"Instance '{filename}' has been created.", "OK");
            }
            #endif
        }
    }
    
    // Extension for DataStructureEditorWindow to set structure directly
    public static class DataStructureEditorWindowExtensions
    {
        public static void SetStructure(this DataStructureEditorWindow window, DataStructure structure)
        {
            var field = typeof(DataStructureEditorWindow).GetField("_targetStructure", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(window, structure);
                
                var method = typeof(DataStructureEditorWindow).GetMethod("SetTargetStructure", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(window, new object[] { structure });
                }
            }
        }
    }
} 