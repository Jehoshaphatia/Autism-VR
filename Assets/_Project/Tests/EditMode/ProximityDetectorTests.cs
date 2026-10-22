using System.Collections.Generic;
using System.Linq;
using AutismVR.Characters;
using AutismVR.Comfort;
using NUnit.Framework;
using UnityEngine;

namespace AutismVR.Tests.EditMode
{
    public class ProximityDetectorTests
    {
        // The child stands at the origin; the trainee's head (1.6 m high) moves along +z.
        static readonly Vector3 Child = Vector3.zero;

        static ProximityDetector Detector()
        {
            var catalog = TriggerCatalog.LoadDefault();
            return new ProximityDetector(catalog.Find(TriggerIds.ApproachTooClose), catalog.Find(CalmingIds.GivesSpace));
        }

        static Vector3 At(float distance) => new Vector3(0f, 1.6f, distance);

        static void Move(ProximityDetector detector, float from, float to, float seconds, List<TriggerHit> hits)
        {
            var steps = Mathf.Max(1, Mathf.RoundToInt(seconds / 0.1f));
            for (var i = 1; i <= steps; i++)
                detector.Sample(At(Mathf.Lerp(from, to, i / (float)steps)), Child, seconds / steps, hits);
        }

        static void Stay(ProximityDetector detector, float distance, float seconds, List<TriggerHit> hits) =>
            Move(detector, distance, distance, seconds, hits);

        static int Count(List<TriggerHit> hits, string id) => hits.Count(hit => hit.TriggerId == id);

        [Test]
        public void WalkInToHalfMetre_OneHitUntilRearmed()
        {
            var detector = Detector();
            var hits = new List<TriggerHit>();
            Stay(detector, 2f, 0.1f, hits);
            Move(detector, 2f, 0.5f, 3f, hits);
            Stay(detector, 0.5f, 5f, hits);
            Assert.That(Count(hits, TriggerIds.ApproachTooClose), Is.EqualTo(1));
            Assert.That(hits.First(hit => hit.TriggerId == TriggerIds.ApproachTooClose).Channel, Is.EqualTo(Channel.Proximity));

            Move(detector, 0.5f, 1.2f, 2f, hits);
            Move(detector, 1.2f, 0.5f, 2f, hits);
            Assert.That(Count(hits, TriggerIds.ApproachTooClose), Is.EqualTo(2));
        }

        [Test]
        public void RushIn_FastApproachHit()
        {
            var detector = Detector();
            var hits = new List<TriggerHit>();
            Stay(detector, 2f, 0.1f, hits);
            Move(detector, 2f, 1.2f, 0.4f, hits);

            Assert.That(Count(hits, TriggerIds.ApproachTooClose), Is.EqualTo(1));
        }

        [Test]
        public void SlowApproachToOneMetre_NoHit()
        {
            var detector = Detector();
            var hits = new List<TriggerHit>();
            Stay(detector, 3f, 0.1f, hits);
            Move(detector, 3f, 1f, 4f, hits);
            Stay(detector, 1f, 5f, hits);

            Assert.That(Count(hits, TriggerIds.ApproachTooClose), Is.EqualTo(0));
        }

        [Test]
        public void KeepingTwoMetres_GivesSpaceEveryFiveSeconds()
        {
            var hits = new List<TriggerHit>();
            Stay(Detector(), 2f, 10.05f, hits);

            Assert.That(Count(hits, CalmingIds.GivesSpace), Is.EqualTo(2));
            Assert.That(Count(hits, TriggerIds.ApproachTooClose), Is.EqualTo(0));
        }

        [Test]
        public void Proximity_SamePosition_NoNaN()
        {
            var detector = Detector();
            var hits = new List<TriggerHit>();
            for (var i = 0; i < 50; i++)
                detector.Sample(Child, Child, i % 2 == 0 ? 0.1f : 0f, hits);

            Assert.That(hits.All(hit => !float.IsNaN(hit.Intensity)), Is.True);
            Assert.That(Count(hits, TriggerIds.ApproachTooClose), Is.EqualTo(1));
        }
    }
}
