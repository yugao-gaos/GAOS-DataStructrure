using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using GAOS.DataStructure;
using GAOS.DataStructure.References;

namespace GAOS.DataStructure.Editor
{
    [CustomEditor(typeof(DataInstance))]
    public class DataInstanceInspector : UnityEditor.Editor
    {
        // UI elements
        private VisualElement _rootElement;
        private VisualElement _treeViewContainer;
        private VisualElement _detailsContent;
        private VisualElement _splitContainer;
        private Label _pathLabel;
        private Label _overrideIndicator;
        
        // Data
        private DataInstance _dataInstance;
        private string _currentPath = "";
        
        // State tracking
        private HashSet<string> _expandedPaths = new HashSet<string>() { "" }; // Start with root expanded
        
        // Property editors
        private SimpleValueEditor _simpleValueEditor;
        private UnityObjectReferenceEditor _unityObjectReferenceEditor;
        
        // Delegate for handling tree item selection
        public delegate void SelectPropertyHandler(string path, string key, Type type);
        
        private float _lastInspectorHeight = 0f;
        private bool _isUpdatingLayout = false;
        private bool _hasInitializedHeight = false;
        
        private void OnEnable()
        {
            _dataInstance = target as DataInstance;
            
            // Initialize editors
            _simpleValueEditor = new SimpleValueEditor();
            _unityObjectReferenceEditor = new UnityObjectReferenceEditor();
        }

        
        
        public override VisualElement CreateInspectorGUI()
        {

            // Create root element
            _rootElement = new VisualElement();
            _rootElement.style.flexDirection = FlexDirection.Column;
            _rootElement.style.flexGrow = 1; // Allow vertical 
            _rootElement.style.flexShrink = 0;
            
            // Add standard fields
            var standardFields = new VisualElement();
            standardFields.style.marginBottom = 10;
            
            // Show Parent Structure field (read-only)
            var parentStructureField = new PropertyField(serializedObject.FindProperty("_parentStructure"));
            parentStructureField.SetEnabled(false); // Read-only
            standardFields.Add(parentStructureField);
            
            // Show Instance ID field
            var instanceIdField = new PropertyField(serializedObject.FindProperty("_instanceId"));
            standardFields.Add(instanceIdField);
            
            // Register change callbacks
            instanceIdField.RegisterCallback<ChangeEvent<string>>(evt => {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            });
            
            _rootElement.Add(standardFields);
            
            // Add separator
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            separator.style.marginTop = 5;
            separator.style.marginBottom = 10;
            _rootElement.Add(separator);
            
            // Create header 
            var header = new Label("Data Overrides Explorer");
            header.style.fontSize = 14;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 5;
            _rootElement.Add(header);
            
            // Create description
            var description = new Label("Select items in the hierarchy to view and override values.");
            description.style.marginBottom = 10;
            _rootElement.Add(description);
            
            // Create a horizontal layout container for our manual split view
            _splitContainer = new VisualElement();
            _splitContainer.style.flexDirection = FlexDirection.Row;
            _splitContainer.style.minHeight = 720;
            _splitContainer.style.height = 1000;
            _splitContainer.style.flexGrow = 1; // Let it take all available vertical space
            _rootElement.Add(_splitContainer);
            
            // Create left panel (hierarchy)
            var leftPanel = new VisualElement();
            leftPanel.style.minWidth = 120;
            leftPanel.style.width = 250; // Initial width
            leftPanel.style.flexDirection = FlexDirection.Column;
            leftPanel.style.flexShrink = 0; // Don't shrink below width
            leftPanel.style.flexGrow = 0; // Don't grow
            
            // Add title row for hierarchy
            var hierarchyTitle = new Label("Hierarchy");
            hierarchyTitle.style.paddingTop = 5;
            hierarchyTitle.style.paddingBottom = 5;
            hierarchyTitle.style.paddingLeft = 5;
            hierarchyTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            hierarchyTitle.style.fontSize = 14;
            hierarchyTitle.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            hierarchyTitle.style.color = Color.white;
            leftPanel.Add(hierarchyTitle);
            
            // Add a separator below the title
            var leftSeparator = new VisualElement();
            leftSeparator.style.height = 1;
            leftSeparator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            leftPanel.Add(leftSeparator);
            
            // Create tree view container with scroll view
            var treeScrollView = new ScrollView();
            treeScrollView.style.flexGrow = 1;
            leftPanel.Add(treeScrollView);
            
            _treeViewContainer = new VisualElement();
            _treeViewContainer.style.flexGrow = 1;
            treeScrollView.Add(_treeViewContainer);
            
            // Create an enhanced drag handle for resizing with hover effects
            var dragHandle = new VisualElement();
            dragHandle.style.width = 8;
            dragHandle.style.flexShrink = 0;
            dragHandle.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.3f); // More transparent by default
            dragHandle.name = "drag-handle"; // Name for styling
            
            // Create the drag handle line
            var line = new VisualElement();
            line.style.width = 2;
            line.style.height = new StyleLength(Length.Percent(100));
            line.style.marginLeft = 3;
            line.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Semi-transparent by default
            line.name = "drag-line"; // Name for styling
            dragHandle.Add(line);
            
            // Add hover effects and drag functionality
            Vector2 startPos = Vector2.zero;
            float startWidth = 0;

            dragHandle.RegisterCallback<MouseEnterEvent>(evt => {
                dragHandle.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.8f); // More opaque on hover
                line.style.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Brighter on hover
                
                // Try to set cursor to resize style - depends on Unity version
                try {
                    dragHandle.style.cursor = StyleKeyword.Initial;
                    // Use current cursor API - this approach works in most Unity versions
                    EditorGUIUtility.AddCursorRect(new Rect(evt.mousePosition, Vector2.one * 32), MouseCursor.ResizeHorizontal);
                }
                catch (Exception) { /* Cursor API might vary between Unity versions */ }
            });

            dragHandle.RegisterCallback<MouseLeaveEvent>(evt => {
                dragHandle.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.3f); // Back to transparent
                line.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Back to default
            });

            // Add drag functionality
            dragHandle.RegisterCallback<MouseDownEvent>(evt => {
                startPos = evt.mousePosition;
                startWidth = leftPanel.style.width.value.value;
                dragHandle.CaptureMouse();
                evt.StopPropagation();
            });

            dragHandle.RegisterCallback<MouseMoveEvent>(evt => {
                if (dragHandle.HasMouseCapture())
                {
                    float dragDelta = evt.mousePosition.x - startPos.x;
                    float newWidth = Mathf.Max(120, startWidth + dragDelta); // Use 120 to match minWidth
                    leftPanel.style.width = newWidth;
                    evt.StopPropagation();
                }
            });

            dragHandle.RegisterCallback<MouseUpEvent>(evt => {
                if (dragHandle.HasMouseCapture())
                {
                    dragHandle.ReleaseMouse();
                    evt.StopPropagation();
                }
            });
            
            // Create right panel (details)
            var rightPanel = new VisualElement();
            rightPanel.style.flexGrow = 1; // Take up remaining space
            rightPanel.style.flexDirection = FlexDirection.Column;
            rightPanel.style.paddingLeft = 10;
            
            // Add title row for details
            var detailsTitle = new Label("Details");
            detailsTitle.style.paddingTop = 5;
            detailsTitle.style.paddingBottom = 5;
            detailsTitle.style.paddingLeft = 5;
            detailsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            detailsTitle.style.fontSize = 14;
            detailsTitle.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            detailsTitle.style.color = Color.white;
            rightPanel.Add(detailsTitle);
            
            // Add a separator below the title
            var rightSeparator = new VisualElement();
            rightSeparator.style.height = 1;
            rightSeparator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            rightPanel.Add(rightSeparator);
            
            // Create path label
            _pathLabel = new Label("Path: Root");
            _pathLabel.style.marginTop = 10;
            _pathLabel.style.marginBottom = 5;
            rightPanel.Add(_pathLabel);
            
            // Create override indicator
            _overrideIndicator = new Label("Status: Using default value from structure");
            _overrideIndicator.style.marginBottom = 10;
            _overrideIndicator.style.color = new Color(0.5f, 0.5f, 0.5f);
            rightPanel.Add(_overrideIndicator);
            
            // Create details container with scroll view
            var detailsScrollView = new ScrollView();
            detailsScrollView.style.flexGrow = 1;
            rightPanel.Add(detailsScrollView);
            
            _detailsContent = new VisualElement();
            detailsScrollView.Add(_detailsContent);
            
            // Add panels to split container
            _splitContainer.Add(leftPanel);
            _splitContainer.Add(dragHandle);
            _splitContainer.Add(rightPanel);
            
            // Check if we have a valid data instance
            if (_dataInstance != null && _dataInstance.ParentStructure != null)
            {
                // Create custom tree view directly
                CreateCustomTreeView();
                
                // Select root by default if no selection
                if (string.IsNullOrEmpty(_currentPath))
                {
                    SelectProperty("", "Root", typeof(DataContainer));
                }
            }
            else
            {
                var errorLabel = new Label("This Data Instance has no parent structure assigned.");
                errorLabel.style.color = Color.red;
                _detailsContent.Add(errorLabel);
            }
            
            return _rootElement;
        }
        
        private void CreateCustomTreeView()
        {
            // Save expanded state before rebuilding
            SaveExpandedState();
            
            // Clear existing content
            _treeViewContainer.Clear();
            
            // Get the parent structure
            var structure = _dataInstance.ParentStructure;
            if (structure == null || structure.Container == null)
                return;
                
            // Create root container element
            var rootContainer = new VisualElement();
            rootContainer.AddToClassList("data-container-root");
            _treeViewContainer.Add(rootContainer);
            
            // Create root item
            var rootItem = CreateTreeItem(rootContainer, "Root", "", typeof(DataContainer), true);
            
            // Get all overrides and determine parent paths that need indicators
            var overrides = _dataInstance.GetAllOverrides();
            var allOverriddenPaths = GetAllOverriddenPathsIncludingParents(overrides.Keys);
            
            // Build the tree recursively
            BuildContainerTree(structure.Container, rootItem.Q<VisualElement>("content"), "", allOverriddenPaths);
            
            // Restore expanded state
            RestoreExpandedState();
            
            // Restore selection if needed
            if (!string.IsNullOrEmpty(_currentPath))
            {
                HighlightItem(_currentPath);
            }
        }
        
        private HashSet<string> GetAllOverriddenPathsIncludingParents(IEnumerable<string> overridePaths)
        {
            var result = new HashSet<string>();
            
            foreach (var path in overridePaths)
            {
                // Add the path itself
                result.Add(path);
                
                // Add all parent paths
                string currentPath = path;
                
                // Handle list/dictionary paths with brackets
                while (currentPath.Contains("["))
                {
                    int bracketIndex = currentPath.IndexOf('[');
                    // Add the path before the bracket
                    string parentPath = currentPath.Substring(0, bracketIndex);
                    result.Add(parentPath);
                    
                    // If there's more path after this bracket section, continue processing
                    int closingBracketIndex = currentPath.IndexOf(']', bracketIndex);
                    if (closingBracketIndex >= 0 && closingBracketIndex + 1 < currentPath.Length && currentPath[closingBracketIndex + 1] == '.')
                    {
                        // Handle paths like "list[0].property" by getting "list[0]" as a parent path
                        currentPath = currentPath.Substring(0, closingBracketIndex + 1);
                        result.Add(currentPath);
                        
                        // Then continue with the dot path parts
                        currentPath = parentPath;
                    }
                    else
                    {
                        // Just process the parent path without brackets
                        currentPath = parentPath;
                    }
                }
                
                // Handle dot notation paths (nested properties)
                while (currentPath.Contains("."))
                {
                    int lastDotIndex = currentPath.LastIndexOf('.');
                    currentPath = currentPath.Substring(0, lastDotIndex);
                    result.Add(currentPath);
                }
            }
            
            // Debug output to verify parent paths
            Debug.Log($"Override paths including parents: {string.Join(", ", result)}");
            
            return result;
        }
        
        private void SaveExpandedState()
        {
            // Create a temporary set to hold current expanded paths
            var currentExpandedPaths = new HashSet<string>();
            
            // Add the root path (always expanded)
            currentExpandedPaths.Add("");
            
            // Find all expanded items in the tree
            var items = _treeViewContainer.Query<VisualElement>().ToList();
            foreach (var item in items)
            {
                if (item.userData is string path && !string.IsNullOrEmpty(path))
                {
                    var content = item.Q<VisualElement>("content");
                    if (content != null && content.style.display == DisplayStyle.Flex)
                    {
                        currentExpandedPaths.Add(path);
                    }
                }
            }
            
            // Merge with previous expanded paths rather than replacing
            // This ensures paths that might not currently be in the tree (due to filtering or other UI changes)
            // are still preserved in the expanded set
            foreach (var path in currentExpandedPaths)
            {
                _expandedPaths.Add(path);
            }
            
            // Debug info to help track state
            Debug.Log($"Saved {currentExpandedPaths.Count} expanded paths. Total tracked: {_expandedPaths.Count}");
        }
        
        private void RestoreExpandedState()
        {
            if (_expandedPaths.Count == 0)
            {
                // Nothing to restore
                return;
            }
            
            int restoredCount = 0;
            
            // First, collect all tree items with their paths
            var allTreeItems = new Dictionary<string, VisualElement>();
            var items = _treeViewContainer.Query<VisualElement>().ToList();
            foreach (var item in items)
            {
                if (item.userData is string path && !string.IsNullOrEmpty(path))
                {
                    allTreeItems[path] = item;
                }
            }
            
            // Now restore expanded state for each path
            foreach (var path in _expandedPaths)
            {
                if (allTreeItems.TryGetValue(path, out var item))
                {
                    var content = item.Q<VisualElement>("content");
                    var toggle = item.Q<Label>("toggle");
                    
                    if (content != null && toggle != null)
                    {
                        content.style.display = DisplayStyle.Flex;
                        toggle.text = "▼"; // Expanded
                        restoredCount++;
                        
                        // Make sure all parent paths are also expanded
                        EnsureParentsExpanded(item);
                    }
                }
            }
            
            // Debug info to help track state
            Debug.Log($"Restored {restoredCount} expanded items out of {_expandedPaths.Count} tracked paths");
        }
        
        private void EnsureParentsExpanded(VisualElement childItem)
        {
            var current = childItem;
            while (current != null && current != _treeViewContainer)
            {
                // First check if this is a content element
                if (current.name == "content")
                {
                    // Find its parent container
                    var container = current.parent;
                    if (container != null)
                    {
                        // Make sure the content is expanded
                        current.style.display = DisplayStyle.Flex;
                        
                        // Update the toggle if it exists
                        var toggle = container.Q<Label>("toggle");
                        if (toggle != null)
                        {
                            toggle.text = "▼";
                        }
                    }
                }
                
                // Move up the tree
                current = current.parent;
            }
        }
        
        private VisualElement CreateTreeItem(VisualElement parent, string label, string path, Type type, bool isRoot = false)
        {
            // Create container for the tree item
            var itemContainer = new VisualElement();
            itemContainer.AddToClassList("tree-item-container");
            itemContainer.userData = path; // Store path in userData for later retrieval
            parent.Add(itemContainer);
            
            // Create row for item
            var row = new VisualElement();
            row.AddToClassList("data-entry-row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.overflow = Overflow.Hidden;
            itemContainer.Add(row);
            
            // Check both if directly overridden or in the parent paths collection
            bool isDirectlyOverridden = _dataInstance.HasOverride(path);
            bool isParentPath = false;
            
            // When building the tree, we already pass the full override paths collection
            // Store some debug data to help us understand what's happening
            itemContainer.userData = new Tuple<string, bool, bool>(path, isDirectlyOverridden, isParentPath);
            
            // Create override indicator if needed
            if (isDirectlyOverridden || isParentPath)
            {
                // Create override indicator - small circle
                var indicator = new VisualElement();
                indicator.name = "override-indicator";
                indicator.style.width = 10;
                indicator.style.height = 10;
                indicator.style.borderTopLeftRadius = 5;
                indicator.style.borderTopRightRadius = 5;
                indicator.style.borderBottomLeftRadius = 5;
                indicator.style.borderBottomRightRadius = 5;
                
                // Direct overrides get a bright green, parent path overrides get a muted green
                indicator.style.backgroundColor = isDirectlyOverridden ? 
                    new Color(0.2f, 0.7f, 0.2f) : // Bright green for direct overrides
                    new Color(0.5f, 0.7f, 0.5f);  // Muted green for parent path
                indicator.style.marginRight = 5;
                row.Add(indicator);
            }
            else
            {
                // Add empty space for alignment
                var spacer = new VisualElement();
                spacer.style.width = 10;
                spacer.style.marginRight = 5;
                row.Add(spacer);
            }
            
            // Create toggle button
            var toggleButton = new Label(isRoot || _expandedPaths.Contains(path) ? "▼" : "►"); // Down for expanded, right for collapsed
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
            content.style.display = (isRoot || _expandedPaths.Contains(path)) ? DisplayStyle.Flex : DisplayStyle.None;
            itemContainer.Add(content);
            
            // Add toggle functionality
            toggleButton.RegisterCallback<ClickEvent>(evt => {
                bool isExpanded = content.style.display == DisplayStyle.Flex;
                content.style.display = isExpanded ? DisplayStyle.None : DisplayStyle.Flex;
                toggleButton.text = isExpanded ? "►" : "▼";
                
                // Track expanded state
                if (isExpanded)
                {
                    _expandedPaths.Remove(path);
                }
                else
                {
                    _expandedPaths.Add(path);
                }
                
                evt.StopPropagation();
            });
            
            // Add selection functionality - directly calling our SelectProperty method
            row.RegisterCallback<ClickEvent>(evt => {
                // Pass the path and key to SelectProperty directly
                string displayKey = string.IsNullOrEmpty(path) ? 
                    "Root" : 
                    (path.Contains("[") ? path.Substring(path.LastIndexOf('[') + 1).TrimEnd(']').Trim('"') : 
                     path.Contains(".") ? path.Substring(path.LastIndexOf('.') + 1) : path);
                
                // Store current path for later restoration
                _currentPath = path;
                
                SelectProperty(path, displayKey, type);
                
                // Highlight this item
                HighlightItem(path);
                evt.StopPropagation();
            });
            
            return itemContainer;
        }
        
        private void BuildContainerTree(DataContainer container, VisualElement parent, string basePath, HashSet<string> overriddenPaths)
        {
            if (container == null)
                return;

            // Get all keys from the container
            var keys = container.GetKeys();

            foreach (var key in keys)
            {
                string path = string.IsNullOrEmpty(basePath) ? key : $"{basePath}.{key}";
                Type valueType = container.GetValueType(key);
                
                // Skip null values
                if (valueType == null)
                    continue;
                
                // Check if this path or any child path is overridden
                bool isOverridden = overriddenPaths.Contains(path);
                
                // Different handling based on type
                if (valueType == typeof(DataContainer))
                {
                    // Nested container
                    var nestedContainer = container.Get<DataContainer>(key);
                    
                    // Use custom tree item for container
                    var containerItem = CreateTreeItem(parent, $"{key} (Container)", path, valueType);
                    
                    // Check if this container has an override indicator - if so, mark its userData
                    if (isOverridden) 
                    {
                        // Add a small green dot for containers
                        AddOverrideIndicator(containerItem, path, false);
                    }
                    
                    // Build nested container tree
                    BuildContainerTree(nestedContainer, containerItem.Q<VisualElement>("content"), path, overriddenPaths);
                }
                else if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    if (valueType.GetGenericArguments()[0] == typeof(DataContainer))
                    {
                        // List of containers
                        var list = container.Get<List<DataContainer>>(key);
                        
                        // Use custom tree item for list container
                        var listItem = CreateTreeItem(parent, $"{key} (List<Container>) [{list.Count} items]", path, valueType);
                        
                        // Check if this list has an override indicator
                        if (isOverridden)
                        {
                            // Add a small green dot for lists
                            AddOverrideIndicator(listItem, path, false);
                        }
                        
                        var listContent = listItem.Q<VisualElement>("content");

                        // Add items
                        for (int i = 0; i < list.Count; i++)
                        {
                            // Use proper list path notation with square brackets
                            string listItemPath = $"{path}[{i}]";
                            var listItemContainer = list[i];
                            
                            bool isItemOverridden = overriddenPaths.Contains(listItemPath);
                            
                            // Use custom tree item for list items
                            var listItemElement = CreateTreeItem(listContent, $"Item {i}", listItemPath, typeof(DataContainer));
                            
                            // Check if this list item has an override indicator
                            if (isItemOverridden)
                            {
                                // Add a small green dot for list items
                                AddOverrideIndicator(listItemElement, listItemPath, false);
                            }
                            
                            // Build tree for the list item container
                            BuildContainerTree(listItemContainer, listItemElement.Q<VisualElement>("content"), listItemPath, overriddenPaths);
                        }
                    }
                    else
                    {
                        // Simple list (non-container elements)
                        CreateBasicTreeItem(parent, key, path, valueType, isOverridden);
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
                        var dictItem = CreateTreeItem(parent, $"{key} (Dictionary<string, Container>) [{dict.Count} items]", path, valueType);
                        
                        // Check if this dictionary has an override indicator
                        if (isOverridden)
                        {
                            // Add a small green dot for dictionaries
                            AddOverrideIndicator(dictItem, path, false);
                        }
                        
                        var dictContent = dictItem.Q<VisualElement>("content");
                        
                        // Add items
                        foreach (var dictKey in dict.OrderedKeys)
                        {
                            // Use proper dictionary path notation with square brackets and quotes
                            string dictItemPath = $"{path}[\"{dictKey}\"]";
                            var dictItemContainer = dict[dictKey];
                            
                            bool isItemOverridden = overriddenPaths.Contains(dictItemPath);
                            
                            // Use custom tree item for dictionary items
                            var dictItemElement = CreateTreeItem(dictContent, $"Key: \"{dictKey}\"", dictItemPath, typeof(DataContainer));
                            
                            // Check if this dictionary item has an override indicator
                            if (isItemOverridden)
                            {
                                // Add a small green dot for dictionary items
                                AddOverrideIndicator(dictItemElement, dictItemPath, false);
                            }
                            
                            // Build tree for the dictionary item container
                            BuildContainerTree(dictItemContainer, dictItemElement.Q<VisualElement>("content"), dictItemPath, overriddenPaths);
                        }
                    }
                    else
                    {
                        // Simple OrderedDictionary (non-container values)
                        CreateBasicTreeItem(parent, key, path, valueType, isOverridden);
                    }
                }
                else
                {
                    // Simple value
                    CreateBasicTreeItem(parent, key, path, valueType, isOverridden);
                }
            }
        }
        
        // Helper method to add override indicator to a tree item
        private void AddOverrideIndicator(VisualElement treeItem, string path, bool isDirectOverride)
        {
            var row = treeItem.Q(className: "data-entry-row");
            if (row == null) return;
            
            // Check if we already have an indicator
            var existingIndicator = row.Q("override-indicator");
            if (existingIndicator != null) return;
            
            // Remove any existing spacer that might be in its place
            var existingSpacer = row.Children().FirstOrDefault(c => c.style.width.value.value == 10 && c.style.marginRight.value.value == 5);
            if (existingSpacer != null)
            {
                row.Remove(existingSpacer);
            }
            
            // Create the indicator
            var indicator = new VisualElement();
            indicator.name = "override-indicator";
            indicator.style.width = 10;
            indicator.style.height = 10;
            indicator.style.borderTopLeftRadius = 5;
            indicator.style.borderTopRightRadius = 5;
            indicator.style.borderBottomLeftRadius = 5;
            indicator.style.borderBottomRightRadius = 5;
            
            // Direct overrides get a bright green, parent path overrides get a muted green
            indicator.style.backgroundColor = isDirectOverride ? 
                new Color(0.2f, 0.7f, 0.2f) : // Bright green for direct override
                new Color(0.5f, 0.7f, 0.5f);  // Muted green for parent override
            
            indicator.style.marginRight = 5;
            
            // Insert at the beginning of the row
            row.Insert(0, indicator);
        }
        
        private VisualElement CreateBasicTreeItem(VisualElement parent, string key, string path, Type valueType, bool isPathOverridden = false)
        {
            var itemContainer = new VisualElement();
            itemContainer.AddToClassList("tree-item-container");
            itemContainer.userData = path;
            parent.Add(itemContainer);
            
            var row = new VisualElement();
            row.AddToClassList("data-entry-row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.overflow = Overflow.Hidden;
            itemContainer.Add(row);
            
            // Check if this item is directly overridden
            bool isDirectlyOverridden = _dataInstance.HasOverride(path);
            
            // Show indicator if directly overridden or if a parent path is overridden
            if (isDirectlyOverridden || isPathOverridden)
            {
                // Create override indicator with the appropriate color based on override type
                var indicator = new VisualElement();
                indicator.name = "override-indicator";
                indicator.style.width = 10;
                indicator.style.height = 10;
                indicator.style.borderTopLeftRadius = 5;
                indicator.style.borderTopRightRadius = 5;
                indicator.style.borderBottomLeftRadius = 5;
                indicator.style.borderBottomRightRadius = 5;
                
                // Direct overrides get a bright green, parent path overrides get a muted green
                indicator.style.backgroundColor = isDirectlyOverridden ? 
                    new Color(0.2f, 0.7f, 0.2f) : // Bright green for direct overrides
                    new Color(0.5f, 0.7f, 0.5f);  // Muted green for parent path overrides
                    
                indicator.style.marginRight = 5;
                row.Add(indicator);
            }
            else
            {
                // Add empty space for alignment
                var spacer = new VisualElement();
                spacer.style.width = 10;
                spacer.style.marginRight = 5;
                row.Add(spacer);
            }
            
            // Add some spacing where the toggle would be
            var toggleSpacer = new VisualElement();
            toggleSpacer.style.width = 20;
            row.Add(toggleSpacer);
            
            // Create label
            var itemLabel = new Label($"{key} ({GetTypeName(valueType)})");
            itemLabel.style.overflow = Overflow.Hidden;
            itemLabel.style.textOverflow = TextOverflow.Ellipsis;
            itemLabel.style.flexGrow = 1;
            row.Add(itemLabel);
            
            // Add selection functionality
            row.RegisterCallback<ClickEvent>(evt => {
                _currentPath = path;
                SelectProperty(path, key, valueType);
                HighlightItem(path);
                evt.StopPropagation();
            });
            
            return itemContainer;
        }
        
        private void HighlightItem(string path)
        {
            // Clear any previous highlights
            var allRows = _treeViewContainer.Query(className: "data-entry-row").ToList();
            foreach (var row in allRows)
            {
                row.style.backgroundColor = Color.clear;
            }
            
            // Find the item with this path
            var items = _treeViewContainer.Query<VisualElement>().ToList();
            foreach (var item in items)
            {
                if (item.userData is string itemPath && itemPath == path)
                {
                    var row = item.Q(className: "data-entry-row");
                    if (row != null)
                    {
                        row.style.backgroundColor = new Color(0.1f, 0.4f, 0.7f, 0.5f);
                        
                        // Ensure all parent containers are expanded to show this item
                        ExpandToPath(item);
                        break;
                    }
                }
            }
        }
        
        private void ExpandToPath(VisualElement targetItem)
        {
            // Start from the target and walk up the tree, expanding each parent
            var current = targetItem;
            while (current != null && current != _treeViewContainer)
            {
                // If this is a content element, find its parent container and expand it
                if (current.name == "content")
                {
                    var container = current.parent;
                    if (container != null)
                    {
                        var toggle = container.Q<Label>("toggle");
                        if (toggle != null && toggle.text == "►") // If collapsed
                        {
                            current.style.display = DisplayStyle.Flex; // Show content
                            toggle.text = "▼"; // Change toggle to expanded
                            
                            // Track expanded state
                            if (container.userData is string path)
                            {
                                _expandedPaths.Add(path);
                            }
                        }
                    }
                }
                
                current = current.parent;
            }
        }
        
        public void SelectProperty(string path, string key, Type type)
        {
            _currentPath = path;
            _pathLabel.text = $"Path: {(string.IsNullOrEmpty(path) ? "Root" : path)}";
            
            // Check if this property is overridden
            bool isOverridden = _dataInstance.HasOverride(path);
            _overrideIndicator.text = isOverridden ? 
                "Status: Overridden ✓" : 
                "Status: Using default value from structure";
            _overrideIndicator.style.color = isOverridden ? 
                new Color(0.2f, 0.7f, 0.2f) : 
                new Color(0.5f, 0.5f, 0.5f);
            
            // Show property details
            ShowPropertyDetails(path, key, type);
        }
        
        private void ShowPropertyDetails(string path, string key, Type type)
        {
            _detailsContent.Clear();
            
            // Create the header
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.marginBottom = 10;

            var title = new Label($"Property: {key}");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 14;
            header.Add(title);

            _detailsContent.Add(header);
            
            // Add type information
            var typeInfo = new Label($"Type: {GetTypeName(type)}");
            typeInfo.style.marginBottom = 10;
            _detailsContent.Add(typeInfo);
            
            // Special handling for containers which can't be edited directly
            if (type == typeof(DataContainer) || 
                type == typeof(List<DataContainer>) || 
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(OrderedDictionary<,>)))
            {
                var containerMessage = new Label("Container items can be edited individually by selecting them in the hierarchy.");
                containerMessage.style.marginTop = 10;
                containerMessage.style.unityFontStyleAndWeight = FontStyle.Italic;
                _detailsContent.Add(containerMessage);
                
                // Show code access paths for containers too
                if (_dataInstance != null && _dataInstance.ParentStructure != null)
                {
                    CreateCodeAccessSection(_detailsContent, path, type);
                }
                return;
            }
            
            // Get current value (possibly overridden)
            object currentValue = null;
            try
            {
                var method = typeof(DataInstance).GetMethod("GetValue").MakeGenericMethod(type);
                currentValue = method.Invoke(_dataInstance, new object[] { path, null });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting value at path '{path}': {ex.Message}");
            }
            
            // Also get the default value from parent structure
            object defaultValue = null;
            if (_dataInstance.ParentStructure != null)
            {
                try
                {
                    var getMethod = typeof(DataContainer).GetMethod("PathGet").MakeGenericMethod(type);
                    defaultValue = getMethod.Invoke(_dataInstance.ParentStructure.Container, new object[] { path, null });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error getting default value at path '{path}': {ex.Message}");
                }
            }
            
            // Create value editor section
            var valueSection = new VisualElement();
            valueSection.style.marginTop = 10;
            valueSection.style.marginBottom = 15;
            
            var valueHeader = new Label("Current Value:");
            valueHeader.style.marginBottom = 5;
            valueSection.Add(valueHeader);
            
            var editorContainer = new VisualElement();
            editorContainer.style.marginLeft = 10;
            valueSection.Add(editorContainer);
            
            // Add appropriate editor based on type
            bool editorCreated = false;
            
            if (_simpleValueEditor.CanHandleType(type))
            {
                var editorField = _simpleValueEditor.CreateEditorField(type, currentValue, newValue => 
                {
                    // Update the value in the data instance (creates an override)
                    UpdateInstanceValue(path, type, newValue);
                });
                editorContainer.Add(editorField);
                editorCreated = true;
            }
            else if (_unityObjectReferenceEditor.CanHandleType(type))
            {
                var editorField = _unityObjectReferenceEditor.CreateEditorField(type, currentValue, newValue => 
                {
                    // Update the value in the data instance (creates an override)
                    UpdateInstanceValue(path, type, newValue);
                });
                editorContainer.Add(editorField);
                editorCreated = true;
            }
            
            if (!editorCreated)
            {
                // Fallback for types we can't edit
                var valueText = new Label(FormatValue(currentValue));
                editorContainer.Add(valueText);
            }
            
            _detailsContent.Add(valueSection);
            
            // Create default value section
            var defaultSection = new VisualElement();
            defaultSection.style.marginTop = 5;
            defaultSection.style.marginBottom = 15;
            
            var defaultHeader = new Label("Default Value (from structure):");
            defaultHeader.style.marginBottom = 5;
            defaultSection.Add(defaultHeader);
            
            var defaultValueText = new Label(FormatValue(defaultValue));
            defaultValueText.style.marginLeft = 10;
            defaultValueText.style.color = new Color(0.5f, 0.5f, 0.5f);
            defaultSection.Add(defaultValueText);
            
            _detailsContent.Add(defaultSection);
            
            // Add reset button if the value is overridden
            if (_dataInstance.HasOverride(path))
            {
                var resetButton = new Button(() => {
                    // Explicitly save the expanded state before removing the override
                    SaveExpandedState();
                    
                    // Remove the override
                    _dataInstance.RemoveOverride(path);
                    
                    // Mark the asset dirty
                    EditorUtility.SetDirty(_dataInstance);
                    
                    // Refresh the tree to update override indicators
                    CreateCustomTreeView();
                    
                    // Extract the key name for display
                    string displayKey = string.IsNullOrEmpty(path) ? 
                        "Root" : 
                        (path.Contains("[") ? path.Substring(path.LastIndexOf('[') + 1).TrimEnd(']').Trim('"') : 
                         path.Contains(".") ? path.Substring(path.LastIndexOf('.') + 1) : path);
                    
                    // First update the current UI indicators
                    _overrideIndicator.text = "Status: Using default value from structure";
                    _overrideIndicator.style.color = new Color(0.5f, 0.5f, 0.5f);
                    
                    // Highlight the path again after rebuild
                    HighlightItem(_currentPath);
                    
                    // Completely refresh the property details to update all displayed values
                    ShowPropertyDetails(path, displayKey, type);
                });
                
                resetButton.text = "Reset to Default";
                resetButton.style.marginTop = 10;
                _detailsContent.Add(resetButton);
            }
            
            // Add code access paths
            if (_dataInstance != null && _dataInstance.ParentStructure != null)
            {
                CreateCodeAccessSection(_detailsContent, path, type);
            }
        }
        
        private void CreateCodeAccessSection(VisualElement container, string path, Type type)
        {
            // Add a horizontal divider between editor and code access paths
            var divider = new VisualElement();
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            divider.style.marginTop = 20;
            divider.style.marginBottom = 20;
            container.Add(divider);
            
            // Create the box for path information
            var pathBox = new VisualElement();
            pathBox.style.marginTop = 20;
            pathBox.style.marginBottom = 20;
            container.Add(pathBox);

            // Full Path Access - as a section with box and copy button
            var fullPathSection = new VisualElement();
            fullPathSection.style.marginBottom = 15;
            
            // Create a container for the header to add the copy button
            var fullPathHeader = new VisualElement();
            fullPathHeader.style.flexDirection = FlexDirection.Row;
            fullPathHeader.style.justifyContent = Justify.SpaceBetween;
            fullPathHeader.style.marginBottom = 5;
            
            var fullPathLabel = new Label("Full Path Access (from DataInstance):");
            fullPathLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            fullPathHeader.Add(fullPathLabel);
            
            // Generate the access code
            string fullAccessCode = GenerateCodeAccessPath(path, type);
            
            // Create copy button for full path
            var fullCopyButton = new Button();
            fullCopyButton.clicked += () => {
                EditorGUIUtility.systemCopyBuffer = fullAccessCode;
                
                // Visual feedback - change icon and then change back after delay
                string originalText = fullCopyButton.text;
                fullCopyButton.text = "✓";
                
                // Schedule to change back after a short delay
                EditorApplication.delayCall += () => {
                    // Need to check if button still exists as it might be destroyed if UI refreshes
                    if (fullCopyButton != null)
                    {
                        fullCopyButton.text = originalText;
                    }
                };
            };
            fullCopyButton.text = "📋";
            fullCopyButton.tooltip = "Copy";
            fullCopyButton.style.width = 26;
            fullCopyButton.style.height = 22;
            fullCopyButton.style.alignSelf = Align.Center;
            fullPathHeader.Add(fullCopyButton);
            
            fullPathSection.Add(fullPathHeader);
            
            // Create the full path text field
            var fullPathField = new TextField();
            fullPathField.value = fullAccessCode;
            fullPathField.isReadOnly = true;
            fullPathField.style.marginBottom = 5;
            
            // Add a code example label
            var fullPathExample = new Label("Example usage:");
            fullPathExample.style.unityFontStyleAndWeight = FontStyle.Italic;
            fullPathExample.style.marginTop = 5;
            
            // Create an example code snippet
            var exampleCode = new Label();
            if (string.IsNullOrEmpty(path))
            {
                exampleCode.text = "var root = myDataInstance.GetRootContainer();";
            }
            else
            {
                exampleCode.text = $"var value = myDataInstance.GetValue<{GetCodeTypeName(type)}>(\"{path}\");";
            }
            exampleCode.style.marginLeft = 10;
            exampleCode.style.whiteSpace = WhiteSpace.Normal;
            
            fullPathSection.Add(fullPathField);
            fullPathSection.Add(fullPathExample);
            fullPathSection.Add(exampleCode);
            pathBox.Add(fullPathSection);
        
            // Relative Path Access - as a section with box and copy button
            var relativePathSection = new VisualElement();
            
            // Create a container for the header to add the copy button
            var relativePathHeader = new VisualElement();
            relativePathHeader.style.flexDirection = FlexDirection.Row;
            relativePathHeader.style.justifyContent = Justify.SpaceBetween;
            relativePathHeader.style.marginBottom = 5;
            relativePathHeader.style.marginTop = 15;
            
            var relativePathLabel = new Label("Relative Path Access (from parent):");
            relativePathLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            relativePathHeader.Add(relativePathLabel);

            // Generate the access code
            string relativeAccessCode = GenerateCodeAccessPath(path, type, true);
            
            // Create copy button for relative path
            var relativeCopyButton = new Button();
            relativeCopyButton.clicked += () => {
                EditorGUIUtility.systemCopyBuffer = relativeAccessCode;
            
                // Visual feedback - change icon and then change back after delay
                string originalText = relativeCopyButton.text;
                relativeCopyButton.text = "✓";
                
                // Schedule to change back after a short delay
                EditorApplication.delayCall += () => {
                    // Need to check if button still exists as it might be destroyed if UI refreshes
                    if (relativeCopyButton != null)
                    {
                        relativeCopyButton.text = originalText;
                    }
                };
            };
            relativeCopyButton.text = "📋";
            relativeCopyButton.tooltip = "Copy";
            relativeCopyButton.style.width = 26;
            relativeCopyButton.style.height = 22;
            relativeCopyButton.style.alignSelf = Align.Center;
            relativePathHeader.Add(relativeCopyButton);

            relativePathSection.Add(relativePathHeader);
            
            // Format the relative path with multi-line textfield
            var relativeCodeField = new TextField();
            relativeCodeField.multiline = true;
            relativeCodeField.SetValueWithoutNotify(relativeAccessCode);
            relativeCodeField.isReadOnly = true;
            relativeCodeField.selectAllOnMouseUp = true;
            relativeCodeField.selectAllOnFocus = true;
            relativeCodeField.style.marginBottom = 5;
            
            // Add a code example label for relative access
            var relativeExample = new Label("Example usage:");
            relativeExample.style.unityFontStyleAndWeight = FontStyle.Italic;
            relativeExample.style.marginTop = 5;
            
            // Create an example code snippet for relative access
            var relativeExampleCode = new Label();
            if (string.IsNullOrEmpty(path))
            {
                relativeExampleCode.text = "// Assuming you have a reference to the container\nvar item = parentContainer;";
            }
            else
            {
                // Check if we're displaying a property inside a container that's in a list/dictionary
                bool isPropertyInContainer = path.Contains("].") || (!DataContainer.IsListIndexPath(path) && !DataContainer.IsDictionaryKeyPath(path));
                
                if (isPropertyInContainer)
                {
                    // For properties inside containers (including list items or dictionary values)
                    string propertyName;
                    if (path.Contains("]."))
                    {
                        // Get everything after the last bracket+dot
                        int lastDot = path.LastIndexOf('.');
                        propertyName = path.Substring(lastDot + 1);
                    }
                    else
                    {
                        propertyName = DataContainer.GetPathKey(path);
                    }
                    
                    relativeExampleCode.text = $"// Assuming you have a reference to the container\nvar value = parentContainer.Get<{GetCodeTypeName(type)}>(\"{propertyName}\");";
                }
                else if (DataContainer.IsListIndexPath(path))
                {
                    // For direct list item access (no properties)
                    int index = int.Parse(DataContainer.GetPathKey(path));
                    relativeExampleCode.text = $"// Assuming you have a reference to the parent list\nvar container = parentList[{index}];";
                }
                else if (DataContainer.IsDictionaryKeyPath(path))
                {
                    // For direct dictionary item access (no properties)
                    string key = DataContainer.GetPathKey(path);
                    relativeExampleCode.text = $"// Assuming you have a reference to the parent dictionary\nvar container = parentDict[\"{key}\"];";
                }
                else
                {
                    // Regular property access
                    string propertyName = DataContainer.GetPathKey(path);
                    relativeExampleCode.text = $"// Assuming you have a reference to the parent container\nvar value = parentContainer.Get<{GetCodeTypeName(type)}>(\"{propertyName}\");";
                }
            }
            relativeExampleCode.style.marginLeft = 10;
            relativeExampleCode.style.whiteSpace = WhiteSpace.Normal;
            
            relativePathSection.Add(relativeCodeField);
            relativePathSection.Add(relativeExample);
            relativePathSection.Add(relativeExampleCode);
            pathBox.Add(relativePathSection);
        }
        
        private string GenerateCodeAccessPath(string path, Type type, bool isRelative = false)
        {
            if (string.IsNullOrEmpty(path))
                return isRelative ? "parentContainer" : "myDataInstance.GetRootContainer()";
            
            // For full path access (from DataInstance root), use GetValue
            if (!isRelative)
            {
                return $"myDataInstance.GetValue<{GetCodeTypeName(type)}>(\"{path}\")";
            }
            
            // For relative access, we need to determine if we're inside a container that's inside a list or dictionary
            // If we are, we should only show the property access from the container itself
            var segments = DataContainer.ParsePath(path);
            if (segments.Count == 0)
                return "parentContainer";
            
            // For relative paths, we want to get the last property name only
            // as we assume the parent reference is to the direct container
            if (segments.Count > 0)
            {
                var lastSegment = segments[segments.Count - 1];
                
                // If the path ends with a property name
                if (lastSegment.Type == PathSegmentType.Property)
                {
                    return $"parentContainer.Get<{GetCodeTypeName(type)}>(\"{lastSegment.PropertyName}\")";
                }
            }
            
            // If we're looking at a direct list or dictionary entry 
            // (not a property within one), then we do need indexing
            if (segments.Count == 1)
            {
                var segment = segments[0];
                
                if (segment.Type == PathSegmentType.ListIndex)
                {
                    return $"parentList[{segment.ListIndex}]";
                }
                else if (segment.Type == PathSegmentType.DictionaryKey)
                {
                    return $"parentDict[\"{segment.DictionaryKey}\"]";
                }
            }
            
            // Handle special case when the path contains brackets but we're viewing a property
            // inside a container that's in a list or dictionary
            int bracketPos = path.IndexOf('[');
            if (bracketPos >= 0 && path.Contains("]."))
            {
                // Get everything after the last bracket+dot
                int lastDotAfterBracket = path.LastIndexOf('.');
                if (lastDotAfterBracket > bracketPos)
                {
                    string propertyName = path.Substring(lastDotAfterBracket + 1);
                    return $"parentContainer.Get<{GetCodeTypeName(type)}>(\"{propertyName}\")";
                }
            }
            
            // Fallback for other cases - simple property access from container
            string key = DataContainer.GetPathKey(path);
            return $"parentContainer.Get<{GetCodeTypeName(type)}>(\"{key}\")";
        }
        
        // Helper method to get the proper C# type name for code generation
        private string GetCodeTypeName(Type type)
        {
            if (type == typeof(string))
                return "string";
            else if (type == typeof(int))
                return "int";
            else if (type == typeof(float))
                return "float";
            else if (type == typeof(bool))
                return "bool";
            else if (type == typeof(Vector2))
                return "Vector2";
            else if (type == typeof(Vector3))
                return "Vector3";
            else if (type == typeof(Color))
                return "Color";
            else if (type == typeof(DataContainer))
                return "DataContainer";
            else if (type == typeof(List<DataContainer>))
                return "List<DataContainer>";
            else if (type == typeof(OrderedDictionary<string, DataContainer>))
                return "OrderedDictionary<string, DataContainer>";
            else if (type == typeof(UnityObjectReference))
                return "UnityObjectReference";
            else
                return type.Name;
        }
        
        private void UpdateInstanceValue(string path, Type type, object newValue)
        {
            try
            {
                // Use reflection to call SetValue with the correct generic type parameter
                var setMethod = typeof(DataInstance).GetMethod("SetValue").MakeGenericMethod(type);
                setMethod.Invoke(_dataInstance, new object[] { path, newValue });
                
                // Mark the asset dirty
                EditorUtility.SetDirty(_dataInstance);
                
                // Explicitly save the expanded state before refreshing the tree
                SaveExpandedState();
                
                // Refresh the tree to show overridden status
                CreateCustomTreeView();
                
                // Highlight the current path again after rebuild
                HighlightItem(_currentPath);
                
                // Update override indicator
                _overrideIndicator.text = "Status: Overridden ✓";
                _overrideIndicator.style.color = new Color(0.2f, 0.7f, 0.2f);
                
                // Extract the key name from the path for display
                string displayKey = string.IsNullOrEmpty(path) ? 
                    "Root" : 
                    (path.Contains("[") ? path.Substring(path.LastIndexOf('[') + 1).TrimEnd(']').Trim('"') : 
                     path.Contains(".") ? path.Substring(path.LastIndexOf('.') + 1) : path);
                
                // Refresh the property details panel to show the reset button
                SelectProperty(path, displayKey, type);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting value at path '{path}': {ex.Message}");
            }
        }
        
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
            if (type.IsGenericType && 
                type.GetGenericTypeDefinition() == typeof(OrderedDictionary<,>) &&
                type.GetGenericArguments()[0] == typeof(string) &&
                type.GetGenericArguments()[1] == typeof(DataContainer))
                return "Dictionary<String, Container>";
            if (type == typeof(UnityObjectReference))
                return "Unity Object Reference";
            
            return type.Name;
        }
        
        private string FormatValue(object value)
        {
            if (value == null)
                return "null";
                
            if (value is Vector2 vec2)
                return $"({vec2.x}, {vec2.y})";
                
            if (value is Vector3 vec3)
                return $"({vec3.x}, {vec3.y}, {vec3.z})";
                
            if (value is Color color)
                return $"RGBA({color.r:F2}, {color.g:F2}, {color.b:F2}, {color.a:F2})";
                
            if (value is UnityObjectReference objRef)
                return $"{objRef.Key} ({objRef.GetObject()?.name ?? "null"})";
                
            return value.ToString();
        }


        // Replace the GetInspectorWindowHeight method with this improved version
        private float GetInspectorWindowHeight()
        {
            

            try
            {
                // Default fallback height if reflection fails
                float fallbackHeight = 800f;
                
                // Get the internal InspectorWindow type
                var inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
                if (inspectorType == null)
                {
                    Debug.LogWarning("InspectorWindow type not found.");
                    return fallbackHeight;
                }

                // Find all InspectorWindow instances
                var inspectors = Resources.FindObjectsOfTypeAll(inspectorType);
                if (inspectors.Length == 0)
                {
                    Debug.LogWarning("No InspectorWindow instances found.");
                    return fallbackHeight;
                }

                // Iterate through InspectorWindow instances
                foreach (var inspector in inspectors)
                {
                    var window = inspector as EditorWindow;
                    if (window == null)
                        continue;

                    // Use reflection to access the 'tracker' property
                    var trackerProperty = inspectorType.GetProperty("tracker", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (trackerProperty == null)
                        continue;

                    var tracker = trackerProperty.GetValue(inspector, null) as ActiveEditorTracker;
                    if (tracker == null)
                        continue;

                    // Check if the current editor is part of the active editors
                    foreach (var editor in tracker.activeEditors)
                    {
                        if (editor == null)
                            continue;
                        
                        // Check if this editor is our DataInstanceInspector
                        if (editor == this)
                        {
                            // Return the height of the Inspector window
                            return window.position.height;
                        }
                    }
                }
                
                Debug.LogWarning("Could not find inspector window containing this editor.");
                return fallbackHeight;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get inspector height via reflection: {ex.Message}");
                float fallbackHeight = 800f;
                return fallbackHeight;
            }
        }
    }
} 