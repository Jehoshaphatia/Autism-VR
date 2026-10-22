using System.Collections.Generic;
using AutismVR.Characters;
using UnityEngine;

namespace AutismVR.Comfort
{
    // Feeds the trainee's head, hands and voice level to a ComfortEngine for the spawned child and plays the
    // chosen reaction. The mic level arrives from the voice feature (F6) through LoudnessDbfs.
    public class ComfortDriver : MonoBehaviour
    {
        [SerializeField] ChildSpawner m_Spawner;
        [SerializeField] Transform m_Head;
        [Tooltip("Hand fingertip transforms; inactive ones (hand not tracked) are skipped.")]
        [SerializeField] Transform[] m_Hands = new Transform[0];

        readonly List<Vector3> m_HandPositions = new List<Vector3>();
        Transform m_ChildHead;
        Transform m_ChildChest;

        public ChildSpawner Spawner => m_Spawner;
        public Transform Head => m_Head;
        public IReadOnlyList<Transform> Hands => m_Hands;
        public float LoudnessDbfs { get; set; } = float.NegativeInfinity;
        public ComfortEngine Engine { get; private set; }

        public void Configure(ChildSpawner spawner, Transform head, Transform[] hands)
        {
            m_Spawner = spawner;
            m_Head = head;
            m_Hands = hands ?? new Transform[0];
        }

        void Update()
        {
            if (Engine == null && !TryStart())
                return;

            m_HandPositions.Clear();
            foreach (var hand in m_Hands)
                if (hand != null && hand.gameObject.activeInHierarchy)
                    m_HandPositions.Add(hand.position);

            var child = m_Spawner.Spawned;
            var frame = new SensorFrame
            {
                HeadPosition = m_Head.position,
                HeadForward = m_Head.forward,
                Hands = m_HandPositions,
                ChildHead = m_ChildHead.position,
                ChildChest = m_ChildChest.position,
                ChildPosition = child.transform.position,
                LoudnessDbfs = LoudnessDbfs,
            };
            var previous = child.Current;
            var reaction = Engine.Step(frame, Time.deltaTime);
            if (reaction != previous)
                child.SetReaction(reaction);
        }

        bool TryStart()
        {
            if (m_Spawner == null || m_Head == null || m_Spawner.Spawned == null || m_Spawner.SpawnedProfile == null)
                return false;

            var animator = m_Spawner.Spawned.GetComponent<Animator>();
            m_ChildHead = animator.GetBoneTransform(HumanBodyBones.Head);
            m_ChildChest = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (m_ChildHead == null || m_ChildChest == null)
            {
                Debug.LogError("ComfortDriver needs a humanoid child with head and chest bones", this);
                enabled = false;
                return false;
            }

            Engine = new ComfortEngine(m_Spawner.SpawnedProfile, TriggerCatalog.LoadDefault());
            return true;
        }
    }
}
