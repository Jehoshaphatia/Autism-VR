using System;
using System.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AutismVR.Editor
{
    // Commands used to stage the Editor before `unity command screenshot` captures evidence.
    public static class VerificationCommands
    {
        [CliCommand("autismvr_open_scene", "Open a scene in the Editor by asset path")]
        public static string OpenScene([CliArg("path", "Scene asset path")] string path,
            [CliArg("discard", "Discard unsaved changes in open scenes")] bool discard = false)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                throw new ArgumentException($"No scene asset at {path}", nameof(path));

            // Over the CLI there is no save prompt, so refuse rather than silently lose edits.
            var dirtyScenes = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt)
                .Where(scene => scene.isDirty)
                .Select(scene => string.IsNullOrEmpty(scene.path) ? "Untitled" : scene.path)
                .ToList();
            if (dirtyScenes.Count > 0 && !discard)
                throw new InvalidOperationException(
                    $"Unsaved changes in {string.Join(", ", dirtyScenes)}; save them or pass --discard true");

            return EditorSceneManager.OpenScene(path, OpenSceneMode.Single).name;
        }

        [CliCommand("autismvr_frame", "Frame the Scene view on a GameObject by name")]
        public static string FrameSceneView([CliArg("target", "GameObject name")] string target)
        {
            var bounds = ComputeFrameBounds(target);

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
                throw new InvalidOperationException("No Scene view is open");

            sceneView.Frame(bounds, false);
            sceneView.Repaint();
            return target;
        }

        public static Bounds ComputeFrameBounds(string target)
        {
            var targetObject = LoadedSceneObjects().FirstOrDefault(go => go.name == target);
            if (targetObject == null)
                throw new ArgumentException($"No GameObject named {target} in the open scenes", nameof(target));

            var renderers = targetObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(targetObject.transform.position, Vector3.one);

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        [CliCommand("autismvr_missing_scripts", "Count missing-script components in the open scenes")]
        public static int MissingScriptCount()
        {
            return LoadedSceneObjects().Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
        }

        static GameObject[] LoadedSceneObjects()
        {
            return Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt)
                .Where(scene => scene.isLoaded)
                .SelectMany(scene => scene.GetRootGameObjects())
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject)
                .ToArray();
        }
    }
}
