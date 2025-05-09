using System;
using System.IO;
using UnityEditor;
using GAOS.Logger;

namespace GAOS.DataStructure.Editor.Codegen
{
    public static class DataStructureCodeGenerator
    {
        public static void RegenerateCode(DataStructure structure)
        {
            if (structure == null)
            {
                GLog.Error<DataSystemEditorLogger>("DataStructureCodeGenerator: structure is null");
                return;
            }

            // Get asset path and directory
            string assetPath = AssetDatabase.GetAssetPath(structure);
            if (string.IsNullOrEmpty(assetPath))
            {
                GLog.Error<DataSystemEditorLogger>($"Could not find asset path for DataStructure: {structure.name}");
                return;
            }
            string directory = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(directory))
            {
                GLog.Error<DataSystemEditorLogger>($"Could not determine directory for DataStructure: {structure.name}");
                return;
            }

            // Generate interface code
            var interfaceGen = new InterfaceGenerator(structure);
            string interfaceCode = interfaceGen.GenerateInterfaceCode();
            string interfaceFile = Path.Combine(directory, $"I{structure.name}Instance.cs");

            // Generate implementation code
            var implGen = new ImplementationGenerator(structure);
            string implCode = implGen.GenerateImplementationCode();
            string implFile = Path.Combine(directory, $"{structure.name}Instance.cs");

            try
            {
                File.WriteAllText(interfaceFile, interfaceCode);
                File.WriteAllText(implFile, implCode);
                AssetDatabase.Refresh();
                GLog.Info<DataSystemEditorLogger>($"Generated interface: {interfaceFile}");
                GLog.Info<DataSystemEditorLogger>($"Generated implementation: {implFile}");

                // After writing the implementation file, set the generated type name
                var implClassName = structure.name + "Instance";
                var ns = "GAOS.Data";
                var fullTypeName = ns + "." + implClassName;
                structure.SetGeneratedInstanceTypeFullName(fullTypeName);
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(structure);
                // Schedule delayed type resolution for this structure only
                EditorApplication.delayCall += () => {
                    var foundType = FindTypeInAssemblies(fullTypeName);
                    if (foundType != null && typeof(DataInstance).IsAssignableFrom(foundType))
                    {
                        var assemblyQualified = foundType.AssemblyQualifiedName;
                        if (fullTypeName != assemblyQualified)
                        {
                            structure.SetGeneratedInstanceTypeFullName(assemblyQualified);
                            UnityEditor.EditorUtility.SetDirty(structure);
                        }
                    }
                };
                #endif
            }
            catch (Exception ex)
            {
                GLog.Error<DataSystemEditorLogger>($"Failed to write generated files: {ex.Message}");
            }
        }

        private static Type FindTypeInAssemblies(string fullTypeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullTypeName);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
} 