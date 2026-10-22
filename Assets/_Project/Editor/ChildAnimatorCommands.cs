using System;
using System.Collections.Generic;
using System.Linq;
using AutismVR.Characters;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AutismVR.Editor
{
    // Builds the one Animator controller all three Humanoid children share: one state per ChildReaction,
    // each reachable from Any State on Reaction == value. Clips are placeholders until proper ones exist.
    public static class ChildAnimatorCommands
    {
        public const string ControllerPath = "Assets/_Project/Characters/ChildReactions.controller";
        public const string ReactionParameter = ChildAnimator.ReactionParameter;

        static readonly Dictionary<ChildReaction, (string clipModel, bool loop)> PlaceholderClips = new()
        {
            { ChildReaction.Calm, ("Assets/Raw Mocap Data/Animations/Idle/Idle_Neutral_1.fbx", true) },
            { ChildReaction.Uneasy, ("Assets/Raw Mocap Data/Animations/Idle/Idle_IdleToIdlesR.fbx", true) },
            { ChildReaction.LooksAway, ("Assets/Animation/jasper@Right Turn.fbx", false) },
            { ChildReaction.Fidgets, ("Assets/Raw Mocap Data/Animations/Idle/IdleR_To_IdleL_1.fbx", true) },
            { ChildReaction.Withdrawn, ("Assets/Animation/stefani@Sad Idle.fbx", true) },
            { ChildReaction.Tantrum, ("Assets/Animation/stefani@Yelling.fbx", true) },
            { ChildReaction.Greeting, ("Assets/Animation/Standing Greeting.fbx", false) },
        };

        [CliCommand("autismvr_build_child_controller", "Generate the shared ChildReactions Animator controller")]
        public static string BuildChildController()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Characters"))
                AssetDatabase.CreateFolder("Assets/_Project", "Characters");
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(ReactionParameter, AnimatorControllerParameterType.Int);
            var machine = controller.layers[0].stateMachine;

            foreach (ChildReaction reaction in Enum.GetValues(typeof(ChildReaction)))
            {
                var (clipModel, loop) = PlaceholderClips[reaction];
                var state = machine.AddState(reaction.ToString());
                state.motion = LoadClip(clipModel, loop);

                var transition = machine.AddAnyStateTransition(state);
                transition.hasExitTime = false;
                transition.duration = 0.2f;
                transition.canTransitionToSelf = false;
                transition.AddCondition(AnimatorConditionMode.Equals, (int)reaction, ReactionParameter);

                if (reaction == ChildReaction.Calm)
                    machine.defaultState = state;
            }

            AssetDatabase.SaveAssets();
            return controller.name;
        }

        static AnimationClip LoadClip(string modelPath, bool loop)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            if (clips.Any(c => c.loopTime != loop))
            {
                foreach (var clip in clips)
                    clip.loopTime = loop;
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                       .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"))
                   ?? throw new InvalidOperationException($"No animation clip in {modelPath}");
        }
    }
}
