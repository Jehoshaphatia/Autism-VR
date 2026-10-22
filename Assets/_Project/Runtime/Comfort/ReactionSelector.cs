using AutismVR.Characters;

namespace AutismVR.Comfort
{
    // Picks the child's reaction: Greeting once greeted, Tantrum in a meltdown, a trigger's own reaction for
    // a short hold, otherwise the band its comfort falls in.
    public sealed class ReactionSelector
    {
        ChildReaction? m_Held;
        float m_HeldFor;

        public ChildReaction Current { get; private set; } = ChildReaction.Calm;

        public void OnHit(TriggerDefinition hit)
        {
            if (hit?.Reaction == null)
                return;
            m_Held = hit.Reaction;
            m_HeldFor = 0f;
        }

        public ChildReaction Update(ComfortModel model, float dt)
        {
            if (m_Held.HasValue)
            {
                m_HeldFor += dt;
                if (m_HeldFor >= ComfortRules.ReactionHoldSeconds - 1e-4f)
                    m_Held = null;
            }

            if (model.Greeted)
                Current = ChildReaction.Greeting;
            else if (model.InMeltdown)
                Current = ChildReaction.Tantrum;
            else
                Current = m_Held ?? Band(model.Comfort);
            return Current;
        }

        static ChildReaction Band(float comfort)
        {
            if (comfort < 0.25f) return ChildReaction.Withdrawn;
            if (comfort < 0.40f) return ChildReaction.Fidgets;
            if (comfort < 0.60f) return ChildReaction.Uneasy;
            return ChildReaction.Calm;
        }
    }
}
