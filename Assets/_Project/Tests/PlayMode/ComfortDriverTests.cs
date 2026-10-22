using System.Collections;
using AutismVR.Characters;
using AutismVR.Comfort;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AutismVR.Tests.PlayMode
{
    public class ComfortDriverTests
    {
        Transform m_Spawn;
        Transform m_Head;
        ChildSpawner m_Spawner;
        ComfortDriver m_Driver;
        float m_TimeScale;

        static CharacterProfile LoadProfile(string id)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterProfile>($"Assets/_Project/Characters/Profiles/{id}.asset");
#else
            return null;
#endif
        }

        [SetUp]
        public void CreateRig()
        {
            SelectedCharacter.Clear();
            m_TimeScale = Time.timeScale;
            m_Spawn = new GameObject("ChildSpawn").transform;
            m_Head = new GameObject("Head").transform;
            m_Head.position = new Vector3(0f, 1.5f, 1.2f);
        }

        [TearDown]
        public void DestroyRig()
        {
            Time.timeScale = m_TimeScale;
            if (m_Spawner != null && m_Spawner.Spawned != null)
                Object.Destroy(m_Spawner.Spawned.gameObject);
            foreach (var component in new Component[] { m_Spawn, m_Head, m_Spawner, m_Driver })
                if (component != null)
                    Object.Destroy(component.gameObject);
        }

        void CreateSpawnerAndDriver(CharacterProfile profile)
        {
            m_Spawner = new GameObject("Spawner").AddComponent<ChildSpawner>();
            m_Spawner.Configure(profile, m_Spawn, m_Head);
            m_Driver = new GameObject("ComfortDriver").AddComponent<ComfortDriver>();
            m_Driver.Configure(m_Spawner, m_Head, new Transform[0]);
        }

        [UnityTest]
        public IEnumerator ComfortDriver_HeadStaringAtChild_ChildLooksAway()
        {
            var profile = LoadProfile("aj");
            if (profile == null)
                Assert.Ignore("profiles load through the AssetDatabase; Editor only");
            CreateSpawnerAndDriver(profile);
            yield return null;
            Assert.That(m_Spawner.Spawned, Is.Not.Null);
            var childHead = m_Spawner.Spawned.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);

            Time.timeScale = 4f;
            for (var elapsed = 0f; elapsed < 5.6f; elapsed += Time.deltaTime)
            {
                m_Head.LookAt(childHead);
                yield return null;
            }

            Assert.That(m_Driver.Engine, Is.Not.Null);
            Assert.That(m_Driver.Engine.Log.Events.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(m_Spawner.Spawned.Current, Is.EqualTo(ChildReaction.LooksAway));
        }

        [UnityTest]
        public IEnumerator ComfortDriver_NoChild_DoesNothing()
        {
            LogAssert.Expect(LogType.Error, "ChildSpawner has no profile with a model, or no spawn point");
            CreateSpawnerAndDriver(null);
            yield return null;
            yield return null;

            Assert.That(m_Spawner.Spawned, Is.Null);
            Assert.That(m_Driver.Engine, Is.Null);
        }
    }
}
