using System;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace AutismVR.Editor
{
    public static class XrSetupCommands
    {
        const string GeneralSettingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
        const string OpenXrLoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader";

        // Hands everywhere; Meta Quest support and eye gaze (Meta VR Glasses look-and-pinch) on Android.
        static readonly Type[] AndroidFeatures =
            { typeof(MetaQuestFeature), typeof(HandTracking), typeof(HandInteractionProfile), typeof(EyeGazeInteraction) };
        static readonly Type[] StandaloneFeatures = { typeof(HandTracking), typeof(HandInteractionProfile) };

        [CliCommand("autismvr_apply_xr",
            "Use the Input System, assign OpenXR for Android and Standalone, and enable hand and gaze features")]
        public static string ApplyXrBaseline()
        {
            UseInputSystemPackage();

            var perBuildTarget = LoadOrCreateGeneralSettings();
            AssignOpenXr(perBuildTarget, BuildTargetGroup.Android);
            AssignOpenXr(perBuildTarget, BuildTargetGroup.Standalone);

            EnableFeatures(BuildTargetGroup.Android, AndroidFeatures);
            EnableFeatures(BuildTargetGroup.Standalone, StandaloneFeatures);

            AssetDatabase.SaveAssets();
            return "xr";
        }

        // Takes effect after the Editor restarts.
        static void UseInputSystemPackage()
        {
            var playerSettings = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            playerSettings.FindProperty("activeInputHandler").intValue = 1;
            playerSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        static XRGeneralSettingsPerBuildTarget LoadOrCreateGeneralSettings()
        {
            if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey,
                    out XRGeneralSettingsPerBuildTarget existing) && existing != null)
                return existing;

            if (!AssetDatabase.IsValidFolder("Assets/XR"))
                AssetDatabase.CreateFolder("Assets", "XR");

            var created = UnityEngine.ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(created, GeneralSettingsPath);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, created, true);
            return created;
        }

        static void AssignOpenXr(XRGeneralSettingsPerBuildTarget perBuildTarget, BuildTargetGroup group)
        {
            if (!perBuildTarget.HasManagerSettingsForBuildTarget(group))
                perBuildTarget.CreateDefaultManagerSettingsForBuildTarget(group);

            XRPackageMetadataStore.AssignLoader(perBuildTarget.ManagerSettingsForBuildTarget(group), OpenXrLoaderType, group);
        }

        static void EnableFeatures(BuildTargetGroup group, Type[] featureTypes)
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            foreach (var featureType in featureTypes)
            {
                var feature = settings.GetFeature(featureType);
                if (feature == null)
                    throw new InvalidOperationException($"OpenXR feature {featureType.Name} not found for {group}");
                feature.enabled = true;
                EditorUtility.SetDirty(feature);
            }
            EditorUtility.SetDirty(settings);
        }
    }
}
