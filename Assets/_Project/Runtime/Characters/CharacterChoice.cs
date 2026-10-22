using AutismVR.Interaction;
using UnityEngine;

namespace AutismVR.Characters
{
    // One child in the chooser: pressing its button picks that child and loads Room.
    public class CharacterChoice : MonoBehaviour
    {
        [SerializeField] CharacterProfile m_Profile;
        [SerializeField] PokeButton m_Button;
        [SerializeField] Transform m_Pedestal;
        [SerializeField] GameObject m_Preview;
        [SerializeField] string m_NextScene = "Room";

        public CharacterProfile Profile => m_Profile;
        public PokeButton Button => m_Button;
        public Transform Pedestal => m_Pedestal;
        public GameObject Preview => m_Preview;
        public string NextScene => m_NextScene;
        public System.Action<string> SceneLoader { get; set; }

        public void Configure(CharacterProfile profile, PokeButton button, Transform pedestal, GameObject preview)
        {
            m_Profile = profile;
            m_Button = button;
            m_Pedestal = pedestal;
            m_Preview = preview;
            if (isActiveAndEnabled)
                Subscribe();
        }

        void OnEnable() => Subscribe();

        void OnDisable()
        {
            if (m_Button != null)
                m_Button.Pressed.RemoveListener(Choose);
        }

        void Subscribe()
        {
            if (m_Button == null)
                return;
            m_Button.Pressed.RemoveListener(Choose);
            m_Button.Pressed.AddListener(Choose);
        }

        void Choose()
        {
            // One choice per visit: switch every choice off so a second hand, a poke-then-pinch or a
            // neighbouring button cannot reload Room or swap the child.
            foreach (var choice in FindObjectsByType<CharacterChoice>(FindObjectsSortMode.None))
                choice.enabled = false;

            SelectedCharacter.Choose(m_Profile);
            (SceneLoader ?? (scene => UnityEngine.SceneManagement.SceneManager.LoadScene(scene)))(m_NextScene);
        }
    }
}
