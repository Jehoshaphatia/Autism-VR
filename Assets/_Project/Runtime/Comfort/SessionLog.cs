using System.Collections.Generic;

namespace AutismVR.Comfort
{
    // One logged hit (spec §8): when, which trigger, through which channel, what was said, comfort before/after.
    public readonly struct TriggerEvent
    {
        public readonly float Time;
        public readonly string TriggerId;
        public readonly Channel Channel;
        public readonly string TranscriptExcerpt;
        public readonly float ComfortBefore;
        public readonly float ComfortAfter;

        public TriggerEvent(float time, string triggerId, Channel channel, string transcriptExcerpt, float comfortBefore, float comfortAfter)
        {
            Time = time;
            TriggerId = triggerId;
            Channel = channel;
            TranscriptExcerpt = transcriptExcerpt;
            ComfortBefore = comfortBefore;
            ComfortAfter = comfortAfter;
        }
    }

    // What happened in a session, for the feedback summary and the upload in F7.
    public sealed class SessionLog
    {
        readonly List<TriggerEvent> m_Events = new List<TriggerEvent>();

        public IReadOnlyList<TriggerEvent> Events => m_Events;
        public bool MeltdownHappened { get; internal set; }
        public bool Deescalated { get; internal set; }
        public bool Greeted { get; internal set; }

        public void Record(float time, ComfortChange change, string excerpt = null)
        {
            m_Events.Add(new TriggerEvent(time, change.TriggerId, change.Channel, excerpt, change.Before, change.After));
        }

        // How many times each trigger was hit; calming behaviours are not triggers and are left out.
        public IReadOnlyDictionary<string, int> TriggerCounts(TriggerCatalog catalog)
        {
            var counts = new Dictionary<string, int>();
            foreach (var e in m_Events)
            {
                if (catalog.Find(e.TriggerId)?.Kind != TriggerKind.Trigger)
                    continue;
                counts.TryGetValue(e.TriggerId, out var count);
                counts[e.TriggerId] = count + 1;
            }
            return counts;
        }
    }
}
