using System.Collections.Generic;
using AutismVR.Characters;
using AutismVR.Comfort;
using NUnit.Framework;
using UnityEngine;

namespace AutismVR.Tests.EditMode
{
    public class StareDetectorTests
    {
        // The child's head at 1.1 m, 1.2 m in front of a trainee whose eyes are at 1.6 m.
        static readonly Vector3 ChildHead = new Vector3(0f, 1.1f, 1.2f);
        static readonly Vector3 Head = new Vector3(0f, 1.6f, 0f);
        static Vector3 AtChild => (ChildHead - Head).normalized;
        static Vector3 Away => Vector3.right;

        static StareDetector Detector() => new StareDetector(TriggerCatalog.LoadDefault().Find(TriggerIds.Staring));

        static List<TriggerHit> Look(StareDetector detector, Vector3 forward, float seconds, List<TriggerHit> hits = null)
        {
            hits ??= new List<TriggerHit>();
            for (var t = 0f; t < seconds - 1e-4f; t += 0.1f)
                detector.Sample(Head, forward, ChildHead, 0.1f, hits);
            return hits;
        }

        [Test]
        public void Glance_UnderThreshold_NoHit()
        {
            var detector = Detector();
            var hits = Look(detector, AtChild, 3f);
            Look(detector, Away, 2f, hits);

            Assert.That(hits, Is.Empty);
        }

        [Test]
        public void Stare_TwentySeconds_ThreeHits()
        {
            var hits = Look(Detector(), AtChild, 20f);

            Assert.That(hits.Count, Is.EqualTo(3), "after the 5 s threshold, one hit per further 5 s: at 10, 15 and 20 s");
            Assert.That(hits[0].TriggerId, Is.EqualTo(TriggerIds.Staring));
            Assert.That(hits[0].Channel, Is.EqualTo(Channel.Gaze));
        }

        [Test]
        public void Stare_LookAwayResetsTimer()
        {
            var detector = Detector();
            var hits = Look(detector, AtChild, 9f);
            Look(detector, Away, 0.5f, hits);
            Look(detector, AtChild, 9f, hits);

            Assert.That(hits, Is.Empty);
        }

        [Test]
        public void Stare_OutsideTenDegreeCone_NoHit()
        {
            var fifteenDegreesOff = Quaternion.AngleAxis(15f, Vector3.up) * AtChild;

            Assert.That(Look(Detector(), fifteenDegreesOff, 30f), Is.Empty);
        }

        [Test]
        public void Stare_ZeroForward_NoHit()
        {
            Assert.That(Look(Detector(), Vector3.zero, 10f), Is.Empty);
        }

        [Test]
        public void Stare_HeadAtChild_NoHit()
        {
            var detector = Detector();
            var hits = new List<TriggerHit>();
            for (var i = 0; i < 100; i++)
                detector.Sample(ChildHead, Vector3.forward, ChildHead, 0.1f, hits);

            Assert.That(hits, Is.Empty);
        }
    }
}
