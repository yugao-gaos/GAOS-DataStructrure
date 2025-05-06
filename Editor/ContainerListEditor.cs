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
    /// Editor for handling List<DataContainer> types
    /// </summary>
    public class ContainerListEditor : ContainerEditorBase
    {
        public ContainerListEditor(DataStructureEditorWindow editorWindow) : base(editorWindow) { }

        /// <summary>
        /// Checks if this editor can handle the specified type
        /// </summary>
        public override bool CanHandleType(Type type)
        {
            return type == typeof(List<DataContainer>);
        }

        /// <summary>
        /// Creates an editor field for List<DataContainer> type
        /// </summary>
        public override VisualElement CreateEditorField(Type type, object value, Action<object> onValueChanged)
        {
            if (type != typeof(List<DataContainer>))
            {
                Debug.LogError($"ContainerListEditor cannot handle type {type.Name}");
                return new Label($"Unsupported type: {type.Name}");
            }

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            
            List<DataContainer> list = value as List<DataContainer>;
            
            if (list == null)
            {
                container.Add(new Label("List is null"));
                return container;
            }
            
            // Create the list editor UI
            CreateListContainerEditor(container, list, onValueChanged);
            
            return container;
        }
        
        /// <summary>
        /// Creates a List<DataContainer> editor UI
        /// </summary>
        private void CreateListContainerEditor(VisualElement container, List<DataContainer> list, Action<object> onValueChanged)
        {
            // Create table header using base class method
            var tableHeader = CreateTableHeader(true, 
                ("Index", 40, false),
                ("Value", 0, true), 
                ("Actions", 60, false));
            
            container.Add(tableHeader);
            
            // Create list items container
            var itemsContainer = new VisualElement();
            itemsContainer.AddToClassList("list-items-container");
            itemsContainer.style.marginLeft = 4;
            container.Add(itemsContainer);
            
            // Add items
            RefreshListItems(itemsContainer, list, onValueChanged);
            
            // Add button to add new item
            var addItemButton = new Button(() => {
                // Create a new container
                var newContainer = new DataContainer();
                list.Add(newContainer);
                
                // Update the list
                onValueChanged(list);
                
                // Refresh the UI
                RefreshListItems(itemsContainer, list, onValueChanged);
                
                // Mark the asset dirty and refresh hierarchy
                ApplyStructuralChange();
            });
            addItemButton.text = "Add Item";
            addItemButton.AddToClassList("add-item-button");
            container.Add(addItemButton);
        }
        
        /// <summary>
        /// Refreshes the list items UI
        /// </summary>
        private void RefreshListItems(VisualElement itemsContainer, List<DataContainer> list, Action<object> onValueChanged)
        {
            itemsContainer.Clear();
            
            for (int i = 0; i < list.Count; i++)
            {
                var row = CreateListItemRow(i, list[i], list, onValueChanged);
                itemsContainer.Add(row);
            }
        }
        
        /// <summary>
        /// Creates a row for a list item
        /// </summary>
        private VisualElement CreateListItemRow(int index, DataContainer container, List<DataContainer> list, Action<object> onValueChanged)
        {
            // Create standard row using base class method
            var row = CreateStandardRow();
            row.userData = new BaseRowData { Index = index, Container = container };
            
            // Add drag handle using base class method
            var dragHandle = CreateDragHandle($"ListItem-{index}");
            
            // Make the drag handle draggable using base class method
            EnableDragForHandle(dragHandle, row, (r, state) => {
                ReorderListItems(state.ParentContainer, list, onValueChanged);
            });
            
            row.Add(dragHandle);
            
            // Index label
            var indexLabel = new Label($"{index}");
            indexLabel.style.width = 40;
            indexLabel.style.flexShrink = 0;
            row.Add(indexLabel);
            
            // Container preview
            var preview = new Label(GetCollectionPreview(container, container.GetKeys().Count(), "properties"));
            preview.style.flexGrow = 1;
            row.Add(preview);
            
            // Actions container using base class method
            var actionsContainer = CreateActionsContainer(60);
            
            // Add buttons using base class method
            actionsContainer.Add(CreateIconButton("✏️", "Edit item", () => {
                // Edit the item when edit button is clicked
                if (index >= 0 && index < list.Count && _editorWindow != null)
                {
                    // Get the parent container path
                    string path = _editorWindow.CurrentPath;
                    if (!string.IsNullOrEmpty(path))
                    {
                        // Generate full path using DataContainer's list item path helper
                        string itemPath = DataContainer.CombineListItemPath(path, index);
                        Debug.Log($"ContainerListEditor: Selecting list item at full path: {itemPath}");
                        _editorWindow.SelectPropertyInternal(itemPath, $"Item {index}", typeof(DataContainer));
                    }
                }
            }));
            
            actionsContainer.Add(CreateIconButton("🗑️", "Delete item", () => {
                // Remove the item when delete button is clicked
                if (index >= 0 && index < list.Count)
                {
                    // Get the parent container of rows before removal
                    var parentElement = row.parent;
                    
                    // Remove the item from the list
                    list.RemoveAt(index);
                    onValueChanged(list);
                    
                    // Refresh the UI
                    if (parentElement != null)
                    {
                        RefreshListItems(parentElement, list, onValueChanged);
                    }
                    
                    // Mark as dirty and refresh hierarchy
                    ApplyStructuralChange();
                }
            }, marginRight: 0));
            
            row.Add(actionsContainer);
            
            return row;
        }
        
        /// <summary>
        /// Reorder items in a list based on visual order
        /// </summary>
        private void ReorderListItems(VisualElement containerElement, List<DataContainer> list, Action<object> onValueChanged)
        {
            // Create a temporary list to store the reordered items
            var newList = new List<DataContainer>(list.Count);
            
            // Initialize with nulls to preserve the correct size
            for (int i = 0; i < list.Count; i++)
            {
                newList.Add(null);
            }
            
            // Map of original indices to new indices
            Dictionary<int, int> reorderMap = new Dictionary<int, int>();
            
            // Figure out the new ordering
            for (int i = 0; i < containerElement.childCount; i++)
            {
                var child = containerElement[i];
                if (child.userData is BaseRowData rowData)
                {
                    int oldIndex = rowData.Index;
                    
                    if (oldIndex >= 0 && oldIndex < list.Count)
                    {
                        // Map the old index to the new index
                        reorderMap[oldIndex] = i;
                        
                        // Place the item in its new position if within bounds
                        if (i < newList.Count)
                        {
                            newList[i] = list[oldIndex];
                        }
                    }
                }
            }
            
            // Fill in any missing items (that weren't reordered)
            for (int i = 0; i < newList.Count; i++)
            {
                if (newList[i] == null)
                {
                    // Find an unused item from the original list
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (!reorderMap.ContainsKey(j))
                        {
                            newList[i] = list[j];
                            reorderMap[j] = i; // Mark as used
                            break;
                        }
                    }
                }
            }
            
            // Update the original list with the new order
            list.Clear();
            list.AddRange(newList.Where(item => item != null));
            
            // Notify listeners
            onValueChanged(list);
            
            // Refresh the UI
            RefreshListItems(containerElement, list, onValueChanged);
            
            // Mark as dirty and refresh hierarchy
            ApplyStructuralChange();
        }
    }
} 