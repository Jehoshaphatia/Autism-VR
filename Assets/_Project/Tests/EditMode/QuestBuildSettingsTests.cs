using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AutismVR.Tests.EditMode
{
    public class QuestBuildSettingsTests
    {
        [Test]
        public void AndroidUsesIl2Cpp()
        {
            Assert.That(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android), Is.EqualTo(ScriptingImplementation.IL2CPP));
        }

        [Test]
        public void AndroidTargetsArm64Only()
        {
            Assert.That(PlayerSettings.Android.targetArchitectures, Is.EqualTo(AndroidArchitecture.ARM64));
        }

        [Test]
        public void AndroidUsesVulkanOnly()
        {
            Assert.That(PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android), Is.False);
            Assert.That(PlayerSettings.GetGraphicsAPIs(BuildTarget.Android), Is.EqualTo(new[] { GraphicsDeviceType.Vulkan }));
        }

        [Test]
        public void ColorSpaceIsLinear()
        {
            Assert.That(PlayerSettings.colorSpace, Is.EqualTo(ColorSpace.Linear));
        }

        [Test]
        public void AndroidTargetSdkIs34()
        {
            Assert.That((int)PlayerSettings.Android.targetSdkVersion, Is.EqualTo(34));
        }

        [Test]
        public void AndroidMinSdkIs32()
        {
            Assert.That((int)PlayerSettings.Android.minSdkVersion, Is.EqualTo(32));
        }

        [Test]
        public void ApplicationIdIsSet()
        {
            Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android), Is.EqualTo("com.Imisi3D.AutismVR"));
        }

        [Test]
        public void UrpRendererHasPostProcessData()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/_Project/Settings/URP-Quest_Renderer.asset");

            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.postProcessData, Is.Not.Null);
        }
    }
}
