using System;
using System.Linq;
using AutismVR.Characters;
using AutismVR.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AutismVR.Tests.EditMode
{
    public class ChildAnimatorControllerTests
    {
        static AnimatorController Controller => AssetDatabase.LoadAssetAtPath<AnimatorController>(ChildAnimatorCommands.ControllerPath);

        static ChildReaction[] Reactions => (ChildReaction[])Enum.GetValues(typeof(ChildReaction));

        [Test]
        public void ControllerHasReactionIntParameter()
        {
            Assert.That(Controller, Is.Not.Null);
            Assert.That(Controller.parameters.Any(p => p.name == ChildAnimatorCommands.ReactionParameter
                                                       && p.type == AnimatorControllerParameterType.Int), Is.True);
        }

        [TestCaseSource(nameof(Reactions))]
        public void ControllerHasStateWithClipForReaction(ChildReaction reaction)
        {
            var state = Controller.layers[0].stateMachine.states.Select(s => s.state)
                .FirstOrDefault(s => s.name == reaction.ToString());

            Assert.That(state, Is.Not.Null, reaction.ToString());
            Assert.That(state.motion, Is.Not.Null, $"{reaction} has no clip");
        }

        [TestCaseSource(nameof(Reactions))]
        public void ReactionIsReachableFromAnyState(ChildReaction reaction)
        {
            var transition = Controller.layers[0].stateMachine.anyStateTransitions
                .FirstOrDefault(t => t.destinationState != null && t.destinationState.name == reaction.ToString());

            Assert.That(transition, Is.Not.Null);
            Assert.That(transition.hasExitTime, Is.False);
            Assert.That(transition.conditions.Any(c => c.parameter == ChildAnimatorCommands.ReactionParameter
                                                       && c.mode == AnimatorConditionMode.Equals
                                                       && Mathf.Approximately(c.threshold, (int)reaction)), Is.True);
        }
    }
}
