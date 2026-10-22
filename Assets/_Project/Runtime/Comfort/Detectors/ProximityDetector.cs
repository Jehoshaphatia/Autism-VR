using System.Collections.Generic;
using UnityEngine;

namespace AutismVR.Comfort
{
    // Moving too close or too fast (spec §5.3), measured on the floor plane. Too close fires once and re-arms
    // after the trainee steps back; a fast approach within range also fires; both share one refractory.
    // Keeping a comfortable distance counts as giving space.
    public sealed class ProximityDetector
    {
        const float Epsilon = 1e-4f;

        readonly TriggerDefinition m_Approach;
        readonly TriggerDefinition m_GivesSpace;
        bool m_Armed = true;
        float m_Refractory;
        float? m_LastDistance;
        float m_SpaceFor;

        public ProximityDetector(TriggerDefinition approach, TriggerDefinition givesSpace)
        {
            // A row whose numbers are missing (parsed as 0) would fire every frame, so it disables that output.
            m_Approach = approach != null && approach.DistanceMeters > 0f && approach.RearmDistanceMeters >= approach.DistanceMeters
                         && approach.RefractorySeconds > 0f && approach.SpeedMetersPerSecond > 0f && approach.RangeMeters > 0f
                ? approach
                : null;
            m_GivesSpace = givesSpace != null && givesSpace.DistanceMeters > 0f && givesSpace.HoldSeconds > 0f && givesSpace.RepeatSeconds > 0f
                ? givesSpace
                : null;
        }

        // Forget the last distance, so the next sample has no speed (after a frame too long to measure it).
        public void ResetMotion()
        {
            m_LastDistance = null;
        }

        public void Sample(Vector3 headPosition, Vector3 childPosition, float dt, List<TriggerHit> hits)
        {
            var offset = headPosition - childPosition;
            offset.y = 0f;
            var distance = offset.magnitude;
            m_Refractory -= dt;

            if (m_Approach != null)
            {
                if (distance > m_Approach.RearmDistanceMeters)
                    m_Armed = true;

                var tooClose = m_Armed && distance < m_Approach.DistanceMeters;
                var speed = m_LastDistance.HasValue && dt > Epsilon ? (m_LastDistance.Value - distance) / dt : 0f;
                var rushing = speed > m_Approach.SpeedMetersPerSecond && distance < m_Approach.RangeMeters;
                if ((tooClose || rushing) && m_Refractory <= Epsilon)
                {
                    hits.Add(new TriggerHit(m_Approach.Id, Channel.Proximity));
                    m_Refractory = m_Approach.RefractorySeconds;
                    if (tooClose)
                        m_Armed = false;
                }
            }

            if (m_GivesSpace != null && distance >= m_GivesSpace.DistanceMeters)
            {
                m_SpaceFor += dt;
                if (m_SpaceFor >= m_GivesSpace.HoldSeconds - Epsilon)
                {
                    hits.Add(new TriggerHit(m_GivesSpace.Id, Channel.Proximity));
                    m_SpaceFor -= m_GivesSpace.RepeatSeconds;
                }
            }
            else
            {
                m_SpaceFor = 0f;
            }

            m_LastDistance = distance;
        }
    }
}
