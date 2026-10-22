using System.Collections.Generic;
using UnityEngine;

namespace AutismVR.Comfort
{
    // Reaching toward or touching the child (spec §5.3): a hand near the child's head or chest, or a hand
    // closing on the child fast within range. A hand that has just started tracking has no speed yet.
    public sealed class HandReachDetector
    {
        const float Epsilon = 1e-4f;

        readonly TriggerDefinition m_Reach;
        readonly List<float> m_LastChestDistance = new List<float>();
        float m_Refractory;

        public HandReachDetector(TriggerDefinition reach)
        {
            m_Reach = reach;
        }

        public void Sample(IReadOnlyList<Vector3> hands, Vector3 childHead, Vector3 childChest, float dt, List<TriggerHit> hits)
        {
            m_Refractory -= dt;
            if (m_Reach == null || hands == null)
            {
                m_LastChestDistance.Clear();
                return;
            }

            // Hands are matched by index, so when the tracked set changes every hand starts without a speed.
            if (m_LastChestDistance.Count != hands.Count)
                m_LastChestDistance.Clear();

            var reaching = false;
            for (var i = 0; i < hands.Count; i++)
            {
                var toChest = Vector3.Distance(hands[i], childChest);
                var toHead = Vector3.Distance(hands[i], childHead);
                if (toChest <= m_Reach.DistanceMeters || toHead <= m_Reach.DistanceMeters)
                    reaching = true;
                if (i < m_LastChestDistance.Count && dt > Epsilon)
                {
                    var closing = (m_LastChestDistance[i] - toChest) / dt;
                    if (closing > m_Reach.SpeedMetersPerSecond && toChest < m_Reach.RangeMeters)
                        reaching = true;
                }

                if (i < m_LastChestDistance.Count)
                    m_LastChestDistance[i] = toChest;
                else
                    m_LastChestDistance.Add(toChest);
            }

            if (reaching && m_Refractory <= Epsilon)
            {
                hits.Add(new TriggerHit(m_Reach.Id, Channel.Hands));
                m_Refractory = m_Reach.RefractorySeconds;
            }
        }
    }
}
