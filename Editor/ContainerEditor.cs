using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using GAOS.DataStructure.References;
using GAOS.DataStructure;

namespace GAOS.DataStructure.Editor
{
    /// <summary>
    /// Editor for handling DataContainer type
    /// </summary>
    public class ContainerEditor : ContainerEditorBase
    {
        private DataStructureEditorWindow _editorWindow;

        public ContainerEditor(DataStructureEditorWindow editorWindow) : base(editorWindow)
        {
            _editorWindow = editorWindow;
        }

        /// <summary>
        /// Checks if this editor can handle the specified type
        /// </summary>
        public override bool CanHandleType(Type type)
        {
            return type == typeof(DataContainer);
        }

        /// <summary>
        /// Creates an editor field for DataContainer type
        /// </summary>
        public override VisualElement CreateEditorField(Type type, object value, Action<object> onValueChanged)
        {
            if (type != typeof(DataContainer))
            {
                Debug.LogError($"ContainerEditor cannot handle type {type.Name}");
                return new Label($"Unsupported type: {type.Name}");
            }

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            
            // Store the current path in the container's userData for context
            container.userData = _editorWindow?.CurrentPath;
            
            DataContainer dataContainer = value as DataContainer;
            
            if (dataContainer == null)
            {
                container.Add(new Label("Container is null"));
                return container;
            }
            
            // Create the container editor UI
            CreateContainerEditor(container, dataContainer, onValueChanged);
            
            return container;
        }
        
        /// <summary>
        /// Creates a DataContainer editor UI
        /// </summary>
        private void CreateContainerEditor(VisualElement container, DataContainer dataContainer, Action<object> onValueChanged)
        {
            Debug.Log($"Creating container editor with {dataContainer.GetKeys().Count()} keys");
            
            // Create table header using base class method
            var tableHeader = CreateTableHeader(true, 
                ("Key", 120, false),
                ("Type", 120, false),
                ("Value Preview", 0, true),
                ("Actions", 88, false));
            
            container.Add(tableHeader);

            // Create container for rows
            var rowsContainer = new VisualElement();
            rowsContainer.AddToClassList("data-container-items");
            container.Add(rowsContainer);
            
            // Add all properties in the container - reversed to match hierarchy tree order
            foreach (var key in dataContainer.GetKeys().Reverse())
            {
                try
                {
                    Type propType = dataContainer.GetValueType(key);
                    if (propType == null)
                    {
                        Debug.LogWarning($"Type for key '{key}' is null, skipping");
                        continue;
                    }
                    
                    // Get a readable value preview
                    string valuePreviewText = GetValuePreview(dataContainer, key, propType);
                    
                    var row = AddContainerPropertyRow(key, propType, valuePreviewText, dataContainer, onValueChanged);
                    rowsContainer.Add(row);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error adding property row for key '{key}': {ex.Message}");
                    // Continue with other properties
                }
            }
            
            // Add new property section
            var newPropertyRow = new VisualElement();
            newPropertyRow.AddToClassList("data-entry-row");
            newPropertyRow.AddToClassList("new-property-row");
            newPropertyRow.style.marginTop = 8;
            container.Add(newPropertyRow);
            
            // Empty cell for alignment with drag handle
            var emptyCell = new VisualElement();
            emptyCell.style.width = 24;
            newPropertyRow.Add(emptyCell);
            
            // Key input field
            var keyField = new TextField();
            keyField.style.width = 120;
            keyField.style.marginRight = 4;
            keyField.style.flexShrink = 0;
            newPropertyRow.Add(keyField);
            
            // Type dropdown
            var typeDropdown = new DropdownField();
            typeDropdown.style.width = 120;
            typeDropdown.style.marginRight = 4;
            typeDropdown.style.flexShrink = 0;
            
            // Add type options
            List<string> typeOptions = new List<string> {
                "Select type...",
                "String",
                "Int",
                "Float",
                "Bool",
                "Vector2",
                "Vector3",
                "Color",
                "Container",
                "List<Container>",
                "OrderedDictionary<string, Container>",
                "UnityObjectReference"
            };
            typeDropdown.choices = typeOptions;
            typeDropdown.index = 0;
            newPropertyRow.Add(typeDropdown);
            
            // Empty value preview placeholder
            var valuePreviewLabel = new Label();
            valuePreviewLabel.style.flexGrow = 1;
            newPropertyRow.Add(valuePreviewLabel);
            
            // Add button
            var actionsContainer = new VisualElement();
            actionsContainer.style.flexDirection = FlexDirection.Row;
            actionsContainer.style.justifyContent = Justify.SpaceBetween;
            actionsContainer.style.width = 88;
            actionsContainer.style.flexShrink = 0;
            newPropertyRow.Add(actionsContainer);
            
            var addButton = new Button(() => {
                try
                {
                    string propertyName = keyField.value;
                    if (string.IsNullOrWhiteSpace(propertyName))
                    {
                        // Show error
                        keyField.AddToClassList("validation-error");
                        return;
                    }
                    
                    if (typeDropdown.index == 0)
                    {
                        // Show error - no type selected
                        typeDropdown.AddToClassList("validation-error");
                        return;
                    }
                    
                    if (dataContainer.Contains(propertyName))
                    {
                        // Show error - duplicate key
                        keyField.AddToClassList("validation-error");
                        keyField.tooltip = $"Property '{propertyName}' already exists.";
                        return;
                    }
                    
                    // Get the selected type
                    Type selectedType = GetTypeFromSelection(typeDropdown.index);
                    if (selectedType == null)
                        return;
                        
                    // Create default value
                    object defaultValue = CreateDefaultValue(selectedType);
                    
                    // Add to container
                    dataContainer.Set(propertyName, defaultValue);
                    
                    // Notify of change
                    onValueChanged(dataContainer);
                    
                    // Reset fields
                    keyField.value = "";
                    typeDropdown.index = 0;
                    keyField.RemoveFromClassList("validation-error");
                    typeDropdown.RemoveFromClassList("validation-error");
                    
                    // Refresh the container editor
                    container.Clear();
                    CreateContainerEditor(container, dataContainer, onValueChanged);
                    
                    // Mark asset dirty and refresh hierarchy
                    ApplyStructuralChange();
                    
                    Debug.Log($"Added new property '{propertyName}' of type {selectedType.Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error adding property: {ex.Message}");
                    EditorUtility.DisplayDialog("Error", $"Failed to add property: {ex.Message}", "OK");
                }
            });
            addButton.text = "Add";
            addButton.SetEnabled(false); // Disabled by default
            actionsContainer.Add(addButton);
            
            // Enable add button only when both fields have values
            keyField.RegisterValueChangedCallback(evt => {
                addButton.SetEnabled(!string.IsNullOrWhiteSpace(evt.newValue) && typeDropdown.index > 0);
                // Reset error styling
                keyField.RemoveFromClassList("validation-error");
            });
            
            typeDropdown.RegisterValueChangedCallback(evt => {
                addButton.SetEnabled(!string.IsNullOrWhiteSpace(keyField.value) && typeDropdown.index > 0);
                // Reset error styling
                typeDropdown.RemoveFromClassList("validation-error");
            });
        }
        
        /// <summary>
        /// Creates a property row for a container item
        /// </summary>
        private VisualElement AddContainerPropertyRow(string key, Type type, string value, DataContainer container, Action<object> onValueChanged)
        {
            // Create standard row using base class method
            var row = CreateStandardRow();
            
            // For drag identification
            var rowData = new BaseRowData { Key = key, Type = type };
            row.userData = rowData;

            // Add drag handle using base class method
            var dragHandle = CreateDragHandle($"Container-{key}");
            
            // Make the drag handle draggable using base class method
            EnableDragForHandle(dragHandle, row, (r, state) => {
                ReorderContainerProperties(state.ParentContainer, container, onValueChanged);
            });
            
            row.Add(dragHandle);

            // Key field
            var keyElement = new Label(key);
            keyElement.style.width = 120;
            keyElement.style.flexShrink = 0;
            keyElement.style.overflow = Overflow.Hidden;
            keyElement.style.textOverflow = TextOverflow.Ellipsis;
            keyElement.tooltip = key; // Show full key on hover
            row.Add(keyElement);

            // Type field
            var typeElement = new Label(GetTypeName(type));
            typeElement.style.width = 120;
            typeElement.style.flexShrink = 0;
            row.Add(typeElement);

            // Value field
            var valueElement = new Label(value);
            valueElement.style.flexGrow = 1;
            valueElement.style.overflow = Overflow.Hidden;
            valueElement.style.textOverflow = TextOverflow.Ellipsis;
            valueElement.tooltip = value; // Show full value on hover
            row.Add(valueElement);

            // Create actions container using base class method
            var actionsContainer = CreateActionsContainer(88);
            
            // Add buttons using base class method
            actionsContainer.Add(CreateIconButton("🏷️", "Rename key", () => {
                RenameContainerKey(container, key, onValueChanged);
            }));

            actionsContainer.Add(CreateIconButton("✏️", "Edit property", () => {
                // Navigate to the property in the main editor instead of opening a popup
                if (_editorWindow != null)
                {
                    string currentPath = _editorWindow.CurrentPath;
                    
                    // Always use DataContainer's path utilities for consistent path construction
                    string itemPath;
                    if (string.IsNullOrEmpty(currentPath))
                    {
                        // If at root, just use the key
                        itemPath = key;
                    }
                    else
                    {
                        // Otherwise combine paths using the utility method
                        itemPath = DataContainer.CombinePath(currentPath, key);
                    }
                    
                    Debug.Log($"ContainerEditor: Selecting property at path: '{itemPath}' (from current path: '{currentPath}', key: '{key}')");
                    _editorWindow.SelectPropertyInternal(itemPath, key, type);
                }
            }));

            actionsContainer.Add(CreateIconButton("🗑️", "Delete property", () => {
                RemoveProperty(container, key, onValueChanged);
            }, marginRight: 0));
            
            row.Add(actionsContainer);

            return row;
        }
        
        /// <summary>
        /// Removes a property from the container
        /// </summary>
        private void RemoveProperty(DataContainer container, string key, Action<object> onValueChanged)
        {
            // Confirm before deleting
            bool confirmDelete = EditorUtility.DisplayDialog(
                "Confirm Delete",
                $"Are you sure you want to delete the property '{key}'?",
                "Delete",
                "Cancel"
            );
            
            if (!confirmDelete)
                return;
                
            // Remove the property
            container.Remove(key);
            
            // Notify of the change
            onValueChanged(container);
            
            // Mark asset dirty and refresh hierarchy
            ApplyStructuralChange();
        }
        
        /// <summary>
        /// Renames a key in the container
        /// </summary>
        private void RenameContainerKey(DataContainer container, string oldKey, Action<object> onValueChanged)
        {
            // Use asynchronous dialog approach
            EditorInputDialog.Show("Rename Key", $"Enter new name for '{oldKey}':", oldKey, newKey => {
                // Dialog completed - handle the result
                
                // Check if dialog was cancelled or empty input
                if (string.IsNullOrEmpty(newKey) || newKey == oldKey)
                    return;
                    
                // Check if key already exists
                if (container.Contains(newKey))
                {
                    EditorUtility.DisplayDialog("Error", $"A property with the key '{newKey}' already exists.", "OK");
                    return;
                }
                
                // Get the value type
                Type valueType = container.GetValueType(oldKey);
                if (valueType == null)
                    return;
                    
                // Get the current value using reflection
                var getMethod = typeof(DataContainer).GetMethod("Get").MakeGenericMethod(valueType);
                object defaultValue = CreateDefaultValue(valueType);
                object value = getMethod.Invoke(container, new[] { oldKey, defaultValue });
                
                // Add new key with same value using reflection
                var setMethod = typeof(DataContainer).GetMethod("Set").MakeGenericMethod(valueType);
                setMethod.Invoke(container, new[] { newKey, value });
                
                // Remove old key
                container.Remove(oldKey);
                
                // Notify of the change
                onValueChanged(container);
                
                // Mark asset dirty and refresh hierarchy
                ApplyStructuralChange();
            });
        }
       
        /// <summary>
        /// Reorder properties in a container based on visual order
        /// </summary>
        private void ReorderContainerProperties(VisualElement containerRowsElement, DataContainer container, Action<object> onValueChanged)
        {
            if (containerRowsElement.childCount == 0)
                return;
                
            // Create a new container with the same properties but in the new order
            var newContainer = new DataContainer();
            
            // Add all properties in the new order
            for (int i = 0; i < containerRowsElement.childCount; i++)
            {
                var rowElement = containerRowsElement[i];
                if (rowElement.userData is BaseRowData rowData)
                {
                    string key = rowData.Key;
                    Type type = rowData.Type;
                    
                    // Skip if key doesn't exist (might be our "Add Property" row)
                    if (!container.Contains(key))
                        continue;
                    
                    // Use reflection to get the property value
                    var getMethod = typeof(DataContainer).GetMethod("Get").MakeGenericMethod(type);
                    object defaultValue = CreateDefaultValue(type);
                    object value = getMethod.Invoke(container, new[] { key, defaultValue });
                    
                    // Add to new container
                    var setMethod = typeof(DataContainer).GetMethod("Set").MakeGenericMethod(type);
                    setMethod.Invoke(newContainer, new[] { key, value });
                }
            }
            
            // Replace all properties in the original container
            var originalKeys = container.GetKeys().ToArray();
            
            // Clear existing properties
            foreach (var key in originalKeys)
            {
                container.Remove(key);
            }
            
            // Copy properties from new container to original container
            foreach (var key in newContainer.GetKeys())
            {
                Type type = newContainer.GetValueType(key);
                var getMethod = typeof(DataContainer).GetMethod("Get").MakeGenericMethod(type);
                object defaultValue = CreateDefaultValue(type);
                object value = getMethod.Invoke(newContainer, new[] { key, defaultValue });
                
                var setMethod = typeof(DataContainer).GetMethod("Set").MakeGenericMethod(type);
                setMethod.Invoke(container, new[] { key, value });
            }
            
            // Notify of the change
            onValueChanged(container);
            
            // Mark as dirty and refresh hierarchy
            ApplyStructuralChange();
        }
        
        /// <summary>
        /// Get a preview of a value for display
        /// </summary>
        private string GetValuePreview(DataContainer container, string key, Type type)
        {
            if (container == null)
                return "(null container)";
                
            if (!container.Contains(key))
                return "(key not found)";
            
            try {
                if (type == typeof(DataContainer))
                {
                    if (container.TryGet<DataContainer>(key, out var nestedContainer))
                    {
                        int keyCount = nestedContainer?.GetKeys()?.Count() ?? 0;
                        return $"Container with {keyCount} properties";
                    }
                    return "(invalid container)";
                }
                else if (type == typeof(List<DataContainer>))
                {
                    if (container.TryGet<List<DataContainer>>(key, out var list))
                    {
                        return $"List with {list?.Count ?? 0} items";
                    }
                    return "(invalid list)";
                }
                else if (type == typeof(OrderedDictionary<string, DataContainer>))
                {
                    if (container.TryGet<OrderedDictionary<string, DataContainer>>(key, out var dict))
                    {
                        return $"Dictionary with {dict?.Count ?? 0} items";
                    }
                    return "(invalid ordered dictionary)";
                }
                else if (type == typeof(UnityObjectReference))
                {
                    if (container.TryGet<UnityObjectReference>(key, out var reference))
                    {
                        return reference.ToString();
                    }
                    return "(invalid reference)";
                }
                else if (type == typeof(string))
                {
                    string strValue = container.Get<string>(key, "");
                    if (strValue.Length > 50)
                        return strValue.Substring(0, 47) + "...";
                    return strValue;
                }
                else
                {
                    return container.Get<object>(key)?.ToString() ?? "(null)";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Convert dropdown index to Type
        /// </summary>
        private Type GetTypeFromSelection(int index)
        {
            switch (index)
            {
                case 0: return null; // "Select type..." - no selection
                case 1: return typeof(string); // String
                case 2: return typeof(int); // Int
                case 3: return typeof(float); // Float
                case 4: return typeof(bool); // Bool
                case 5: return typeof(Vector2); // Vector2
                case 6: return typeof(Vector3); // Vector3
                case 7: return typeof(Color); // Color
                case 8: return typeof(DataContainer); // Container
                case 9: return typeof(List<DataContainer>); // List<Container>
                case 10: return typeof(OrderedDictionary<string, DataContainer>); // OrderedDictionary<string, Container>
                case 11: return typeof(UnityObjectReference); // UnityObjectReference
                default: return null;
            }
        }
        
        /// <summary>
        /// Get a readable type name
        /// </summary>
        private string GetTypeName(Type type)
        {
            if (type == null)
                return "(none)";

            if (type == typeof(string))
                return "String";
            if (type == typeof(int))
                return "Integer";
            if (type == typeof(float))
                return "Float";
            if (type == typeof(bool))
                return "Boolean";
            if (type == typeof(Vector2))
                return "Vector2";
            if (type == typeof(Vector3))
                return "Vector3";
            if (type == typeof(Color))
                return "Color";
            if (type == typeof(DataContainer))
                return "Container";
            if (type == typeof(List<DataContainer>))
                return "Container List";
            if (type == typeof(OrderedDictionary<string, DataContainer>))
                return "OrderedDictionary<String, Container>";
            if (type == typeof(UnityObjectReference))
                return "Unity Object Reference";
            
            // For other types, use type name
            return type.Name;
        }
        
        /// <summary>
        /// Create a default value for a type
        /// </summary>
        private object CreateDefaultValue(Type type)
        {
            if (type == null)
                return null;
                
            if (type == typeof(string))
                return string.Empty;
                
            if (type == typeof(UnityObjectReference))
            {
                // Create a properly initialized UnityObjectReference with default values
                return new UnityObjectReference(
                    ReferenceStorageType.Registry,  // Default to Registry storage
                    "Default",                      // Default key
                    typeof(UnityEngine.GameObject).AssemblyQualifiedName  // Default to GameObject type
                );
            }
                
            if (type == typeof(DataContainer))
            {
                // Create a new, empty DataContainer
                return new DataContainer();
            }
            
            if (type == typeof(OrderedDictionary<string, DataContainer>))
            {
                // Create a new empty OrderedDictionary for string to DataContainer mapping
                return new OrderedDictionary<string, DataContainer>();
            }
                
            if (type.IsValueType)
                return Activator.CreateInstance(type);
                
            if (type.IsArray)
                return Array.CreateInstance(type.GetElementType(), 0);
                
            if (typeof(System.Collections.IList).IsAssignableFrom(type) && type.IsGenericType)
            {
                Type listType = typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]);
                return Activator.CreateInstance(listType);
            }
            
            if (typeof(System.Collections.IDictionary).IsAssignableFrom(type) && type.IsGenericType)
            {
                Type dictType = typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments()[0], type.GetGenericArguments()[1]);
                return Activator.CreateInstance(dictType);
            }
            
            return null;
        }
    }
} 