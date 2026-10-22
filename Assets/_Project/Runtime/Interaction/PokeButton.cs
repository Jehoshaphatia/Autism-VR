using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AutismVR.Interaction
{
    // A button pressed with a fingertip from above (it lies flat, facing up). Ray and other
    // interactors are ignored, so hands-only Quest users must touch it; gaze-and-pinch selection
    // for Meta VR Glasses is not handled here.
    [RequireComponent(typeof(XRSimpleInteractable), typeof(XRPokeFilter))]
    public class PokeButton : MonoBehaviour
    {
        [SerializeField]
        UnityEvent m_Pressed = new UnityEvent();

        [SerializeField]
        [Tooltip("Side the fingertip comes from, in the button's local space. NegativeY = pressed down from above.")]
        PokeAxis m_PressFrom = PokeAxis.NegativeY;

        XRSimpleInteractable m_Interactable;

        public UnityEvent Pressed => m_Pressed;

        public int PressCount { get; private set; }

        void Awake()
        {
            m_Interactable = GetComponent<XRSimpleInteractable>();
            ConfigurePokeFilter();
        }

        // XRPokeFilter defaults to PokeAxis.Z (a jab in from the front), which rejects a press on the top.
        public void ConfigurePokeFilter()
        {
            var filter = GetComponent<XRPokeFilter>();
            if (filter.pokeConfiguration.Value.pokeDirection == m_PressFrom)
                return;
            filter.pokeConfiguration = new PokeThresholdDatumProperty(new PokeThresholdData { pokeDirection = m_PressFrom });
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
