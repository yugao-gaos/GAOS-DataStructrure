using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GAOS.DataStructure;
using GAOS.DataStructure.References;
using GAOS.Logger;

namespace GAOS.DataStructure.Editor
{
    /// <summary>
    /// Class responsible for building and managing the hierarchy tree view
    /// </summary>
    public class HierarchyTreeBuilder
    {
        private DataStructure _dataStructure;
        private DataStructureEditorWindow _editorWindow;
        private VisualElement _treeViewContainer;
        private Dictionary<string, VisualElement> _treeItems = new Dictionary<string, VisualElement>();
        private string _highlightedPath;
        
        // Add a HashSet to track expanded items
        private HashSet<string> _expandedItems = new HashSet<string>();
        
        /// <summary>
        /// Gets all tree items in the hierarchy
        /// </summary>
        public Dictionary<string, VisualElement> TreeItems => _treeItems;
        
        public HierarchyTreeBuilder(DataStructureEditorWindow editorWindow, DataStructure dataStructure, VisualElement treeViewContainer)
        {
            _editorWindow = editorWindow;
            _dataStructure = dataStructure;
            _treeViewContainer = treeViewContainer;
        }
        
        /// <summary>
        /// Refreshes the hierarchy tree view
        /// </summary>
        public void RefreshHierarchyTree()
        {
            // Store expanded states before refreshing
            SaveExpandedStates();
            
            _treeViewContainer.Clear();
            _treeItems.Clear();

            if (_dataStructure == null)
                return;

            // Create root container
            var rootContainer = new VisualElement();
            rootContainer.AddToClassList("data-container-root");
            _treeViewContainer.Add(rootContainer);

            // Create custom tree item for root
            var rootItem = CreateCustomTreeItem(rootContainer, "Root", "", typeof(DataContainer), _dataStructure.Container, true);
            
            // Build tree (will recursively build all children)
            BuildContainerTree(_dataStructure.Container, rootItem.Q<VisualElement>("content"), "");
            
            // Restore expanded states
            RestoreExpandedStates();
        }
        
        /// <summary>
        /// Saves the expanded state of all tree items
        /// </summary>
        private void SaveExpandedStates()
        {
            _expandedItems.Clear();
            
            foreach (var kvp in _treeItems)
            {
                string path = kvp.Key;
                var item = kvp.Value;
                
                var contentElement = item.Q<VisualElement>("content");
                var toggleLabel = item.Q<Label>("toggle");
                
                if (contentElement != null && toggleLabel != null)
                {
                    // If content is visible, the item is expanded
                    if (contentElement.style.display == DisplayStyle.Flex)
                    {
                        _expandedItems.Add(path);
                        GLog.Info<DataSystemEditorLogger>($"Saving expanded state for: {path}");
                    }
                }
            }
            
            GLog.Info<DataSystemEditorLogger>($"Saved {_expandedItems.Count} expanded items");
        }
        
        /// <summary>
        /// Restores the expanded state of all tree items
        /// </summary>
        private void RestoreExpandedStates()
        {
            // Always ensure the root is expanded
            if (_treeItems.ContainsKey(""))
            {
                SetItemExpanded(_treeItems[""], true);
            }
            
            foreach (var path in _expandedItems)
            {
                if (_treeItems.ContainsKey(path))
                {
                    SetItemExpanded(_treeItems[path], true);
                    GLog.Info<DataSystemEditorLogger>($"Restored expanded state for: {path}");
                }
            }
            
            GLog.Info<DataSystemEditorLogger>($"Restored expanded states for {_expandedItems.Count} items");
        }
        
        /// <summary>
        /// Creates a custom tree item with a toggle button instead of a Foldout
        /// </summary>
        private VisualElement CreateCustomTreeItem(VisualElement parent, string label, string path, Type type, object value, bool isRoot = false)
        {
            // Create container for the tree item
            var itemContainer = new VisualElement();
            itemContainer.AddToClassList("tree-item-container");
            parent.Add(itemContainer);
            
            // Create row for item
            var row = new VisualElement();
            row.AddToClassList("data-entry-row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.overflow = Overflow.Hidden;
            itemContainer.Add(row);
            
            // Create toggle button
            var toggleButton = new Label(isRoot ? "▼" : "►"); // Down for expanded, right for collapsed
            toggleButton.name = "toggle";
            toggleButton.style.width = 20;
            row.Add(toggleButton);
            
            // Create label
            var itemLabel = new Label(label);
            itemLabel.style.overflow = Overflow.Hidden;
            itemLabel.style.textOverflow = TextOverflow.Ellipsis;
            itemLabel.style.flexGrow = 1;
            row.Add(itemLabel);
            
            // Create content container for children
            var content = new VisualElement();
            content.name = "content";
            content.style.marginLeft = 20;
            content.style.display = isRoot ? DisplayStyle.Flex : DisplayStyle.None; // Only show root by default
            itemContainer.Add(content);
            
            // Add toggle functionality
            toggleButton.RegisterCallback<ClickEvent>(evt => {
                bool isExpanded = content.style.display == DisplayStyle.Flex;
                content.style.display = isExpanded ? DisplayStyle.None : DisplayStyle.Flex;
                toggleButton.text = isExpanded ? "►" : "▼";
                
                // Save expanded state when toggling
                if (isExpanded)
                {
                    _expandedItems.Remove(path);
                }
                else
                {
                    _expandedItems.Add(path);
                }
                
                evt.StopPropagation();
            });
            
            // Add selection functionality
            itemLabel.RegisterCallback<ClickEvent>(evt => {
                // Make sure we pass the full path - it should be properly formed already
                // In tree building we're using proper DataContainer.CombineListItemPath and CombineDictionaryItemPath
                
                // If the path contains brackets, ensure they are processed correctly
                if (path.Contains("["))
                {
                    GLog.Info<DataSystemEditorLogger>($"Tree item clicked with path: {path}");
                    
                    // Get the last part of the path for display in the property details
                    string displayKey = string.IsNullOrEmpty(path) ? "Root" : DataContainer.GetPathKey(path);
                    
                    // Just use the full path that was passed to us during tree construction
                    _editorWindow.SelectProperty(path, displayKey, type);
                }
                else
                {
                    // Normal behavior for non-bracketed paths
                    _editorWindow.SelectProperty(path, string.IsNullOrEmpty(path) ? "Root" : DataContainer.GetPathKey(path), type);
                }
                
                HighlightItem(path);
                evt.StopPropagation();
            });
            
            // Store reference to tree item
            _treeItems[path] = itemContainer;
            return itemContainer;
        }
        
        /// <summary>
        /// Recursively builds the container tree
        /// </summary>
        private void BuildContainerTree(DataContainer container, VisualElement parent, string basePath)
        {
            if (container == null)
                return;

            // Get all keys and reverse the order for container properties
            // This will make the hierarchy display match the ContainerEditor display
            var keys = container.GetKeys().Reverse();

            foreach (var key in keys)
            {
                string path = string.IsNullOrEmpty(basePath) ? key : DataContainer.CombinePath(basePath, key);
                Type valueType = container.GetValueType(key);
                
                // Skip null values
                if (valueType == null)
                    continue;
                
                // Different handling based on type
                if (valueType == typeof(DataContainer))
                {
                    // Nested container
                    var nestedContainer = container.Get<DataContainer>(key);
                    
                    // Use custom tree item for container
                    CreateCustomTreeItem(parent, $"{key} (Container)", path, valueType, nestedContainer);
                    
                    // Build nested container tree
                    BuildContainerTree(nestedContainer, _treeItems[path].Q<VisualElement>("content"), path);
                }
                else if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    if (valueType.GetGenericArguments()[0] == typeof(DataContainer))
                    {
                        // List of containers
                        var list = container.Get<List<DataContainer>>(key);
                        
                        // Use custom tree item for list container
                        CreateCustomTreeItem(parent, $"{key} (List<Container>) [{list.Count} items]", path, valueType, null);
                        
                        var listContent = _treeItems[path].Q<VisualElement>("content");

                        // Add items
                        for (int i = 0; i < list.Count; i++)
                        {
                            // Use proper list path notation with square brackets
                            string listItemPath = DataContainer.CombineListItemPath(path, i);
                            var listItem = list[i];
                            
                            // Use custom tree item for list items
                            CreateCustomTreeItem(listContent, $"Item {i}", listItemPath, typeof(DataContainer), listItem);
                            
                            // Build tree for the list item container
                            BuildContainerTree(listItem, _treeItems[listItemPath].Q<VisualElement>("content"), listItemPath);
                        }
                    }
                    else
                    {
                        // Simple list (non-container elements)
                        CreateBasicTypeItem(parent, key, path, valueType);
                    }
                }
                else if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(OrderedDictionary<,>))
                {
                    if (valueType.GetGenericArguments()[0] == typeof(string) && 
                        valueType.GetGenericArguments()[1] == typeof(DataContainer))
                    {
                        // OrderedDictionary of containers
                        var dict = container.Get<OrderedDictionary<string, DataContainer>>(key);
                        
                        // Use custom tree item for OrderedDictionary container
                        CreateCustomTreeItem(parent, $"{key} (Dictionary<string, Container>) [{dict.Count} items]", path, valueType, null);
                        
                        var dictContent = _treeItems[path].Q<VisualElement>("content");
                        
                        // Add items
                        foreach (var kvp in dict)
                        {
                            // Use proper dictionary path notation with square brackets and quotes
                            string dictItemPath = DataContainer.CombineDictionaryItemPath(path, kvp.Key);
                            var dictItemContainer = kvp.Value;
                            
                            // Use custom tree item for dictionary items
                            CreateCustomTreeItem(dictContent, $"Key: \"{kvp.Key}\"", dictItemPath, typeof(DataContainer), dictItemContainer);
                            
                            // Build tree for the dictionary item container
                            BuildContainerTree(dictItemContainer, _treeItems[dictItemPath].Q<VisualElement>("content"), dictItemPath);
                        }
                    }
                    else
                    {
                        // Simple OrderedDictionary (non-container values)
                        CreateBasicTypeItem(parent, key, path, valueType);
                    }
                }
                else
                {
                    // Simple value
                    CreateBasicTypeItem(parent, key, path, valueType);
                }
            }
        }
        
        /// <summary>
        /// Creates a basic tree item for a simple value type
        /// </summary>
        private VisualElement CreateBasicTypeItem(VisualElement parent, string key, string path, Type valueType)
        {
            var treeItem = new VisualElement();
            treeItem.AddToClassList("data-entry-row");
            treeItem.AddToClassList("tree-item-container");
            treeItem.style.width = Length.Percent(100);
            treeItem.style.overflow = Overflow.Hidden;
            parent.Add(treeItem);
            
            var label = new Label($"{key} ({GetTypeName(valueType)})");
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            treeItem.Add(label);

            // Click handler for simple value
            treeItem.RegisterCallback<ClickEvent>(evt => {
                // Be consistent with the CreateCustomTreeItem click handler
                if (path.Contains("["))
                {
                    GLog.Info<DataSystemEditorLogger>($"Basic tree item clicked with path: {path}");
                    
                    // Get the last part of the path for display in the property details
                    string displayKey = string.IsNullOrEmpty(path) ? "Root" : DataContainer.GetPathKey(path); 
                    
                    // Use the full path from tree construction
                    _editorWindow.SelectProperty(path, displayKey, valueType);
                }
                else
                {
                    // Normal behavior for non-bracketed paths
                    _editorWindow.SelectProperty(path, key, valueType);
                }
                
                evt.StopPropagation();
            });
            
            _treeItems.Add(path, treeItem);
            return treeItem;
        }
        
        /// <summary>
        /// Highlights a specific item in the tree
        /// </summary>
        public void HighlightItem(string path)
        {
            // Special case for bracket-only paths like [0] or ["key"]
            if (!string.IsNullOrEmpty(path) && path.StartsWith("[") && !_treeItems.ContainsKey(path))
            {
                // Try to find the proper path by combining with the current path
                string currentPath = _editorWindow.CurrentPath;
                if (!string.IsNullOrEmpty(currentPath) && !currentPath.EndsWith("]"))
                {
                    string fullPath = currentPath + path;
                    GLog.Info<DataSystemEditorLogger>($"HighlightItem: Converted bracket-only path '{path}' to full path '{fullPath}'");
                    
                    // Update the path for highlighting if it exists in the tree
                    if (_treeItems.ContainsKey(fullPath))
                    {
                        path = fullPath;
                    }
                }
            }
        
            // Clear previous highlight
            if (!string.IsNullOrEmpty(_highlightedPath) && _treeItems.ContainsKey(_highlightedPath))
            {
                var prevItem = _treeItems[_highlightedPath];
                var prevRow = prevItem.Q(className: "data-entry-row");
                if (prevRow != null)
                    prevRow.style.backgroundColor = Color.clear;
            }
            
            // Set new highlight
            if (!string.IsNullOrEmpty(path) && _treeItems.ContainsKey(path))
            {
                var newItem = _treeItems[path];
                var newRow = newItem.Q(className: "data-entry-row");
                if (newRow != null)
                    newRow.style.backgroundColor = new Color(0.1f, 0.4f, 0.7f, 0.5f);
                _highlightedPath = path;
            }
            else
            {
                _highlightedPath = null;
                GLog.Warning<DataSystemEditorLogger>($"HighlightItem: Could not find path '{path}' in tree items");
            }
        }
        
        /// <summary>
        /// Expands all parents in the path to make an item visible
        /// </summary>
        public void ExpandToPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            
            GLog.Info<DataSystemEditorLogger>($"ExpandToPath: Expanding to path: {path}");
            
            // Special case: if the path is just a list index or dictionary key without a parent path
            if (path.StartsWith("["))
            {
                // The path is just an index or key without a parent path
                // This happens when selecting list items directly
                // We need to find the correct container in the tree first
                
                string currentPath = _editorWindow.CurrentPath;
                if (!string.IsNullOrEmpty(currentPath))
                {
                    // Current path is the parent path, combine with the index/key
                    string fullPath = currentPath + path;
                    GLog.Info<DataSystemEditorLogger>($"ExpandToPath: Converted bracket-only path to full path: {fullPath}");
                    
                    // Now expand using the full path
                    ExpandToPath(fullPath);
                    return;
                }
                else
                {
                    GLog.Warning<DataSystemEditorLogger>($"ExpandToPath: Cannot expand path starting with brackets without parent path: {path}");
                    return;
                }
            }
            
            // For paths with list or dictionary notation with brackets (e.g., "list[0]" or "dict["key"]")
            if (path.Contains("["))
            {
                // Get the parent path before any brackets
                int bracketIndex = path.IndexOf('[');
                string parentPath = path.Substring(0, bracketIndex);
                
                GLog.Info<DataSystemEditorLogger>($"ExpandToPath: For bracketed path, first expand parent: {parentPath}");
                
                // First, expand the parent container if it's a nested path
                if (parentPath.Contains("."))
                {
                    // Handle dot notation in parent path
                    var parts = parentPath.Split('.');
                    string currentPath = "";
                    
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (i > 0)
                            currentPath += ".";
                        
                        currentPath += parts[i];
                        
                        if (_treeItems.ContainsKey(currentPath))
                        {
                            var item = _treeItems[currentPath];
                            SetItemExpanded(item, true);
                            _expandedItems.Add(currentPath);
                            GLog.Info<DataSystemEditorLogger>($"ExpandToPath: Expanded parent item: {currentPath}");
                        }
                    }
                }
                
                // Now expand the container that holds the list/dictionary if needed
                if (!string.IsNullOrEmpty(parentPath) && _treeItems.ContainsKey(parentPath))
                {
                    var item = _treeItems[parentPath];
                    SetItemExpanded(item, true);
                    _expandedItems.Add(parentPath);
                    GLog.Info<DataSystemEditorLogger>($"ExpandToPath: Expanded container item: {parentPath}");
                }
                
                // Finally, if the full path exists in tree items, expand it too if it's a container
                if (_treeItems.ContainsKey(path))
                {
                    var item = _treeItems[path];
                    // Only expand if it has child content
                    var content = item.Q<VisualElement>("content");
                    if (content != null && content.childCount > 0)
                    {
                        SetItemExpanded(item, true);
                        _expandedItems.Add(path);
                        GLog.Info<DataSystemEditorLogger>($"ExpandToPath: Expanded full path item: {path}");
                    }
                }
                else
                {
                    GLog.Warning<DataSystemEditorLogger>($"ExpandToPath: Full path not found in tree items: {path}");
                }
            }
            else
            {
                // Traditional dot notation path
                var parts = path.Split('.');
                string currentPath = "";
                
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i > 0)
                        currentPath += ".";
                    
                    currentPath += parts[i];
                    
                    if (_treeItems.ContainsKey(currentPath))
                    {
                        var item = _treeItems[currentPath];
                        SetItemExpanded(item, true);
                        // Add to expanded items set
                        _expandedItems.Add(currentPath);
                        GLog.Info<DataSystemEditorLogger>($"ExpandToPath: Expanded item: {currentPath}");
                    }
                    else
                    {
                        GLog.Warning<DataSystemEditorLogger>($"ExpandToPath: Path not found in tree items: {currentPath}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Sets the expanded state of a tree item
        /// </summary>
        private void SetItemExpanded(VisualElement item, bool expanded)
        {
            var contentElement = item.Q<VisualElement>("content");
            var toggleLabel = item.Q<Label>("toggle");
            
            if (contentElement != null && toggleLabel != null)
            {
                contentElement.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
                toggleLabel.text = expanded ? "▼" : "►"; // Down arrow for expanded, right arrow for collapsed
                
                // Update expanded items set
                // Find the path for this item by looking up in the _treeItems dictionary
                string itemPath = null;
                foreach (var kvp in _treeItems)
                {
                    if (kvp.Value == item)
                    {
                        itemPath = kvp.Key;
                        break;
                    }
                }
                
                if (itemPath != null)
                {
                    if (expanded)
                    {
                        _expandedItems.Add(itemPath);
                    }
                    else
                    {
                        _expandedItems.Remove(itemPath);
                    }
                }
            }
        }
        
        #region Utility Methods
        
        /// <summary>
        /// Gets a readable type name
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
            
            return type.Name;
        }
        
        /// <summary>
        /// Gets the last component of a path
        /// </summary>
        private string GetLastPathComponent(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "Root";
                
            int lastDotIndex = path.LastIndexOf('.');
            return lastDotIndex >= 0 ? path.Substring(lastDotIndex + 1) : path;
        }
        
        #endregion
    }
} 