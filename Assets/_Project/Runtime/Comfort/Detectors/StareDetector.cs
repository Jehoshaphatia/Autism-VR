using System.Collections.Generic;
using UnityEngine;

namespace AutismVR.Comfort
{
    // Sustained staring at the child's face (spec §5.2): after the threshold, one hit per repeat interval while
    // the gaze stays in the cone. Glances shorter than the threshold change nothing; looking away resets.
    // Uses head direction; eye direction can replace it where eye tracking exists.
    public sealed class StareDetector
    {
        const float Epsilon = 1e-4f;

        readonly TriggerDefinition m_Staring;
        float m_StaringFor;
        float m_NextHitAt;

        public StareDetector(TriggerDefinition staring)
        {
            // A row whose numbers are missing (parsed as 0) would fire every frame, so it disables the detector.
            m_Staring = staring != null && staring.HoldSeconds >= 0f && staring.RepeatSeconds > 0f && staring.ConeDegrees > 0f ? staring : null;
            Reset();
        }

        public void Sample(Vector3 headPosition, Vector3 headForward, Vector3 childHead, float dt, List<TriggerHit> hits)
        {
            if (m_Staring == null)
                return;

            var toChild = childHead - headPosition;
            var looking = headForward.sqrMagnitude > Epsilon && toChild.sqrMagnitude > Epsilon
                          && Vector3.Angle(headForward, toChild) <= m_Staring.ConeDegrees;
            if (!looking)
            {
                Reset();
                return;
            }

            m_StaringFor += dt;
            if (m_StaringFor >= m_NextHitAt - Epsilon)
            {
                hits.Add(new TriggerHit(m_Staring.Id, Channel.Gaze));
                m_NextHitAt += m_Staring.RepeatSeconds;
            }
        }

        void Reset()
        {
            m_StaringFor = 0f;
            m_NextHitAt = m_Staring == null ? 0f : m_Staring.HoldSeconds + m_Staring.RepeatSeconds;
        }
    }
}
