using AutismVR.Characters;
using AutismVR.Comfort;
using NUnit.Framework;

namespace AutismVR.Tests.EditMode
{
    public class SessionLogTests
    {
        [Test]
        public void Record_KeepsOrderAndFields()
        {
            var log = new SessionLog();
            log.Record(1.5f, new ComfortChange(TriggerIds.Shouting, Channel.Loudness, 0.5f, 0.35f));
            log.Record(4f, new ComfortChange(TriggerIds.DemandingEyeContact, Channel.Words, 0.35f, 0.23f), "look at me");

            Assert.That(log.Events.Count, Is.EqualTo(2));
            var first = log.Events[0];
            Assert.That((first.Time, first.TriggerId, first.Channel, first.TranscriptExcerpt, first.ComfortBefore, first.ComfortAfter),
                Is.EqualTo((1.5f, TriggerIds.Shouting, Channel.Loudness, (string)null, 0.5f, 0.35f)));
            Assert.That(log.Events[1].TriggerId, Is.EqualTo(TriggerIds.DemandingEyeContact));
            Assert.That(log.Events[1].TranscriptExcerpt, Is.EqualTo("look at me"));
        }

        [Test]
        public void TriggerCounts_CountsTriggersNotCalming()
        {
            var log = new SessionLog();
            log.Record(1f, new ComfortChange(TriggerIds.Shouting, Channel.Loudness, 0.5f, 0.35f));
            log.Record(2f, new ComfortChange(CalmingIds.GivesSpace, Channel.Proximity, 0.35f, 0.39f));
            log.Record(5f, new ComfortChange(TriggerIds.Shouting, Channel.Words, 0.39f, 0.24f));
            log.Record(6f, new ComfortChange(TriggerIds.Staring, Channel.Gaze, 0.24f, 0.20f));

            var counts = log.TriggerCounts(TriggerCatalog.LoadDefault());

            Assert.That(counts.Count, Is.EqualTo(2));
            Assert.That(counts[TriggerIds.Shouting], Is.EqualTo(2));
            Assert.That(counts[TriggerIds.Staring], Is.EqualTo(1));
            Assert.That(counts.ContainsKey(CalmingIds.GivesSpace), Is.False);
        }

        [Test]
        public void TriggerCounts_EmptySession_Empty()
        {
            var log = new SessionLog();

            Assert.That(log.TriggerCounts(TriggerCatalog.LoadDefault()), Is.Empty);
            Assert.That((log.MeltdownHappened, log.Deescalated, log.Greeted), Is.EqualTo((false, false, false)));
        }
    }
}
