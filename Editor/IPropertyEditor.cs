using System;
using UnityEngine;
using UnityEngine.UIElements;
using GAOS.Logger;
using GAOS.DataStructure.Editor;

namespace GAOS.DataStructure.Editor
{
    /// <summary>
    /// Interface for all property editors in the DataStructure system
    /// </summary>
    public interface IPropertyEditor
    {
        /// <summary>
        /// Determines if this editor can handle the specified type
        /// </summary>
        bool CanHandleType(Type type);
        
        /// <summary>
        /// Creates an editor field for the specified type and value
        /// </summary>
        VisualElement CreateEditorField(Type type, object value, Action<object> onValueChanged);
    }
} 