namespace AutismVR.Characters
{
    // The child the trainee picked in the chooser; Room reads it when it loads.
    public static class SelectedCharacter
    {
        public static CharacterProfile Current { get; private set; }

        public static void Choose(CharacterProfile profile) => Current = profile;

        public static void Clear() => Current = null;
    }
}
