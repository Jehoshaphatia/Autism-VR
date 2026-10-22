using NUnit.Framework;
using UnityEngine;

namespace AutismVR.Tests.EditMode
{
    // Shared reach-and-view rule for buttons in front of the trainer (spec §3, F3 plan Task 5).
    static class ButtonPlacementAssert
    {
        const float AssumedEyeHeight = 1.6f;

        public static void InReachAndInView(Transform button, Transform trainerSpawn)
        {
            var local = trainerSpawn.InverseTransformPoint(button.position);
            Assert.That(local.z, Is.InRange(0.5f, 0.7f), $"{button.name}: distance in front of the trainer");
            Assert.That(local.y, Is.InRange(1.0f, 1.2f), $"{button.name}: height above the floor");
            var degreesBelowEyeLine = Mathf.Atan2(AssumedEyeHeight - local.y, local.z) * Mathf.Rad2Deg;
            Assert.That(degreesBelowEyeLine, Is.LessThanOrEqualTo(35f), $"{button.name}: angle below eye line");
        }

        public static void SitsOnPedestal(Transform button, Transform pedestal)
        {
            var buttonBottom = button.GetComponent<Collider>().bounds.min.y;
            var pedestalTop = pedestal.GetComponent<Renderer>().bounds.max.y;
            Assert.That(Mathf.Abs(buttonBottom - pedestalTop), Is.LessThan(0.005f), $"{button.name} gap to its pedestal");
        }
    }
}
