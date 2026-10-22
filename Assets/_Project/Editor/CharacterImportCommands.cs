using Unity.Pipeline.Commands;
using UnityEditor;

namespace AutismVR.Editor
{
    // The three Mixamo children are imported as Humanoid so they share one Animator controller
    // and retarget the Mixamo and mocap placeholder clips.
    public static class CharacterImportCommands
    {
        public static readonly string[] ChildModelPaths =
        {
            "Assets/Aj.fbx",
            "Assets/Ch12_nonPBR.fbx",
            "Assets/Ch46_nonPBR.fbx",
        };

        [CliCommand("autismvr_configure_characters", "Import the child characters as Humanoid with their own avatars")]
        public static int ConfigureCharacters()
        {
            var changed = 0;
            foreach (var path in ChildModelPaths)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if (importer.animationType == ModelImporterAnimationType.Human
                    && importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)
                    continue;

                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
                changed++;
            }
            return changed;
        }
    }
}
