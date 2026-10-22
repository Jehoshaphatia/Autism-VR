using System.Collections;
using AutismVR.Characters;
using AutismVR.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AutismVR.Tests.PlayMode
{
    public class CharacterChoiceTests
    {
        [UnityTest]
        public IEnumerator CharacterChoice_Pressed_ChoosesProfileAndLoadsRoom()
        {
            SelectedCharacter.Clear();
            var profile = ScriptableObject.CreateInstance<CharacterProfile>();
            profile.Id = "test";
            var buttonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var button = buttonObject.AddComponent<PokeButton>();
            var choice = new GameObject("Choice").AddComponent<CharacterChoice>();
            choice.Configure(profile, button, null, null);
            string loaded = null;
            choice.SceneLoader = scene => loaded = scene;
            try
            {
                yield return null;

                button.Pressed.Invoke();

                Assert.That(SelectedCharacter.Current, Is.SameAs(profile));
                Assert.That(loaded, Is.EqualTo("Room"));
            }
            finally
            {
                SelectedCharacter.Clear();
                Object.Destroy(buttonObject);
                Object.Destroy(choice.gameObject);
            }
        }
    
        // A stray second press (other hand, poke then pinch, a neighbouring button) must not reload Room
        // or switch the child.
        [UnityTest]
        public IEnumerator CharacterChoice_AfterFirstChoice_FurtherPressesAreIgnored()
        {
            SelectedCharacter.Clear();
            var first = ScriptableObject.CreateInstance<CharacterProfile>();
            var second = ScriptableObject.CreateInstance<CharacterProfile>();
            var firstButtonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var secondButtonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var firstChoice = new GameObject("First").AddComponent<CharacterChoice>();
            var secondChoice = new GameObject("Second").AddComponent<CharacterChoice>();
            firstChoice.Configure(first, firstButtonObject.AddComponent<PokeButton>(), null, null);
            secondChoice.Configure(second, secondButtonObject.AddComponent<PokeButton>(), null, null);
            var loads = 0;
            firstChoice.SceneLoader = _ => loads++;
            secondChoice.SceneLoader = _ => loads++;
            try
            {
                yield return null;

                firstChoice.Button.Pressed.Invoke();
                firstChoice.Button.Pressed.Invoke();
                secondChoice.Button.Pressed.Invoke();

                Assert.That(loads, Is.EqualTo(1));
                Assert.That(SelectedCharacter.Current, Is.SameAs(first));
            }
            finally
            {
                SelectedCharacter.Clear();
                Object.Destroy(firstButtonObject);
                Object.Destroy(secondButtonObject);
                Object.Destroy(firstChoice.gameObject);
                Object.Destroy(secondChoice.gameObject);
            }
        }
    }
}
