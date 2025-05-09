using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using GAOS.DataStructure;
using GAOS.Logger;
using GAOS.DataStructure.Editor;
using System.Reflection;

namespace GAOS.DataStructure.Editor
{
    [CustomEditor(typeof(DataStructure))]
    public class DataStructureEditor : UnityEditor.Editor
    {
        private static Dictionary<DataStructure, Type> _generatedTypes = new Dictionary<DataStructure, Type>();

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
            
            // --- Open in Editor Button (moved up) ---
            var openButton = new Button(() => {
                DataStructureEditorWindow.ShowWindow();
                var window = EditorWindow.GetWindow<DataStructureEditorWindow>();
                window.SetStructure(structure);
            });
            openButton.text = "Open in Editor";
            openButton.style.marginBottom = 8;
            root.Add(openButton);
            
            // --- Interface Property Toggles Section ---
            var togglesSection = new VisualElement();
            togglesSection.style.marginTop = 10;
            togglesSection.style.marginBottom = 10;
            togglesSection.style.paddingTop = 10;
            togglesSection.style.paddingBottom = 10;
            togglesSection.style.paddingLeft = 10;
            togglesSection.style.paddingRight = 10;
            togglesSection.style.backgroundColor = new Color(0.18f, 0.22f, 0.28f, 0.18f);
            togglesSection.style.borderTopLeftRadius = 5;
            togglesSection.style.borderTopRightRadius = 5;
            togglesSection.style.borderBottomLeftRadius = 5;
            togglesSection.style.borderBottomRightRadius = 5;

            var togglesHeader = new Label("Interface Property Generation");
            togglesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            togglesHeader.style.marginBottom = 4;
            togglesSection.Add(togglesHeader);

            var togglesDesc = new Label("Toggle which properties are included in the generated DataInstance interface.");
            togglesDesc.style.fontSize = 11;
            togglesDesc.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            togglesDesc.style.marginBottom = 8;
            togglesSection.Add(togglesDesc);
            
            // Create header for the columns
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.marginBottom = 8;
            
            var propHeader = new Label("Property");
            propHeader.style.flexGrow = 1;
            propHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            var getSetHeader = new Label("Include");
            getSetHeader.style.width = 60;
            getSetHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            var metaHeader = new Label("Metadata");
            metaHeader.style.width = 70;
            metaHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            headerRow.Add(propHeader);
            headerRow.Add(getSetHeader);
            headerRow.Add(metaHeader);
            
            togglesSection.Add(headerRow);

            var allPaths = structure.GetAllPaths().ToList();
            foreach (var path in allPaths)
            {
                var type = structure.GetPathType(path);
                if (type == null) continue;
                bool isStructureType =
                    type == typeof(DataContainer) ||
                    (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && type.GetGenericArguments()[0] == typeof(DataContainer)) ||
                    (type.IsGenericType && type.GetGenericTypeDefinition().Name.StartsWith("OrderedDictionary") && type.GetGenericArguments()[0] == typeof(string) && type.GetGenericArguments()[1] == typeof(DataContainer));
                
                bool isIncluded = structure.InterfacePropertyPaths.Contains(path);
                bool hasMetadataAccess = structure.MetadataAccessPaths.Contains(path);
                
                // Default: ON for value types, OFF for structure types
                if (!isIncluded && !structure.InterfacePropertyPaths.Contains(path))
                {
                    if (!isStructureType)
                    {
                        structure.InterfacePropertyPaths.Add(path);
                        isIncluded = true;
                        EditorUtility.SetDirty(structure);
                    }
                }
                
                // Create a row for each property
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginBottom = 2;
                
                var propLabel = new Label($"{path} ({type.Name})");
                propLabel.style.flexGrow = 1;
                
                // Declare both toggles at the beginning
                var includeToggle = new Toggle();
                var metadataToggle = new Toggle();
                
                // Include property toggle
                includeToggle.style.width = 60;
                includeToggle.value = isIncluded;
                includeToggle.RegisterValueChangedCallback(evt => {
                    if (evt.newValue)
                    {
                        if (!structure.InterfacePropertyPaths.Contains(path))
                            structure.InterfacePropertyPaths.Add(path);
                    }
                    else
                    {
                        structure.InterfacePropertyPaths.Remove(path);
                        
                        // If we're disabling the property, also disable metadata access
                        if (structure.MetadataAccessPaths.Contains(path))
                        {
                            structure.MetadataAccessPaths.Remove(path);
                            metadataToggle.value = false;
                        }
                    }
                    EditorUtility.SetDirty(structure);
                });
                
                // Metadata access toggle
                metadataToggle.style.width = 70;
                metadataToggle.value = hasMetadataAccess;
                metadataToggle.SetEnabled(isIncluded); // Only enable if property is included
                metadataToggle.RegisterValueChangedCallback(evt => {
                    if (evt.newValue)
                    {
                        if (!structure.MetadataAccessPaths.Contains(path))
                            structure.MetadataAccessPaths.Add(path);
                    }
                    else
                    {
                        structure.MetadataAccessPaths.Remove(path);
                    }
                    EditorUtility.SetDirty(structure);
                });
                
                // Update metadata toggle when include toggle changes
                includeToggle.RegisterValueChangedCallback(evt => {
                    metadataToggle.SetEnabled(evt.newValue);
                    if (!evt.newValue && metadataToggle.value)
                    {
                        metadataToggle.value = false;
                    }
                });
                
                row.Add(propLabel);
                row.Add(includeToggle);
                row.Add(metadataToggle);
                
                togglesSection.Add(row);
            }
            root.Add(togglesSection);

            // --- Code Generation Button ---
            var codegenButton = new Button(() => {
                try
                {
                    Codegen.DataStructureCodeGenerator.RegenerateCode(structure);
                    EditorUtility.DisplayDialog("Code Generation", "Code generation completed successfully.", "OK");
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Code Generation Error", $"Failed to generate code: {ex.Message}", "OK");
                }
            });
            codegenButton.text = "Generate Code";
            codegenButton.style.marginBottom = 8;
            root.Add(codegenButton);
            
            // Buttons section
            var buttonsContainer = new VisualElement();
            buttonsContainer.AddToClassList("buttons-container");
            
            // --- Type Resolution (immediate, with debug logging) ---
            if (!string.IsNullOrEmpty(structure.GeneratedInstanceTypeFullName))
            {
                Debug.Log($"[DataStructureEditor] Checking type: {structure.GeneratedInstanceTypeFullName}");
                var foundType = FindTypeInAssemblies(structure.GeneratedInstanceTypeFullName);
                Debug.Log(foundType != null
                    ? $"[DataStructureEditor] Found type: {foundType.FullName}"
                    : "[DataStructureEditor] Type not found");
                if (foundType != null && typeof(DataInstance).IsAssignableFrom(foundType))
                {
                    Debug.Log($"[DataStructureEditor] Type is assignable to DataInstance: {foundType.FullName}");
                    _generatedTypes[structure] = foundType;
                }
                else
                {
                    Debug.Log("[DataStructureEditor] Type is not assignable to DataInstance or not found");
                    _generatedTypes[structure] = null;
                }
            }

            // --- Create Instance button (now type-checked) ---
            var createInstanceButton = new Button(() => {
                CreateInstanceFromStructure(structure);
            });
            createInstanceButton.text = "Create Instance";
            bool typeValid = _generatedTypes.TryGetValue(structure, out var genType) && genType != null;
            createInstanceButton.SetEnabled(typeValid);
            if (!typeValid)
            {
                var warn = new Label("Generated DataInstance type not found or invalid. Please generate code and recompile.");
                warn.style.color = new Color(1f, 0.6f, 0.2f, 1f);
                warn.style.marginBottom = 4;
                root.Add(warn);
            }
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

        private static Type FindTypeInAssemblies(string typeNameOrAssemblyQualified)
        {
            // Extract type name before comma if assembly-qualified
            var typeName = typeNameOrAssemblyQualified.Split(',')[0].Trim();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
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

    [InitializeOnLoad]
    public class DataStructureTypeResolver
    {
        static DataStructureTypeResolver()
        {
            EditorApplication.delayCall += ResolveAllDataStructureTypes;
        }

        private static void ResolveAllDataStructureTypes()
        {
            var guids = AssetDatabase.FindAssets("t:DataStructure");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var structure = AssetDatabase.LoadAssetAtPath<DataStructure>(path);
                if (structure == null) continue;
                var typeName = structure.GeneratedInstanceTypeFullName;
                if (string.IsNullOrEmpty(typeName)) continue;
                var foundType = FindTypeInAssemblies(typeName);
                if (foundType != null && typeof(DataInstance).IsAssignableFrom(foundType))
                {
                    var assemblyQualified = foundType.AssemblyQualifiedName;
                    if (typeName != assemblyQualified)
                    {
                        structure.SetGeneratedInstanceTypeFullName(assemblyQualified);
                        EditorUtility.SetDirty(structure);
                    }
                }
            }
        }

        private static Type FindTypeInAssemblies(string fullTypeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullTypeName);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
} 