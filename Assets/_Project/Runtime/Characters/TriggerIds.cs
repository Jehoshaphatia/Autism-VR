namespace AutismVR.Characters
{
    // Ids for the triggers in the spec §5 catalog: the clinician's list plus the researched additions.
    // Character profiles reference these; the trigger catalog (F4) defines what each one detects.
    public static class TriggerIds
    {
        public const string Shouting = "shouting";
        public const string DemandingEyeContact = "demanding_eye_contact";
        public const string Staring = "staring";
        public const string RapidQuestions = "rapid_questions";
        public const string MultiStepInstructions = "multi_step_instructions";
        public const string Sarcasm = "sarcasm";
        public const string Anger = "anger";
        public const string PressuringSpeech = "pressuring_speech";
        public const string ApproachTooClose = "approach_too_close";
        public const string ReachingOrTouch = "reaching_or_touch";
        public const string DemandsDuringMeltdown = "demands_during_meltdown";

        public static readonly string[] All =
        {
            Shouting, DemandingEyeContact, Staring, RapidQuestions, MultiStepInstructions, Sarcasm,
            Anger, PressuringSpeech, ApproachTooClose, ReachingOrTouch, DemandsDuringMeltdown,
        };
    }
}
