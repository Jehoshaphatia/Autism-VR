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

        [Test]
        public void NoProjectMaterialUsesBuiltInShaders()
        {
            var offenders = AssetDatabase.FindAssets("t:Material", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => (path, material: AssetDatabase.LoadAssetAtPath<Material>(path)))
                .Where(entry => entry.material != null)
                .Where(entry => !UrpCompatibleShaderPrefixes.Any(prefix => entry.material.shader.name.StartsWith(prefix)))
                .Select(entry => $"{entry.path} -> {entry.material.shader.name}")
                .ToList();

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }
    }
}
