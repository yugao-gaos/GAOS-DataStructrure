using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using GAOS.Logger;

namespace GAOS.DataStructure.Editor
{
    /// <summary>
    /// Property editor for enum values in the DataStructure editor.
    /// </summary>
    public class EnumEditor : IPropertyEditor
    {
        /// <summary>
        /// Check if this editor can handle the provided type.
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if the type is an enum and is registered in EnumRegistry</returns>
        public bool CanHandleType(Type type)
        {
            // Check if the type is an enum and is registered
            return type != null && type.IsEnum && EnumRegistry.IsEnumRegistered(type);
        }

        /// <summary>
        /// Check if an enum type is marked with the [Flags] attribute
        /// </summary>
        private bool IsFlagsEnum(Type enumType)
        {
            return enumType.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;
        }

        /// <summary>
        /// Create a UI field for editing an enum value.
        /// </summary>
        /// <param name="type">The enum type</param>
        /// <param name="value">Current enum value</param>
        /// <param name="onValueChanged">Callback when value changes</param>
        /// <returns>VisualElement containing the enum editor</returns>
        public VisualElement CreateEditorField(Type type, object value, Action<object> onValueChanged)
        {
            var container = new VisualElement();
            
            try
            {
                // Create a horizontal layout for the label and field
                var fieldRow = new VisualElement();
                fieldRow.style.flexDirection = FlexDirection.Row;
                fieldRow.style.alignItems = Align.Center;
                
                // Add the "Value" label
                var valueLabel = new Label("Value:");
                valueLabel.style.width = 50;
                valueLabel.style.marginRight = 5;
                valueLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
                fieldRow.Add(valueLabel);
                
                // Convert value to the correct enum type or use first value if null/invalid
                Enum enumValue;
                if (value != null && value.GetType() == type)
                {
                    enumValue = (Enum)value;
                }
                else
                {
                    // Get first value as default
                    Array enumValues = Enum.GetValues(type);
                    enumValue = (Enum)enumValues.GetValue(0);
                }
                
                // Check if this is a flags enum
                bool isFlags = IsFlagsEnum(type);
                
                if (isFlags)
                {
                    // For flags enums, use a MaskField
                    var intValue = Convert.ToInt32(enumValue);
                    var maskField = new MaskField(new List<string>(GetEnumDisplayNames(type)), intValue);
                    maskField.style.flexGrow = 1;
                    
                    // Register for value change events
                    maskField.RegisterValueChangedCallback(evt => {
                        // Convert int mask back to enum
                        var newEnumValue = Enum.ToObject(type, evt.newValue);
                        onValueChanged?.Invoke(newEnumValue);
                    });
                    
                    fieldRow.Add(maskField);
                }
                else
                {
                    // For regular enums, use an EnumField
                    var enumField = new EnumField(enumValue);
                    enumField.style.flexGrow = 1;
                    
                    // Register for value change events
                    enumField.RegisterValueChangedCallback(evt => {
                        onValueChanged?.Invoke(evt.newValue);
                    });
                    
                    fieldRow.Add(enumField);
                }
                
                container.Add(fieldRow);
                
                // Add extra info about the enum type for clarity
                var typeInfo = new Label($"Enum Type: {type.Name}" + (isFlags ? " [Flags]" : ""));
                typeInfo.style.fontSize = 10;
                typeInfo.style.color = new UnityEngine.Color(0.7f, 0.7f, 0.7f);
                typeInfo.style.marginTop = 4;
                container.Add(typeInfo);
            }
            catch (Exception ex)
            {
                // Add error message if enum field creation fails
                var errorLabel = new Label($"Error creating enum editor: {ex.Message}");
                errorLabel.style.color = new UnityEngine.Color(1, 0, 0);
                container.Add(errorLabel);
                
                // Log the error
                GLog.Error<DataSystemEditorLogger>($"Error creating enum editor for {type?.Name}: {ex.Message}");
            }
            
            return container;
        }
        
        /// <summary>
        /// Get array of display names for the enum
        /// </summary>
        private string[] GetEnumDisplayNames(Type enumType)
        {
            Array enumValues = Enum.GetValues(enumType);
            string[] names = new string[enumValues.Length];
            
            for (int i = 0; i < enumValues.Length; i++)
            {
                names[i] = enumValues.GetValue(i).ToString();
            }
            
            return names;
        }
    }
} 