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
    }
}
