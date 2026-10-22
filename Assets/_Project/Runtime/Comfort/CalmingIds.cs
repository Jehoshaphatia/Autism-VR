namespace AutismVR.Comfort
{
    // Ids for the calming behaviours in the catalog (spec §5.3) that raise the child's comfort.
    public static class CalmingIds
    {
        public const string CalmQuietVoice = "calm_quiet_voice";
        public const string WaitsQuietly = "waits_quietly";
        public const string LetsChildLookAway = "lets_child_look_away";
        public const string GivesSpace = "gives_space";

        public static readonly string[] All = { CalmQuietVoice, WaitsQuietly, LetsChildLookAway, GivesSpace };
    }
}
