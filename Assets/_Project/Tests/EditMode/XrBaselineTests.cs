using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace AutismVR.Tests.EditMode
{
    public class XrBaselineTests
    {
        // ProjectSettings "activeInputHandler": 0 = Input Manager, 1 = Input System Package, 2 = both.
        [Test]
        public void InputSystemIsActiveHandler()
        {
            var playerSettings = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);

            Assert.That(playerSettings.FindProperty("activeInputHandler").intValue, Is.EqualTo(1));
        }

        [TestCase(BuildTargetGroup.Android)]
        [TestCase(BuildTargetGroup.Standalone)]
        public void OpenXrLoaderIsAssigned(BuildTargetGroup group)
        {
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.Manager.activeLoaders.OfType<OpenXRLoader>(), Is.Not.Empty);
        }

        [TestCase(typeof(MetaQuestFeature))]
        [TestCase(typeof(HandTracking))]
        [TestCase(typeof(HandInteractionProfile))]
        [TestCase(typeof(EyeGazeInteraction))]
        public void OpenXrFeatureEnabledOnAndroid(Type featureType)
        {
            var feature = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android).GetFeature(featureType);

            Assert.That(feature != null && feature.enabled, Is.True, featureType.Name);
        }

        [TestCase(typeof(HandTracking))]
        [TestCase(typeof(HandInteractionProfile))]
        public void HandFeatureEnabledOnStandalone(Type featureType)
        {
            var feature = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone).GetFeature(featureType);

            Assert.That(feature != null && feature.enabled, Is.True, featureType.Name);
        }

        [Test]
        public void ForceRemoveInternetPermissionIsOff()
        {
            var metaQuest = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android)
                .GetFeature<MetaQuestFeature>();

            Assert.That(metaQuest, Is.Not.Null);
            Assert.That(metaQuest.ForceRemoveInternetPermission, Is.False);
        }
    }
}
