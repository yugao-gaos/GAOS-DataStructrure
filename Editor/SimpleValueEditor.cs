using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using GAOS.Logger;
using GAOS.DataStructure.Editor;

namespace GAOS.DataStructure.Editor
{
    /// <summary>
    /// Editor for handling simple value types like string, int, float, bool, Vector2, Vector3, and Color
    /// </summary>
    public class SimpleValueEditor : IPropertyEditor
    {
        /// <summary>
        /// Checks if this editor can handle the specified type
        /// </summary>
        public bool CanHandleType(Type type)
        {
            return type == typeof(string) ||
                   type == typeof(int) ||
                   type == typeof(float) ||
                   type == typeof(bool) ||
                   type == typeof(Vector2) ||
                   type == typeof(Vector3) ||
                   type == typeof(Color);
        }

        /// <summary>
        /// Creates an editor field for the specified simple value type
        /// </summary>
        public VisualElement CreateEditorField(Type type, object value, Action<object> onValueChanged)
        {
            // Create container to hold the label and field
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;

            // Add the Value label
            var label = new Label("Value");
            label.style.minWidth = 80;
            label.style.alignSelf = Align.Center;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(label);

            if (type == typeof(string))
            {
                var field = new TextField();
                field.style.flexGrow = 1;
                field.value = (string)value ?? "";
                
                // Instead of updating on every keystroke, update only when focus is lost or Enter is pressed
                field.RegisterCallback<FocusOutEvent>(evt => {
                    onValueChanged(field.value);
                });
                
                // Register for key down event to detect Enter key
                field.RegisterCallback<KeyDownEvent>(evt => {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        field.Blur(); // Remove focus to trigger update
                        evt.StopPropagation();
                    }
                });
                
                container.Add(field);
                return container;
            }
            else if (type == typeof(int))
            {
                var field = new IntegerField();
                field.style.flexGrow = 1;
                field.value = value != null ? (int)value : 0;
                
                // Update only on focus lost or Enter key
                field.RegisterCallback<FocusOutEvent>(evt => {
                    onValueChanged(field.value);
                });
                
                field.RegisterCallback<KeyDownEvent>(evt => {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        field.Blur(); // Remove focus to trigger update
                        evt.StopPropagation();
                    }
                });
                
                container.Add(field);
                return container;
            }
            else if (type == typeof(float))
            {
                var field = new FloatField();
                field.style.flexGrow = 1;
                field.value = value != null ? (float)value : 0f;
                
                // Update only on focus lost or Enter key
                field.RegisterCallback<FocusOutEvent>(evt => {
                    onValueChanged(field.value);
                });
                
                field.RegisterCallback<KeyDownEvent>(evt => {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        field.Blur(); // Remove focus to trigger update
                        evt.StopPropagation();
                    }
                });
                
                container.Add(field);
                return container;
            }
            else if (type == typeof(bool))
            {
                var field = new Toggle();
                field.style.flexGrow = 1;
                field.value = value != null && (bool)value;
                
                // Toggle is an immediate action so we can keep the original behavior
                field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
                
                container.Add(field);
                return container;
            }
            else if (type == typeof(Vector2))
            {
                var field = new Vector2Field();
                field.style.flexGrow = 1;
                field.value = value != null ? (Vector2)value : Vector2.zero;
                
                // Update only on focus lost or Enter key
                field.RegisterCallback<FocusOutEvent>(evt => {
                    onValueChanged(field.value);
                });
                
                // Add enter key support in vector fields
                field.Q<FloatField>()?.RegisterCallback<KeyDownEvent>(evt => {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        field.Blur(); // Remove focus to trigger update
                        evt.StopPropagation();
                    }
                });
                
                container.Add(field);
                return container;
            }
            else if (type == typeof(Vector3))
            {
                var field = new Vector3Field();
                field.style.flexGrow = 1;
                field.value = value != null ? (Vector3)value : Vector3.zero;
                
                // Update only on focus lost or Enter key
                field.RegisterCallback<FocusOutEvent>(evt => {
                    onValueChanged(field.value);
                });
                
                // Add enter key support in vector fields
                field.Q<FloatField>()?.RegisterCallback<KeyDownEvent>(evt => {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        field.Blur(); // Remove focus to trigger update
                        evt.StopPropagation();
                    }
                });
                
                container.Add(field);
                return container;
            }
            else if (type == typeof(Color))
            {
                var field = new ColorField();
                field.style.flexGrow = 1;
                field.value = value != null ? (Color)value : Color.white;
                
                // Color field interaction is more explicit, so we can keep this immediate
                field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
                
                container.Add(field);
                return container;
            }
            
            // Should never reach here due to CanHandleType check
            GLog.Error<DataSystemEditorLogger>($"SimpleValueEditor cannot handle type {type.Name}");
            return new Label($"Unsupported type: {type.Name}");
        }
    }
} 