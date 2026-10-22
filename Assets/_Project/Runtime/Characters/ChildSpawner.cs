using UnityEngine;

namespace AutismVR.Characters
{
    // Places the child chosen in the chooser (or the default when Room is entered directly) at the
    // spawn point, facing the trainer.
    public class ChildSpawner : MonoBehaviour
    {
        [SerializeField] CharacterProfile m_DefaultProfile;
        [SerializeField] Transform m_SpawnPoint;
        [SerializeField] Transform m_FaceTowards;

        public ChildAnimator Spawned { get; private set; }
        public CharacterProfile DefaultProfile => m_DefaultProfile;
        public Transform SpawnPoint => m_SpawnPoint;
        public Transform FaceTowards => m_FaceTowards;

        public void Configure(CharacterProfile defaultProfile, Transform spawnPoint, Transform faceTowards)
        {
            m_DefaultProfile = defaultProfile;
            m_SpawnPoint = spawnPoint;
            m_FaceTowards = faceTowards;
        }

        void Start()
        {
            var profile = SelectedCharacter.Current != null ? SelectedCharacter.Current : m_DefaultProfile;
            if (profile == null || profile.ModelPrefab == null || m_SpawnPoint == null)
            {
                Debug.LogError("ChildSpawner has no profile, model or spawn point", this);
                return;
            }

            var facing = Vector3.ProjectOnPlane(m_FaceTowards.position - m_SpawnPoint.position, Vector3.up);
            var rotation = facing.sqrMagnitude > 0f ? Quaternion.LookRotation(facing) : m_SpawnPoint.rotation;
            var child = Instantiate(profile.ModelPrefab, m_SpawnPoint.position, rotation);
            Spawned = child.GetComponent<ChildAnimator>();
        }
    }
}
