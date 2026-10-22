using System.Linq;
using Unity.Pipeline.Commands;
using UnityEngine;
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

        public const string TexturesFolder = "Assets/_Project/Characters/Textures";
        public const string MaterialsFolder = "Assets/_Project/Characters/Materials";
        static readonly string[] FacePartNames = { "Eyes", "Brows", "Mouth" };

        // Humanoid with their own avatars, and their embedded textures extracted into the project so the
        // imported materials pick them up (embedded media alone imports untextured white).
        [CliCommand("autismvr_configure_characters", "Import the child characters as Humanoid with their textures")]
        public static int ConfigureCharacters()
        {
            EnsureFolder(TexturesFolder);
            foreach (var path in ChildModelPaths)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.materialSearch = ModelImporterMaterialSearch.Everywhere;

                var textureFolder = $"{TexturesFolder}/{System.IO.Path.GetFileNameWithoutExtension(path)}";
                if (!AssetDatabase.IsValidFolder(textureFolder))
                {
                    EnsureFolder(textureFolder);
                    importer.ExtractTextures(textureFolder);
                    AssetDatabase.Refresh();
                }
                importer.SaveAndReimport();
                AssignTextures(path, textureFolder);
            }
            AssetDatabase.SaveAssets();
            return ChildModelPaths.Length;
        }

        // URP's FBX material import does not resolve these models' embedded texture references, so
        // the materials are extracted to .mat files and given the extracted diffuse and normal maps.
        static void AssignTextures(string modelPath, string textureFolder)
        {
            var modelName = System.IO.Path.GetFileNameWithoutExtension(modelPath);
            var materialFolder = $"{MaterialsFolder}/{modelName}";
            EnsureFolder(MaterialsFolder);
            EnsureFolder(materialFolder);

            foreach (var embedded in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>().ToList())
            {
                var error = AssetDatabase.ExtractAsset(embedded, $"{materialFolder}/{embedded.name}.mat");
                if (!string.IsNullOrEmpty(error))
                    throw new System.InvalidOperationException($"Extracting {embedded.name}: {error}");
            }
            AssetDatabase.WriteImportSettingsIfDirty(modelPath);
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);

            var diffuse = FindTexture(textureFolder, "diffuse");
            var facial = FindTexture(textureFolder, "facial");
            var normal = FindTexture(textureFolder, "normal");
            if (normal != null)
            {
                var normalImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal));
                if (normalImporter.textureType != TextureImporterType.NormalMap)
                {
                    normalImporter.textureType = TextureImporterType.NormalMap;
                    normalImporter.SaveAndReimport();
                }
            }

            foreach (var materialPath in AssetDatabase.FindAssets("t:Material", new[] { materialFolder }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                // Aj's eye, brow and mouth meshes are UV-mapped to its facial map, not the body atlas.
                var isFacePart = facial != null && FacePartNames.Any(part => material.name.Contains(part));
                material.SetTexture("_BaseMap", isFacePart ? facial : diffuse);
                material.SetColor("_BaseColor", Color.white);
                if (normal != null && !isFacePart)
                {
                    material.SetTexture("_BumpMap", normal);
                    material.EnableKeyword("_NORMALMAP");
                }
                else
                {
                    material.SetTexture("_BumpMap", null);
                    material.DisableKeyword("_NORMALMAP");
                }
                EditorUtility.SetDirty(material);
            }
        }

        static Texture2D FindTexture(string folder, string nameContains)
        {
            return AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Contains(nameContains))
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                .FirstOrDefault();
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'), System.IO.Path.GetFileName(path));
        }
    }
}
