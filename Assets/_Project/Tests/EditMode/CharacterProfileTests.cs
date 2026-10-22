using System.Linq;
using AutismVR.Characters;
using AutismVR.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AutismVR.Tests.EditMode
{
    public class CharacterProfileTests
    {
        static readonly string[] ProfileIds = CharacterProfileCommands.ProfileIds;

        static CharacterProfile Load(string id) =>
            AssetDatabase.LoadAssetAtPath<CharacterProfile>(CharacterProfileCommands.ProfilePath(id));

        [TestCaseSource(nameof(ProfileIds))]
        public void ProfileIsComplete(string id)
        {
            var profile = Load(id);

            Assert.That(profile, Is.Not.Null, id);
            Assert.That(profile.Id, Is.EqualTo(id));
            Assert.That(profile.DisplayName, Is.Not.Empty);
            Assert.That(profile.GreetingClip, Is.Not.Null);
            Assert.That(profile.GreetingLine, Is.Not.Empty);
            Assert.That(profile.Personality, Is.Not.Empty);
            Assert.That(profile.StartingComfort, Is.LessThan(profile.GreetingThreshold));
            Assert.That(profile.RecoveryPerSecond, Is.GreaterThan(0f));
        }

        [TestCaseSource(nameof(ProfileIds))]
        public void ProfileModelIsAnimatedChild(string id)
        {
            var model = Load(id).ModelPrefab;

            Assert.That(model, Is.Not.Null);
            Assert.That(model.GetComponent<ChildAnimator>(), Is.Not.Null);
            var animator = model.GetComponent<Animator>();
            Assert.That(animator.runtimeAnimatorController,
                Is.SameAs(AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ChildAnimatorCommands.ControllerPath)));
            Assert.That(animator.avatar != null && animator.avatar.isHuman, Is.True);
        }

        // The Mixamo models are adult-sized (1.6-2.0 m); a child-interaction trainer needs child-sized
        // characters, so each prefab is scaled to a provisional child height.
        [TestCaseSource(nameof(ProfileIds))]
        public void ProfileModelIsChildHeight(string id)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(Load(id).ModelPrefab);
            try
            {
                var renderers = instance.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                    bounds.Encapsulate(renderer.bounds);

                Assert.That(bounds.size.y, Is.InRange(1.0f, 1.4f), $"{id} height");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [TestCaseSource(nameof(ProfileIds))]
        public void ProfileOnlyReferencesKnownTriggers(string id)
        {
            var profile = Load(id);
            var referenced = profile.TriggerSensitivities.Select(s => s.TriggerId).Concat(profile.ExcludedTriggers);

            Assert.That(referenced.Except(TriggerIds.All), Is.Empty);
        }

        [Test]
        public void ProfilesHaveDistinctPersonalities()
        {
            Assert.That(ProfileIds.Select(id => Load(id).Personality).Distinct().Count(), Is.EqualTo(ProfileIds.Length));
        }

        [Test]
        public void GetSensitivity_UnlistedTrigger_IsOne()
        {
            var profile = ScriptableObject.CreateInstance<CharacterProfile>();

            Assert.That(profile.GetSensitivity(TriggerIds.Sarcasm), Is.EqualTo(1f));
            Assert.That(profile.Applies(TriggerIds.Sarcasm), Is.True);
        }

        [Test]
        public void GetSensitivity_ListedTrigger_IsItsMultiplier()
        {
            var profile = ScriptableObject.CreateInstance<CharacterProfile>();
            profile.TriggerSensitivities.Add(new TriggerSensitivity { TriggerId = TriggerIds.Shouting, Multiplier = 1.5f });

            Assert.That(profile.GetSensitivity(TriggerIds.Shouting), Is.EqualTo(1.5f));
        }

        [Test]
        public void GetSensitivity_ExcludedTrigger_IsZero()
        {
            var profile = ScriptableObject.CreateInstance<CharacterProfile>();
            profile.TriggerSensitivities.Add(new TriggerSensitivity { TriggerId = TriggerIds.Staring, Multiplier = 2f });
            profile.ExcludedTriggers.Add(TriggerIds.Staring);

            Assert.That(profile.GetSensitivity(TriggerIds.Staring), Is.EqualTo(0f));
            Assert.That(profile.Applies(TriggerIds.Staring), Is.False);
        }
    }
}
