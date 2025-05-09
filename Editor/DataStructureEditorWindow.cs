using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using GAOS.DataStructure;
using GAOS.DataStructure.References;
using GAOS.Logger;

namespace GAOS.DataStructure.Editor
{
    /// <summary>
    /// Editor window for viewing and editing DataContainer-based assets
    /// </summary>
    public class DataStructureEditorWindow : EditorWindow
    {
        private VisualElement _treeViewContainer;
        private VisualElement _detailsContent;
        private Label _pathLabel;
        private Label _defaultMessageLabel;
        
        private DataStructure _currentDataStructure;
        private string _currentPath = "";
        
        // Tree builder for hierarchy view
        private HierarchyTreeBuilder _treeBuilder;

        // Property editors
        private IPropertyEditor _simpleValueEditor;
        private IPropertyEditor _unityObjectReferenceEditor;
        private IPropertyEditor _containerListEditor;
        private IPropertyEditor _containerDictionaryEditor;
        private IPropertyEditor _containerEditor;
        
        // Public property to access the current path
        public string CurrentPath => _currentPath;

        // Removing duplicate menu item
        // [MenuItem("GAOS/DataStructure/Data Structure Editor", false, 15)]
        public static void ShowWindow()
        {
            var window = GetWindow<DataStructureEditorWindow>();
            window.titleContent = new GUIContent("Data Structure Editor");
            window.minSize = new Vector2(800, 500);
        }
        
        /// <summary>
        /// Creates the window UI
        /// </summary>
        public void CreateGUI()
        {
            // Setup property editors
            _simpleValueEditor = new SimpleValueEditor();
            _unityObjectReferenceEditor = new UnityObjectReferenceEditor();
            _containerListEditor = new ContainerListEditor(this);
            _containerDictionaryEditor = new ContainerDictionaryEditor(this);
            _containerEditor = new ContainerEditor(this);
            
            // Load UI from UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.gaos.datastructure/Editor/UIToolkit/DataStructureEditor.uxml");
                visualTree.CloneTree(rootVisualElement);

            // Get references to UI elements
            _treeViewContainer = rootVisualElement.Q<VisualElement>("treeViewContainer");
            _detailsContent = rootVisualElement.Q<VisualElement>("detailsContent");
            _pathLabel = rootVisualElement.Q<Label>("pathLabel");
            _defaultMessageLabel = rootVisualElement.Q<Label>("defaultMessage");
            
            // Set minimum width for hierarchy panel to prevent it from being dragged too small
            var hierarchyPanel = rootVisualElement.Q<VisualElement>("hierarchyPanel");
            if (hierarchyPanel != null)
            {
                hierarchyPanel.style.minWidth = 250;
                // Add constraint to prevent overflow
                hierarchyPanel.style.overflow = Overflow.Hidden;
            }
            
            // Set minimum width for the details panel as well
            var detailsPanel = rootVisualElement.Q<VisualElement>("detailsPanel");
            if (detailsPanel != null)
            {
                detailsPanel.style.minWidth = 350;
            }
            
            // Ensure the tree view container has proper constraints
            if (_treeViewContainer != null)
            {
                _treeViewContainer.style.width = Length.Percent(100);
                _treeViewContainer.style.overflow = Overflow.Hidden;
            }
            
            // Register for selection change events
            Selection.selectionChanged += OnSelectionChanged;
            
            // Load current selection if it's a DataStructure
            OnSelectionChanged();
            
            // Register for focus events to detect when window loses focus
            rootVisualElement.RegisterCallback<FocusOutEvent>(OnWindowLostFocus);
        }

        private void OnSelectionChanged()
        {
            UnityEngine.Object selectedObject = Selection.activeObject;
            if (selectedObject is DataStructure dataStructure)
            {
                LoadDataStructure(dataStructure);
            }
        }
        
        private void OnDisable()
        {
            // Unregister from selection change events to prevent memory leaks
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void LoadDataStructure(DataStructure dataStructure)
        {
            GLog.Info<DataSystemEditorLogger>($"Loading data structure: {dataStructure.name}");
            
            _currentDataStructure = dataStructure;
            _currentPath = "";
            
            // Make sure DataStructure is valid
            if (_currentDataStructure.Container == null)
            {
                GLog.Error<DataSystemEditorLogger>("DataStructure container is null!");
                _detailsContent.Clear();
                var errorLabel = new Label("Error: Data structure container is null!");
                errorLabel.style.color = new Color(1, 0, 0);
                _detailsContent.Add(errorLabel);
                return;
            }
            
            // Debug keys in root container
            int keyCount = _currentDataStructure.Container.GetKeys().Count();
            GLog.Info<DataSystemEditorLogger>($"DataStructure root container has {keyCount} keys");
            foreach (var key in _currentDataStructure.Container.GetKeys())
            {
                Type valueType = _currentDataStructure.Container.GetValueType(key);
                GLog.Info<DataSystemEditorLogger>($"Root key: {key}, Type: {valueType?.Name}");
            }
            
            // Initialize tree builder with current data structure
            _treeBuilder = new HierarchyTreeBuilder(this, _currentDataStructure, _treeViewContainer);
            
            // Update UI
            _pathLabel.text = "Path: Root";
            RefreshHierarchyTree();
            
            // Select the root container and show its details using the ContainerEditor
            SelectProperty("", "Root", typeof(DataContainer));
            
            // Ensure the default message is hidden as we're showing the root container
            _defaultMessageLabel.style.display = DisplayStyle.None;
            
            GLog.Info<DataSystemEditorLogger>("DataStructure loaded successfully, root container selected");
        }

        /// <summary>
        /// Refreshes the tree view using the tree builder
        /// </summary>
        internal void RefreshHierarchyTree()
        {
            if (_currentDataStructure == null)
                return;

            if (_treeBuilder == null)
                _treeBuilder = new HierarchyTreeBuilder(this, _currentDataStructure, _treeViewContainer);

            _treeBuilder.RefreshHierarchyTree();
            
            // Ensure something is selected after refreshing
            // If a path is already set, try to reselect it
            // Otherwise, select the root
            if (string.IsNullOrEmpty(_currentPath))
            {
                SelectProperty("", "Root", typeof(DataContainer));
                GLog.Info<DataSystemEditorLogger>("RefreshHierarchyTree: Selected root as default after refresh");
            }
            else
            {
                EnsureSelection(_currentPath);
                GLog.Info<DataSystemEditorLogger>($"RefreshHierarchyTree: Restored selection to '{_currentPath}' after refresh");
            }
        }

        /// <summary>
        /// Selects a property in the hierarchy and shows its details
        /// </summary>
        public void SelectProperty(string path, string key, Type type)
        {
            // Special case for index-only paths
            if (!string.IsNullOrEmpty(path) && path.StartsWith("[") && !_currentPath.EndsWith("]"))
            {
                GLog.Info<DataSystemEditorLogger>($"SelectProperty: Converting index-only path '{path}' by appending to current path '{_currentPath}'");
                // Only append if we have a current path and it makes sense
                if (!string.IsNullOrEmpty(_currentPath) && 
                    (_currentPath.EndsWith("list") || 
                     path.EndsWith("List") || 
                     type == typeof(List<DataContainer>)))
                {
                    path = _currentPath + path;
                    GLog.Info<DataSystemEditorLogger>($"SelectProperty: Converted to full path: {path}");
                }
            }
            
            _currentPath = path;
            _pathLabel.text = $"Path: {(string.IsNullOrEmpty(path) ? "Root" : path)}";
            
            // Highlight the selected item in the tree
            if (_treeBuilder != null)
            {
                _treeBuilder.HighlightItem(path);
                _treeBuilder.ExpandToPath(path);
            }
            
            // Debug info
            GLog.Info<DataSystemEditorLogger>($"Selected property: Path={path}, Key={key}, Type={type?.Name}");
            
            // For dictionary items with bracket notation, add extra debugging
            if (path.Contains("[") && path.Contains("]") && type == typeof(DataContainer))
            {
                DataContainer container = GetContainerAtPath(path);
                if (container != null)
                {
                    GLog.Info<DataSystemEditorLogger>($"SelectProperty: Successfully got container at path '{path}' with {container.GetKeys().Count()} keys");
                    foreach (var containerKey in container.GetKeys())
                    {
                        Type keyType = container.GetValueType(containerKey);
                        GLog.Info<DataSystemEditorLogger>($"SelectProperty: Container has key '{containerKey}' of type {keyType?.Name}");
                    }
                }
                else
                {
                    GLog.Error<DataSystemEditorLogger>($"SelectProperty: Failed to get container at path '{path}'");
                }
            }
            
            // Show property details
            ShowPropertyDetails(path, key, type);
        }

        private void ShowPropertyDetails(string path, string key, Type type)
        {
            GLog.Info<DataSystemEditorLogger>($"ShowPropertyDetails: path='{path}', key='{key}', type={type?.Name}");
            _detailsContent.Clear();

            // Special case for dictionary items with bracket notation
            bool isDictionaryItem = path.Contains("[") && path.EndsWith("]") && type == typeof(DataContainer);
            if (isDictionaryItem)
            {
                GLog.Info<DataSystemEditorLogger>($"ShowPropertyDetails: Handling dictionary item at path '{path}'");
                
                // Check if we have a valid data structure with a container
                if (_currentDataStructure != null)
                {
                    // Display the container editor instead of default message
                    _defaultMessageLabel.style.display = DisplayStyle.None;
                    
                    // Create the header
                    var containerHeader = new VisualElement();
                    containerHeader.style.flexDirection = FlexDirection.Row;
                    containerHeader.style.marginBottom = 10;

                    var headerTitle = new Label($"Property Details: {key}");
                    headerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                    headerTitle.style.fontSize = 16;
                    containerHeader.Add(headerTitle);
                    _detailsContent.Add(containerHeader);

                    // Add type information
                    var containerTypeInfo = new Label($"Type: {GetTypeName(type)}");
                    containerTypeInfo.style.marginBottom = 10;
                    _detailsContent.Add(containerTypeInfo);

                    // Create property editor container
                    var containerEditorElement = new VisualElement();
                    containerEditorElement.style.marginTop = 10;
                    _detailsContent.Add(containerEditorElement);
                    
                    // Get the container at this path
                    DataContainer dictionaryItemContainer = GetContainerAtPath(path);
                    
                    if (dictionaryItemContainer != null)
                    {
                        GLog.Info<DataSystemEditorLogger>($"ShowPropertyDetails: Successfully got container with {dictionaryItemContainer.GetKeys().Count()} keys");
                        
                        // Use the container editor to edit this container
                        if (_containerEditor != null && _containerEditor.CanHandleType(typeof(DataContainer)))
                        {
                            containerEditorElement.Add(_containerEditor.CreateEditorField(typeof(DataContainer), dictionaryItemContainer, newValue => {
                                // Update the container in the dictionary
                                SetPathValue(path, newValue);
                                
                                // Mark dirty and refresh hierarchy
                                ApplyStructuralChange();
                                
                                GLog.Info<DataSystemEditorLogger>($"Dictionary item container at '{path}' updated");
                            }));
                        }
                    }
                    else
                    {
                        GLog.Error<DataSystemEditorLogger>($"ShowPropertyDetails: Failed to get container at path '{path}'");
                        containerEditorElement.Add(new Label("Error: Could not load container contents."));
                    }
                    
                    // Add access code display
                    CreateCodeAccessSection(_detailsContent, path, type);
                    return;
                }
            }

            // If the path is empty and key is "Root" (root container)
            if (string.IsNullOrEmpty(path) && key == "Root")
            {
                // Check if we have a valid data structure with a container
                if (_currentDataStructure != null && _currentDataStructure.Container != null)
                {
                    // Display the root container editor instead of default message
                    _defaultMessageLabel.style.display = DisplayStyle.None;
                    
                    // Create the header
                    var rootHeader = new VisualElement();
                    rootHeader.style.flexDirection = FlexDirection.Row;
                    rootHeader.style.marginBottom = 10;

                    var rootTitle = new Label("Root Container");
                    rootTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                    rootTitle.style.fontSize = 16;
                    rootHeader.Add(rootTitle);

                    _detailsContent.Add(rootHeader);

                    // Add type information
                    var rootTypeInfo = new Label($"Type: {GetTypeName(typeof(DataContainer))}");
                    rootTypeInfo.style.marginBottom = 10;
                    _detailsContent.Add(rootTypeInfo);

                    // Create property editor container
                    var rootEditorContainer = new VisualElement();
                    rootEditorContainer.style.marginTop = 10;
                    _detailsContent.Add(rootEditorContainer);
                    
                    // Use the container editor to edit the root container
                    if (_containerEditor != null && _containerEditor.CanHandleType(typeof(DataContainer)))
                    {
                        // IMPORTANT: Pass the actual container directly, not a copy.
                        // This ensures we edit the real container and not an orphaned copy
                        var rootContainer = _currentDataStructure.Container;
                        
                        GLog.Info<DataSystemEditorLogger>("Using ContainerEditor to edit root container with " + rootContainer.GetKeys().Count() + " keys");
                        
                        rootEditorContainer.Add(_containerEditor.CreateEditorField(typeof(DataContainer), rootContainer, newValue => {
                            // Root container updates are handled directly by the ContainerEditor
                            // We DON'T need to handle copying here, as each individual property change
                            // is already applied to the real container directly.
                            
                            // Just mark the asset as dirty to ensure changes are saved
                            ApplyStructuralChange();
                            
                            GLog.Info<DataSystemEditorLogger>("Root container updated via ContainerEditor");
                        }));
                    }
                    
                    // Add access code display for root container
                    CreateCodeAccessSection(_detailsContent, "", typeof(DataContainer));
                }
                else
                {
                    // If we don't have a valid container, show the default message
                ShowDefaultDetails();
                }
                return;
            }

            // Create the header
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.marginBottom = 10;

            var title = new Label($"Property Details: {key}");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 16;
            header.Add(title);

            _detailsContent.Add(header);

            // Add type information
            var typeInfo = new Label($"Type: {GetTypeName(type)}");
            typeInfo.style.marginBottom = 10;
            _detailsContent.Add(typeInfo);

            // Create property editor container
            var editorContainer = new VisualElement();
            editorContainer.style.marginTop = 10;
            _detailsContent.Add(editorContainer);

            // Get the value of the property
            object value = GetPathValue(path, type);
            
            // Try to use the appropriate editor based on type
            bool editorFound = false;
            
            // First check if any of our specialized editors can handle this type
            if (type != null)
            {
                foreach (var editor in new IPropertyEditor[] { 
                    _simpleValueEditor, 
                    _unityObjectReferenceEditor, 
                    _containerEditor,
                    _containerListEditor, 
                    _containerDictionaryEditor
                })
                {
                    if (editor != null && editor.CanHandleType(type))
                    {
                        editorContainer.Add(editor.CreateEditorField(type, value, newValue => {
                            // Update the value in the data structure
                            SetPathValue(path, newValue);
                            
                            // Mark the asset dirty and refresh hierarchy
                            ApplyStructuralChange();
                        }));
                        editorFound = true;
                        break;
                    }
                }
            }
            
          
            
            // Final fallback to a basic value editor
            if (!editorFound && type != null)
            {
                GLog.Error<DataSystemEditorLogger>($"Error creating editor for {path}");
            }
            
            // Add access code display
            if (_currentDataStructure != null)
            {
                CreateCodeAccessSection(_detailsContent, path, type);
            }
        }
        
        /// <summary>
        /// Marks the current DataStructure asset as dirty and refreshes the hierarchy tree
        /// </summary>
        public void ApplyStructuralChange()
        {
            // First mark the asset as dirty
            if (_currentDataStructure != null)
            {
                EditorUtility.SetDirty(_currentDataStructure);
                AssetDatabase.SaveAssetIfDirty(_currentDataStructure);
            }
            
            // Then refresh the hierarchy tree to show changes
            RefreshHierarchyTree();
            
            GLog.Info<DataSystemEditorLogger>("Applied structural change: Marked asset dirty and refreshed hierarchy tree");
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
            fullPathSection.AddToClassList("path-access-section");
            
            // Create a container for the header to add the copy button
            var fullPathHeader = new VisualElement();
            fullPathHeader.AddToClassList("path-access-header");
            
            var fullPathLabel = new Label("Full Path Access (from DataStructure):");
            fullPathLabel.AddToClassList("path-access-title");
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
            fullCopyButton.AddToClassList("icon-button");
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
            fullPathField.AddToClassList("code-field");
            
            // Add a code example label
            var fullPathExample = new Label("Example usage:");
            fullPathExample.AddToClassList("path-access-example");
            
            // Create an example code snippet
            var exampleCode = new Label();
            if (string.IsNullOrEmpty(path))
            {
                exampleCode.text = "var root = myDataStructure.Container;";
            }
            else
            {
                exampleCode.text = $"var value = myDataStructure.Container.PathGet<{GetCodeTypeName(type)}>(\"{path}\");";
            }
            exampleCode.AddToClassList("path-access-example-code");
            
            fullPathSection.Add(fullPathField);
            fullPathSection.Add(fullPathExample);
            fullPathSection.Add(exampleCode);
            pathBox.Add(fullPathSection);
                
            // Relative Path Access - as a section with box and copy button
            var relativePathSection = new VisualElement();
            relativePathSection.AddToClassList("path-access-section");
            
            // Create a container for the header to add the copy button
            var relativePathHeader = new VisualElement();
            relativePathHeader.AddToClassList("path-access-header");
            
            var relativePathLabel = new Label("Relative Path Access (from parent):");
            relativePathLabel.AddToClassList("path-access-title");
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
            relativeCopyButton.AddToClassList("icon-button");
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
            relativeCodeField.AddToClassList("code-field");
            
            // Add a code example label for relative access
            var relativeExample = new Label("Example usage:");
            relativeExample.AddToClassList("path-access-example");
            
            // Create an example code snippet for relative access
            var relativeExampleCode = new Label();
            if (string.IsNullOrEmpty(path))
            {
                relativeExampleCode.text = "// Assuming you have a reference to the container\nvar item = parentContainer;";
            }
            else
            {
                // Check if we're displaying a property inside a container that's in a list or dictionary
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
            relativeExampleCode.AddToClassList("path-access-example-code");
            
            relativePathSection.Add(relativeCodeField);
            relativePathSection.Add(relativeExample);
            relativePathSection.Add(relativeExampleCode);
            pathBox.Add(relativePathSection);
        }

        private DataContainer GetContainerAtPath(string path)
        {
            GLog.Info<DataSystemEditorLogger>($"GetContainerAtPath: Resolving path '{path}'");
            
            if (_currentDataStructure == null)
            {
                GLog.Error<DataSystemEditorLogger>("GetContainerAtPath: _currentDataStructure is null");
                return null;
            }
        
            if (_currentDataStructure.Container == null)
            {
                GLog.Error<DataSystemEditorLogger>("GetContainerAtPath: _currentDataStructure.Container is null");
                return null;
            }
        
            if (string.IsNullOrEmpty(path))
            {
                GLog.Info<DataSystemEditorLogger>("GetContainerAtPath: Empty path, returning root container");
                return _currentDataStructure.Container;
            }
            
            // Use the new PathGet method for all paths (dot notation or bracket notation)
            return _currentDataStructure.Container.PathGet<DataContainer>(path);
        }

        private object GetPathValue(string path, Type type)
        {
            try
            {
                GLog.Info<DataSystemEditorLogger>($"GetPathValue called with path='{path}', type={type?.Name}");
                
                if (_currentDataStructure == null || _currentDataStructure.Container == null)
                {
                    GLog.Warning<DataSystemEditorLogger>($"GetPathValue: Missing data - DataStructure: {_currentDataStructure != null}, Container: {_currentDataStructure?.Container != null}, Path: '{path}'");
                        return null;
                    }
                    
                // Use DataContainer's path utilities to parse the path
                if (DataContainer.IsDictionaryKeyPath(path))
                {
                    // Extract dictionary path and key
                    string parentPath = DataContainer.GetParentPath(path);
                    string dictionaryKey = DataContainer.GetPathKey(path);
                    
                    // Get the dictionary
                    var dictionary = _currentDataStructure.Container.PathGet<OrderedDictionary<string, DataContainer>>(parentPath);
                    if (dictionary == null || !dictionary.ContainsKey(dictionaryKey))
                        {
                        GLog.Warning<DataSystemEditorLogger>($"GetPathValue: Dictionary not found or key not in dictionary. Path: {path}, Parent: {parentPath}, Key: {dictionaryKey}");
                        return null;
                    }
                    
                    // Return the dictionary value
                    return dictionary[dictionaryKey];
                    }
                else if (DataContainer.IsListIndexPath(path))
                    {
                    // Extract list path and index
                    string parentPath = DataContainer.GetParentPath(path);
                    string indexStr = DataContainer.GetPathKey(path);
                    if (!int.TryParse(indexStr, out int index))
                        {
                        GLog.Error<DataSystemEditorLogger>($"GetPathValue: Invalid list index: {indexStr}");
            return null;
        }

                    // Get the list
                    var list = _currentDataStructure.Container.PathGet<List<DataContainer>>(parentPath);
                            if (list == null || index < 0 || index >= list.Count)
                    {
                        GLog.Warning<DataSystemEditorLogger>($"GetPathValue: List not found or index out of range. Path: {path}, Parent: {parentPath}, Index: {index}");
                            return null;
                        }
                    
                    // Return the list item
                    return list[index];
                        }
                        else
                        {
                    // Use generic PathGet for regular paths
                    // This relies on reflection to get the value with the right type
                    var getMethod = typeof(DataContainer).GetMethod("PathGet").MakeGenericMethod(type);
                    return getMethod.Invoke(_currentDataStructure.Container, new object[] { path, null });
                    }
                }
            catch (Exception ex)
                {
                GLog.Error<DataSystemEditorLogger>($"GetPathValue Error: {ex.Message}\n{ex.StackTrace}");
                        return null;
            }
        }

        private void SetPathValue(string path, object value)
        {
            try
            {
                GLog.Info<DataSystemEditorLogger>($"SetPathValue: path='{path}', value={value}");
                
                if (_currentDataStructure == null || _currentDataStructure.Container == null)
            {
                    GLog.Warning<DataSystemEditorLogger>($"SetPathValue: Missing data - DataStructure: {_currentDataStructure != null}, Container: {_currentDataStructure?.Container != null}");
                return;
            }
            
                // Use the generic PathSet method with the appropriate type
                // We need to use reflection because we don't know the type at compile time
                Type valueType = value?.GetType() ?? typeof(object);
                var method = typeof(DataContainer).GetMethod("PathSet").MakeGenericMethod(valueType);
                method.Invoke(_currentDataStructure.Container, new[] { path, value });
                
                // Mark the data structure as dirty
                ApplyStructuralChange();
                }
                catch (Exception ex)
                {
                GLog.Error<DataSystemEditorLogger>($"SetPathValue Error: {ex.Message}\n{ex.StackTrace}");
                }
        }

        // Add this method before the ShowPropertyDetails method or elsewhere in the class
        private string GenerateCodeAccessPath(string path, Type type, bool isRelative = false)
        {
            if (string.IsNullOrEmpty(path))
                return isRelative ? "parentContainer" : "myDataStructure.Container";
            
            // For full path access (from DataStructure root), use PathGet
            if (!isRelative)
            {
                return $"myDataStructure.Container.PathGet<{GetCodeTypeName(type)}>(\"{path}\")";
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

        // Helper method to get the proper C# type name
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

        /// <summary>
        /// Marks the current DataStructure asset as dirty and saves it
        /// </summary>
        public void MarkDirty()
        {
            // Forward to ApplyStructuralChange for consistent behavior
            ApplyStructuralChange();
        }

        /// <summary>
        /// Helper method that allows container editors to select a property
        /// </summary>
        public void SelectPropertyInternal(string path, string key, Type type)
        {
            // This is called from the list and dictionary editors to select a property
            SelectProperty(path, key, type);
        }

        #region Utility Methods

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

        // Helper method to create default values for types
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

        internal void ShowDefaultDetails()
        {
            _detailsContent.Clear();
            _defaultMessageLabel.style.display = DisplayStyle.Flex;
        }

        private string GetLastPathComponent(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "Root";
                
            int lastDotIndex = path.LastIndexOf('.');
            return lastDotIndex >= 0 ? path.Substring(lastDotIndex + 1) : path;
        }

        private void OnWindowLostFocus(FocusOutEvent evt)
        {
            // Only show prompt if the asset is dirty
            if (_currentDataStructure != null && EditorUtility.IsDirty(_currentDataStructure))
            {
                // Show dialog asking if user wants to save
                bool shouldSave = EditorUtility.DisplayDialog(
                    "Save Changes?", 
                    $"Do you want to save changes to {_currentDataStructure.name}?", 
                    "Save", 
                    "Don't Save");
                    
                if (shouldSave)
                {
                    // Save the asset
                    AssetDatabase.SaveAssetIfDirty(_currentDataStructure);
                }
            }
        }

        /// <summary>
        /// Ensures that something is selected in the hierarchy
        /// If the given path exists, it will be selected
        /// Otherwise, it will fall back to the root
        /// </summary>
        public void EnsureSelection(string path)
        {
            GLog.Info<DataSystemEditorLogger>($"EnsureSelection called with path: '{path}'");
            
            // If we have a tree builder, check if the path exists in it
            if (_treeBuilder != null && _treeBuilder.TreeItems.ContainsKey(path))
            {
                // Path exists, select it
                var pathSegments = path.Split(new[] { '.', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                string key = pathSegments.Length > 0 ? pathSegments[pathSegments.Length - 1] : "Root";
                
                // Special handling for key that might have escaped quotes
                if (key.StartsWith("\"") && key.EndsWith("\""))
                {
                    key = key.Substring(1, key.Length - 2);
                }
                
                // Determine the correct type (important to ensure the right editor is shown)
                Type type = typeof(DataContainer); // Default type
                
                if (_currentDataStructure != null)
                {
                    // Different approaches based on whether the path is empty or not
                    if (string.IsNullOrEmpty(path))
                    {
                        // Root container
                        type = typeof(DataContainer);
                    }
                    else if (!path.Contains(".") && !path.Contains("["))
                    {
                        // Direct root property (might be a list or dictionary or container)
                        if (_currentDataStructure.Container.Contains(path))
                        {
                            type = _currentDataStructure.Container.GetValueType(path);
                            GLog.Info<DataSystemEditorLogger>($"Root property '{path}' has type: {type?.Name}");
                        }
                    }
                    else
                    {
                        // Try to get the parent container to determine the real type
                        int lastDotIndex = path.LastIndexOf('.');
                        int lastBracketIndex = path.LastIndexOf('[');
                        int lastSeparatorIndex = Math.Max(lastDotIndex, lastBracketIndex);
                        
                        if (lastSeparatorIndex > 0)
                        {
                            string parentPath = path.Substring(0, lastSeparatorIndex);
                            string itemKey = path.Substring(lastSeparatorIndex + 1);
                            
                            // Remove trailing bracket if present
                            if (itemKey.EndsWith("]"))
                            {
                                itemKey = itemKey.Substring(0, itemKey.Length - 1);
                            }
                            
                            // Remove quotes if present (for dictionary keys)
                            if (itemKey.StartsWith("\"") && itemKey.EndsWith("\""))
                            {
                                itemKey = itemKey.Substring(1, itemKey.Length - 2);
                            }
                            
                            GLog.Info<DataSystemEditorLogger>($"Checking parent path: '{parentPath}', item: '{itemKey}'");
                            
                            // Get parent container
                            DataContainer parentContainer = GetContainerAtPath(parentPath);
                            if (parentContainer != null && parentContainer.Contains(itemKey))
                            {
                                // Get actual type from parent container
                                type = parentContainer.GetValueType(itemKey);
                                GLog.Info<DataSystemEditorLogger>($"Item '{itemKey}' in parent has type: {type?.Name}");
                            }
                        }
                    }
                }
                
                // Select the property with the correct type
                GLog.Info<DataSystemEditorLogger>($"EnsureSelection: Selected path: '{path}', key: '{key}', with type: {type?.Name}");
                SelectProperty(path, key, type);
                return;
            }
            
            // Path doesn't exist or no tree builder, fall back to root
            SelectProperty("", "Root", typeof(DataContainer));
            GLog.Info<DataSystemEditorLogger>("EnsureSelection: Selected root container as fallback");
        }

        #endregion
    }
} 