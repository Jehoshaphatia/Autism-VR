using System.Collections.Generic;
using AutismVR.Characters;
using UnityEngine;

namespace AutismVR.Comfort
{
    // Runs the detectors each frame, applies their hits to the child's comfort, logs them, and picks the
    // reaction. Words triggers (Decisions, F6) arrive through Report.
    public sealed class ComfortEngine
    {
        readonly TriggerCatalog m_Catalog;
        readonly LoudnessDetector m_Loudness;
        readonly StareDetector m_Stare;
        readonly ProximityDetector m_Proximity;
        readonly HandReachDetector m_Hands;
        readonly List<TriggerHit> m_Hits = new List<TriggerHit>();

        public ComfortEngine(CharacterProfile profile, TriggerCatalog catalog)
        {
            m_Catalog = catalog;
            Model = new ComfortModel(profile, catalog);
            m_Loudness = new LoudnessDetector(Active(TriggerIds.Shouting), Active(CalmingIds.WaitsQuietly));
            m_Stare = new StareDetector(Active(TriggerIds.Staring));
            m_Proximity = new ProximityDetector(Active(TriggerIds.ApproachTooClose), Active(CalmingIds.GivesSpace));
            m_Hands = new HandReachDetector(Active(TriggerIds.ReachingOrTouch));
            CopyOutcome();
        }

        public float Time { get; private set; }
        public ComfortModel Model { get; }
        public ReactionSelector Reactions { get; } = new ReactionSelector();
        public SessionLog Log { get; } = new SessionLog();

        public ChildReaction Step(in SensorFrame frame, float dt)
        {
            // After a hitch the distance moved spans more time than the clamped step, so speeds would read high.
            if (dt > ComfortRules.MaxDeltaTime)
            {
                m_Proximity.ResetMotion();
                m_Hands.ResetMotion();
            }
            dt = Mathf.Clamp(dt, 0f, ComfortRules.MaxDeltaTime);
            Time += dt;

            m_Hits.Clear();
            m_Loudness.Sample(frame.LoudnessDbfs, dt, m_Hits);
            m_Stare.Sample(frame.HeadPosition, frame.HeadForward, frame.ChildHead, dt, m_Hits);
            m_Proximity.Sample(frame.HeadPosition, frame.ChildPosition, dt, m_Hits);
            m_Hands.Sample(frame.Hands, frame.ChildHead, frame.ChildChest, dt, m_Hits);
            foreach (var hit in m_Hits)
                Apply(hit.TriggerId, hit.Channel, hit.Intensity, null);

            Model.Advance(dt);
            CopyOutcome();
            return Reactions.Update(Model, dt);
        }

        // True when the trigger or calming behaviour changed comfort and was logged.
        public bool Report(string triggerId, Channel channel, float intensity = 1f, string excerpt = null)
        {
            var applied = Apply(triggerId, channel, intensity, excerpt);
            CopyOutcome();
            return applied;
        }

        bool Apply(string id, Channel channel, float intensity, string excerpt)
        {
            var change = Model.Apply(id, channel, intensity);
            if (!change.HasValue)
                return false;
            Log.Record(Time, change.Value, excerpt);
            Reactions.OnHit(m_Catalog.Find(id));
            return true;
        }

        TriggerDefinition Active(string id) => m_Catalog.IsActive(id) ? m_Catalog.Find(id) : null;

        void CopyOutcome()
        {
            Log.MeltdownHappened = Model.MeltdownHappened;
            Log.Deescalated = Model.Deescalated;
            Log.Greeted = Model.Greeted;
        }
    }
}
