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
        const string RoomPath = RoomSceneCommands.RoomScenePath;
        const string MissingScriptFixturePath = "Assets/_Project/Tests/EditMode/Fixtures/MissingScript.unity";

        [SetUp]
        public void StartFromCleanScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void OpenScene_Room_ReturnsSceneName()
        {
            Assert.That(VerificationCommands.OpenScene(RoomPath), Is.EqualTo("Room"));
        }

        [Test]
        public void OpenScene_UnsavedChanges_ThrowsAndKeepsScene()
        {
            new GameObject("UnsavedEdit");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Assert.Throws<InvalidOperationException>(() => VerificationCommands.OpenScene(RoomPath));
            Assert.That(GameObject.Find("UnsavedEdit"), Is.Not.Null);
        }

        [Test]
        public void OpenScene_UnsavedChangesWithDiscard_Opens()
        {
            new GameObject("UnsavedEdit");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Assert.That(VerificationCommands.OpenScene(RoomPath, discard: true), Is.EqualTo("Room"));
        }

        [Test]
        public void MissingScriptCount_SceneWithMissingScript_CountsIt()
        {
            // A MonoBehaviour whose script cannot be resolved; loaded from a minimal fixture scene.
            VerificationCommands.OpenScene(MissingScriptFixturePath);

            Assert.That(VerificationCommands.MissingScriptCount(), Is.EqualTo(1));
        }

        [Test]
        public void MissingScriptCount_Room_IsZero()
        {
            VerificationCommands.OpenScene(RoomPath);

            Assert.That(VerificationCommands.MissingScriptCount(), Is.EqualTo(0));
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
