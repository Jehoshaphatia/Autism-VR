using System.Collections;
using AutismVR.Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AutismVR.Tests.PlayMode
{
    public class ChildSpawnerTests
    {
        Transform m_Spawn;
        Transform m_Trainer;
        ChildSpawner m_Spawner;

        static CharacterProfile LoadProfile(string id)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterProfile>($"Assets/_Project/Characters/Profiles/{id}.asset");
#else
            return null;
#endif
        }

        [SetUp]
        public void CreateSpawner()
        {
            SelectedCharacter.Clear();
            m_Spawn = new GameObject("ChildSpawn").transform;
            m_Spawn.position = new Vector3(1f, 0f, 4f);
            m_Trainer = new GameObject("TrainerSpawn").transform;
        }

        // Created inside each test, after any choice: the chooser always picks before Room loads.
        void CreateSpawnerNow()
        {
            m_Spawner = new GameObject("Spawner").AddComponent<ChildSpawner>();
            m_Spawner.Configure(LoadProfile("aj"), m_Spawn, m_Trainer);
        }

        [TearDown]
        public void DestroyObjects()
        {
            SelectedCharacter.Clear();
            Object.Destroy(m_Spawn.gameObject);
            Object.Destroy(m_Trainer.gameObject);
            if (m_Spawner == null)
                return;
            if (m_Spawner.Spawned != null)
                Object.Destroy(m_Spawner.Spawned.gameObject);
            Object.Destroy(m_Spawner.gameObject);
        }

        [UnityTest]
        public IEnumerator ChildSpawner_WithSelection_SpawnsSelectedProfileAtSpawnPoint()
        {
            var chosen = LoadProfile("ch12");
            SelectedCharacter.Choose(chosen);
            CreateSpawnerNow();
            yield return null;

            Assert.That(m_Spawner.Spawned, Is.Not.Null);
            Assert.That(m_Spawner.Spawned.name, Does.StartWith(chosen.ModelPrefab.name));
            Assert.That(Vector3.Distance(m_Spawner.Spawned.transform.position, m_Spawn.position), Is.LessThan(0.01f));
            var toTrainer = Vector3.ProjectOnPlane(m_Trainer.position - m_Spawn.position, Vector3.up).normalized;
            Assert.That(Vector3.Angle(m_Spawner.Spawned.transform.forward, toTrainer), Is.LessThan(5f));
        }

        [UnityTest]
        public IEnumerator ChildSpawner_NoSelection_SpawnsDefaultProfile()
        {
            CreateSpawnerNow();
            yield return null;

            Assert.That(m_Spawner.Spawned, Is.Not.Null);
            Assert.That(m_Spawner.Spawned.name, Does.StartWith(LoadProfile("aj").ModelPrefab.name));
        }
    }
}
