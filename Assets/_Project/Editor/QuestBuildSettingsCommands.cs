using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace AutismVR.Editor
{
    public static class QuestBuildSettingsCommands
    {
        public const string ApplicationId = "com.Imisi3D.AutismVR";

        // Meta Horizon requires target API 34 and minimum API 32 (since 2026-03-01);
        // Meta VR Glasses need IL2CPP + ARM64.
        [CliCommand("autismvr_apply_quest_settings", "Apply Android player settings for Meta Quest and Meta VR Glasses")]
        public static string ApplyQuestPlayerSettings()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)32;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);

            RenderPipelineCommands.EnsurePostProcessData();
            AssetDatabase.SaveAssets();
            return ApplicationId;
        }
    }
}
