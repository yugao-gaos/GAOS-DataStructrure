using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GAOS.Logger;

namespace GAOS.DataStructure.Editor.Codegen
{
    public abstract class CodeGeneratorBase
    {
        protected readonly StringBuilder _builder = new StringBuilder();
        protected readonly HashSet<string> _addedUsings = new HashSet<string>();

        protected void AddUsing(string ns)
        {
            if (_addedUsings.Add(ns))
                _builder.AppendLine($"using {ns};");
        }

        protected void Log(string message)
        {
            GLog.Info<DataSystemEditorLogger>(message);
        }
        protected void LogWarning(string message)
        {
            GLog.Warning<DataSystemEditorLogger>(message);
        }
        protected void LogError(string message)
        {
            GLog.Error<DataSystemEditorLogger>(message);
        }

        protected string GetTypeName(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(double)) return "double";
            if (type == typeof(long)) return "long";
            if (type == typeof(short)) return "short";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(char)) return "char";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(UnityEngine.Vector2)) return "Vector2";
            if (type == typeof(UnityEngine.Vector3)) return "Vector3";
            if (type == typeof(UnityEngine.Vector4)) return "Vector4";
            if (type == typeof(UnityEngine.Quaternion)) return "Quaternion";
            if (type == typeof(UnityEngine.Color)) return "Color";
            if (type == typeof(UnityEngine.Color32)) return "Color32";
            if (type == typeof(UnityEngine.Rect)) return "Rect";
            if (type == typeof(UnityEngine.Bounds)) return "Bounds";
            if (type == typeof(UnityEngine.Matrix4x4)) return "Matrix4x4";
            if (type == typeof(UnityEngine.AnimationCurve)) return "AnimationCurve";
            if (type == typeof(UnityEngine.Gradient)) return "Gradient";
            if (type == typeof(UnityEngine.Object)) return "UnityEngine.Object";
            if (type == typeof(GAOS.DataStructure.DataContainer)) return "DataContainer";
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return $"List<{GetTypeName(type.GetGenericArguments()[0])}>";
            if (type.IsGenericType && type.GetGenericTypeDefinition().Name.StartsWith("OrderedDictionary"))
                return $"OrderedDictionary<{GetTypeName(type.GetGenericArguments()[0])}, {GetTypeName(type.GetGenericArguments()[1])}>";
            return type.Name;
        }

        protected string ValidateIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Property";
            var sb = new StringBuilder();
            if (!char.IsLetter(name[0]) && name[0] != '_') sb.Append('_');
            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
                else sb.Append('_');
            }
            return sb.ToString();
        }
    }
} 