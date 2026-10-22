using System.Collections.Generic;
using AutismVR.Characters;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace AutismVR.Editor
{
    // Creates a prefab and a provisional profile for each child. Personalities, names and values are
    // placeholders for the owner and admins to tune (spec §4.1, §8a).
    public static class CharacterProfileCommands
    {
        public const string ProfilesFolder = "Assets/_Project/Characters/Profiles";
        public const string PrefabsFolder = "Assets/_Project/Characters/Prefabs";
        public const string DefaultProfileId = "aj";
        public const string GreetingClipPath = "Assets/Tunde Intro/Hello_Uncle.wav";
        public static readonly string[] ProfileIds = { "aj", "ch12", "ch46" };

        public static string ProfilePath(string id) => $"{ProfilesFolder}/{id}.asset";

        public static CharacterProfile Load(string id) => AssetDatabase.LoadAssetAtPath<CharacterProfile>(ProfilePath(id));

        struct ProfileSeed
        {
            public string Id, Model, DisplayName, Personality;
            public float StartingComfort, GreetingThreshold, RecoveryPerSecond, Height;
            public (string trigger, float multiplier)[] Sensitivities;
        }

        static readonly ProfileSeed[] Seeds =
        {
            new ProfileSeed
            {
                Id = "aj", Model = "Assets/Aj.fbx", DisplayName = "Aj",
                Personality = "Very sensitive to loud voices and anger. Calms when spoken to quietly and slowly.",
                StartingComfort = 0.35f, GreetingThreshold = 0.75f, RecoveryPerSecond = 0.02f, Height = 1.25f,
                Sensitivities = new[] { (TriggerIds.Shouting, 1.5f), (TriggerIds.Anger, 1.3f) },
            },
            new ProfileSeed
            {
                Id = "ch12", Model = "Assets/Ch12_nonPBR.fbx", DisplayName = "Ch12",
                Personality = "Needs personal space. Becomes distressed when approached quickly or reached towards.",
                StartingComfort = 0.4f, GreetingThreshold = 0.75f, RecoveryPerSecond = 0.03f, Height = 1.3f,
                Sensitivities = new[] { (TriggerIds.ApproachTooClose, 1.5f), (TriggerIds.ReachingOrTouch, 1.6f) },
            },
            new ProfileSeed
            {
                Id = "ch46", Model = "Assets/Ch46_nonPBR.fbx", DisplayName = "Ch46",
                Personality = "Needs time to process. Overwhelmed by rapid questions, pressure to speak and demands for eye contact.",
                StartingComfort = 0.3f, GreetingThreshold = 0.8f, RecoveryPerSecond = 0.015f, Height = 1.2f,
                Sensitivities = new[]
                {
                    (TriggerIds.RapidQuestions, 1.5f), (TriggerIds.PressuringSpeech, 1.4f), (TriggerIds.DemandingEyeContact, 1.3f),
                },
            },
        };

        [CliCommand("autismvr_create_profiles", "Create the child prefabs and their provisional character profiles")]
        public static int CreateProfiles()
        {
            EnsureFolder(PrefabsFolder);
            EnsureFolder(ProfilesFolder);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ChildAnimatorCommands.ControllerPath);
            var greeting = AssetDatabase.LoadAssetAtPath<AudioClip>(GreetingClipPath);

            foreach (var seed in Seeds)
            {
                var profile = Load(seed.Id);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<CharacterProfile>();
                    AssetDatabase.CreateAsset(profile, ProfilePath(seed.Id));
                }

                profile.Id = seed.Id;
                profile.DisplayName = seed.DisplayName;
                profile.ModelPrefab = CreateChildPrefab(seed.Id, seed.Model, controller, seed.Height);
                profile.GreetingClip = greeting;
                profile.GreetingLine = "Hello!";
                profile.Personality = seed.Personality;
                profile.StartingComfort = seed.StartingComfort;
                profile.GreetingThreshold = seed.GreetingThreshold;
                profile.RecoveryPerSecond = seed.RecoveryPerSecond;
                profile.TriggerSensitivities = new List<TriggerSensitivity>();
                foreach (var (trigger, multiplier) in seed.Sensitivities)
                    profile.TriggerSensitivities.Add(new TriggerSensitivity { TriggerId = trigger, Multiplier = multiplier });
                profile.ExcludedTriggers = new List<string>();
                EditorUtility.SetDirty(profile);
            }

            AssetDatabase.SaveAssets();
            return Seeds.Length;
        }

        // The Mixamo models are adult-sized, so each prefab is scaled uniformly to a provisional child height.
        static GameObject CreateChildPrefab(string id, string modelPath, RuntimeAnimatorController controller, float height)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
            try
            {
                instance.transform.localScale = Vector3.one;
                var renderers = instance.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                    bounds.Encapsulate(renderer.bounds);
                instance.transform.localScale = Vector3.one * (height / bounds.size.y);

                var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                if (instance.GetComponent<ChildAnimator>() == null)
                    instance.AddComponent<ChildAnimator>();
                return PrefabUtility.SaveAsPrefabAsset(instance, $"{PrefabsFolder}/{id}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'), System.IO.Path.GetFileName(path));
        }
    }
}
