using System.Linq;
using AutismVR.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AutismVR.Tests.EditMode
{
    // PokeButton ignores interactors under the modality manager's controllers (hands only). That only
    // works if each scene's rig names its controllers and keeps their interactors under them.
    public class HandsOnlyRigTests
    {
        static readonly string[] Scenes = { RoomSceneCommands.RoomScenePath, ChooserSceneCommands.ChooserScenePath };

        [TestCaseSource(nameof(Scenes))]
        public void EveryButtonPressingInteractorIsUnderAHandOrAController(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var managers = Object.FindObjectsByType<XRInputModalityManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(managers, Has.Length.EqualTo(1));
            var modality = managers[0];
            Assert.That(modality.leftController, Is.Not.Null);
            Assert.That(modality.rightController, Is.Not.Null);

            bool Under(Component c, GameObject root) => root != null && c.transform.IsChildOf(root.transform);
            var interactors = Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Cast<Component>()
                .Concat(Object.FindObjectsByType<XRPokeInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                .ToList();
            var onControllers = interactors.Count(c => Under(c, modality.leftController) || Under(c, modality.rightController));
            var unplaced = interactors
                .Where(c => !Under(c, modality.leftHand) && !Under(c, modality.rightHand)
                            && !Under(c, modality.leftController) && !Under(c, modality.rightController))
                .Select(c => c.name)
                .ToList();

            Assert.That(onControllers, Is.GreaterThan(0), "expected the starter rig's controller interactors");
            Assert.That(unplaced, Is.Empty, string.Join(", ", unplaced));
        }
    }
}
