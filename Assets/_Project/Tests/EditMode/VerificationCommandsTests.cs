using System;
using System.Text.RegularExpressions;
using AutismVR.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace AutismVR.Tests.EditMode
{
    public class VerificationCommandsTests
    {
        [Test]
        public void OpenScene_Autism1_ReturnsSceneName()
        {
            // Autism1 still holds prefab instances from the removed SDKs (MORPH3D child,
            // ViveCameraRig); Unity logs one error per missing prefab but the scene opens.
            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Problem detected while opening the Scene file"));

            Assert.That(VerificationCommands.OpenScene("Assets/_Scenes/Autism1.unity"), Is.EqualTo("Autism1"));
        }

        [Test]
        public void OpenScene_MissingPath_Throws()
        {
            Assert.Throws<ArgumentException>(() => VerificationCommands.OpenScene("Assets/_Scenes/DoesNotExist.unity"));
        }

        // Framing itself needs a Scene view, which batch test runs do not have; it is exercised
        // live by `unity command autismvr_frame` before screenshots.
        [Test]
        public void ComputeFrameBounds_KnownObject_EnclosesItsRenderers()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "FrameTarget";
            cube.transform.position = new Vector3(2f, 0f, 0f);
            var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.transform.SetParent(cube.transform, false);
            child.transform.localPosition = new Vector3(2f, 0f, 0f);

            var bounds = VerificationCommands.ComputeFrameBounds("FrameTarget");

            Assert.That(bounds.center, Is.EqualTo(new Vector3(3f, 0f, 0f)));
            Assert.That(bounds.size, Is.EqualTo(new Vector3(3f, 1f, 1f)));
        }

        [Test]
        public void ComputeFrameBounds_UnknownObject_Throws()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Assert.Throws<ArgumentException>(() => VerificationCommands.ComputeFrameBounds("Nope"));
        }

        [Test]
        public void FrameSceneView_UnknownObject_Throws()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Assert.Throws<ArgumentException>(() => VerificationCommands.FrameSceneView("Nope"));
        }

        [Test]
        public void MissingScriptCount_EmptyScene_IsZero()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Assert.That(VerificationCommands.MissingScriptCount(), Is.EqualTo(0));
        }
    }
}
