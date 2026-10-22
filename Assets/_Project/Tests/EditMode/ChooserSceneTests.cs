using System.Linq;
using AutismVR.Characters;
using AutismVR.Editor;
using AutismVR.Interaction;
using NUnit.Framework;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AutismVR.Tests.EditMode
{
    public class ChooserSceneTests
    {
        [SetUp]
        public void OpenChooser()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ChooserSceneCommands.ChooserScenePath), Is.Not.Null,
                "Chooser scene is missing");
            EditorSceneManager.OpenScene(ChooserSceneCommands.ChooserScenePath, OpenSceneMode.Single);
        }

        static CharacterChoice[] Choices() =>
            Object.FindObjectsByType<CharacterChoice>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        [Test]
        public void ChooserHasOneFloorTrackedHandsRigAtTrainerSpawn()
        {
            var origins = Object.FindObjectsByType<XROrigin>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.That(origins, Has.Length.EqualTo(1));
            Assert.That(origins[0].RequestedTrackingOriginMode, Is.EqualTo(XROrigin.TrackingOriginMode.Floor));
            Assert.That(Vector3.Distance(origins[0].transform.position,
                GameObject.Find(RoomSceneCommands.TrainerSpawnName).transform.position), Is.LessThan(0.01f));
        }

        [Test]
        public void ChooserHasOnlyNeutralDirectionalLight()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.That(lights, Has.Length.EqualTo(1));
            Assert.That(lights[0].color, Is.EqualTo(RoomSceneCommands.NeutralLightColor));
        }

        [Test]
        public void ChooserOffersEveryProfileOnce()
        {
            var offered = Choices().Select(choice => choice.Profile.Id).ToList();

            Assert.That(offered, Is.EquivalentTo(CharacterProfileCommands.ProfileIds));
        }

        [Test]
        public void EveryChoiceShowsItsChildAndHasAButtonInReachAndInView()
        {
            var trainer = GameObject.Find(RoomSceneCommands.TrainerSpawnName).transform;
            foreach (var choice in Choices())
            {
                Assert.That(choice.Preview, Is.Not.Null, choice.name);
                Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(choice.Preview), Is.SameAs(choice.Profile.ModelPrefab));
                Assert.That(choice.Button, Is.Not.Null);
                Assert.That(choice.Button.GetComponent<PokeButton>(), Is.Not.Null);
                ButtonPlacementAssert.InReachAndInView(choice.Button.transform, trainer);
                ButtonPlacementAssert.SitsOnPedestal(choice.Button.transform, choice.Pedestal);
                Assert.That(choice.NextScene, Is.EqualTo("Room"));
            }
        }

        [Test]
        public void ChooserRenderersUseUrpCompatibleMaterials()
        {
            var offenders = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .SelectMany(r => r.sharedMaterials.Select(m => (r, m)))
                .Where(entry => !RoomSceneCommands.IsUrpCompatible(entry.m))
                .Select(entry => $"{entry.r.name} -> {(entry.m == null ? "null" : entry.m.shader.name)}")
                .ToList();

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        [Test]
        public void ChooserHasNoMissingScripts()
        {
            Assert.That(VerificationCommands.MissingScriptCount(), Is.EqualTo(0));
        }

        [Test]
        public void BuildScenesAreChooserThenRoom()
        {
            Assert.That(EditorBuildSettings.scenes.Select(s => s.path),
                Is.EqualTo(new[] { ChooserSceneCommands.ChooserScenePath, RoomSceneCommands.RoomScenePath }));
            Assert.That(EditorBuildSettings.scenes.All(s => s.enabled), Is.True);
        }
    }
}
