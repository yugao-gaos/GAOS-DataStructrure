# Implementation Plan: Supporting List<UnityObjectReference> and OrderedDictionary<string, UnityObjectReference>

## Overview

This document outlines the implementation plan for adding support for two new data structures in the GAOS DataStructure system:
1. `List<UnityObjectReference>`
2. `OrderedDictionary<string, UnityObjectReference>`

These structures will allow for collections of UnityEngine.Object references to be stored and managed within the DataContainer system, with full editor support.

## Implementation Steps

### 1. Update Container Editor Types (ContainerEditor.cs)

- Add new type options to the dropdown menu in `CreateContainerEditor` method:
  ```csharp
  // Add type options
  List<string> typeOptions = new List<string> {
      "Select type...",
      "String",
      // ... existing types ...
      "List<UnityObjectReference>",
      "OrderedDictionary<string, UnityObjectReference>"
  };
  ```

- Update the `GetTypeFromSelection` method to return these new types:
  ```csharp
  private Type GetTypeFromSelection(int index)
  {
      switch (index)
      {
          // ... existing cases ...
          case 12: return typeof(List<UnityObjectReference>);
          case 13: return typeof(OrderedDictionary<string, UnityObjectReference>);
          default: return null;
      }
  }
  ```

- Update the value preview handling in `GetValuePreview` to show these collection types properly:
  ```csharp
  else if (type == typeof(List<UnityObjectReference>))
  {
      if (container.TryGet<List<UnityObjectReference>>(key, out var list))
      {
          return $"List<UnityObjectReference> with {list?.Count ?? 0} items";
      }
      return "(invalid list)";
  }
  else if (type == typeof(OrderedDictionary<string, UnityObjectReference>))
  {
      if (container.TryGet<OrderedDictionary<string, UnityObjectReference>>(key, out var dict))
      {
          return $"Dictionary<String, UnityObjectReference> with {dict?.Count ?? 0} items";
      }
      return "(invalid ordered dictionary)";
  }
  ```

### 2. Create UnityObjectReferenceListEditor

Create a new class `UnityObjectReferenceListEditor.cs` in the Editor folder:

```csharp
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GAOS.DataStructure.References;

namespace GAOS.DataStructure.Editor
{
    public class UnityObjectReferenceListEditor : ContainerEditorBase, IPropertyEditor
    {
        public UnityObjectReferenceListEditor(DataStructureEditorWindow editorWindow) : base(editorWindow)
        {
        }

        public override bool CanHandleType(Type type)
        {
            return type == typeof(List<UnityObjectReference>);
        }

        public override VisualElement CreateEditorField(Type type, object value, Action<object> onValueChanged)
        {
            // Implementation similar to ContainerListEditor but for UnityObjectReference
            // ...
        }

        // Additional methods for handling list items
        // ...
    }
}
```

### 3. Create UnityObjectReferenceDictionaryEditor

Create a new class `UnityObjectReferenceDictionaryEditor.cs` in the Editor folder:

```csharp
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GAOS.DataStructure.References;

namespace GAOS.DataStructure.Editor
{
    public class UnityObjectReferenceDictionaryEditor : ContainerEditorBase, IPropertyEditor
    {
        public UnityObjectReferenceDictionaryEditor(DataStructureEditorWindow editorWindow) : base(editorWindow)
        {
        }

        public override bool CanHandleType(Type type)
        {
            return type == typeof(OrderedDictionary<string, UnityObjectReference>);
        }

        public override VisualElement CreateEditorField(Type type, object value, Action<object> onValueChanged)
        {
            // Implementation similar to ContainerDictionaryEditor but for UnityObjectReference
            // ...
        }

        // Additional methods for handling dictionary items
        // ...
    }
}
```

### 4. Update DataStructureEditorWindow Registration

Modify `DataStructureEditorWindow.cs` to register and use the new editors:

- Add new editor fields:
```csharp
private IPropertyEditor _unityObjectReferenceListEditor;
private IPropertyEditor _unityObjectReferenceDictionaryEditor;
```

- Initialize these editors in `CreateGUI()`:
```csharp
_unityObjectReferenceListEditor = new UnityObjectReferenceListEditor(this);
_unityObjectReferenceDictionaryEditor = new UnityObjectReferenceDictionaryEditor(this);
```

- Update the `ShowPropertyDetails` method to include these editors in the editor selection:
```csharp
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
        _containerDictionaryEditor,
        _unityObjectReferenceListEditor,
        _unityObjectReferenceDictionaryEditor
    })
    {
        // ... existing code ...
    }
}
```

### 5. Update GetTypeName Method for UI Display

Update the `GetTypeName` method in `DataStructureEditorWindow.cs` to properly display the new types:

```csharp
private string GetTypeName(Type type)
{
    // ... existing code ...
    if (type == typeof(List<UnityObjectReference>))
        return "Unity Object Reference List";
    if (type == typeof(OrderedDictionary<string, UnityObjectReference>))
        return "Dictionary<String, Unity Object Reference>";
    
    return type.Name;
}
```

### 6. Update DataInstanceInspector for Tree View

Modify `DataInstanceInspector.cs` to properly display and handle the new types in the hierarchy:

- Update the `BuildContainerTree` method to handle these new types:
```csharp
else if (valueType == typeof(List<UnityObjectReference>))
{
    // Simple list (non-container elements)
    CreateBasicTreeItem(parent, key, path, valueType, isOverridden);
}
else if (valueType == typeof(OrderedDictionary<string, UnityObjectReference>))
{
    // Simple OrderedDictionary (non-container values)
    CreateBasicTreeItem(parent, key, path, valueType, isOverridden);
}
```

- Update the `GetTypeName` method to display these types properly:
```csharp
private string GetTypeName(Type type)
{
    // ... existing code ...
    if (type == typeof(List<UnityObjectReference>))
        return "Unity Object Reference List";
    if (type == typeof(OrderedDictionary<string, UnityObjectReference>))
        return "Dictionary<String, Unity Object Reference>";
    
    return type.Name;
}
```

### 7. Test and Verify Integration

1. Add a `List<UnityObjectReference>` property to a DataContainer via the editor
2. Verify that the property can be edited in the inspector
3. Add and remove items from the list
4. Test similar operations with `OrderedDictionary<string, UnityObjectReference>`
5. Test overrides in DataInstance for both collection types

## Potential Challenges

1. **Serialization**: Unity's serialization has limitations with generic collections. Ensure that the existing serialization helpers can properly handle these types.

2. **UI Performance**: For large collections, the UI might become slow. Consider implementing virtualization for list views if this becomes an issue.

3. **Drag and Drop**: Implementing proper drag and drop for UnityEngine.Objects might require additional work to properly handle reference tracking.

4. **Undo Support**: Ensure that all operations support Unity's undo system for a better user experience.

## Conclusion

This implementation will extend the DataStructure system's capabilities to handle collections of Unity object references, making it more powerful and flexible for game development use cases. 