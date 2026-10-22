using System.Collections.Generic;
using System.Linq;
using AutismVR.Characters;
using AutismVR.Comfort;
using NUnit.Framework;

namespace AutismVR.Tests.EditMode
{
    public class LoudnessDetectorTests
    {
        static LoudnessDetector Detector()
        {
            var catalog = TriggerCatalog.LoadDefault();
            return new LoudnessDetector(catalog.Find(TriggerIds.Shouting), catalog.Find(CalmingIds.WaitsQuietly));
        }

        static List<TriggerHit> Feed(LoudnessDetector detector, float dbfs, float seconds, List<TriggerHit> hits = null)
        {
            hits ??= new List<TriggerHit>();
            for (var t = 0f; t < seconds - 1e-4f; t += 0.1f)
                detector.Sample(dbfs, 0.1f, hits);
            return hits;
        }

        static int Count(List<TriggerHit> hits, string id) => hits.Count(hit => hit.TriggerId == id);

        [Test]
        public void Shout_ShorterThanHold_NoHit()
        {
            Assert.That(Feed(Detector(), -5f, 0.2f), Is.Empty);
        }

        [Test]
        public void Shout_Sustained_OneHitThenRefractory()
        {
            var detector = Detector();
            var hits = Feed(detector, -5f, 1f);
            Assert.That(Count(hits, TriggerIds.Shouting), Is.EqualTo(1));
            Assert.That(hits[0].Channel, Is.EqualTo(Channel.Loudness));
            Assert.That(hits[0].Intensity, Is.EqualTo(1f));

            Feed(detector, -5f, 2.5f, hits);
            Assert.That(Count(hits, TriggerIds.Shouting), Is.EqualTo(2));
        }

        [Test]
        public void Quiet_FiveSeconds_WaitsQuietlyHit()
        {
            var detector = Detector();
            Assert.That(Count(Feed(detector, -50f, 4.9f), CalmingIds.WaitsQuietly), Is.EqualTo(0));

            var hits = Feed(detector, -50f, 5.2f);
            Assert.That(Count(hits, CalmingIds.WaitsQuietly), Is.EqualTo(2), "at 5 s and 10 s");
        }

        [Test]
        public void NormalSpeech_NoHits()
        {
            Assert.That(Feed(Detector(), -30f, 10f), Is.Empty);
        }

        [TestCase(float.NegativeInfinity)]
        [TestCase(float.NaN)]
        public void NoMic_NoHits(float dbfs)
        {
            Assert.That(Feed(Detector(), dbfs, 20f), Is.Empty);
        }

        [Test]
        public void NullDefinitions_NoHits()
        {
            var detector = new LoudnessDetector(null, null);

            Assert.That(Feed(detector, -5f, 3f), Is.Empty);
            Assert.That(Feed(detector, -50f, 10f), Is.Empty);
        }
    }
}
