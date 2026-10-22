using AutismVR.Characters;
using AutismVR.Interaction;
using Unity.Pipeline.Commands;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AutismVR.Editor
{
    // The character stage the trainee starts in (spec §4.1): the three children stand in an arc, each
    // with its own fingertip button; pressing one picks that child and loads Room.
    public static class ChooserSceneCommands
    {
        public const string ChooserScenePath = "Assets/_Project/Scenes/Chooser.unity";
        const string FloorMaterialPath = "Assets/floor.mat";
        const float PreviewDistance = 1.6f;
        const float PreviewSpacing = 0.8f;
        const float ButtonSpacing = 0.3f;

        [CliCommand("autismvr_build_chooser", "Generate the character chooser scene and set build scenes to Chooser, Room")]
        public static string BuildChooserScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = RoomSceneCommands.FloorColliderName;
            floor.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            floor.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterialPath);

            var trainer = new GameObject(RoomSceneCommands.TrainerSpawnName).transform;
            RoomSceneCommands.Relight();

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(RoomSceneCommands.HandsRigPrefabPath));
            rig.transform.SetPositionAndRotation(trainer.position, trainer.rotation);
            rig.GetComponent<XROrigin>().RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            var ids = CharacterProfileCommands.ProfileIds;
            for (var i = 0; i < ids.Length; i++)
            {
                var profile = CharacterProfileCommands.Load(ids[i]);
                var offset = i - (ids.Length - 1) / 2f;

                var preview = (GameObject)PrefabUtility.InstantiatePrefab(profile.ModelPrefab);
                var previewPosition = trainer.TransformPoint(offset * PreviewSpacing, 0f, PreviewDistance);
                preview.transform.SetPositionAndRotation(previewPosition,
                    Quaternion.LookRotation(Vector3.ProjectOnPlane(trainer.position - previewPosition, Vector3.up)));

                var (button, pedestal) = RoomSceneCommands.PlaceButtonOnPedestal(trainer,
                    new Vector3(offset * ButtonSpacing, RoomSceneCommands.ButtonCentreHeight, RoomSceneCommands.ButtonDistanceAhead),
                    $"Choose_{profile.Id}");

                var choice = new GameObject($"Choice_{profile.Id}").AddComponent<CharacterChoice>();
                choice.Configure(profile, button.GetComponent<PokeButton>(), pedestal.transform, preview);
            }

            RoomSceneCommands.ReplaceNonUrpRendererMaterials(scene);
            EditorSceneManager.SaveScene(scene, ChooserScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ChooserScenePath, true),
                new EditorBuildSettingsScene(RoomSceneCommands.RoomScenePath, true),
            };
            AssetDatabase.SaveAssets();
            return scene.name;
        }
    }
}
