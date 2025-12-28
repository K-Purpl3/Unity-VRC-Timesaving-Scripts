using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditor.Animations;
using System.Linq;

//you select your main model or avatar, then select the asset you want to be removed, it will remove that asset and all existing toggles or radials associated with it so it doesn't give any problems when uploading it

public class VRChatAssetRemover : EditorWindow
{
    private GameObject avatarRoot;
    private GameObject assetToRemove;

    [MenuItem("Tools/VRChat/Remove Asset & Toggles")]
    public static void ShowWindow()
    {
        GetWindow<VRChatAssetRemover>("VRChat Asset Remover");
    }

    private void OnGUI()
    {
        GUILayout.Label("VRChat Asset & Toggle Cleaner", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        avatarRoot = (GameObject)EditorGUILayout.ObjectField(
            "Avatar Root",
            avatarRoot,
            typeof(GameObject),
            true
        );

        assetToRemove = (GameObject)EditorGUILayout.ObjectField(
            "Asset to Remove",
            assetToRemove,
            typeof(GameObject),
            true
        );

        EditorGUILayout.Space();

        GUI.enabled = avatarRoot != null && assetToRemove != null;

        if (GUILayout.Button("Remove Asset & Clean Toggles"))
        {
            RemoveAssetAndToggles();
        }

        GUI.enabled = true;
    }

    private void RemoveAssetAndToggles()
    {
        var avatar = avatarRoot.GetComponent<VRCAvatarDescriptor>();
        if (!avatar)
        {
            Debug.LogError("Selected Avatar Root has no VRCAvatarDescriptor.");
            return;
        }

        string assetName = assetToRemove.name;

        RemoveMenuControls(avatar, assetName);
        RemoveAnimatorParameters(avatar, assetName);
        RemoveFXAnimations(avatar, assetToRemove);

        DestroyImmediate(assetToRemove);

        Debug.Log($"Removed '{assetName}' and cleaned related toggles.");
    }

    private void RemoveMenuControls(VRCAvatarDescriptor avatar, string assetName)
    {
        var menu = avatar.expressionsMenu;
        if (!menu) return;

        menu.controls.RemoveAll(c =>
            c.parameter != null &&
            c.parameter.name.Contains(assetName)
        );

        EditorUtility.SetDirty(menu);
    }

    private void RemoveAnimatorParameters(VRCAvatarDescriptor avatar, string assetName)
    {
        var fxLayer = avatar.baseAnimationLayers
            .FirstOrDefault(l => l.type == VRCAvatarDescriptor.AnimLayerType.FX);

        if (fxLayer.animatorController == null) return;

        var controller = fxLayer.animatorController as AnimatorController;
        if (!controller) return;

        controller.parameters = controller.parameters
            .Where(p => !p.name.Contains(assetName))
            .ToArray();

        EditorUtility.SetDirty(controller);
    }

    private void RemoveFXAnimations(VRCAvatarDescriptor avatar, GameObject target)
    {
        var fxLayer = avatar.baseAnimationLayers
            .FirstOrDefault(l => l.type == VRCAvatarDescriptor.AnimLayerType.FX);

        if (fxLayer.animatorController == null) return;

        var controller = fxLayer.animatorController as AnimatorController;
        if (!controller) return;

        foreach (var layer in controller.layers)
        {
            var stateMachine = layer.stateMachine;

            foreach (var state in stateMachine.states)
            {
                var motion = state.state.motion as AnimationClip;
                if (!motion) continue;

                var bindings = AnimationUtility.GetCurveBindings(motion);

                foreach (var binding in bindings)
                {
                    if (binding.path.Contains(target.name))
                    {
                        AnimationUtility.SetEditorCurve(
                            motion,
                            binding,
                            null
                        );
                        EditorUtility.SetDirty(motion);
                    }
                }
            }
        }
    }
}
