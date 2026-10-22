using System;
using System.Linq;
using AutismVR.Characters;
using AutismVR.Comfort;
using NUnit.Framework;

namespace AutismVR.Tests.EditMode
{
    public class TriggerCatalogTests
    {
        static TriggerCatalog Default => TriggerCatalog.LoadDefault();

        static ChildReaction? Reaction(string name) => string.IsNullOrEmpty(name) ? null : Enum.Parse<ChildReaction>(name);

        [Test]
        public void DefaultCatalog_HasEveryTriggerAndCalmingId()
        {
            var ids = Default.Entries.Select(e => e.Id).ToList();
            var expected = TriggerIds.All.Concat(CalmingIds.All).ToList();

            Assert.That(ids, Is.EquivalentTo(expected));
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
        }

        // The provisional table in the F4 plan's Global Constraints.
        [TestCase(TriggerIds.Shouting, Channel.Loudness | Channel.Words, -0.15f, "Withdrawn")]
        [TestCase(TriggerIds.DemandingEyeContact, Channel.Words, -0.12f, "LooksAway")]
        [TestCase(TriggerIds.Staring, Channel.Gaze, -0.02f, "LooksAway")]
        [TestCase(TriggerIds.RapidQuestions, Channel.Words, -0.08f, "")]
        [TestCase(TriggerIds.MultiStepInstructions, Channel.Words, -0.06f, "")]
        [TestCase(TriggerIds.Sarcasm, Channel.Words, -0.05f, "")]
        [TestCase(TriggerIds.Anger, Channel.Words | Channel.Loudness, -0.12f, "")]
        [TestCase(TriggerIds.PressuringSpeech, Channel.Words, -0.10f, "")]
        [TestCase(TriggerIds.ApproachTooClose, Channel.Proximity, -0.12f, "LooksAway")]
        [TestCase(TriggerIds.ReachingOrTouch, Channel.Hands, -0.15f, "Withdrawn")]
        [TestCase(TriggerIds.DemandsDuringMeltdown, Channel.Words, -0.10f, "")]
        [TestCase(CalmingIds.CalmQuietVoice, Channel.Words, 0.03f, "")]
        [TestCase(CalmingIds.WaitsQuietly, Channel.Loudness, 0.02f, "")]
        [TestCase(CalmingIds.LetsChildLookAway, Channel.Words, 0.05f, "")]
        [TestCase(CalmingIds.GivesSpace, Channel.Proximity, 0.04f, "")]
        public void DefaultCatalog_MatchesProvisionalTable(string id, Channel channels, float effect, string reaction)
        {
            var entry = Default.Find(id);

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Channels, Is.EqualTo(channels));
            Assert.That(entry.Effect, Is.EqualTo(effect).Within(1e-6f));
            Assert.That(entry.Reaction, Is.EqualTo(Reaction(reaction)));
            Assert.That(entry.Kind, Is.EqualTo(effect < 0f ? TriggerKind.Trigger : TriggerKind.Calming));
        }

        [Test]
        public void DefaultCatalog_HasProvisionalChannelParameters()
        {
            var shouting = Default.Find(TriggerIds.Shouting);
            Assert.That((shouting.ThresholdDbfs, shouting.HoldSeconds, shouting.RefractorySeconds), Is.EqualTo((-15f, 0.3f, 2f)));

            var quiet = Default.Find(CalmingIds.WaitsQuietly);
            Assert.That((quiet.ThresholdDbfs, quiet.HoldSeconds, quiet.RepeatSeconds), Is.EqualTo((-40f, 5f, 5f)));

            var staring = Default.Find(TriggerIds.Staring);
            Assert.That((staring.ConeDegrees, staring.HoldSeconds, staring.RepeatSeconds), Is.EqualTo((10f, 5f, 5f)));

            var close = Default.Find(TriggerIds.ApproachTooClose);
            Assert.That((close.DistanceMeters, close.RearmDistanceMeters, close.SpeedMetersPerSecond, close.RangeMeters, close.RefractorySeconds),
                Is.EqualTo((0.8f, 1.0f, 1.0f, 2.0f, 2f)));

            var space = Default.Find(CalmingIds.GivesSpace);
            Assert.That((space.DistanceMeters, space.HoldSeconds, space.RepeatSeconds), Is.EqualTo((1.5f, 5f, 5f)));

            var reach = Default.Find(TriggerIds.ReachingOrTouch);
            Assert.That((reach.DistanceMeters, reach.SpeedMetersPerSecond, reach.RangeMeters, reach.RefractorySeconds),
                Is.EqualTo((0.35f, 1.5f, 1.0f, 2f)));
        }

        [Test]
        public void DefaultCatalog_EntriesAreEnabledProvisionalAndDescribed()
        {
            foreach (var entry in Default.Entries)
            {
                Assert.That(entry.Enabled, Is.True, entry.Id);
                Assert.That(entry.Provisional, Is.True, entry.Id);
                Assert.That(entry.Name, Is.Not.Empty, entry.Id);
                Assert.That(entry.Description, Is.Not.Empty, entry.Id);
            }
        }

        [Test]
        public void TryParse_RoundTripsToJson()
        {
            var original = Default;

            Assert.That(TriggerCatalog.TryParse(original.ToJson(), out var parsed), Is.True);
            Assert.That(parsed.Entries.Count, Is.EqualTo(original.Entries.Count));
            foreach (var a in original.Entries)
            {
                var b = parsed.Find(a.Id);
                Assert.That(b, Is.Not.Null, a.Id);
                Assert.That((b.Kind, b.Channels, b.Effect, b.Reaction, b.Enabled, b.Provisional, b.Name, b.Description),
                    Is.EqualTo((a.Kind, a.Channels, a.Effect, a.Reaction, a.Enabled, a.Provisional, a.Name, a.Description)), a.Id);
                Assert.That((b.ThresholdDbfs, b.HoldSeconds, b.RefractorySeconds, b.RepeatSeconds, b.ConeDegrees),
                    Is.EqualTo((a.ThresholdDbfs, a.HoldSeconds, a.RefractorySeconds, a.RepeatSeconds, a.ConeDegrees)), a.Id);
                Assert.That((b.DistanceMeters, b.RearmDistanceMeters, b.SpeedMetersPerSecond, b.RangeMeters),
                    Is.EqualTo((a.DistanceMeters, a.RearmDistanceMeters, a.SpeedMetersPerSecond, a.RangeMeters)), a.Id);
            }
        }

        [TestCase("not json")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("{}")]
        [TestCase("{\"version\":1,\"entries\":[]}")]
        public void TryParse_Garbage_ReturnsFalse(string json)
        {
            Assert.That(TriggerCatalog.TryParse(json, out var catalog), Is.False);
            Assert.That(catalog, Is.Null);
        }

        [Test]
        public void TryParse_UnknownChannel_SkipsEntryKeepsOthers()
        {
            const string json = "{\"version\":1,\"entries\":[" +
                "{\"id\":\"sniffing\",\"name\":\"Sniffing\",\"description\":\"d\",\"kind\":\"trigger\",\"channels\":[\"smell\"],\"effect\":-0.1,\"enabled\":true}," +
                "{\"id\":\"shouting\",\"name\":\"Shouting\",\"description\":\"d\",\"kind\":\"trigger\",\"channels\":[\"loudness\"],\"effect\":-0.15,\"reaction\":\"Withdrawn\",\"enabled\":true}]}";

            Assert.That(TriggerCatalog.TryParse(json, out var catalog), Is.True);
            Assert.That(catalog.Entries.Select(e => e.Id), Is.EqualTo(new[] { "shouting" }));
        }

        [Test]
        public void TryParse_UnknownReactionOrKind_SkipsEntry()
        {
            const string json = "{\"version\":1,\"entries\":[" +
                "{\"id\":\"a\",\"name\":\"A\",\"description\":\"d\",\"kind\":\"trigger\",\"channels\":[\"words\"],\"effect\":-0.1,\"reaction\":\"Sulks\",\"enabled\":true}," +
                "{\"id\":\"b\",\"name\":\"B\",\"description\":\"d\",\"kind\":\"mood\",\"channels\":[\"words\"],\"effect\":-0.1,\"enabled\":true}," +
                "{\"id\":\"c\",\"name\":\"C\",\"description\":\"d\",\"kind\":\"calming\",\"channels\":[\"words\"],\"effect\":0.1,\"enabled\":true}]}";

            Assert.That(TriggerCatalog.TryParse(json, out var catalog), Is.True);
            Assert.That(catalog.Entries.Select(e => e.Id), Is.EqualTo(new[] { "c" }));
        }

        [Test]
        public void IsActive_DisabledEntry_False()
        {
            const string json = "{\"version\":1,\"entries\":[" +
                "{\"id\":\"shouting\",\"name\":\"Shouting\",\"description\":\"d\",\"kind\":\"trigger\",\"channels\":[\"loudness\"],\"effect\":-0.15,\"enabled\":false}," +
                "{\"id\":\"anger\",\"name\":\"Anger\",\"description\":\"d\",\"kind\":\"trigger\",\"channels\":[\"words\"],\"effect\":-0.12,\"enabled\":true}]}";

            Assert.That(TriggerCatalog.TryParse(json, out var catalog), Is.True);
            Assert.That(catalog.IsActive("shouting"), Is.False);
            Assert.That(catalog.IsActive("anger"), Is.True);
            Assert.That(catalog.IsActive("missing"), Is.False);
        }

        [Test]
        public void TryParse_WrongSignEffect_SkipsEntry()
        {
            const string json = "{\"version\":1,\"entries\":[" +
                "{\"id\":\"a\",\"name\":\"A\",\"description\":\"d\",\"kind\":\"trigger\",\"channels\":[\"words\"],\"effect\":0.1,\"enabled\":true}," +
                "{\"id\":\"b\",\"name\":\"B\",\"description\":\"d\",\"kind\":\"calming\",\"channels\":[\"words\"],\"effect\":-0.1,\"enabled\":true}," +
                "{\"id\":\"c\",\"name\":\"C\",\"description\":\"d\",\"kind\":\"trigger\",\"channels\":[\"words\"],\"effect\":0,\"enabled\":true}," +
                "{\"id\":\"d\",\"name\":\"D\",\"description\":\"d\",\"kind\":\"calming\",\"channels\":[\"words\"],\"effect\":0.1,\"enabled\":true}]}";

            Assert.That(TriggerCatalog.TryParse(json, out var catalog), Is.True);
            Assert.That(catalog.Entries.Select(e => e.Id), Is.EqualTo(new[] { "d" }));
        }
    }
}
