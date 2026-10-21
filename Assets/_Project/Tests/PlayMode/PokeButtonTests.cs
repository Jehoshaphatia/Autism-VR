using System.Collections;
using AutismVR.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
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
            m_Manager = new GameObject("Manager").AddComponent<XRInteractionManager>();
            var buttonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_Button = buttonObject.AddComponent<PokeButton>();
        }

        [TearDown]
        public void DestroyObjects()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.Destroy(go);
        }

        // Bypasses XRPokeFilter's physical poke-depth check (XRI's behaviour, exercised live in the
        // XR Interaction Simulator) so these tests pin the button's own poke-only rule.
        [UnityTest]
        public IEnumerator PokeButton_PokeSelect_RaisesPressed()
        {
            var poke = new GameObject("Poke").AddComponent<XRPokeInteractor>();
            var pressedEvents = 0;
            m_Button.Pressed.AddListener(() => pressedEvents++);
            yield return null;

            m_Manager.SelectEnterUnconditionally((IXRSelectInteractor)poke, m_Button.GetComponent<XRSimpleInteractable>());
            yield return null;

            Assert.That(m_Button.PressCount, Is.EqualTo(1));
            Assert.That(pressedEvents, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PokeButton_RaySelect_DoesNotRaisePressed()
        {
            var ray = new GameObject("Ray").AddComponent<XRRayInteractor>();
            yield return null;

            m_Manager.SelectEnterUnconditionally((IXRSelectInteractor)ray, m_Button.GetComponent<XRSimpleInteractable>());
            yield return null;

            Assert.That(m_Button.PressCount, Is.EqualTo(0));
        }
    }
}
