using AutismVR.Characters;
using AutismVR.Comfort;
using NUnit.Framework;
using UnityEngine;

namespace AutismVR.Tests.EditMode
{
    public class ReactionSelectorTests
    {
        CharacterProfile m_Profile;

        [TearDown]
        public void DestroyProfile()
        {
            Object.DestroyImmediate(m_Profile);
        }

        ComfortModel Model(float start, float threshold = 0.75f)
        {
            m_Profile = ScriptableObject.CreateInstance<CharacterProfile>();
            m_Profile.StartingComfort = start;
            m_Profile.GreetingThreshold = threshold;
            return new ComfortModel(m_Profile, TriggerCatalog.LoadDefault());
        }

        static TriggerDefinition Entry(string id) => TriggerCatalog.LoadDefault().Find(id);

        [TestCase(0.2f, ChildReaction.Withdrawn)]
        [TestCase(0.3f, ChildReaction.Fidgets)]
        [TestCase(0.5f, ChildReaction.Uneasy)]
        [TestCase(0.7f, ChildReaction.Calm)]
        public void Band_FollowsComfort(float comfort, ChildReaction expected)
        {
            var selector = new ReactionSelector();

            Assert.That(selector.Update(Model(comfort), 0.1f), Is.EqualTo(expected));
            Assert.That(selector.Current, Is.EqualTo(expected));
        }

        [Test]
        public void Meltdown_IsTantrumEvenWithHeldReaction()
        {
            var model = Model(0.05f);
            var selector = new ReactionSelector();
            selector.OnHit(Entry(TriggerIds.Staring));

            Assert.That(model.InMeltdown, Is.True);
            Assert.That(selector.Update(model, 0.1f), Is.EqualTo(ChildReaction.Tantrum));
        }

        [Test]
        public void HeldReaction_LastsTwoSecondsThenBand()
        {
            var model = Model(0.5f);
            var selector = new ReactionSelector();
            selector.OnHit(Entry(TriggerIds.Staring));

            Assert.That(selector.Update(model, 0.1f), Is.EqualTo(ChildReaction.LooksAway));
            for (var i = 0; i < 18; i++)
                selector.Update(model, 0.1f);
            Assert.That(selector.Current, Is.EqualTo(ChildReaction.LooksAway), "still held at 1.9 s");

            selector.Update(model, 0.1f);
            selector.Update(model, 0.1f);
            Assert.That(selector.Current, Is.EqualTo(ChildReaction.Uneasy));
        }

        [Test]
        public void HitWithoutReaction_KeepsBand()
        {
            var selector = new ReactionSelector();
            selector.OnHit(Entry(TriggerIds.Sarcasm));

            Assert.That(selector.Update(Model(0.7f), 0.1f), Is.EqualTo(ChildReaction.Calm));
        }

        [Test]
        public void Greeted_IsGreetingForever()
        {
            var model = Model(0.8f, threshold: 0.75f);
            var selector = new ReactionSelector();

            Assert.That(model.Greeted, Is.True);
            Assert.That(selector.Update(model, 0.1f), Is.EqualTo(ChildReaction.Greeting));
            selector.OnHit(Entry(TriggerIds.Shouting));
            Assert.That(selector.Update(model, 0.1f), Is.EqualTo(ChildReaction.Greeting));
        }
    }
}
