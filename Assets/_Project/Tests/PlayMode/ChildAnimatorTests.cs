using System.Collections;
using AutismVR.Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AutismVR.Tests.PlayMode
{
    public class ChildAnimatorTests
    {
        GameObject m_Child;

        [TearDown]
        public void DestroyChild()
        {
            if (m_Child != null)
                Object.Destroy(m_Child);
        }

        [UnityTest]
        public IEnumerator ChildAnimator_SetReaction_EntersMatchingState()
        {
#if UNITY_EDITOR
            var model = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Aj.fbx");
            var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/_Project/Characters/ChildReactions.controller");
            m_Child = Object.Instantiate(model);
            var animator = m_Child.GetComponent<Animator>() ?? m_Child.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            var child = m_Child.AddComponent<ChildAnimator>();
            yield return null;

            child.SetReaction(ChildReaction.Tantrum);
            yield return new WaitForSeconds(0.5f);

            Assert.That(child.Current, Is.EqualTo(ChildReaction.Tantrum));
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Tantrum"), Is.True);
#else
            Assert.Ignore("Loads assets through AssetDatabase; runs in the Editor only.");
            yield break;
#endif
        }
    }
}
