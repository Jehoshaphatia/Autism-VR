using System;

namespace AutismVR.Comfort
{
    // How a trigger or calming behaviour is detected (spec §5.3).
    [Flags]
    public enum Channel
    {
        None = 0,
        Words = 1,
        Loudness = 2,
        Gaze = 4,
        Proximity = 8,
        Hands = 16,
    }

    public enum TriggerKind
    {
        Trigger,
        Calming,
    }
}
