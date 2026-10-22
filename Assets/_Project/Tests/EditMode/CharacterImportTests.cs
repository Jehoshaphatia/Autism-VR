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

        [TestCaseSource(nameof(ChildModels))]
        public void ChildModelIsHumanoidWithValidAvatar(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();

            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Human));
            Assert.That(avatar, Is.Not.Null);
            Assert.That(avatar.isValid && avatar.isHuman, Is.True);
        }

        [TestCaseSource(nameof(ChildModels))]
        public void ChildModelMaterialsAreUrpCompatible(string path)
        {
            var offenders = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>()
                .Where(material => !RoomSceneCommands.IsUrpCompatible(material))
                .Select(material => $"{material.name} -> {material.shader.name}")
                .ToList();

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }
    }
}
