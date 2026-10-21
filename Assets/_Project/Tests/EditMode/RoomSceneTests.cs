using System.Linq;
using AutismVR.Editor;
using NUnit.Framework;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AutismVR.Tests.EditMode
{
    public class RoomSceneTests
    {
        [SetUp]
        public void OpenRoom()
        {
            Assume.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(RoomSceneCommands.RoomScenePath), Is.Not.Null,
                "Room scene has not been generated");
            EditorSceneManager.OpenScene(RoomSceneCommands.RoomScenePath, OpenSceneMode.Single);
        }

        [Test]
        public void RoomHasNoMissingScripts()
        {
            Assert.That(VerificationCommands.MissingScriptCount(), Is.EqualTo(0));
        }

        [Test]
        public void RoomHasNoMissingPrefabs()
        {
            var missing = SceneObjects().Where(PrefabUtility.IsPrefabAssetMissing).Select(go => go.name).ToList();

            Assert.That(missing, Is.Empty, string.Join(", ", missing));
        }

        [Test]
        public void RoomHasExactlyOneXrOrigin()
        {
            Assert.That(Object.FindObjectsByType<XROrigin>(FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        }

        [Test]
        public void RoomHasExactlyOneCameraAndItIsTheXrCamera()
        {
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var origin = Object.FindFirstObjectByType<XROrigin>();

            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(cameras[0], Is.SameAs(origin.Camera));
        }

        [Test]
        public void BothHandsHavePokeInteractors()
        {
            var handedness = Object.FindObjectsByType<XRPokeInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(poke => poke.handedness).ToList();

            Assert.That(handedness, Does.Contain(InteractorHandedness.Left));
            Assert.That(handedness, Does.Contain(InteractorHandedness.Right));
        }

        [Test]
        public void XrOriginStandsAtTrainerSpawnFacingChildSpawn()
        {
            var trainer = GameObject.Find(RoomSceneCommands.TrainerSpawnName);
            var child = GameObject.Find(RoomSceneCommands.ChildSpawnName);
            var origin = Object.FindFirstObjectByType<XROrigin>();

            Assert.That(trainer, Is.Not.Null);
            Assert.That(child, Is.Not.Null);
            Assert.That(Vector3.Distance(origin.transform.position, trainer.transform.position), Is.LessThan(0.01f));
            var toChild = Vector3.ProjectOnPlane(child.transform.position - trainer.transform.position, Vector3.up).normalized;
            Assert.That(Vector3.Dot(origin.transform.forward, toChild), Is.GreaterThan(0.99f));
        }

        [Test]
        public void RoomHasOnlyNeutralDirectionalLight()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.That(lights, Has.Length.EqualTo(1));
            Assert.That(lights[0].type, Is.EqualTo(LightType.Directional));
            Assert.That(lights[0].color, Is.EqualTo(RoomSceneCommands.NeutralLightColor));
        }

        [Test]
        public void RoomHasNoBakedLightingReference()
        {
            Assert.That(Lightmapping.lightingDataAsset, Is.Null);
        }

        [Test]
        public void BuildScenesAreExactlyRoom()
        {
            var scenes = EditorBuildSettings.scenes;

            Assert.That(scenes.Select(s => s.path), Is.EqualTo(new[] { RoomSceneCommands.RoomScenePath }));
            Assert.That(scenes[0].enabled, Is.True);
        }

        static GameObject[] SceneObjects()
        {
            return EditorSceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject).ToArray();
        }
    }
}
