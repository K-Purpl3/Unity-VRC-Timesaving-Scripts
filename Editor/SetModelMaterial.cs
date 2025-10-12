using UnityEngine;
using UnityEditor;
using System.IO;

public class SetModelMaterialMode : EditorWindow
{
    [MenuItem("Tools/Set Model Material Mode to None")]
    public static void SetMaterialModeToNone()
    {
        string[] modelPaths = Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories);
        int count = 0;

        foreach (string path in modelPaths)
        {
            if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".obj", System.StringComparison.OrdinalIgnoreCase))
            {
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer != null)
                {
                    if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
                    {
                        importer.materialImportMode = ModelImporterMaterialImportMode.None;
                        importer.SaveAndReimport();
                        count++;
                        Debug.Log($"Updated: {path}");
                    }
                }
            }
        }
        Debug.Log($"Finished updating {count} model(s).");
    }
}
