using System.Linq;
using AutismVR.Editor;
using NUnit.Framework;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

namespace AutismVR.Tests.EditMode
{
    public class RoomSceneTests
    {
        [SetUp]
        public void OpenRoom()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(RoomSceneCommands.RoomScenePath), Is.Not.Null,
                "Room scene is missing");
            EditorSceneManager.OpenScene(RoomSceneCommands.RoomScenePath, OpenSceneMode.Single);
        }

        [Test]
        public void BuildRoomScene_WithoutAutism1_Throws()
        {
            Assume.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(RoomSceneCommands.SourceScenePath), Is.Null,
                "Autism1 still exists, so BuildRoomScene would regenerate Room");

            var error = Assert.Throws<System.InvalidOperationException>(() => RoomSceneCommands.BuildRoomScene());
            Assert.That(error.Message, Does.Contain("Room.unity is the source of truth"));
        }

        // Renderers can point at Unity's built-in Default-Material (Standard), which no material-asset
        // check sees and which renders magenta under URP.
        [Test]
        public void RoomRenderersUseUrpCompatibleMaterials()
        {
            var offenders = SceneObjects()
                .SelectMany(go => go.GetComponents<Renderer>())
                .SelectMany(r => r.sharedMaterials.Select(m => (r, m)))
                .Where(entry => !RoomSceneCommands.IsUrpCompatible(entry.m))
                .Select(entry => $"{entry.r.name} -> {(entry.m == null ? "null" : entry.m.shader.name)}")
                .ToList();

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        [Test]
        public void RoomHasNoLegacyScoreCanvas()
        {
            Assert.That(SceneObjects().Where(go => go.name == "ScoreCanvas"), Is.Empty);
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
            // The rig also carries controller pokes; only pokes under the modality manager's hand objects count.
            var modality = Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Inputs.XRInputModalityManager>();
            Assert.That(modality, Is.Not.Null);
            var handRoots = new[] { modality.leftHand, modality.rightHand }.Where(h => h != null).Select(h => h.transform).ToList();
            var handedness = Object.FindObjectsByType<XRPokeInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(poke => handRoots.Any(root => poke.transform.IsChildOf(root)))
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
        public void RoomHasChildSpawnerWiredToSpawns()
        {
            var spawners = Object.FindObjectsByType<AutismVR.Characters.ChildSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.That(spawners, Has.Length.EqualTo(1));
            Assert.That(spawners[0].SpawnPoint, Is.SameAs(GameObject.Find(RoomSceneCommands.ChildSpawnName).transform));
            Assert.That(spawners[0].FaceTowards, Is.SameAs(GameObject.Find(RoomSceneCommands.TrainerSpawnName).transform));
            Assert.That(spawners[0].DefaultProfile, Is.SameAs(CharacterProfileCommands.Load(CharacterProfileCommands.DefaultProfileId)));
        }

        [Test]
        public void XrOriginTracksFromTheFloor()
        {
            var origin = Object.FindFirstObjectByType<XROrigin>();

            Assert.That(origin.RequestedTrackingOriginMode, Is.EqualTo(XROrigin.TrackingOriginMode.Floor));
        }

        [TestCase(RoomSceneCommands.TrainerSpawnName)]
        [TestCase(RoomSceneCommands.ChildSpawnName)]
        public void SpawnStandsOnTheFloorSurface(string spawnName)
        {
            var spawn = GameObject.Find(spawnName).transform.position;
            Physics.SyncTransforms();
            var hit = Physics.RaycastAll(spawn + Vector3.up * 1.5f, Vector3.down, 3f)
                .Where(h => h.collider.name == RoomSceneCommands.FloorColliderName)
                .OrderBy(h => h.distance).First();

            Assert.That(spawn.y, Is.EqualTo(hit.point.y).Within(0.01f));
        }

        [Test]
        public void StartButtonIsPressedFromAbove()
        {
            var filter = GameObject.Find(RoomSceneCommands.StartButtonName)
                .GetComponent<UnityEngine.XR.Interaction.Toolkit.Filtering.XRPokeFilter>();

            Assert.That(filter.pokeConfiguration.Value.pokeDirection, Is.EqualTo(PokeAxis.NegativeY));
        }

        [Test]
        public void RoomHasStartButtonInFrontOfSpawn()
        {
            var button = GameObject.Find(RoomSceneCommands.StartButtonName);
            var spawn = GameObject.Find(RoomSceneCommands.TrainerSpawnName).transform;

            Assert.That(button, Is.Not.Null);
            Assert.That(button.GetComponent<AutismVR.Interaction.PokeButton>(), Is.Not.Null);
            var local = spawn.InverseTransformPoint(button.transform.position);
            Assert.That(local.z, Is.InRange(0.5f, 0.7f), "distance in front of the spawn");
            Assert.That(local.y, Is.InRange(0.8f, 1.0f), "height above the floor");
            Assert.That(Mathf.Abs(local.x), Is.LessThan(0.2f), "centred on the trainer");
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
