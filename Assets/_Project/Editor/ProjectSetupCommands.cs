using Unity.Pipeline.Commands;
using UnityEditor;

namespace AutismVR.Editor
{
    public static class ProjectSetupCommands
    {
        [CliCommand("autismvr_apply_project_baseline",
            "Force text serialization and visible meta files, then reserialize all assets")]
        public static string ApplyProjectBaseline()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            AssetDatabase.ForceReserializeAssets();
            AssetDatabase.SaveAssets();
            return "ForceText";
        }
    }
}
