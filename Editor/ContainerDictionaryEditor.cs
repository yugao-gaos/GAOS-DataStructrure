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
    /// <summary>
    /// Editor for handling OrderedDictionary<string, DataContainer> types
    /// </summary>
    public class ContainerDictionaryEditor : ContainerEditorBase
    {
        public ContainerDictionaryEditor(DataStructureEditorWindow editorWindow) : base(editorWindow) { }

        /// <summary>
        /// Checks if this editor can handle the specified type
        /// </summary>
        public override bool CanHandleType(Type type)
        {
            return type == typeof(OrderedDictionary<string, DataContainer>);
        }

        /// <summary>
        /// Creates an editor field for OrderedDictionary<string, DataContainer> type
        /// </summary>
        public override VisualElement CreateEditorField(Type type, object value, Action<object> onValueChanged)
        {
            if (type != typeof(OrderedDictionary<string, DataContainer>))
            {
                Debug.LogError($"ContainerDictionaryEditor cannot handle type {type.Name}");
                return new Label($"Unsupported type: {type.Name}");
            }

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            
            OrderedDictionary<string, DataContainer> dictionary = value as OrderedDictionary<string, DataContainer>;
            
            if (dictionary == null)
            {
                container.Add(new Label("Dictionary is null"));
                return container;
            }
            
            // Create the dictionary editor UI
            CreateDictionaryEditor(container, dictionary, onValueChanged);
            
            return container;
        }
        
        /// <summary>
        /// Creates a OrderedDictionary<string, DataContainer> editor UI
        /// </summary>
        private void CreateDictionaryEditor(VisualElement container, OrderedDictionary<string, DataContainer> dictionary, Action<object> onValueChanged)
        {
            // DEBUG: Log the current path we're editing
            string currentPath = _editorWindow?.CurrentPath;
            Debug.Log($"Creating dictionary editor for path: {currentPath}, Dictionary items: {dictionary.Count}");
            
            // Create table header using base class method
            var tableHeader = CreateTableHeader(true, 
                ("Key", 150, false),
                ("Value", 0, true),
                ("Actions", 80, false));
                
            container.Add(tableHeader);
            
            // Create dictionary items container
            var itemsContainer = new VisualElement();
            itemsContainer.AddToClassList("dictionary-items-container");
            itemsContainer.style.marginLeft = 4;
            container.Add(itemsContainer);
            
            // Add items
            RefreshDictionaryItems(itemsContainer, dictionary, onValueChanged);
            
            // Add new entry section
            var addSection = new VisualElement();
            addSection.style.flexDirection = FlexDirection.Row;
            addSection.style.marginTop = 8;
            container.Add(addSection);
            
            var newKeyField = new TextField();
            newKeyField.style.flexGrow = 1;
            newKeyField.style.marginRight = 4;
            addSection.Add(newKeyField);
            
            var addButton = new Button(() => AddDictionaryItem(dictionary, newKeyField, onValueChanged));
            addButton.text = "Add Item";
            addButton.AddToClassList("add-item-button");
            addSection.Add(addButton);
        }
        
        /// <summary>
        /// Refreshes the dictionary items UI
        /// </summary>
        private void RefreshDictionaryItems(VisualElement itemsContainer, OrderedDictionary<string, DataContainer> dictionary, Action<object> onValueChanged)
        {
            itemsContainer.Clear();
            
            int index = 0;
            foreach (var kvp in dictionary)
            {
                string key = kvp.Key;
                DataContainer container = kvp.Value;
                
                // Create row
                var row = CreateDictionaryRow(key, container, dictionary, onValueChanged, index);
                itemsContainer.Add(row);
                index++;
            }
        }
        
        /// <summary>
        /// Creates a dictionary item row
        /// </summary>
        private VisualElement CreateDictionaryRow(string key, DataContainer container, OrderedDictionary<string, DataContainer> dictionary, 
                                              Action<object> onValueChanged, int index)
        {
            // Create standard row using base class method
            var row = CreateStandardRow();
            row.userData = new BaseRowData { Key = key, Container = container, Index = index };
            
            // Add drag handle using base class method
            var dragHandle = CreateDragHandle($"Dict-{key}");
            
            // Make the drag handle draggable using base class method
            EnableDragForHandle(dragHandle, row, (r, state) => {
                ReorderDictionaryItems(state.ParentContainer, dictionary, onValueChanged);
            });
            
            row.Add(dragHandle);
            
            // Key label
            var keyLabel = new Label(key);
            keyLabel.style.width = 150;
            keyLabel.style.flexShrink = 0;
            keyLabel.style.overflow = Overflow.Hidden;
            keyLabel.style.textOverflow = TextOverflow.Ellipsis;
            keyLabel.tooltip = key; // Show full key on hover
            row.Add(keyLabel);
            
            // Container preview
            var preview = new Label(GetCollectionPreview(container, container.GetKeys().Count(), "properties"));
            preview.style.flexGrow = 1;
            row.Add(preview);
            
            // Actions container
            var actionsContainer = CreateActionsContainer();
            
            // Add buttons
            actionsContainer.Add(CreateIconButton("✏️", "Edit item", () => {
                if (_editorWindow != null)
                {
                    string path = _editorWindow.CurrentPath;
                    if (!string.IsNullOrEmpty(path))
                    {
                        // Log the current path when editing an item for debugging
                        Debug.Log($"Editing dictionary item: Current Path='{path}', Key='{key}'");
                        
                        // Use the proper DataContainer utility method for dictionary paths
                        string itemPath = DataContainer.CombineDictionaryItemPath(path, key);
                        
                        Debug.Log($"ContainerDictionaryEditor: Selecting dictionary item at full path: {itemPath}");
                        _editorWindow.SelectPropertyInternal(itemPath, key, typeof(DataContainer));
                    }
                }
            }));
            
            actionsContainer.Add(CreateIconButton("🔄", "Rename key", () => {
                RenameDictionaryKey(key, dictionary, onValueChanged);
            }));
            
            actionsContainer.Add(CreateIconButton("🗑️", "Delete item", () => {
                RemoveDictionaryItem(key, dictionary, onValueChanged);
            }, marginRight: 0));
            
            row.Add(actionsContainer);
            
            return row;
        }
        
        /// <summary>
        /// Reorders dictionary items based on visual order
        /// </summary>
        private void ReorderDictionaryItems(VisualElement containerElement, OrderedDictionary<string, DataContainer> dictionary, Action<object> onValueChanged)
        {
            // Create a new ordered dictionary with the new order
            var newDict = new OrderedDictionary<string, DataContainer>();
            
            // First, sort the children by their visual position (Y coordinate)
            for (int i = 0; i < containerElement.childCount; i++)
            {
                var child = containerElement[i];
                if (child.userData is BaseRowData rowData)
                {
                    string key = rowData.Key;
                    
                    // Only add keys that exist in the original dictionary
                    if (dictionary.ContainsKey(key))
                    {
                        newDict[key] = dictionary[key];
                    }
                }
            }
            
            // Clear the old dict and add items in the new order
            dictionary.Clear();
            foreach (var kvp in newDict)
            {
                dictionary[kvp.Key] = kvp.Value;
            }
            
            // Notify of the change
            onValueChanged(dictionary);
            
            // Refresh the UI (re-create the rows)
            RefreshDictionaryItems(containerElement, dictionary, onValueChanged);
            
            // Mark as dirty and refresh hierarchy
            ApplyStructuralChange();
        }
        
        /// <summary>
        /// Adds a new item to the dictionary
        /// </summary>
        private void AddDictionaryItem(OrderedDictionary<string, DataContainer> dictionary, TextField keyField, Action<object> onValueChanged)
        {
            // Debug the current path to see where we're adding items
            string currentPath = _editorWindow?.CurrentPath;
            Debug.Log($"Adding dictionary item to path: '{currentPath}', Dictionary items: {dictionary.Count}");
            
            string key = keyField.value;
            
            // Validate key
            if (string.IsNullOrEmpty(key))
            {
                EditorUtility.DisplayDialog("Invalid Key", "Dictionary key cannot be empty.", "OK");
                return;
            }
            
            // Check if key already exists
            if (dictionary.ContainsKey(key))
            {
                EditorUtility.DisplayDialog("Duplicate Key", $"A dictionary key '{key}' already exists.", "OK");
                return;
            }
            
            // Create a new empty DataContainer
            var newContainer = new DataContainer();
            
            // Add to the dictionary
            dictionary[key] = newContainer;
            
            // Notify of the change
            onValueChanged(dictionary);
            
            // Clear the key field
            keyField.value = "";
            
            // Mark asset dirty and refresh hierarchy
            ApplyStructuralChange();
        }
        
        /// <summary>
        /// Removes an item from the dictionary
        /// </summary>
        public void RemoveDictionaryItem(string key, OrderedDictionary<string, DataContainer> dictionary, Action<object> onValueChanged)
        {
            if (!dictionary.ContainsKey(key))
                return;
            
            // Confirm before deleting
            bool confirmDelete = EditorUtility.DisplayDialog(
                "Confirm Delete",
                $"Are you sure you want to delete the dictionary item '{key}'?",
                "Delete",
                "Cancel"
            );
            
            if (!confirmDelete)
                return;
                
            // Remove item
            dictionary.Remove(key);
            
            // Notify of the change
            onValueChanged(dictionary);
            
            // Mark asset dirty and refresh hierarchy
            ApplyStructuralChange();
        }
        
        /// <summary>
        /// Renames a dictionary key
        /// </summary>
        public void RenameDictionaryKey(string oldKey, OrderedDictionary<string, DataContainer> dictionary, Action<object> onValueChanged)
        {
            if (!dictionary.ContainsKey(oldKey))
                return;
                
            // Use the async dialog to get the new key name
            EditorInputDialog.Show("Rename Key", $"Enter new name for '{oldKey}':", oldKey, newKey => {
                // Dialog completed - handle the result asynchronously
                
                // Check if dialog was cancelled or empty input
                if (string.IsNullOrEmpty(newKey) || newKey == oldKey)
                    return;
                    
                // Check if key already exists
                if (dictionary.ContainsKey(newKey))
                {
                    EditorUtility.DisplayDialog("Error", $"A dictionary key '{newKey}' already exists.", "OK");
                    return;
                }
                
                // Get the current container value
                var dictValue = dictionary[oldKey];
                
                // Create a new dictionary to maintain order and replace the renamed key
                var newDict = new OrderedDictionary<string, DataContainer>();
                
                // Copy all keys, replacing oldKey with newKey
                foreach (var kvp in dictionary)
                {
                    if (kvp.Key == oldKey)
                    {
                        newDict[newKey] = kvp.Value;
                    }
                    else
                    {
                        newDict[kvp.Key] = kvp.Value;
                    }
                }
                
                // Replace the old dictionary with the new one
                dictionary.Clear();
                foreach (var kvp in newDict)
                {
                    dictionary[kvp.Key] = kvp.Value;
                }
                
                // Notify of the change
                onValueChanged(dictionary);
                
                // Mark asset dirty and refresh hierarchy
                ApplyStructuralChange();
            });
        }
    }
} 