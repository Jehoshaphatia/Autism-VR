namespace AutismVR.Comfort
{
    // A detector's report that a trigger or calming behaviour happened.
    public readonly struct TriggerHit
    {
        public readonly string TriggerId;
        public readonly Channel Channel;
        public readonly float Intensity;

        public TriggerHit(string triggerId, Channel channel, float intensity = 1f)
        {
            TriggerId = triggerId;
            Channel = channel;
            Intensity = intensity;
        }
    }
}
