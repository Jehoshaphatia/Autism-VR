using UnityEngine;

namespace AutismVR.Characters
{
    // Plays a ChildReaction through the shared ChildReactions controller's "Reaction" parameter.
    [RequireComponent(typeof(Animator))]
    public class ChildAnimator : MonoBehaviour
    {
        public const string ReactionParameter = "Reaction";
        static readonly int k_Reaction = Animator.StringToHash(ReactionParameter);

        Animator m_Animator;

        public ChildReaction Current { get; private set; }

        void Awake()
        {
            m_Animator = GetComponent<Animator>();
        }

        public void SetReaction(ChildReaction reaction)
        {
            Current = reaction;
            m_Animator.SetInteger(k_Reaction, (int)reaction);
        }
    }
}
