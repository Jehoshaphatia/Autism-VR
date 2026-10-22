using System;
using System.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace AutismVR.Editor
{
    // Generates Room.unity from the 2017 Autism1 scene: keeps its room, drops everything that
    // belonged to the removed SDKs, relights it neutrally and adds the XRI hands rig.
    public static class RoomSceneCommands
    {
        public const string RoomScenePath = "Assets/_Project/Scenes/Room.unity";
        public const string SourceScenePath = "Assets/_Scenes/Autism1.unity";
        public const string LightingSettingsPath = "Assets/_Project/Scenes/RoomLighting.lighting";
        public const string HandsRigPrefabPath =
            "Assets/Samples/XR Interaction Toolkit/3.6.1/Hands Interaction Demo/Prefabs/XR Origin Hands (XR Rig).prefab";
        public const string TrainerSpawnName = "TrainerSpawn";
        public const string ChildSpawnName = "ChildSpawn";
        public const string StartButtonName = "StartButton";
        public const string FloorColliderName = "Floor";
        public static readonly Color NeutralLightColor = new Color32(0xFF, 0xF4, 0xE5, 0xFF);

        // The furnished room in Autism1; every other root was legacy rig, UI, the old child character,
        // inactive leftovers, or furniture placed far outside the room.
        static readonly string[] KeptRoots = { "RoomComponent", "NewHouse", "Trash", "Mirror", "Refrigerator" };
        const string LegacyRigPrefix = "ViveCameraRig";
        const string LegacyChildRoot = "Container";

        [CliCommand("autismvr_build_room", "Generate Room.unity from Autism1 with the XRI hands rig")]
        public static string BuildRoomScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
                throw new InvalidOperationException(
                    $"{SourceScenePath} is gone; Room.unity is the source of truth and is edited directly");

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RoomScenePath) != null)
                AssetDatabase.DeleteAsset(RoomScenePath);
            EnsureFolder("Assets/_Project/Scenes");
            AssetDatabase.CopyAsset(SourceScenePath, RoomScenePath);

            var scene = EditorSceneManager.OpenScene(RoomScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var trainerPosition = FindRoot(roots, name => name.StartsWith(LegacyRigPrefix)).transform.position;
            var childPosition = FindRoot(roots, name => name == LegacyChildRoot).transform.position;

            foreach (var root in roots.Where(root => !KeptRoots.Contains(root.name)))
                UnityEngine.Object.DestroyImmediate(root);

            var trainerSpawn = CreateSpawn(TrainerSpawnName, trainerPosition, childPosition);
            CreateSpawn(ChildSpawnName, childPosition, trainerPosition);
            Relight();
            AddHandsRig(trainerSpawn.transform);
            AddStartButton(trainerSpawn.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RoomScenePath, true) };
            AssetDatabase.SaveAssets();
            return scene.name;
        }

        static GameObject FindRoot(GameObject[] roots, Func<string, bool> match)
        {
            return roots.FirstOrDefault(root => match(root.name))
                   ?? throw new InvalidOperationException($"Expected root object missing in {SourceScenePath}");
        }

        static GameObject CreateSpawn(string name, Vector3 position, Vector3 facing)
        {
            var spawn = new GameObject(name);
            spawn.transform.position = position;
            spawn.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(facing - position, Vector3.up));
            return spawn;
        }

        // Shadowless key light: the room is enclosed, so a shadow-casting sun would leave it dark.
        public static void Relight()
        {
            var sun = new GameObject("Sun", typeof(Light));
            var light = sun.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = NeutralLightColor;
            light.intensity = 1f;
            light.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.5f);
            RenderSettings.sun = light;

            var lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
            if (lightingSettings == null)
            {
                lightingSettings = new LightingSettings { bakedGI = false, realtimeGI = false };
                AssetDatabase.CreateAsset(lightingSettings, LightingSettingsPath);
            }
            Lightmapping.lightingSettings = lightingSettings;
            Lightmapping.lightingDataAsset = null;
        }

        static void AddHandsRig(Transform spawn)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HandsRigPrefabPath)
                         ?? throw new InvalidOperationException($"Missing {HandsRigPrefabPath}; run autismvr_import_xr_samples");
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rig.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        }

        // A pedestal with a fingertip button 0.6 m in front of the trainer at 0.9 m.
        static void AddStartButton(Transform spawn)
        {
            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "StartButtonPedestal";
            pedestal.transform.SetPositionAndRotation(spawn.TransformPoint(0f, 0.425f, 0.6f), spawn.rotation);
            pedestal.transform.localScale = new Vector3(0.2f, 0.425f, 0.2f);

            var button = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            button.name = StartButtonName;
            button.transform.SetPositionAndRotation(spawn.TransformPoint(0f, 0.9f, 0.6f), spawn.rotation);
            button.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
            UnityEngine.Object.DestroyImmediate(button.GetComponent<Collider>());
            button.AddComponent<BoxCollider>();
            button.AddComponent<AutismVR.Interaction.PokeButton>();
        }

        // Unity built-in Skybox/, UI/, Sprites/ and Unlit/ shaders render under URP; shaders shipped in the
        // imported XRI/XR Hands samples are maintained for URP.
        static readonly string[] UrpCompatibleShaderPrefixes =
        {
            "Universal Render Pipeline/", "Shader Graphs/", "Skybox/", "UI/", "Sprites/", "Unlit/"
        };

        public static bool IsUrpCompatible(Material material)
        {
            if (material == null || material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                return false;
            return UrpCompatibleShaderPrefixes.Any(prefix => material.shader.name.StartsWith(prefix))
                   || AssetDatabase.GetAssetPath(material.shader).StartsWith("Assets/Samples/");
        }

        // Room.unity is edited in place now; this removes what the Autism1 copy carried over unnoticed:
        // renderers on Unity's built-in Default-Material (magenta under URP) and the old game's score sign.
        [CliCommand("autismvr_clean_room", "Replace non-URP renderer materials and remove the legacy score canvas in Room")]
        public static string CleanRoom()
        {
            var scene = EditorSceneManager.OpenScene(RoomScenePath, OpenSceneMode.Single);
            var replaced = ReplaceNonUrpRendererMaterials(scene);

            foreach (var canvas in scene.GetRootGameObjects()
                         .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                         .Where(t => t.name == "ScoreCanvas").Select(t => t.gameObject).ToList())
                UnityEngine.Object.DestroyImmediate(canvas);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return replaced.ToString();
        }

        // Puts the trainer and child on the floor surface, tracks the rig from the floor, and writes the
        // StartButton's press-from-above poke setting into the scene.
        [CliCommand("autismvr_align_room", "Align Room spawns, rig and StartButton to the floor and set floor tracking")]
        public static string AlignRoom()
        {
            var scene = EditorSceneManager.OpenScene(RoomScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();

            var trainer = GameObject.Find(TrainerSpawnName).transform;
            var child = GameObject.Find(ChildSpawnName).transform;
            var lift = FloorHeightBelow(trainer.position) - trainer.position.y;
            child.position = new Vector3(child.position.x, FloorHeightBelow(child.position), child.position.z);
            trainer.position += Vector3.up * lift;
            foreach (var name in new[] { StartButtonName, "StartButtonPedestal" })
                GameObject.Find(name).transform.position += Vector3.up * lift;

            var origin = UnityEngine.Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            origin.transform.SetPositionAndRotation(trainer.position, trainer.rotation);
            origin.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor;
            EditorUtility.SetDirty(origin);

            var button = GameObject.Find(StartButtonName).GetComponent<AutismVR.Interaction.PokeButton>();
            button.ConfigurePokeFilter();
            EditorUtility.SetDirty(button.GetComponent<UnityEngine.XR.Interaction.Toolkit.Filtering.XRPokeFilter>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return lift.ToString("F3");
        }

        static float FloorHeightBelow(Vector3 position)
        {
            var hits = Physics.RaycastAll(position + Vector3.up * 1.5f, Vector3.down, 3f)
                .Where(hit => hit.collider.name == FloorColliderName).OrderBy(hit => hit.distance).ToList();
            if (hits.Count == 0)
                throw new InvalidOperationException($"No {FloorColliderName} collider below {position}");
            return hits[0].point.y;
        }

        // Adds the spawner that places the chosen child at ChildSpawn, facing the trainer.
        [CliCommand("autismvr_add_child_spawner", "Add the ChildSpawner to Room, wired to the spawns and the default profile")]
        public static string AddChildSpawner()
        {
            var scene = EditorSceneManager.OpenScene(RoomScenePath, OpenSceneMode.Single);
            var spawner = UnityEngine.Object.FindFirstObjectByType<AutismVR.Characters.ChildSpawner>()
                          ?? new GameObject("ChildSpawner").AddComponent<AutismVR.Characters.ChildSpawner>();
            spawner.Configure(CharacterProfileCommands.Load(CharacterProfileCommands.DefaultProfileId),
                GameObject.Find(ChildSpawnName).transform, GameObject.Find(TrainerSpawnName).transform);
            EditorUtility.SetDirty(spawner);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return spawner.name;
        }

        // Button centre height and distance ahead of the trainer: about 33 degrees below a 1.6 m eye
        // line, so it is in view and within arm's reach.
        public const float ButtonCentreHeight = 1.18f;
        public const float ButtonDistanceAhead = 0.65f;
        const float ButtonHalfThickness = 0.02f;

        // A fingertip button seated on a pedestal at `local` (x, centre y, z) in the spawn's space.
        public static (GameObject button, GameObject pedestal) PlaceButtonOnPedestal(
            Transform spawn, Vector3 local, string buttonName, GameObject button = null, GameObject pedestal = null)
        {
            if (pedestal == null)
            {
                pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pedestal.name = buttonName + "Pedestal";
            }
            var pedestalHalfHeight = (local.y - ButtonHalfThickness) / 2f;
            pedestal.transform.SetPositionAndRotation(spawn.TransformPoint(local.x, pedestalHalfHeight, local.z), spawn.rotation);
            pedestal.transform.localScale = new Vector3(0.2f, pedestalHalfHeight, 0.2f);

            if (button == null)
            {
                button = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                button.name = buttonName;
                UnityEngine.Object.DestroyImmediate(button.GetComponent<Collider>());
                button.AddComponent<BoxCollider>();
                button.AddComponent<AutismVR.Interaction.PokeButton>();
            }
            button.transform.SetPositionAndRotation(spawn.TransformPoint(local), spawn.rotation);
            button.transform.localScale = new Vector3(0.08f, ButtonHalfThickness, 0.08f);
            return (button, pedestal);
        }

        [CliCommand("autismvr_place_start_button", "Move Room's StartButton into view and seat it on its pedestal")]
        public static string PlaceStartButton()
        {
            var scene = EditorSceneManager.OpenScene(RoomScenePath, OpenSceneMode.Single);
            PlaceButtonOnPedestal(GameObject.Find(TrainerSpawnName).transform,
                new Vector3(0f, ButtonCentreHeight, ButtonDistanceAhead), StartButtonName,
                GameObject.Find(StartButtonName), GameObject.Find("StartButtonPedestal"));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return StartButtonName;
        }

        // Renderers on Unity's built-in Default-Material or Default-Line (as shipped on the XRI hands rig)
        // render magenta under URP; give them URP Lit or a URP particles line material.
        public static int ReplaceNonUrpRendererMaterials(UnityEngine.SceneManagement.Scene scene)
        {
            var urpDefault = GraphicsSettings.defaultRenderPipeline.defaultMaterial;
            var lineMaterial = LoadOrCreateLineMaterial();
            var replaced = 0;

            foreach (var renderer in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Renderer>(true)))
            {
                var materials = renderer.sharedMaterials;
                var changed = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (IsUrpCompatible(materials[i]))
                        continue;
                    materials[i] = renderer is LineRenderer || renderer is TrailRenderer ? lineMaterial : urpDefault;
                    changed = true;
                    replaced++;
                }
                if (changed)
                    renderer.sharedMaterials = materials;
            }
            return replaced;
        }

        public const string LineMaterialPath = "Assets/_Project/Materials/LineUnlit.mat";

        // Interactor line visuals ship with Unity's built-in Default-Line (legacy particles shader).
        static Material LoadOrCreateLineMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(LineMaterialPath);
            if (material != null)
                return material;

            EnsureFolder("Assets/_Project/Materials");
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "LineUnlit" };
            AssetDatabase.CreateAsset(material, LineMaterialPath);
            return material;
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder("Assets/_Project", path.Substring("Assets/_Project/".Length));
        }
    }
}
