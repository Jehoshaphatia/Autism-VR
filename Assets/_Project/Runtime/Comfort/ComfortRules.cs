namespace AutismVR.Comfort
{
    // Provisional comfort rules (F4 plan); the owner and a clinician tune them.
    public static class ComfortRules
    {
        public const float MeltdownEnter = 0.10f;
        public const float MeltdownExit = 0.30f;
        public const float QuietSecondsBeforeRecovery = 3f;
        public const float ReactionHoldSeconds = 2f;
        // Longest frame step the engine accepts, so a pause or hitch cannot stack up timers.
        public const float MaxDeltaTime = 0.25f;
    }
}
