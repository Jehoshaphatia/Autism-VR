using System.Collections.Generic;
using System.Linq;
using AutismVR.Characters;
using AutismVR.Comfort;
using NUnit.Framework;
using UnityEngine;

namespace AutismVR.Tests.EditMode
{
    public class ComfortEngineTests
    {
        // The child stands at the origin; the trainee faces it from +z.
        static readonly Vector3 ChildHead = new Vector3(0f, 1.1f, 0f);
        static readonly Vector3 ChildChest = new Vector3(0f, 0.9f, 0f);
        const float Speech = -30f;

        readonly List<Object> m_Created = new List<Object>();

        [TearDown]
        public void DestroyProfiles()
        {
            foreach (var created in m_Created)
                Object.DestroyImmediate(created);
            m_Created.Clear();
        }

        CharacterProfile Profile(float start = 0.5f, float threshold = 0.75f, float recovery = 0.02f,
            string sensitiveTo = null, float multiplier = 1f, string excluded = null)
        {
            var profile = ScriptableObject.CreateInstance<CharacterProfile>();
            m_Created.Add(profile);
            profile.StartingComfort = start;
            profile.GreetingThreshold = threshold;
            profile.RecoveryPerSecond = recovery;
            if (sensitiveTo != null)
                profile.TriggerSensitivities.Add(new TriggerSensitivity { TriggerId = sensitiveTo, Multiplier = multiplier });
            if (excluded != null)
                profile.ExcludedTriggers.Add(excluded);
            return profile;
        }

        // Trainee head at `distance`, looking at the child or to the side, hands hanging at their sides.
        static SensorFrame Frame(float distance, bool lookingAtChild, float dbfs)
        {
            var head = new Vector3(0f, 1.6f, distance);
            return new SensorFrame
            {
                HeadPosition = head,
                HeadForward = lookingAtChild ? (ChildHead - head).normalized : Vector3.right,
                Hands = new List<Vector3> { head + new Vector3(-0.25f, -0.7f, 0f), head + new Vector3(0.25f, -0.7f, 0f) },
                ChildHead = ChildHead,
                ChildChest = ChildChest,
                ChildPosition = Vector3.zero,
                LoudnessDbfs = dbfs,
            };
        }

        static void Run(ComfortEngine engine, SensorFrame frame, float seconds)
        {
            for (var t = 0f; t < seconds - 1e-4f; t += 0.1f)
                engine.Step(frame, 0.1f);
        }

        static bool RunUntil(ComfortEngine engine, SensorFrame frame, float maxSeconds, System.Func<bool> done)
        {
            for (var t = 0f; t < maxSeconds; t += 0.1f)
            {
                engine.Step(frame, 0.1f);
                if (done())
                    return true;
            }
            return false;
        }

        [Test]
        public void Engine_StaresAtChild_LowersComfortAndLooksAway()
        {
            var engine = new ComfortEngine(Profile(start: 0.5f), TriggerCatalog.LoadDefault());

            Run(engine, Frame(1.2f, true, Speech), 6f);

            Assert.That(engine.Model.Comfort, Is.LessThan(0.5f));
            Assert.That(engine.Reactions.Current, Is.EqualTo(ChildReaction.LooksAway));
            Assert.That(engine.Log.Events.Select(e => e.TriggerId), Is.EqualTo(new[] { TriggerIds.Staring, TriggerIds.Staring }));
        }

        [Test]
        public void Engine_DisabledOrExcludedTrigger_NoChangeNoEvent()
        {
            const string json = "{\"version\":1,\"entries\":[" +
                "{\"id\":\"staring\",\"name\":\"Staring\",\"description\":\"d\",\"kind\":\"trigger\",\"channels\":[\"gaze\"],\"effect\":-0.04,\"enabled\":false,\"hold_seconds\":4,\"cone_degrees\":15,\"repeat_seconds\":1}," +
                "{\"id\":\"shouting\",\"name\":\"Shouting\",\"description\":\"d\",\"kind\":\"trigger\",\"channels\":[\"loudness\"],\"effect\":-0.15,\"enabled\":true,\"threshold_dbfs\":-15,\"hold_seconds\":0.3,\"refractory_seconds\":2}]}";
            Assert.That(TriggerCatalog.TryParse(json, out var catalog), Is.True);
            var engine = new ComfortEngine(Profile(start: 0.5f, excluded: TriggerIds.Shouting), catalog);

            Run(engine, Frame(1.2f, true, -5f), 8f);

            Assert.That(engine.Report(TriggerIds.Shouting, Channel.Words), Is.False);
            Assert.That(engine.Log.Events, Is.Empty);
            Assert.That(engine.Model.Comfort, Is.EqualTo(0.5f));
        }

        [Test]
        public void Engine_LongFrameHitch_ClampsDt()
        {
            var engine = new ComfortEngine(Profile(start: 0.5f), TriggerCatalog.LoadDefault());
            Run(engine, Frame(1.2f, true, Speech), 3.9f);
            var time = engine.Time;

            engine.Step(Frame(1.2f, true, Speech), 5f);

            Assert.That(engine.Time - time, Is.EqualTo(ComfortRules.MaxDeltaTime).Within(1e-5f));
            Assert.That(engine.Log.Events.Count, Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void Engine_Report_LogsExcerptAndHoldsReaction()
        {
            var engine = new ComfortEngine(Profile(start: 0.7f), TriggerCatalog.LoadDefault());

            Assert.That(engine.Report(TriggerIds.DemandingEyeContact, Channel.Words, 1f, "look at me"), Is.True);
            engine.Step(Frame(1.2f, false, Speech), 0.1f);

            Assert.That(engine.Log.Events.Single().TranscriptExcerpt, Is.EqualTo("look at me"));
            Assert.That(engine.Reactions.Current, Is.EqualTo(ChildReaction.LooksAway));
        }

        // Spec §8b end to end: shouting causes a meltdown; quiet and space de-escalate it; the child then greets.
        [Test]
        public void Engine_Scenario_ShoutToMeltdownThenDeescalateThenGreet()
        {
            var engine = new ComfortEngine(
                Profile(start: 0.35f, threshold: 0.75f, recovery: 0.02f, sensitiveTo: TriggerIds.Shouting, multiplier: 1.5f),
                TriggerCatalog.LoadDefault());

            Assert.That(RunUntil(engine, Frame(0.5f, true, -5f), 10f, () => engine.Model.InMeltdown), Is.True, "shouting up close melts down");
            Assert.That(engine.Reactions.Current, Is.EqualTo(ChildReaction.Tantrum));
            Assert.That(engine.Log.MeltdownHappened, Is.True);

            var calm = Frame(2f, false, -50f);
            Assert.That(RunUntil(engine, calm, 60f, () => engine.Log.Deescalated), Is.True, "quiet and space de-escalate");
            Assert.That(engine.Model.InMeltdown, Is.False);

            Assert.That(RunUntil(engine, calm, 300f, () => engine.Log.Greeted), Is.True, "the child greets once comfortable");
            Assert.That(engine.Reactions.Current, Is.EqualTo(ChildReaction.Greeting));

            var comfort = engine.Model.Comfort;
            var events = engine.Log.Events.Count;
            Run(engine, Frame(0.5f, true, -5f), 5f);
            Assert.That(engine.Model.Comfort, Is.EqualTo(comfort));
            Assert.That(engine.Log.Events.Count, Is.EqualTo(events));
            Assert.That(engine.Reactions.Current, Is.EqualTo(ChildReaction.Greeting));
        }

        [Test]
        public void Engine_NormalConversation_NoHits()
        {
            var engine = new ComfortEngine(Profile(start: 0.5f), TriggerCatalog.LoadDefault());

            for (var cycle = 0; cycle < 10; cycle++)
            {
                Run(engine, Frame(1.2f, true, Speech), 2f);
                Run(engine, Frame(1.2f, false, Speech), 4f);
            }

            Assert.That(engine.Log.Events, Is.Empty);
            Assert.That(engine.Model.Comfort, Is.EqualTo(0.5f));
        }
    }
}
