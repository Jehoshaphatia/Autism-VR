using System.Collections.Generic;
using AutismVR.Characters;
using AutismVR.Comfort;
using NUnit.Framework;
using UnityEngine;

namespace AutismVR.Tests.EditMode
{
    public class ComfortModelTests
    {
        readonly List<Object> m_Created = new List<Object>();

        [TearDown]
        public void DestroyProfiles()
        {
            foreach (var created in m_Created)
                Object.DestroyImmediate(created);
            m_Created.Clear();
        }

        CharacterProfile Profile(float start = 0.5f, float threshold = 0.75f, float recovery = 0.02f,
            string sensitiveTo = null, float multiplier = 1f, string excluded = null)
        {
            var profile = ScriptableObject.CreateInstance<CharacterProfile>();
            m_Created.Add(profile);
            profile.StartingComfort = start;
            profile.GreetingThreshold = threshold;
            profile.RecoveryPerSecond = recovery;
            if (sensitiveTo != null)
                profile.TriggerSensitivities.Add(new TriggerSensitivity { TriggerId = sensitiveTo, Multiplier = multiplier });
            if (excluded != null)
                profile.ExcludedTriggers.Add(excluded);
            return profile;
        }

        static TriggerCatalog Catalog => TriggerCatalog.LoadDefault();

        static void AdvanceFor(ComfortModel model, float seconds)
        {
            for (var t = 0f; t < seconds - 1e-4f; t += 0.1f)
                model.Advance(0.1f);
        }

        static ComfortModel InMeltdownAtZero(ComfortModel model)
        {
            while (!model.InMeltdown)
                model.Apply(TriggerIds.Shouting, Channel.Loudness);
            while (model.Comfort > 0f)
                model.Apply(TriggerIds.Shouting, Channel.Loudness);
            return model;
        }

        [Test]
        public void Comfort_StartsAtProfileStartingComfort()
        {
            Assert.That(new ComfortModel(Profile(start: 0.35f), Catalog).Comfort, Is.EqualTo(0.35f));
        }

        [Test]
        public void Apply_Shouting_ScalesBySensitivity()
        {
            var model = new ComfortModel(Profile(start: 0.5f, sensitiveTo: TriggerIds.Shouting, multiplier: 1.5f), Catalog);

            var change = model.Apply(TriggerIds.Shouting, Channel.Loudness);

            Assert.That(change.HasValue, Is.True);
            Assert.That(model.Comfort, Is.EqualTo(0.5f - 0.225f).Within(1e-5f));
            Assert.That(change.Value.Before, Is.EqualTo(0.5f));
            Assert.That(change.Value.After, Is.EqualTo(model.Comfort));
            Assert.That(change.Value.TriggerId, Is.EqualTo(TriggerIds.Shouting));
            Assert.That(change.Value.Channel, Is.EqualTo(Channel.Loudness));
        }

        [Test]
        public void Apply_Calming_IgnoresSensitivity()
        {
            var model = new ComfortModel(Profile(start: 0.5f, sensitiveTo: CalmingIds.GivesSpace, multiplier: 3f), Catalog);

            model.Apply(CalmingIds.GivesSpace, Channel.Proximity);

            Assert.That(model.Comfort, Is.EqualTo(0.54f).Within(1e-5f));
        }

        [Test]
        public void Apply_Intensity_ScalesEffect()
        {
            var model = new ComfortModel(Profile(start: 0.5f), Catalog);

            model.Apply(TriggerIds.Shouting, Channel.Loudness, 0.5f);

            Assert.That(model.Comfort, Is.EqualTo(0.425f).Within(1e-5f));
        }

        [Test]
        public void Comfort_ClampedToUnitRange()
        {
            var model = new ComfortModel(Profile(start: 0.5f, threshold: 1f), Catalog);
            for (var i = 0; i < 20; i++)
                model.Apply(TriggerIds.Shouting, Channel.Loudness);
            Assert.That(model.Comfort, Is.EqualTo(0f));

            for (var i = 0; i < 100; i++)
                model.Apply(CalmingIds.GivesSpace, Channel.Proximity);
            Assert.That(model.Comfort, Is.EqualTo(1f));
        }

        [Test]
        public void Comfort_EntersMeltdownAtOrBelowEnterLevel()
        {
            var model = new ComfortModel(Profile(start: 0.26f), Catalog);

            model.Apply(TriggerIds.Shouting, Channel.Loudness);
            Assert.That(model.Comfort, Is.GreaterThan(ComfortRules.MeltdownEnter));
            Assert.That(model.InMeltdown, Is.False);

            model.Apply(TriggerIds.Sarcasm, Channel.Words);
            Assert.That(model.Comfort, Is.LessThanOrEqualTo(ComfortRules.MeltdownEnter));
            Assert.That(model.InMeltdown, Is.True);
            Assert.That(model.MeltdownHappened, Is.True);
            Assert.That(model.Deescalated, Is.False);
        }

        [Test]
        public void Comfort_MeltdownRecoversAfterQuiet()
        {
            var model = InMeltdownAtZero(new ComfortModel(Profile(start: 0.3f, recovery: 0.02f), Catalog));

            AdvanceFor(model, ComfortRules.QuietSecondsBeforeRecovery);
            Assert.That(model.Comfort, Is.EqualTo(0f).Within(0.003f), "no recovery inside the quiet window");

            AdvanceFor(model, 16f);
            Assert.That(model.Comfort, Is.GreaterThanOrEqualTo(ComfortRules.MeltdownExit));
            Assert.That(model.InMeltdown, Is.False);
            Assert.That(model.Deescalated, Is.True);
            Assert.That(model.MeltdownHappened, Is.True);
        }

        [Test]
        public void Advance_OutsideMeltdown_NoDrift()
        {
            var model = new ComfortModel(Profile(start: 0.5f), Catalog);

            AdvanceFor(model, 60f);

            Assert.That(model.Comfort, Is.EqualTo(0.5f));
        }

        [Test]
        public void Advance_TriggerWithinQuietWindow_DelaysRecovery()
        {
            var model = InMeltdownAtZero(new ComfortModel(Profile(start: 0.3f), Catalog));

            AdvanceFor(model, 2f);
            model.Apply(TriggerIds.Anger, Channel.Words);
            AdvanceFor(model, 2f);
            Assert.That(model.Comfort, Is.EqualTo(0f));

            AdvanceFor(model, 1.5f);
            Assert.That(model.Comfort, Is.GreaterThan(0f));
        }

        [Test]
        public void Advance_ClampsLongDt()
        {
            var model = InMeltdownAtZero(new ComfortModel(Profile(start: 0.3f, recovery: 0.02f), Catalog));
            AdvanceFor(model, ComfortRules.QuietSecondsBeforeRecovery + 0.1f);
            var before = model.Comfort;

            model.Advance(5f);

            Assert.That(model.Comfort - before, Is.LessThanOrEqualTo(ComfortRules.MaxDeltaTime * 0.02f + 1e-6f));
        }

        [Test]
        public void Comfort_CrossingThreshold_GreetsOnceThenIgnoresTriggers()
        {
            var model = new ComfortModel(Profile(start: 0.7f, threshold: 0.75f), Catalog);

            model.Apply(CalmingIds.GivesSpace, Channel.Proximity);
            Assert.That(model.Greeted, Is.False);
            model.Apply(CalmingIds.GivesSpace, Channel.Proximity);
            Assert.That(model.Greeted, Is.True);

            var comfort = model.Comfort;
            Assert.That(model.Apply(TriggerIds.Shouting, Channel.Loudness), Is.Null);
            Assert.That(model.Comfort, Is.EqualTo(comfort));
            Assert.That(model.Greeted, Is.True);
        }

        [Test]
        public void Apply_ExcludedOrDisabledOrUnknown_ReturnsNull()
        {
            var excluded = new ComfortModel(Profile(excluded: TriggerIds.Shouting), Catalog);
            Assert.That(excluded.Apply(TriggerIds.Shouting, Channel.Loudness), Is.Null);
            Assert.That(excluded.Comfort, Is.EqualTo(0.5f));

            const string json = "{\"version\":1,\"entries\":[" +
                "{\"id\":\"anger\",\"name\":\"Anger\",\"description\":\"d\",\"kind\":\"trigger\",\"channels\":[\"words\"],\"effect\":-0.12,\"enabled\":false}]}";
            Assert.That(TriggerCatalog.TryParse(json, out var catalog), Is.True);
            var disabled = new ComfortModel(Profile(), catalog);
            Assert.That(disabled.Apply(TriggerIds.Anger, Channel.Words), Is.Null);
            Assert.That(disabled.Apply("not_in_catalog", Channel.Words), Is.Null);
            Assert.That(disabled.Comfort, Is.EqualTo(0.5f));
        }
    }
}
