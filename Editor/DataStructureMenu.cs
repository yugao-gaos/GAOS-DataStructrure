using UnityEngine;
using UnityEditor;
using System.IO;

namespace GAOS.DataStructure.Editor
{
    public static class DataStructureMenu
    {
        private const string MENU_ROOT = "GAOS/Data Structure/";
        private const string DOCUMENTATION_MENU = MENU_ROOT + "Documentation";
        private const string PACKAGE_NAME = "com.gaos.datastructure";

        [MenuItem(DOCUMENTATION_MENU, false, 20)]
        private static void OpenDocumentation()
        {
            string packagePath = Path.GetFullPath("Packages/" + PACKAGE_NAME);
            string docPath = Path.Combine(packagePath, "Documentation~", "index.html");

            if (File.Exists(docPath))
            {
                Application.OpenURL("file:///" + docPath.Replace("\\", "/"));
            }
            else
            {
                Debug.LogError($"Documentation not found at: {docPath}");
                
                // Fallback to package documentation URL from package.json
                string packageJsonPath = Path.Combine(packagePath, "package.json");
                if (File.Exists(packageJsonPath))
                {
                    string jsonContent = File.ReadAllText(packageJsonPath);
                    var packageJson = JsonUtility.FromJson<PackageJson>(jsonContent);
                    if (!string.IsNullOrEmpty(packageJson.documentationUrl))
                    {
                        Application.OpenURL("file:///" + Path.GetFullPath(packageJson.documentationUrl).Replace("\\", "/"));
                    }
                }
            }
        }

        [MenuItem(MENU_ROOT + "Open Data Structure Editor", false, 10)]
        public static void OpenDataStructureEditor()
        {
            DataStructureEditorWindow.ShowWindow();
        }

        private class PackageJson
        {
            public string documentationUrl;
        }
    }
} 