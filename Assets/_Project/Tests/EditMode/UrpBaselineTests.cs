using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AutismVR.Tests.EditMode
{
    public class UrpBaselineTests
    {
        // Skybox/, UI/, Sprites/ and Unlit/ are Unity built-in shaders that render under URP
        // (unlit passes run as SRPDefaultUnlit).
        static readonly string[] UrpCompatibleShaderPrefixes =
        {
            "Universal Render Pipeline/", "Shader Graphs/", "Hidden/", "Skybox/", "UI/", "Sprites/", "Unlit/"
        };

        // Shaders shipped inside imported package samples (XRI, XR Hands) are maintained by Unity for URP.
        const string SamplesFolder = "Assets/Samples/";

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

        // A material stamped -1 is skipped by URP's material upgraders in future URP releases.
        [Test]
        public void EveryMaterialIsAtCurrentUrpMaterialVersion()
        {
            var projectVersion = Regex.Match(File.ReadAllText("ProjectSettings/URPProjectSettings.asset"),
                @"m_LastMaterialVersion: (-?\d+)").Groups[1].Value;
            var stale = Directory.GetFiles("Assets", "*.mat", SearchOption.AllDirectories)
                .Select(path => (path, version: Regex.Match(File.ReadAllText(path),
                    @"UnityEditor\.Rendering\.Universal\.AssetVersion\s*\n\s*version: (-?\d+)").Groups[1].Value))
                .Where(entry => entry.version.Length > 0 && entry.version != projectVersion)
                .Select(entry => $"{entry.path} -> {entry.version}")
                .ToList();

            Assert.That(projectVersion, Is.Not.Empty);
            Assert.That(stale, Is.Empty, string.Join("\n", stale));
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
                                || (!UrpCompatibleShaderPrefixes.Any(prefix => entry.material.shader.name.StartsWith(prefix))
                                    && !AssetDatabase.GetAssetPath(entry.material.shader).StartsWith(SamplesFolder)))
                .Select(entry => $"{entry.path} ({entry.material.name}) -> {entry.material.shader.name}")
                .ToList();
        }
    }
}
