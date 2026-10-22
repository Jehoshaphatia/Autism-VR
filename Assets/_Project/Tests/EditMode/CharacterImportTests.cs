using System.Linq;
using AutismVR.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AutismVR.Tests.EditMode
{
    public class CharacterImportTests
    {
        static readonly string[] ChildModels = CharacterImportCommands.ChildModelPaths;

        // What the model actually renders with, whether materials are embedded or remapped to .mat files.
        static Material[] RendererMaterials(string path) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials).Distinct().ToArray();

        [TestCaseSource(nameof(ChildModels))]
        public void ChildModelIsHumanoidWithValidAvatar(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();

            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Human));
            Assert.That(avatar, Is.Not.Null);
            Assert.That(avatar.isValid && avatar.isHuman, Is.True);
        }

        // Mixamo FBX files embed their textures; without extraction the materials import untextured white.
        [TestCaseSource(nameof(ChildModels))]
        public void ChildModelMaterialsHaveBaseTextures(string path)
        {
            var untextured = RendererMaterials(path)
                .Where(material => material == null || !material.HasProperty("_BaseMap") || material.GetTexture("_BaseMap") == null)
                .Select(material => material == null ? "null" : material.name)
                .ToList();

            Assert.That(untextured, Is.Empty, string.Join(", ", untextured));
        }

        [TestCaseSource(nameof(ChildModels))]
        public void ChildModelMaterialsAreUrpCompatible(string path)
        {
            var offenders = RendererMaterials(path)
                .Where(material => !RoomSceneCommands.IsUrpCompatible(material))
                .Select(material => material == null ? "null" : $"{material.name} -> {material.shader.name}")
                .ToList();

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }
    }
}
