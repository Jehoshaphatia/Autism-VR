using System.Collections.Generic;

namespace AutismVR.Comfort
{
    // Shouting is volume, not words (spec §5.3): a sustained level above the threshold. Staying below the
    // quiet threshold counts as waiting quietly. A level of -infinity or NaN means no mic, so nothing fires.
    public sealed class LoudnessDetector
    {
        const float Epsilon = 1e-4f;

        readonly TriggerDefinition m_Shouting;
        readonly TriggerDefinition m_WaitsQuietly;
        float m_LoudFor;
        float m_Refractory;
        float m_QuietFor;

        public LoudnessDetector(TriggerDefinition shouting, TriggerDefinition waitsQuietly)
        {
            m_Shouting = shouting;
            m_WaitsQuietly = waitsQuietly;
        }

        public void Sample(float dbfs, float dt, List<TriggerHit> hits)
        {
            m_Refractory -= dt;
            if (float.IsNaN(dbfs) || float.IsNegativeInfinity(dbfs))
            {
                m_LoudFor = 0f;
                m_QuietFor = 0f;
                return;
            }

            if (m_Shouting != null && dbfs > m_Shouting.ThresholdDbfs)
            {
                m_LoudFor += dt;
                if (m_LoudFor >= m_Shouting.HoldSeconds - Epsilon && m_Refractory <= Epsilon)
                {
                    hits.Add(new TriggerHit(m_Shouting.Id, Channel.Loudness));
                    m_Refractory = m_Shouting.RefractorySeconds;
                }
            }
            else
            {
                m_LoudFor = 0f;
            }

            if (m_WaitsQuietly != null && dbfs < m_WaitsQuietly.ThresholdDbfs)
            {
                m_QuietFor += dt;
                if (m_QuietFor >= m_WaitsQuietly.HoldSeconds - Epsilon)
                {
                    hits.Add(new TriggerHit(m_WaitsQuietly.Id, Channel.Loudness));
                    m_QuietFor -= m_WaitsQuietly.RepeatSeconds;
                }
            }
            else
            {
                m_QuietFor = 0f;
            }
        }
    }
}
