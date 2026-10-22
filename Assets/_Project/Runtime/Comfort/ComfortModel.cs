using AutismVR.Characters;
using UnityEngine;

namespace AutismVR.Comfort
{
    public readonly struct ComfortChange
    {
        public readonly string TriggerId;
        public readonly Channel Channel;
        public readonly float Before;
        public readonly float After;

        public ComfortChange(string triggerId, Channel channel, float before, float after)
        {
            TriggerId = triggerId;
            Channel = channel;
            Before = before;
            After = after;
        }
    }

    // The child's hidden comfort (spec §4): triggers lower it, calming behaviours raise it. A meltdown is
    // recoverable (spec §8b): once the trainee stops triggering, comfort recovers at the profile's rate.
    // Outside a meltdown comfort does not drift; only calming behaviour raises it. The greeting is terminal.
    public sealed class ComfortModel
    {
        readonly CharacterProfile m_Profile;
        readonly TriggerCatalog m_Catalog;
        float m_SecondsSinceTrigger;

        public ComfortModel(CharacterProfile profile, TriggerCatalog catalog)
        {
            m_Profile = profile;
            m_Catalog = catalog;
            Comfort = Mathf.Clamp01(profile.StartingComfort);
            Evaluate();
        }

        public float Comfort { get; private set; }
        public bool InMeltdown { get; private set; }
        public bool MeltdownHappened { get; private set; }
        public bool Deescalated { get; private set; }
        public bool Greeted { get; private set; }

        // Null when the entry is missing or disabled, the profile excludes it, or the child has already greeted.
        public ComfortChange? Apply(string id, Channel channel, float intensity = 1f)
        {
            if (Greeted || !m_Catalog.IsActive(id) || !m_Profile.Applies(id))
                return null;

            var entry = m_Catalog.Find(id);
            var sensitivity = entry.Kind == TriggerKind.Trigger ? m_Profile.GetSensitivity(id) : 1f;
            var before = Comfort;
            Comfort = Mathf.Clamp01(Comfort + entry.Effect * sensitivity * intensity);
            if (entry.Kind == TriggerKind.Trigger)
                m_SecondsSinceTrigger = 0f;
            Evaluate();
            return new ComfortChange(id, channel, before, Comfort);
        }

        public void Advance(float dt)
        {
            if (Greeted)
                return;
            dt = Mathf.Clamp(dt, 0f, ComfortRules.MaxDeltaTime);
            var quietBefore = m_SecondsSinceTrigger;
            m_SecondsSinceTrigger += dt;
            if (InMeltdown && m_SecondsSinceTrigger > ComfortRules.QuietSecondsBeforeRecovery)
            {
                var recovering = Mathf.Min(dt, m_SecondsSinceTrigger - Mathf.Max(quietBefore, ComfortRules.QuietSecondsBeforeRecovery));
                Comfort = Mathf.Clamp01(Comfort + m_Profile.RecoveryPerSecond * recovering);
            }
            Evaluate();
        }

        void Evaluate()
        {
            if (!InMeltdown && Comfort <= ComfortRules.MeltdownEnter)
            {
                InMeltdown = true;
                MeltdownHappened = true;
            }
            else if (InMeltdown && Comfort >= ComfortRules.MeltdownExit)
            {
                InMeltdown = false;
                Deescalated = true;
            }

            if (!InMeltdown && Comfort >= m_Profile.GreetingThreshold)
                Greeted = true;
        }
    }
}
