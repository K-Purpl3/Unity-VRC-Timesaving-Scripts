using UnityEngine;
using UnityEditor;
using System.IO;

public class SetVRCMobileShader : EditorWindow
{
    [MenuItem("Tools/Set All Materials to VRChat-Mobile-Standard Lite")]
    public static void SetAllMaterialsShader()
    {
        string[] materialPaths = Directory.GetFiles("Assets", "*.mat", SearchOption.AllDirectories);
        Shader targetShader = Shader.Find("VRChat/Mobile/Standard Lite");
        if (targetShader == null)
        {
            Debug.LogError("Shader 'VRChat/Mobile/Standard Lite' not found. Make sure the VRChat SDK is imported.");
            return;
        }

        int changedCount = 0;
        int skippedCount = 0;

        foreach (string path in materialPaths)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string shaderName = mat.shader != null ? mat.shader.name : "None";

            // skip excluded shaders
            if (shaderName == "Particles/Standard Unlit" || shaderName == "VRChat/Mobile/Particles/Additive")
            {
                skippedCount++;
                continue;
            }

            if (mat.shader != targetShader)
            {
                mat.shader = targetShader;
                EditorUtility.SetDirty(mat);
                changedCount++;
                Debug.Log($"Changed shader for: {path}");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Done. Changed {changedCount} materials, skipped {skippedCount}.");
    }
}
