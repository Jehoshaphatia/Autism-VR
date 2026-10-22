using System.Collections;
using AutismVR.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AutismVR.Tests.PlayMode
{
    public class PokeButtonTests
    {
        XRInteractionManager m_Manager;
        PokeButton m_Button;

        [SetUp]
        public void CreateButton()
        {
            m_Manager = Track(new GameObject("Manager")).AddComponent<XRInteractionManager>();
            var buttonObject = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            buttonObject.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
            m_Button = buttonObject.AddComponent<PokeButton>();
        }

        readonly System.Collections.Generic.List<GameObject> m_Created = new System.Collections.Generic.List<GameObject>();

        GameObject Track(GameObject go)
        {
            m_Created.Add(go);
            return go;
        }

        [TearDown]
        public void DestroyObjects()
        {
            foreach (var go in m_Created)
                if (go != null)
                    Object.Destroy(go);
            m_Created.Clear();
        }

        // Moves a real poke interactor from `from` to `to` over many frames, so XRPokeFilter's depth and
        // direction checks run exactly as they do with a tracked fingertip.
        IEnumerator MovePoke(XRPokeInteractor poke, Vector3 from, Vector3 to, int frames = 40)
        {
            for (var i = 0; i <= frames; i++)
            {
                poke.transform.position = Vector3.Lerp(from, to, i / (float)frames);
                yield return null;
            }
        }

        // The StartButton is a flat disc lying on a pedestal, so a trainee presses its top downward.
        [UnityTest]
        public IEnumerator PokeButton_FingertipPressingDownOnTop_RaisesPressed()
        {
            var poke = Track(new GameObject("Poke")).AddComponent<XRPokeInteractor>();
            yield return null;

            yield return MovePoke(poke, new Vector3(0f, 0.1f, 0f), new Vector3(0f, -0.005f, 0f));

            Assert.That(m_Button.PressCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PokeButton_FingertipJabbingSideways_DoesNotRaisePressed()
        {
            var poke = Track(new GameObject("Poke")).AddComponent<XRPokeInteractor>();
            yield return null;

            yield return MovePoke(poke, new Vector3(0f, 0f, -0.1f), new Vector3(0f, 0f, 0.02f));

            Assert.That(m_Button.PressCount, Is.EqualTo(0));
        }

        // Bypasses XRPokeFilter's physical poke-depth check (XRI's behaviour, exercised live in the
        // XR Interaction Simulator) so these tests pin the button's own poke-only rule.
        [UnityTest]
        public IEnumerator PokeButton_PokeSelect_RaisesPressed()
        {
            var poke = Track(new GameObject("Poke")).AddComponent<XRPokeInteractor>();
            var pressedEvents = 0;
            m_Button.Pressed.AddListener(() => pressedEvents++);
            yield return null;

            m_Manager.SelectEnterUnconditionally((IXRSelectInteractor)poke, m_Button.GetComponent<XRSimpleInteractable>());
            yield return null;

            Assert.That(m_Button.PressCount, Is.EqualTo(1));
            Assert.That(pressedEvents, Is.EqualTo(1));
        }

        // On Meta VR Glasses a look-and-pinch reaches the app as the hand's NearFarInteractor select
        // (Meta XR Simulator spike, F3).
        [UnityTest]
        public IEnumerator PokeButton_LookAndPinchSelect_RaisesPressed()
        {
            var nearFar = Track(new GameObject("NearFar")).AddComponent<NearFarInteractor>();
            var interactable = m_Button.GetComponent<XRSimpleInteractable>();
            yield return null;

            m_Manager.SelectEnterUnconditionally((IXRSelectInteractor)nearFar, interactable);
            yield return null;

            Assert.That(interactable.isSelected, Is.True);
            Assert.That(m_Button.PressCount, Is.EqualTo(1));
        }

        // SelectEnterUnconditionally skips select filters; check the button's XRPokeFilter lets the
        // look-and-pinch interactor through, so the real path is not blocked.
        [UnityTest]
        public IEnumerator PokeButton_PokeFilter_AllowsLookAndPinchInteractor()
        {
            var nearFar = Track(new GameObject("NearFar")).AddComponent<NearFarInteractor>();
            yield return null;

            var filter = (UnityEngine.XR.Interaction.Toolkit.Filtering.IXRSelectFilter)m_Button.GetComponent<UnityEngine.XR.Interaction.Toolkit.Filtering.XRPokeFilter>();
            Assert.That(filter.Process(nearFar, m_Button.GetComponent<XRSimpleInteractable>()), Is.True);
        }

        // Owner ruling (F3): Glasses look-and-pinch and the Quest hand ray are the same NearFarInteractor,
        // so a far hand-ray pinch on Quest presses too. Accepted; this pins it.
        [UnityTest]
        public IEnumerator PokeButton_FarHandRayPinch_RaisesPressed()
        {
            var nearFar = Track(new GameObject("NearFar")).AddComponent<NearFarInteractor>();
            nearFar.transform.SetPositionAndRotation(m_Button.transform.position + new Vector3(0f, 0f, -3f), Quaternion.identity);
            var interactable = m_Button.GetComponent<XRSimpleInteractable>();
            yield return null;

            m_Manager.SelectEnterUnconditionally((IXRSelectInteractor)nearFar, interactable);
            yield return null;

            Assert.That(Vector3.Distance(nearFar.transform.position, m_Button.transform.position), Is.GreaterThan(2f));
            Assert.That(m_Button.PressCount, Is.EqualTo(1));
        }

        // The rig's modality manager names the hand and controller objects. Kept on an inactive object so it
        // does not toggle them from (absent) device tracking during the test.
        XRInputModalityManager CreateModalityManager(out GameObject hand, out GameObject controller)
        {
            var managerObject = Track(new GameObject("Modality Manager"));
            managerObject.SetActive(false);
            var manager = managerObject.AddComponent<XRInputModalityManager>();
            hand = Track(new GameObject("Right Hand"));
            controller = Track(new GameObject("Right Controller"));
            manager.rightHand = hand;
            manager.rightController = controller;
            return manager;
        }

        IEnumerator SelectFrom<T>(GameObject parent) where T : Component, IXRSelectInteractor
        {
            var interactor = Track(new GameObject(typeof(T).Name)).AddComponent<T>();
            interactor.transform.SetParent(parent.transform, false);
            yield return null;
            m_Manager.SelectEnterUnconditionally(interactor, m_Button.GetComponent<XRSimpleInteractable>());
            yield return null;
        }

        // Owner ruling (F3): hands only. A controller's NearFarInteractor or poke must not press.
        [UnityTest]
        public IEnumerator PokeButton_ControllerNearFarSelect_DoesNotRaisePressed()
        {
            CreateModalityManager(out _, out var controller);
            yield return SelectFrom<NearFarInteractor>(controller);

            Assert.That(m_Button.GetComponent<XRSimpleInteractable>().isSelected, Is.True, "the select must reach the button for this test to mean anything");
            Assert.That(m_Button.PressCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator PokeButton_ControllerPokeSelect_DoesNotRaisePressed()
        {
            CreateModalityManager(out _, out var controller);
            var selects = 0;
            m_Button.GetComponent<XRSimpleInteractable>().selectEntered.AddListener(_ => selects++);
            yield return SelectFrom<XRPokeInteractor>(controller);

            // A poke select is released the next frame, so count the select instead of checking isSelected.
            Assert.That(selects, Is.EqualTo(1), "the select must reach the button for this test to mean anything");
            Assert.That(m_Button.PressCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator PokeButton_HandNearFarSelectWithModalityManager_RaisesPressed()
        {
            CreateModalityManager(out var hand, out _);
            yield return SelectFrom<NearFarInteractor>(hand);

            Assert.That(m_Button.PressCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PokeButton_RaySelect_DoesNotRaisePressed()
        {
            var ray = Track(new GameObject("Ray")).AddComponent<XRRayInteractor>();
            var interactable = m_Button.GetComponent<XRSimpleInteractable>();
            yield return null;

            m_Manager.SelectEnterUnconditionally((IXRSelectInteractor)ray, interactable);
            yield return null;

            Assert.That(interactable.isSelected, Is.True, "the ray select must reach the button for this test to mean anything");
            Assert.That(m_Button.PressCount, Is.EqualTo(0));
        }
    }
}
