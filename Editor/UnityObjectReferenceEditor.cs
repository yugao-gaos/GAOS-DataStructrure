using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using GAOS.DataStructure.References;

namespace GAOS.DataStructure.Editor
{
    /// <summary>
    /// Editor for handling UnityObjectReference types
    /// </summary>
    public class UnityObjectReferenceEditor : IPropertyEditor
    {
        /// <summary>
        /// Checks if this editor can handle the specified type
        /// </summary>
        public bool CanHandleType(Type type)
        {
            return type == typeof(UnityObjectReference);
        }

        /// <summary>
        /// Creates an editor field for UnityObjectReference type
        /// </summary>
        public VisualElement CreateEditorField(Type type, object value, Action<object> onValueChanged)
        {
            if (type != typeof(UnityObjectReference))
            {
                Debug.LogError($"UnityObjectReferenceEditor cannot handle type {type.Name}");
                return new Label($"Unsupported type: {type.Name}");
            }

            var objRef = value as UnityObjectReference;
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            
            // Create a composite field with storage type dropdown and object field
            var referenceContainer = new VisualElement();
            referenceContainer.style.flexDirection = FlexDirection.Row;
            
            // Storage type dropdown
            var storageTypeField = new EnumField("Storage Type", ReferenceStorageType.Registry);
            storageTypeField.style.flexGrow = 1;
            storageTypeField.style.marginRight = 5;
            
            // Add to container
            referenceContainer.Add(storageTypeField);
            container.Add(referenceContainer);
            
            // Object field container with clear button
            var objectFieldContainer = new VisualElement();
            objectFieldContainer.style.flexDirection = FlexDirection.Row;
            objectFieldContainer.style.marginBottom = 5;
            
            // Reference key field - moved up before it's used in clear button
            var keyField = new TextField("Reference Key");
            
            // Object reference field
            var objectField = new ObjectField();
            objectField.style.flexGrow = 1;
            objectField.label = "Reference";
            objectField.objectType = typeof(UnityEngine.Object);
            objectFieldContainer.Add(objectField);
            
            // Add clear button
            var clearButton = new Button(() => {
                // Set the field to null
                objectField.value = null;
                
                // Set default key if clearing
                keyField.value = "Default";
                
                // Force immediate update
                UpdateUnityObjectReference();
                
                // Ensure it stays null after the update
                EditorApplication.delayCall += () => {
                    objectField.SetValueWithoutNotify(null);
                };
            });
            clearButton.text = "Clear";
            clearButton.style.marginLeft = 5;
            clearButton.style.width = 50;
            objectFieldContainer.Add(clearButton);
            
            container.Add(objectFieldContainer);
            
            // Add the key field to the container
            container.Add(keyField);
            
            // Initialize values if we have existing reference
            if (objRef != null)
            {
                storageTypeField.value = objRef.StorageType;
                keyField.value = objRef.Key;
                
                // Try to get the referenced object
                var referencedObject = objRef.GetObject();
                if (referencedObject != null)
                {
                    objectField.value = referencedObject;
                    
                    // Set object type if available
                    var objType = objRef.GetObjectType();
                    if (objType != null && typeof(UnityEngine.Object).IsAssignableFrom(objType))
                    {
                        objectField.objectType = objType;
                    }
                }
            }
            
            // Handle changes to storage type
            storageTypeField.RegisterValueChangedCallback(evt => 
            {
                var newStorageType = (ReferenceStorageType)evt.newValue;
                
                // Update field visibility based on storage type
                if (newStorageType == ReferenceStorageType.Registry)
                {
                    objectField.style.display = DisplayStyle.Flex;
                    
                    // If we have an object, use its name as the key
                    if (objectField.value != null && string.IsNullOrEmpty(keyField.value))
                    {
                        keyField.value = objectField.value.name;
                    }
                }
                else if (newStorageType == ReferenceStorageType.Resources)
                {
                    objectField.style.display = DisplayStyle.Flex;
                    
                    // For Resources, we should show a path field
                    keyField.label = "Resource Path";
                }
                else if (newStorageType == ReferenceStorageType.Addressable)
                {
                    objectField.style.display = DisplayStyle.Flex;
                    
                    // For Addressables, we should show an address field
                    keyField.label = "Addressable Address";
                }
                
                // Small delay before updating to allow for input processing
                EditorApplication.delayCall += () => {
                    // Create a new reference with updated values
                    UpdateUnityObjectReference();
                };
            });
            
            // Handle changes to the object reference
            objectField.RegisterValueChangedCallback(evt => 
            {
                var newObject = evt.newValue as UnityEngine.Object;
                
                Debug.Log($"Object field changed: {(evt.previousValue != null ? evt.previousValue.name : "null")} -> {(newObject != null ? newObject.name : "null")}");
                
                // Special handling for null (deletion)
                if (newObject == null && evt.previousValue != null)
                {
                    Debug.Log("Object was explicitly set to null - this is a deletion operation");
                    
                    // If object was set to null, we need to ensure it stays null
                    objectField.SetValueWithoutNotify(null);
                    
                    // Small delay before updating to allow for input processing
                    EditorApplication.delayCall += () => 
                    {
                        // Double-check to ensure the field is still null
                        objectField.SetValueWithoutNotify(null);
                        
                        // Create a new reference with updated values
                        UpdateUnityObjectReference();
                        
                        // Force the UI to refresh with null value
                        EditorApplication.delayCall += () => 
                        {
                            objectField.SetValueWithoutNotify(null);
                        };
                    };
                    
                    return;
                }
                
                // If object changed, update key based on storage type
                if (newObject != null)
                {
                    if ((ReferenceStorageType)storageTypeField.value == ReferenceStorageType.Registry)
                    {
                        // For Registry, use object name as key
                        keyField.value = newObject.name;
                    }
                }
                
                // Small delay before updating to allow for input processing
                EditorApplication.delayCall += () => {
                    // Create a new reference with updated values
                    UpdateUnityObjectReference();
                };
            });
            
            // Handle changes to the key - update only when focus is lost or Enter is pressed
            keyField.RegisterCallback<FocusOutEvent>(evt => {
                // Create a new reference with updated values
                UpdateUnityObjectReference();
            });
            
            // Add support for Enter key in key field
            keyField.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    keyField.Blur(); // Remove focus to trigger update
                    evt.StopPropagation();
                }
            });
            
            // Helper method to create a new reference and notify of changes
            void UpdateUnityObjectReference()
            {
                try
                {
                    var storageType = (ReferenceStorageType)storageTypeField.value;
                    var key = keyField.value;
                    var obj = objectField.value;
                    
                    // Debug output to check values
                    Debug.Log($"Updating UnityObjectReference: StorageType={storageType}, Key={key}, Object={(obj != null ? obj.name : "null")}");
                    
                    // Make sure we have a key
                    if (string.IsNullOrEmpty(key))
                    {
                        if (obj != null)
                        {
                            key = obj.name;
                        }
                        else
                        {
                            key = "Default";
                        }
                        
                        keyField.value = key;
                    }
                    
                    UnityObjectReference newRef;
                    
                    if (obj != null)
                    {
                        // We have an object, so use the constructor with object
                        newRef = new UnityObjectReference(obj, storageType, key);
                    }
                    else
                    {
                        // No object - check if this is an intentional null (deletion) or initial setup
                        if (objRef != null && objectField.value == null)
                        {
                            Debug.Log("Reference was set to null - creating a default reference with null object");
                            
                            // Get the existing type if available
                            Type objectType = typeof(UnityEngine.Object);
                            var existingType = objRef.GetObjectType();
                            if (existingType != null)
                            {
                                objectType = existingType;
                            }
                            
                            // Create a reference with null object but preserved type
                            newRef = new UnityObjectReference(storageType, key, objectType.AssemblyQualifiedName);
                            
                            // Ensure field stays null
                            objectField.SetValueWithoutNotify(null);
                        }
                        else
                        {
                            // Standard null case - use the constructor with type
                            Type objectType = typeof(UnityEngine.Object);
                            if (objRef != null)
                            {
                                // Try to preserve the original type
                                var existingType = objRef.GetObjectType();
                                if (existingType != null)
                                {
                                    objectType = existingType;
                                }
                            }
                            
                            newRef = new UnityObjectReference(storageType, key, objectType.AssemblyQualifiedName);
                        }
                    }
                    
                    // Notify of change and ensure it's saved
                    onValueChanged(newRef);
                    
                    // Explicitly mark the asset dirty
                    var activeObject = Selection.activeObject;
                    if (activeObject != null)
                    {
                        EditorUtility.SetDirty(activeObject);
                        AssetDatabase.SaveAssetIfDirty(activeObject);
                        Debug.Log($"Marked asset dirty and saved: {activeObject.name}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error updating Unity Object Reference: {ex.Message}");
                }
            }
            
            return container;
        }
    }
} 