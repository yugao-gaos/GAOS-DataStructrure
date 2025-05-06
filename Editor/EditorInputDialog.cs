using System;
using UnityEngine;
using UnityEditor;

namespace GAOS.DataStructure.Editor
{
    /// <summary>
    /// A modal dialog box for getting text input from the user
    /// </summary>
    public class EditorInputDialog : EditorWindow
    {
        private string _title;
        private string _message;
        private string _inputText;
        private Action<string> _onComplete;
        private bool _isCancelled;

        // Static callback for result handling
        private static Action<string> s_pendingCallback;

        /// <summary>
        /// Shows a modal dialog box with a text field
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Dialog message</param>
        /// <param name="defaultValue">Default value for the text field</param>
        /// <param name="callback">Callback that receives the entered text when dialog is closed</param>
        public static void Show(string title, string message, string defaultValue, Action<string> callback)
        {
            // Store the callback for async completion
            s_pendingCallback = callback;

            var window = CreateInstance<EditorInputDialog>();
            window.titleContent = new GUIContent(title);
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 100);
            window._title = title;
            window._message = message;
            window._inputText = defaultValue;
            window._onComplete = value => {
                // Pass the result to the static callback
                s_pendingCallback?.Invoke(value);
                s_pendingCallback = null;
            };
            window.ShowModal();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(_message);
            GUI.SetNextControlName("InputField");
            _inputText = EditorGUILayout.TextField(_inputText);
            
            // Focus the text field by default
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.FocusTextInControl("InputField");
            }
            
            // Handle key events
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    Complete();
                }
                else if (Event.current.keyCode == KeyCode.Escape)
                {
                    Cancel();
                }
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                Complete();
            }
            if (GUILayout.Button("Cancel"))
            {
                Cancel();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void Complete()
        {
            _onComplete?.Invoke(_inputText);
            Close();
        }

        private void Cancel()
        {
            _isCancelled = true;
            _onComplete?.Invoke(null);
            Close();
        }
    }
} 