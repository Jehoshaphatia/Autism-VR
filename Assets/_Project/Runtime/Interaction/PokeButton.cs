using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AutismVR.Interaction
{
    // A button pressed with a fingertip. Ray and other interactors are ignored so it works the same
    // on hands-only Quest and look-and-pinch Meta VR Glasses setups.
    [RequireComponent(typeof(XRSimpleInteractable), typeof(XRPokeFilter))]
    public class PokeButton : MonoBehaviour
    {
        [SerializeField]
        UnityEvent m_Pressed = new UnityEvent();

        XRSimpleInteractable m_Interactable;

        public UnityEvent Pressed => m_Pressed;

        public int PressCount { get; private set; }

        void Awake()
        {
            m_Interactable = GetComponent<XRSimpleInteractable>();
        }

        void OnEnable()
        {
            m_Interactable.selectEntered.AddListener(OnSelectEntered);
        }

        void OnDisable()
        {
            m_Interactable.selectEntered.RemoveListener(OnSelectEntered);
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactorObject is not XRPokeInteractor)
                return;

            PressCount++;
            m_Pressed.Invoke();
        }
    }
}
