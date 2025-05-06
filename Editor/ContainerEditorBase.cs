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
    /// Base class for all container-related property editors
    /// </summary>
    public abstract class ContainerEditorBase : IPropertyEditor
    {
        // Reference to the main editor window
        protected DataStructureEditorWindow _editorWindow;

        protected ContainerEditorBase(DataStructureEditorWindow editorWindow)
        {
            _editorWindow = editorWindow;
        }

        /// <summary>
        /// Determines if this editor can handle the specified type
        /// </summary>
        public abstract bool CanHandleType(Type type);

        /// <summary>
        /// Creates an editor field for the specified type
        /// </summary>
        public abstract VisualElement CreateEditorField(Type type, object value, Action<object> onValueChanged);

        #region UI Construction Helpers

        /// <summary>
        /// Creates a standard table header with columns
        /// </summary>
        /// <param name="includesDragHandle">Whether to include a drag handle column</param>
        /// <param name="columns">Column definitions (label, width, isFlexible)</param>
        protected VisualElement CreateTableHeader(bool includesDragHandle, params (string Label, float Width, bool IsFlexible)[] columns)
        {
            var header = new VisualElement();
            header.AddToClassList("data-entry-header-row");
            header.style.height = 24;
            header.style.alignItems = Align.Center;

            // Add drag handle column if needed
            if (includesDragHandle)
            {
                var dragHandleHeader = new VisualElement();
                dragHandleHeader.style.width = 24;
                header.Add(dragHandleHeader);
            }

            // Add columns
            foreach (var column in columns)
            {
                var label = new Label(column.Label);
                
                if (column.IsFlexible)
                {
                    label.style.flexGrow = 1;
                }
                else
                {
                    label.style.width = column.Width;
                    label.style.flexShrink = 0;
                }
                
                header.Add(label);
            }

            return header;
        }

        /// <summary>
        /// Creates a standard row with consistent styling
        /// </summary>
        protected VisualElement CreateStandardRow()
        {
            var row = new VisualElement();
            row.AddToClassList("data-entry-row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.minHeight = 28;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;
            row.style.marginBottom = 2;
            row.style.alignItems = Align.Center;
            return row;
        }

        /// <summary>
        /// Creates a drag handle for a row
        /// </summary>
        protected VisualElement CreateDragHandle(string identifier)
        {
            var dragHandle = new VisualElement();
            dragHandle.AddToClassList("drag-handle");
            dragHandle.tooltip = "Drag to reorder";
            dragHandle.name = $"DragHandle-{identifier}";
            
            // Create hamburger icon for the handle
            var dragIcon = new Label();
            dragIcon.text = "≡";
            dragIcon.style.fontSize = 14;
            dragIcon.style.unityTextAlign = TextAnchor.MiddleCenter;
            dragIcon.style.color = new Color(0.8f, 0.8f, 0.8f, 1);
            dragHandle.Add(dragIcon);
            
            return dragHandle;
        }

        /// <summary>
        /// Creates an action button with an icon
        /// </summary>
        protected Button CreateIconButton(string icon, string tooltip, Action onClick, float width = 24, float height = 22, float marginRight = 4)
        {
            var button = new Button(onClick);
            button.text = icon;
            button.AddToClassList("icon-button");
            button.tooltip = tooltip;
            button.style.width = width;
            button.style.height = height;
            
            if (marginRight > 0)
            {
                button.style.marginRight = marginRight;
            }
            
            return button;
        }

        /// <summary>
        /// Creates a container for action buttons with standard styling
        /// </summary>
        protected VisualElement CreateActionsContainer(float width = 80)
        {
            var container = new VisualElement();
            container.AddToClassList("data-entry-actions");
            container.style.flexDirection = FlexDirection.Row;
            container.style.width = width;
            container.style.flexShrink = 0;
            return container;
        }

        #endregion

        #region Hierarchy Refresh Methods

        /// <summary>
        /// Refreshes the hierarchy tree in the editor window
        /// </summary>
        protected void RefreshHierarchy()
        {
            if (_editorWindow != null)
            {
                _editorWindow.RefreshHierarchyTree();
            }
        }

        /// <summary>
        /// Updates the hierarchy and marks asset as dirty
        /// To be called after any structural changes like reordering
        /// </summary>
        protected void ApplyStructuralChange()
        {
            // Store current path before refreshing
            string currentPath = _editorWindow?.CurrentPath ?? "";
            
            // Debug current path and editor type to help diagnose selection issues
            Debug.Log($"ApplyStructuralChange: About to refresh hierarchy. CurrentPath='{currentPath}', Editor={GetType().Name}");
            
            // Mark asset dirty first
            MarkDirty();
            
            // Then refresh the hierarchy view
            RefreshHierarchy();
            
            // Restore selection after hierarchy is refreshed
            if (_editorWindow != null)
            {
                // If current path is empty, select root
                if (string.IsNullOrEmpty(currentPath))
                {
                    Debug.Log("ApplyStructuralChange: Selecting root container");
                    _editorWindow.SelectProperty("", "Root", typeof(DataContainer));
                }
                else
                {
                    // Try to reselect the previous path
                    Debug.Log($"ApplyStructuralChange: Restoring selection to path '{currentPath}'");
                    _editorWindow.EnsureSelection(currentPath);
                }
            }
        }

        #endregion

        #region Drag and Drop Functionality

        /// <summary>
        /// Sets up drag functionality for reordering
        /// </summary>
        protected void EnableDragForHandle(VisualElement handle, VisualElement row, Action<VisualElement, DragState> onDragComplete)
        {
            handle.RegisterCallback<MouseDownEvent>(evt => {
                if (evt.button == 0) // Left mouse button
                {
                    // Get the parent container that holds all rows
                    var parentContainer = row.parent;
                    if (parentContainer == null) 
                    {
                        Debug.LogWarning("Parent container is null, cannot start drag");
                        return;
                    }
                    
                    // Initialize drag
                    handle.AddToClassList("dragging");
                    row.AddToClassList("dragging");
                    
                    // Capture current mouse position for drag delta calculations
                    Vector2 startMousePosition = evt.mousePosition;
                    int startIndex = parentContainer.IndexOf(row);
                    
                    // Store data for use during drag
                    handle.userData = new DragState { 
                        StartMousePosition = startMousePosition,
                        StartIndex = startIndex,
                        CurrentIndex = startIndex,
                        ParentContainer = parentContainer,
                        Row = row
                    };
                    
                    // Capture the mouse moves during drag
                    handle.CaptureMouse();
                    evt.StopPropagation();
                }
            });
            
            handle.RegisterCallback<MouseMoveEvent>(evt => {
                if (handle.HasMouseCapture() && handle.userData is DragState state)
                {
                    // Get distance moved
                    float deltaY = evt.mousePosition.y - state.StartMousePosition.y;
                    
                    // Get the new index based on mouse position
                    int rowCount = state.ParentContainer.childCount;
                    float rowHeight = row.layout.height;
                    int rowsToMove = Mathf.RoundToInt(deltaY / rowHeight);
                    
                    int newIndex = Mathf.Clamp(state.StartIndex + rowsToMove, 0, rowCount - 1);
                    if (newIndex != state.CurrentIndex)
                    {
                        // Move the row in the UI only - don't apply changes yet
                        state.ParentContainer.RemoveAt(state.ParentContainer.IndexOf(row));
                        state.ParentContainer.Insert(newIndex, row);
                        state.CurrentIndex = newIndex;
                    }
                    
                    evt.StopPropagation();
                }
            });
            
            handle.RegisterCallback<MouseUpEvent>(evt => {
                if (handle.HasMouseCapture())
                {
                    handle.RemoveFromClassList("dragging");
                    row.RemoveFromClassList("dragging");
                    handle.ReleaseMouse();
                    
                    // Final reordering
                    if (handle.userData is DragState state)
                    {
                        // Only apply reordering if we've actually moved
                        if (state.StartIndex != state.CurrentIndex)
                        {
                            onDragComplete?.Invoke(row, state);
                            
                            // Refresh the hierarchy after reordering is complete
                            RefreshHierarchy();
                        }
                    }
                    
                    evt.StopPropagation();
                }
            });
        }

        /// <summary>
        /// Data class for drag operations
        /// </summary>
        protected class DragState
        {
            public Vector2 StartMousePosition;
            public int StartIndex;
            public int CurrentIndex;
            public VisualElement ParentContainer;
            public VisualElement Row;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Creates a default value for a type
        /// </summary>
        protected object CreateDefaultValue(Type type)
        {
            if (type == null)
                return null;
                
            if (type == typeof(string))
                return string.Empty;
                
            if (type == typeof(UnityObjectReference))
            {
                return new UnityObjectReference(
                    ReferenceStorageType.Registry,
                    "Default",
                    typeof(UnityEngine.GameObject).AssemblyQualifiedName
                );
            }
                
            if (type == typeof(DataContainer))
            {
                return new DataContainer();
            }
            
            if (type == typeof(OrderedDictionary<string, DataContainer>))
            {
                return new OrderedDictionary<string, DataContainer>();
            }
            
            if (type == typeof(List<DataContainer>))
            {
                return new List<DataContainer>();
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

        /// <summary>
        /// Gets a readable type name
        /// </summary>
        protected string GetTypeName(Type type)
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
        /// Gets a preview of a value for containers and collections
        /// </summary>
        protected string GetCollectionPreview(DataContainer container, int count, string itemType)
        {
            return $"Container with {count} {itemType}";
        }

        /// <summary>
        /// Marks the asset dirty through the editor window
        /// </summary>
        protected void MarkDirty()
        {
            _editorWindow?.MarkDirty();
        }

        #endregion
        
        #region Row Data Structure
        
        /// <summary>
        /// Base class for row data used in drag operations
        /// Can be extended by derived editors for specific needs
        /// </summary>
        protected class BaseRowData
        {
            public string Key { get; set; }
            public Type Type { get; set; }
            public int Index { get; set; } 
            public DataContainer Container { get; set; }
        }
        
        #endregion
    }
} 