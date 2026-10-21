using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AutismVR.Tests.EditMode
{
    public class UrpBaselineTests
    {
        // Skybox/, UI/ and Sprites/ are Unity built-in shaders that render correctly under URP.
        static readonly string[] UrpCompatibleShaderPrefixes =
        {
            "Universal Render Pipeline/", "Shader Graphs/", "Hidden/", "Skybox/", "UI/", "Sprites/"
        };

        [Test]
        public void DefaultRenderPipelineIsUrp()
        {
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
        }

        [Test]
        public void EveryQualityLevelUsesUrpOrDefault()
        {
            for (var i = 0; i < QualitySettings.count; i++)
            {
                var asset = QualitySettings.GetRenderPipelineAssetAt(i);
                Assert.That(asset == null || asset is UniversalRenderPipelineAsset,
                    $"Quality level {QualitySettings.names[i]} uses {asset}");
            }
        }

        const string TempFolder = "Assets/_Project/Tests/EditMode/TempMaterials";

        [TearDown]
        public void DeleteTempMaterials()
        {
            AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void NoProjectMaterialUsesBuiltInShaders()
        {
            var offenders = FindNonUrpMaterials("Assets");

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        [Test]
        public void FindNonUrpMaterials_FlagsMaterialWithErrorShader()
        {
            AssetDatabase.CreateFolder("Assets/_Project/Tests/EditMode", "TempMaterials");
            var path = $"{TempFolder}/Broken.mat";
            AssetDatabase.CreateAsset(new Material(Shader.Find("Hidden/InternalErrorShader")), path);

            Assert.That(FindNonUrpMaterials(TempFolder), Has.Some.StartsWith(path));
        }

        // Covers every material in a file (models embed several), and treats the
        // error shader that renders magenta as a failure even though it is Hidden/.
        static List<string> FindNonUrpMaterials(string folder)
        {
            return AssetDatabase.FindAssets("t:Material", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>()
                    .Select(material => (path, material)))
                .Where(entry => entry.material.shader.name == "Hidden/InternalErrorShader"
                                || !UrpCompatibleShaderPrefixes.Any(prefix => entry.material.shader.name.StartsWith(prefix)))
                .Select(entry => $"{entry.path} ({entry.material.name}) -> {entry.material.shader.name}")
                .ToList();
        }
    }
}
