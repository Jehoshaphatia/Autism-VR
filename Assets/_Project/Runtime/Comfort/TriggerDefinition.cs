using AutismVR.Characters;

namespace AutismVR.Comfort
{
    // One row of the trigger catalog (spec §5.4). Channel parameters are 0 when a detector does not use them.
    public sealed class TriggerDefinition
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public TriggerKind Kind { get; internal set; }
        public Channel Channels { get; internal set; }
        // Change to comfort per hit: negative for triggers, positive for calming behaviours.
        public float Effect { get; internal set; }
        public ChildReaction? Reaction { get; internal set; }
        public bool Enabled { get; internal set; }
        public bool Provisional { get; internal set; }

        public float ThresholdDbfs { get; internal set; }
        public float HoldSeconds { get; internal set; }
        public float RefractorySeconds { get; internal set; }
        public float RepeatSeconds { get; internal set; }
        public float ConeDegrees { get; internal set; }
        public float DistanceMeters { get; internal set; }
        public float RearmDistanceMeters { get; internal set; }
        public float SpeedMetersPerSecond { get; internal set; }
        public float RangeMeters { get; internal set; }
    }
}
