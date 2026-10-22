using System.Collections.Generic;
using AutismVR.Characters;
using AutismVR.Comfort;
using NUnit.Framework;
using UnityEngine;

namespace AutismVR.Tests.EditMode
{
    public class HandReachDetectorTests
    {
        static readonly Vector3 ChildHead = new Vector3(0f, 1.1f, 0f);
        static readonly Vector3 ChildChest = new Vector3(0f, 0.9f, 0f);

        static HandReachDetector Detector() => new HandReachDetector(TriggerCatalog.LoadDefault().Find(TriggerIds.ReachingOrTouch));

        static void Hold(HandReachDetector detector, Vector3[] hands, float seconds, List<TriggerHit> hits)
        {
            for (var t = 0f; t < seconds - 1e-4f; t += 0.1f)
                detector.Sample(hands, ChildHead, ChildChest, 0.1f, hits);
        }

        [Test]
        public void HandNearHead_Hit()
        {
            var hits = new List<TriggerHit>();
            Hold(Detector(), new[] { ChildHead + new Vector3(0f, 0f, 0.3f) }, 0.2f, hits);

            Assert.That(hits.Count, Is.EqualTo(1));
            Assert.That(hits[0].TriggerId, Is.EqualTo(TriggerIds.ReachingOrTouch));
            Assert.That(hits[0].Channel, Is.EqualTo(Channel.Hands));
        }

        [Test]
        public void HandSwipeTowardChest_Hit()
        {
            var detector = Detector();
            var hits = new List<TriggerHit>();
            Hold(detector, new[] { ChildChest + new Vector3(0f, 0f, 0.9f) }, 0.1f, hits);
            detector.Sample(new[] { ChildChest + new Vector3(0f, 0f, 0.7f) }, ChildHead, ChildChest, 0.1f, hits);
            detector.Sample(new[] { ChildChest + new Vector3(0f, 0f, 0.5f) }, ChildHead, ChildChest, 0.1f, hits);

            Assert.That(hits.Count, Is.EqualTo(1));
        }

        [Test]
        public void HandsAtSides_NoHit()
        {
            // Trainee 1.2 m away with hands hanging at their sides.
            var hands = new[] { new Vector3(-0.25f, 0.9f, 1.2f), new Vector3(0.25f, 0.9f, 1.2f) };
            var hits = new List<TriggerHit>();
            Hold(Detector(), hands, 10f, hits);

            Assert.That(hits, Is.Empty);
        }

        [Test]
        public void HandReach_NoHands_NoHit()
        {
            var hits = new List<TriggerHit>();
            Hold(Detector(), new Vector3[0], 5f, hits);

            Assert.That(hits, Is.Empty);
        }

        [Test]
        public void HandReach_RefractoryBetweenHits()
        {
            var hits = new List<TriggerHit>();
            Hold(Detector(), new[] { ChildChest + new Vector3(0f, 0f, 0.2f) }, 3f, hits);

            Assert.That(hits.Count, Is.EqualTo(2), "at 0 s and again after the 2 s refractory");
        }

        [Test]
        public void HandLosingTracking_OtherHandIsNotReadAsJumping()
        {
            // Two hands, then the far one stops tracking: the near one shifts to index 0 but did not move.
            var near = ChildChest + new Vector3(0f, 0f, 0.5f);
            var far = ChildChest + new Vector3(0f, 0f, 1.0f);
            var detector = Detector();
            var hits = new List<TriggerHit>();
            Hold(detector, new[] { far, near }, 0.5f, hits);
            Hold(detector, new[] { near }, 0.5f, hits);

            Assert.That(hits, Is.Empty);
        }

        [Test]
        public void HandAppearing_NearChild_NoSpeedSpike()
        {
            // A hand that starts tracking at 0.6 m must not read as a jump from nowhere.
            var detector = Detector();
            var hits = new List<TriggerHit>();
            Hold(detector, new Vector3[0], 0.5f, hits);
            Hold(detector, new[] { ChildChest + new Vector3(0f, 0f, 0.6f) }, 2f, hits);

            Assert.That(hits, Is.Empty);
        }
    }
}
