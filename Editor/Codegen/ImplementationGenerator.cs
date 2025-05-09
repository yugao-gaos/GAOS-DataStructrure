using System;
using System.Collections.Generic;
using System.Linq;
using GAOS.DataStructure;
using GAOS.DataStructure.References;

namespace GAOS.DataStructure.Editor.Codegen
{
    public class ImplementationGenerator : CodeGeneratorBase
    {
        private readonly DataStructure _structure;

        public ImplementationGenerator(DataStructure structure)
        {
            _structure = structure;
        }

        public string GenerateImplementationCode()
        {
            _builder.Clear();
            _addedUsings.Clear();

            // Before emitting usings, check if any property type is or contains UnityObjectReference
            bool needsUnityObjectReferenceUsing = false;
            IEnumerable<string> propertyPaths = (_structure.InterfacePropertyPaths != null && _structure.InterfacePropertyPaths.Count > 0)
                ? _structure.InterfacePropertyPaths
                : _structure.GetAllPaths();
            foreach (var path in propertyPaths)
            {
                var type = _structure.GetPathType(path);
                if (type == null) continue;
                if (type == typeof(UnityObjectReference) ||
                    (type.IsGenericType && type.GetGenericArguments().Any(t => t == typeof(UnityObjectReference))))
                {
                    needsUnityObjectReferenceUsing = true;
                    break;
                }
            }
            if (needsUnityObjectReferenceUsing)
                AddUsing("GAOS.DataStructure.References");

            // Add usings
            AddUsing("System");
            AddUsing("System.Collections.Generic");
            AddUsing("UnityEngine");
            AddUsing("GAOS.DataStructure");

            // Namespace
            _builder.AppendLine();
            _builder.AppendLine("namespace GAOS.Data");
            _builder.AppendLine("{");

            // Class doc
            _builder.AppendLine("    /// <summary>");
            _builder.AppendLine($"    /// Concrete DataInstance for {_structure.name}.");
            if (!string.IsNullOrEmpty(_structure.Description))
            {
                _builder.AppendLine("    /// <remarks>");
                _builder.AppendLine($"    /// {_structure.Description}");
                _builder.AppendLine("    /// </remarks>");
            }
            _builder.AppendLine("    /// </summary>");

            // Class definition
            _builder.AppendLine($"    public class {_structure.name}Instance : DataInstance, I{_structure.name}Instance");
            _builder.AppendLine("    {");

            // Property generation
            propertyPaths = (_structure.InterfacePropertyPaths != null && _structure.InterfacePropertyPaths.Count > 0)
                ? _structure.InterfacePropertyPaths
                : _structure.GetAllPaths();
            foreach (var path in propertyPaths)
            {
                var type = _structure.GetPathType(path);
                if (type == null) continue;
                string propertyName = ValidateIdentifier(GetPropertyNameFromPath(path));
                _builder.AppendLine();
                _builder.AppendLine("        /// <summary>");
                _builder.AppendLine($"        /// Gets{(IsStructureType(type) ? "" : " or sets")} the {propertyName}.");
                _builder.AppendLine("        /// </summary>");
                if (IsStructureType(type))
                {
                    string escapedPath = path.Replace("\"", "\\\"");
                    _builder.AppendLine($"        public {GetTypeName(type)} {propertyName} => GetValue<{GetTypeName(type)}>(\"{escapedPath}\");");
                }
                else
                {
                    string escapedPath = path.Replace("\"", "\\\"");
                    _builder.AppendLine($"        public {GetTypeName(type)} {propertyName} {{ get => GetValue<{GetTypeName(type)}>(\"{escapedPath}\"); set => SetValue(\"{escapedPath}\", value); }}");
                }
            }

            _builder.AppendLine("    }");
            _builder.AppendLine("}");
            return _builder.ToString();
        }

        private string GetPropertyNameFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "Root";
            if (path.Contains("["))
            {
                var bracketIndex = path.IndexOf("[");
                var parentPath = path.Substring(0, bracketIndex);
                var indexPart = path.Substring(bracketIndex).Trim('[', ']');
                if (indexPart.StartsWith("\"") && indexPart.EndsWith("\""))
                    indexPart = indexPart.Substring(1, indexPart.Length - 2);
                return $"{GetPropertyNameFromPath(parentPath)}Item{indexPart}";
            }
            var segments = path.Split('.');
            return segments[segments.Length - 1];
        }

        private bool IsStructureType(Type type)
        {
            if (type == typeof(DataContainer)) return true;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && type.GetGenericArguments()[0] == typeof(DataContainer)) return true;
            if (type.IsGenericType && type.GetGenericTypeDefinition().Name.StartsWith("OrderedDictionary") && type.GetGenericArguments()[0] == typeof(string) && type.GetGenericArguments()[1] == typeof(DataContainer)) return true;
            return false;
        }
    }
} 