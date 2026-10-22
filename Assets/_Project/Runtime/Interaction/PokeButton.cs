using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AutismVR.Interaction
{
    // A button pressed with a fingertip from above (it lies flat, facing up), or with look-and-pinch:
    // on Meta VR Glasses that arrives as the hand's NearFarInteractor select. The teleport/ray
    // interactor and gaze dwell are ignored.
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
            if (args.interactorObject is not (XRPokeInteractor or NearFarInteractor))
                return;

            PressCount++;
            m_Pressed.Invoke();
        }
    }
}
