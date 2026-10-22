# F4 — Comfort and Trigger Engine Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The spawned child reacts to the trainee. An editable trigger catalog and the chosen child's profile drive a hidden comfort value. Head, hand and loudness detectors lower or raise it. Comfort picks the child's reaction, a meltdown can be de-escalated, the child greets once comfort is high enough, and every hit is logged for the feedback summary.

**Architecture:** Plain C# classes (no MonoBehaviour) in `AutismVR.Comfort`, each with one job:
- the catalog (data loaded from JSON, so F5/F7 can feed it from Supabase);
- `ComfortModel` (the number, meltdown and greeting rules);
- `ReactionSelector` (comfort to `ChildReaction`);
- four detectors (samples to trigger hits);
- `SessionLog` (events and summary);
- `ComfortEngine`, which composes them per frame from a `SensorFrame`.

One thin MonoBehaviour, `ComfortDriver`, samples head, hands and child head in `Room` and plays the result through `ChildAnimator`. Words triggers (Decisions) and real mic loudness arrive in F6 through `ComfortEngine.Report` and `ComfortDriver.LoudnessDbfs`. No network.

**Tech Stack:** Unity 6000.6.0f1, Unity Test Framework (EditMode-heavy), `JsonUtility`, XRI 3.6.1 rig from F2, Unity CLI + `com.unity.pipeline`, Meta XR Simulator 207.1.

**Spec:** `docs/superpowers/specs/2026-10-21-autism-vr-rebuild-design.md`:
- §4 comfort and reaction states
- §4.1 per-character baseline
- §5.1 trigger list
- §5.2 eye-contact rule
- §5.3 detection channels
- §5.4 catalog fields
- §8 trigger event fields
- §8b meltdown is recoverable, plus outcomes

Also the roadmap F4 row.

## Global Constraints

- **Comfort:** a hidden value. Triggers lower it; good behaviour slowly raises it. Reaction follows comfort, and the greeting happens when comfort crosses a threshold (§4).
- **Eye contact (§5.2):**
  - Penalise **demanding** eye contact and **sustained staring**, not ordinary eye contact. Natural glances: no change.
  - Staring threshold is configurable, starting at 3–5 s. **Use 4 s.**
  - Comfort drops gradually while staring continues.
  - Eye direction on Meta VR Glasses falls back to head direction where eye tracking is absent (§8a). **F4 uses head direction only.**
- **Catalog entry fields (§5.4):** id/name, plain-language description, detection channel(s) and channel parameters, effect on comfort (strength), resulting reaction (optional), enabled flag. Adding a row adds a trigger; disabling a row removes it. Starting values are **provisional** (§8a).
- **Channels (§5.3):** `words`, `loudness`, `gaze`, `proximity`, `hands`.
- **Calming behaviours (§5.3):** calm quiet voice, short simple sentences, waiting, letting the child look away, keeping distance, giving space during a meltdown.
- **Profile (§4.1, F3 `CharacterProfile`):** `StartingComfort`, `GreetingThreshold`, `RecoveryPerSecond` ("recovery speed after a meltdown"), `GetSensitivity(id)` (1 unlisted, 0 excluded).
- **Meltdowns:** recoverable when the trainee de-escalates. Record whether a meltdown happened and whether it was de-escalated (§8b). The session ends when the child greets (§8b), so **Greeting is terminal** in the engine.
- **Trigger event (§8):** timestamp, trigger id, channel, transcript excerpt, comfort before/after.
- **Engine code:** pure C# under `Assets/_Project/Runtime/Comfort/`, namespace `AutismVR.Comfort`. `UnityEngine` value types (`Vector3`, `Mathf`, `JsonUtility`) are allowed; MonoBehaviours only in Task 8.
- **Commits and PR:** commits Udacity style, **no Co-Authored-By**; PR body has **no "Generated with Claude Code" line**.
- **Green:** EditMode + PlayMode exit 0, Android build exit 0, evidence committed under `docs/verification/f4-comfort-trigger-engine/`. `$P` is the repo root.

### Provisional numbers *(assistant's calls; owner and clinician tune them in the catalog, F8)*

**Trigger effects** (negative lowers comfort; scaled by profile sensitivity × intensity):

| id | channels | effect | reaction |
|---|---|---|---|
| `shouting` | loudness, words | −0.15 | Withdrawn |
| `demanding_eye_contact` | words | −0.12 | LooksAway |
| `staring` | gaze | −0.02 per 5 s beyond threshold *(final review: was −0.04 per second)* | LooksAway |
| `rapid_questions` | words | −0.08 | — |
| `multi_step_instructions` | words | −0.06 | — |
| `sarcasm` | words | −0.05 | — |
| `anger` | words, loudness | −0.12 | — |
| `pressuring_speech` | words | −0.10 | — |
| `approach_too_close` | proximity | −0.12 | LooksAway |
| `reaching_or_touch` | hands | −0.15 | Withdrawn |
| `demands_during_meltdown` | words | −0.10 | — |

**Calming effects** (positive; not scaled by sensitivity):

| id | channel | effect |
|---|---|---|
| `calm_quiet_voice` | words | +0.03 |
| `waits_quietly` | loudness | +0.02 |
| `lets_child_look_away` | words | +0.05 |
| `gives_space` | proximity | +0.04 |

**Channel parameters:**

| Channel | Parameters |
|---|---|
| Shouting | above −15 dBFS for 0.3 s; refractory 2 s |
| Waiting quietly | below −40 dBFS for 5 s; repeats every 5 s |
| Staring | within a 10° cone of the child's head for 5 s, then one hit per further 5 s; resets when the gaze leaves the cone *(final review: was 15° / 4 s / every second)* |
| Too close | under 0.8 m, re-armed after going beyond 1.0 m; or approaching faster than 1.0 m/s within 2 m; refractory 2 s |
| Giving space | at least 1.5 m for 5 s; repeats every 5 s |
| Reaching or touch | a hand within 0.35 m of the child's head or chest, or moving toward the child faster than 1.5 m/s within 1.0 m; refractory 2 s |

**Comfort rules:**
- Meltdown starts at comfort ≤ 0.10 and ends at comfort ≥ 0.30 (the end marks it de-escalated).
- During a meltdown, comfort recovers at `RecoveryPerSecond` once no trigger has hit for 3 s. Outside a meltdown there is no passive drift: only calming behaviours raise comfort.
- A catalog reaction holds for 2 s.
- Frame `dt` is clamped to 0.25 s.

**Reaction bands:**

| Comfort | Reaction |
|---|---|
| meltdown | Tantrum |
| < 0.25 | Withdrawn |
| < 0.40 | Fidgets |
| < 0.60 | Uneasy |
| otherwise | Calm |

Greeting overrides all of these once reached, and is terminal.

## Review Focus

1. **A catalog JSON that is empty, garbled, or names an unknown channel or reaction** (F7 loads it from a cache or network): the child must still react. Expected: `TriggerCatalog.TryParse` returns false and the caller uses the built-in default; unknown channel/reaction strings skip only that entry. Pinned by `TryParse_Garbage_ReturnsFalse` and `TryParse_UnknownChannel_SkipsEntryKeepsOthers` (Task 1).
2. **A disabled entry, or a trigger the child's profile excludes, still fires.** Expected: no comfort change and no logged hit, because the feedback summary must only list real hits. Pinned by `Engine_DisabledOrExcludedTrigger_NoChangeNoEvent` (Task 7).
3. **A frame hitch** (headset taken off, app paused for seconds, then `dt` = 5 s). Expected: no burst of stacked staring or waiting hits and no comfort jump; `dt` is clamped. Pinned by `Engine_LongFrameHitch_ClampsDt` (Task 7).
4. **Relentless triggers, or calming far beyond the top.** Expected: comfort stays within [0, 1]; a meltdown still ends once the trainee de-escalates. Pinned by `Comfort_ClampedToUnitRange` and `Comfort_MeltdownRecoversAfterQuiet` (Task 2), plus the Task 7 scenario.
5. **No child spawned yet, or degenerate vectors** (zero forward, head at the child's position, no hands tracked). Expected: detectors emit nothing and never produce NaN. Pinned by `Stare_ZeroForward_NoHit`, `Proximity_SamePosition_NoNaN`, `HandReach_NoHands_NoHit` (Tasks 4–5), and `ComfortDriver_NoChild_DoesNothing` (Task 8).

---

### Task 0: F3 review follow-ups (hands-only guard)

**Files:**
- Modify: `Assets/_Project/Tests/EditMode/HandsOnlyRigTests.cs`
- Modify: `Assets/_Project/Tests/PlayMode/PokeButtonTests.cs`
- Modify: `Assets/_Project/Runtime/Interaction/PokeButton.cs` (comment only)

- [ ] Extend `EveryButtonPressingInteractorIsUnderAHandOrAController`:
  - `leftHand` and `rightHand` are non-null;
  - at least one interactor is under a hand;
  - no interactor is under both a hand and a controller.

  Run: EditMode passes. This is a guard, so it is expected to pass on the current rig.
- [ ] `PokeButton_ControllerPokeSelect_DoesNotRaisePressed`: count `XRSimpleInteractable.selectEntered` with a listener and assert it is 1, so "blocked" is distinguishable from "never selected".
- [ ] Reword the `PokeButton` comment. The project's OpenXR settings enable no controller profile, so this guard covers the XRI rig's controller interactors as defence in depth. It is not a device-verified block.
- [ ] Suites green. Commit `test: Tighten hands-only rig checks`.

### Task 1: Trigger catalog

**Files:**
- Create: `Assets/_Project/Runtime/Comfort/Channel.cs`
- Create: `Assets/_Project/Runtime/Comfort/TriggerDefinition.cs`
- Create: `Assets/_Project/Runtime/Comfort/TriggerCatalog.cs`
- Create: `Assets/_Project/Runtime/Comfort/CalmingIds.cs`
- Create: `Assets/_Project/Resources/Comfort/trigger-catalog.json`
- Test: `Assets/_Project/Tests/EditMode/TriggerCatalogTests.cs`

**Interfaces:**
- Produces:
  - `[Flags] enum Channel { None = 0, Words = 1, Loudness = 2, Gaze = 4, Proximity = 8, Hands = 16 }`
  - `enum TriggerKind { Trigger, Calming }`
  - `CalmingIds` constants: `CalmQuietVoice = "calm_quiet_voice"`, `WaitsQuietly = "waits_quietly"`, `LetsChildLookAway = "lets_child_look_away"`, `GivesSpace = "gives_space"`, plus `All`.
  - `sealed class TriggerDefinition`, immutable, with properties:
    - `Id`, `Name`, `Description`
    - `TriggerKind Kind`, `Channel Channels`, `float Effect`, `ChildReaction? Reaction`
    - `bool Enabled`, `bool Provisional`
    - parameters, each 0 when unused: `float ThresholdDbfs`, `HoldSeconds`, `RefractorySeconds`, `RepeatSeconds`, `ConeDegrees`, `DistanceMeters`, `RearmDistanceMeters`, `SpeedMetersPerSecond`, `RangeMeters`
  - `sealed class TriggerCatalog`:
    - `IReadOnlyList<TriggerDefinition> Entries`
    - `TriggerDefinition Find(string id)` (null if absent)
    - `bool IsActive(string id)` (present and enabled)
    - `static bool TryParse(string json, out TriggerCatalog catalog)`
    - `static TriggerCatalog LoadDefault()`, which reads the `Resources` asset `Comfort/trigger-catalog`
    - `string ToJson()`
- JSON shape, with snake_case to match the future Supabase columns. The serializable DTO classes are private to `TriggerCatalog`.

  ```json
  {"version":1,"entries":[{"id":"shouting","name":"Shouting","description":"…","kind":"trigger","channels":["loudness","words"],"effect":-0.15,"reaction":"Withdrawn","enabled":true,"provisional":true,"threshold_dbfs":-15,"hold_seconds":0.3,"refractory_seconds":2}]}
  ```

- [ ] **Write the failing tests:**
  - `DefaultCatalog_HasEveryTriggerAndCalmingId`: the ids equal `TriggerIds.All ∪ CalmingIds.All`, each exactly once.
  - `DefaultCatalog_MatchesProvisionalTable`: a `[TestCase]` per row of the Global Constraints tables, checking effect, channels and reaction.
  - `DefaultCatalog_EntriesAreEnabledProvisionalAndDescribed`: every entry is enabled, provisional, and has a non-empty description.
  - `TryParse_RoundTripsToJson`: `TryParse(LoadDefault().ToJson())` gives entries equal by id, effect, channels and parameters.
  - `TryParse_Garbage_ReturnsFalse`: `"not json"`, `""` and `"{}"` (no entries) all return false.
  - `TryParse_UnknownChannel_SkipsEntryKeepsOthers`: one entry with channel `"smell"`, one valid entry → one entry, true.
  - `IsActive_DisabledEntry_False`.
- [ ] Run: `unity test $P --mode EditMode --filter TriggerCatalogTests`. Expected: fails to compile (types missing).
- [ ] **Implement.** Write the description text in plain language for the Decisions prompt (§5.4), from §5.1–5.3. Example: shouting is "The adult raises their voice to a shout or yells."
- [ ] Run the filter again: PASS. Then run the full EditMode suite: PASS.
- [ ] Commit `feat: Add editable trigger catalog`.

### Task 2: Comfort model

**Files:**
- Create: `Assets/_Project/Runtime/Comfort/ComfortModel.cs`
- Create: `Assets/_Project/Runtime/Comfort/ComfortRules.cs`
- Test: `Assets/_Project/Tests/EditMode/ComfortModelTests.cs`

**Interfaces:**
- Consumes: `CharacterProfile` (F3), `TriggerCatalog` / `TriggerDefinition` (Task 1).
- Produces:
  - `static class ComfortRules` with constants: `MeltdownEnter = 0.10f`, `MeltdownExit = 0.30f`, `QuietSecondsBeforeRecovery = 3f`, `ReactionHoldSeconds = 2f`, `MaxDeltaTime = 0.25f`.
  - `readonly struct ComfortChange (string TriggerId, Channel Channel, float Before, float After)`.
  - `sealed class ComfortModel`:
    - constructor `ComfortModel(CharacterProfile profile, TriggerCatalog catalog)`
    - state: `float Comfort`, `bool InMeltdown`, `bool MeltdownHappened`, `bool Deescalated`, `bool Greeted`
    - `ComfortChange? Apply(string id, Channel channel, float intensity = 1f)`: returns null if the id is inactive, the profile excludes it, or `Greeted` is set.
    - `void Advance(float dt)`: clamps `dt` to `MaxDeltaTime`, then applies meltdown recovery.
- **Rules:**
  - Trigger delta = `Effect × profile.GetSensitivity(id) × intensity`. Calming delta = `Effect × intensity`.
  - Clamp to [0, 1].
  - Meltdown hysteresis: enter ≤ 0.10, exit ≥ 0.30, which sets `Deescalated`.
  - `Greeted` becomes true the first time comfort ≥ `GreetingThreshold` while not in a meltdown, and stays true. After that, `Apply` returns null.

- [ ] **Write the failing tests.** Use `ScriptableObject.CreateInstance<CharacterProfile>()` with explicit values; the catalog is `LoadDefault()`.
  - `Comfort_StartsAtProfileStartingComfort`.
  - `Apply_Shouting_ScalesBySensitivity`: with sensitivity 1.5 and start 0.5, comfort ends at 0.5 − 0.225.
  - `Apply_Calming_IgnoresSensitivity`.
  - `Comfort_ClampedToUnitRange`: 20 shouts gives 0; 100 `gives_space` gives ≤ 1.
  - `Comfort_EntersMeltdownAtOrBelowEnterLevel`.
  - `Comfort_MeltdownRecoversAfterQuiet`: in a meltdown with recovery 0.02/s, no hits for 3 s, then 10 s of `Advance` in 0.1 s steps reaches 0.30 or more, `InMeltdown` false, `Deescalated` true.
  - `Advance_OutsideMeltdown_NoDrift`.
  - `Advance_TriggerWithinQuietWindow_DelaysRecovery`.
  - `Comfort_CrossingThreshold_GreetsOnceThenIgnoresTriggers`.
  - `Apply_ExcludedOrDisabled_ReturnsNull`.
- [ ] Run: fails to compile.
- [ ] Implement.
- [ ] Run the filter: PASS. Then the full EditMode suite: PASS.
- [ ] Commit `feat: Add comfort model with recoverable meltdowns`.

### Task 3: Reaction selection

**Files:**
- Create: `Assets/_Project/Runtime/Comfort/ReactionSelector.cs`
- Test: `Assets/_Project/Tests/EditMode/ReactionSelectorTests.cs`

**Interfaces:**
- Consumes: `ComfortModel`, `ComfortRules`, `TriggerDefinition.Reaction`.
- Produces `sealed class ReactionSelector`:
  - `ChildReaction Current`
  - `void OnHit(TriggerDefinition hit)`: starts a 2 s hold if `hit.Reaction` has a value.
  - `ChildReaction Update(ComfortModel model, float dt)`

  Precedence: Greeting (if `Greeted`) > Tantrum (if `InMeltdown`) > held catalog reaction > comfort band.

- [ ] **Write the failing tests:**
  - `[TestCase]` bands: 0.05 (not in meltdown) → Withdrawn, 0.3 → Fidgets, 0.5 → Uneasy, 0.7 → Calm.
  - `Meltdown_IsTantrumEvenWithHeldReaction`.
  - `HeldReaction_LastsTwoSecondsThenBand`.
  - `Greeted_IsGreetingForever`.
- [ ] Run → fail. Implement. Run → PASS.
- [ ] Commit `feat: Select child reaction from comfort`.

### Task 4: Loudness and stare detectors

**Files:**
- Create: `Assets/_Project/Runtime/Comfort/Detectors/TriggerHit.cs`
- Create: `Assets/_Project/Runtime/Comfort/Detectors/LoudnessDetector.cs`
- Create: `Assets/_Project/Runtime/Comfort/Detectors/StareDetector.cs`
- Test: `Assets/_Project/Tests/EditMode/LoudnessDetectorTests.cs`
- Test: `Assets/_Project/Tests/EditMode/StareDetectorTests.cs`

**Interfaces:**
- Produces:
  - `readonly struct TriggerHit (string TriggerId, Channel Channel, float Intensity)`.
  - `LoudnessDetector(TriggerDefinition shouting, TriggerDefinition waitsQuietly)` with `void Sample(float dbfs, float dt, List<TriggerHit> hits)`:
    - shouting after `HoldSeconds` above `ThresholdDbfs`, then refractory;
    - `waits_quietly` every `RepeatSeconds` while below its `ThresholdDbfs`.
  - `StareDetector(TriggerDefinition staring)` with `void Sample(Vector3 headPosition, Vector3 headForward, Vector3 childHead, float dt, List<TriggerHit> hits)`:
    - inside `ConeDegrees` continuously for `HoldSeconds` (4) → one hit per further second, intensity 1;
    - leaving the cone resets the timer.
  - A null definition disables that output.

- [ ] **Write the failing tests:**
  - `Shout_ShorterThanHold_NoHit`.
  - `Shout_Sustained_OneHitThenRefractory`: −5 dBFS for 1 s gives 1 hit; after 2.5 s more, a second hit.
  - `Quiet_FiveSeconds_WaitsQuietlyHit`.
  - `NormalSpeech_NoHits`: −30 dBFS for 10 s.
  - `Glance_UnderThreshold_NoHit`: 3 s in the cone, then away.
  - `Stare_SixSeconds_TwoHits`.
  - `Stare_LookAwayResetsTimer`.
  - `Stare_OutsideCone_NoHit`: 30° off.
  - `Stare_ZeroForward_NoHit`.
- [ ] Run → fail. Implement. Run → PASS.
- [ ] Commit `feat: Detect shouting, quiet waiting and staring`.

### Task 5: Proximity and hand-reach detectors

**Files:**
- Create: `Assets/_Project/Runtime/Comfort/Detectors/ProximityDetector.cs`
- Create: `Assets/_Project/Runtime/Comfort/Detectors/HandReachDetector.cs`
- Test: `Assets/_Project/Tests/EditMode/ProximityDetectorTests.cs`
- Test: `Assets/_Project/Tests/EditMode/HandReachDetectorTests.cs`

**Interfaces:**
- Produces:
  - `ProximityDetector(TriggerDefinition approach, TriggerDefinition givesSpace)` with `void Sample(Vector3 headPosition, Vector3 childPosition, float dt, List<TriggerHit> hits)`. Distances are measured on the floor plane (y ignored).
    - Too close: under `DistanceMeters` (0.8), firing once until re-armed beyond `RearmDistanceMeters` (1.0).
    - Fast approach: approach speed > `SpeedMetersPerSecond` (1.0) within `RangeMeters` (2.0), with refractory.
    - `gives_space`: every `RepeatSeconds` (5) while at or beyond its `DistanceMeters` (1.5).
  - `HandReachDetector(TriggerDefinition reach)` with `void Sample(IReadOnlyList<Vector3> hands, Vector3 childHead, Vector3 childChest, float dt, List<TriggerHit> hits)`:
    - a hand within `DistanceMeters` (0.35) of the head or chest; or
    - a hand closing on the chest faster than `SpeedMetersPerSecond` (1.5) within `RangeMeters` (1.0);
    - refractory between hits.
    - Per-hand previous positions are keyed by list index.

- [ ] **Write the failing tests:**
  - `WalkInToHalfMetre_OneHitUntilRearmed`.
  - `RushIn_FastApproachHit`: 2.0 m to 1.2 m in 0.4 s.
  - `SlowApproachToOneMetre_NoHit`.
  - `KeepingTwoMetres_GivesSpaceEveryFiveSeconds`.
  - `Proximity_SamePosition_NoNaN`: no exception and no NaN.
  - `HandNearHead_Hit`.
  - `HandSwipeTowardChest_Hit`.
  - `HandsAtSides_NoHit`.
  - `HandReach_NoHands_NoHit`.
  - `HandReach_RefractoryBetweenHits`.
- [ ] Run → fail. Implement. Run → PASS.
- [ ] Commit `feat: Detect approaching and reaching toward the child`.

### Task 6: Session log

**Files:**
- Create: `Assets/_Project/Runtime/Comfort/SessionLog.cs`
- Test: `Assets/_Project/Tests/EditMode/SessionLogTests.cs`

**Interfaces:**
- Produces:
  - `readonly struct TriggerEvent (float Time, string TriggerId, Channel Channel, string TranscriptExcerpt, float ComfortBefore, float ComfortAfter)`.
  - `sealed class SessionLog`:
    - `IReadOnlyList<TriggerEvent> Events`
    - `void Record(float time, ComfortChange change, string excerpt = null)`
    - `IReadOnlyDictionary<string, int> TriggerCounts(TriggerCatalog catalog)`: counts only `Kind == Trigger` entries, not calming.
    - `bool MeltdownHappened`, `bool Deescalated`, `bool Greeted`, set by the engine.

- [ ] **Write the failing tests:**
  - `Record_KeepsOrderAndFields`.
  - `TriggerCounts_CountsTriggersNotCalming`.
  - `TriggerCounts_EmptySession_Empty`.
- [ ] Run → fail. Implement. Run → PASS.
- [ ] Commit `feat: Log trigger events for the session summary`.

### Task 7: Comfort engine

**Files:**
- Create: `Assets/_Project/Runtime/Comfort/SensorFrame.cs`
- Create: `Assets/_Project/Runtime/Comfort/ComfortEngine.cs`
- Test: `Assets/_Project/Tests/EditMode/ComfortEngineTests.cs`

**Interfaces:**
- Consumes: Tasks 1–6.
- Produces:
  - `struct SensorFrame` with fields:
    - `Vector3 HeadPosition`, `HeadForward`
    - `List<Vector3> Hands` (tracked hands only)
    - `Vector3 ChildHead`, `ChildChest`, `ChildPosition`
    - `float LoudnessDbfs` (`float.NegativeInfinity` when there is no mic)
  - `sealed class ComfortEngine`:
    - constructor `ComfortEngine(CharacterProfile profile, TriggerCatalog catalog)`, which builds detectors from the catalog entries. A missing or disabled entry gives a null definition, which disables that output.
    - `float Time`
    - `ComfortModel Model`, `ReactionSelector Reactions`, `SessionLog Log`
    - `ChildReaction Step(in SensorFrame frame, float dt)`: clamp `dt`, run the detectors, apply each hit, record each non-null change, `OnHit`, advance, update the reaction, and copy the outcome flags to the log.
    - `bool Report(string triggerId, Channel channel, float intensity = 1f, string excerpt = null)`: the F6 entry point for words triggers. True if applied.

- [ ] **Write the failing tests.** Build frames with a helper `Frame(head, forward, hands…)`; the child is at the origin facing +z.
  - `Engine_StaresAtChild_LowersComfortAndLooksAway`.
  - `Engine_DisabledOrExcludedTrigger_NoChangeNoEvent`. Use a catalog from `TryParse` with `staring` disabled, plus a profile excluding `shouting`. Assert that `Report("shouting", …)` returns false and no event is logged.
  - `Engine_LongFrameHitch_ClampsDt`: one `Step` with `dt` 5 s while staring gives at most 1 hit.
  - `Engine_Scenario_ShoutToMeltdownThenDeescalateThenGreet`, which pins §8b end to end:
    1. Start with the `aj`-like profile.
    2. Repeated −5 dBFS shouting at 0.5 m reaches Tantrum, and `MeltdownHappened` is true.
    3. Then quiet (−50 dBFS) at 2 m without looking reaches `Deescalated`.
    4. Keep going until Greeting, which is terminal: the log's `Greeted` is true and further shouting changes nothing.
  - `Engine_NormalConversation_NoHits`: 60 s at 1.2 m, −30 dBFS, glancing at the child 2 s in every 6, hands at the sides. Expect no trigger events.
- [ ] Run → fail. Implement. Run → PASS. Then the full EditMode suite: PASS.
- [ ] Commit `feat: Compose comfort engine from detectors`.

### Task 8: Drive the spawned child in Room

**Files:**
- Create: `Assets/_Project/Runtime/Comfort/ComfortDriver.cs`
- Modify: `Assets/_Project/Runtime/Characters/ChildSpawner.cs`: add `public CharacterProfile SpawnedProfile { get; private set; }`, set alongside `Spawned`.
- Modify: `Assets/_Project/Editor/RoomSceneCommands.cs`: `AddComfortDriver()` `[CliCommand("autismvr_add_comfort_driver", …)]`, idempotent.
- Test: `Assets/_Project/Tests/PlayMode/ComfortDriverTests.cs`
- Test: `Assets/_Project/Tests/EditMode/RoomSceneTests.cs` (one new test)

**Interfaces:**
- Consumes: `ComfortEngine`, `ChildSpawner.Spawned` / `SpawnedProfile`, `ChildAnimator.SetReaction`.
- Produces `ComfortDriver : MonoBehaviour`:
  - serialized `ChildSpawner m_Spawner`, `Transform m_Head` (the rig's Main Camera), `Transform[] m_Hands` (the rig's hand poke interactors; inactive ones are skipped);
  - `public float LoudnessDbfs { get; set; } = float.NegativeInfinity` (F6 sets this from the mic);
  - `public ComfortEngine Engine { get; private set; }`, created on the first `Update` after the spawner has a child;
  - `void Configure(ChildSpawner spawner, Transform head, Transform[] hands)`.

  The child's head and chest come from `Animator.GetBoneTransform(HumanBodyBones.Head / Chest)`. Each `Update` builds a `SensorFrame`, calls `Engine.Step`, and calls `SetReaction` only when the reaction changes.

- [ ] **Write the failing tests:**
  - PlayMode `ComfortDriver_HeadStaringAtChild_ChildLooksAway`: spawn `aj` through a spawner with an empty selection, use a fake head transform 1.2 m away facing the child, wait 5 s, then assert `ChildAnimator.Current == LooksAway`.
  - PlayMode `ComfortDriver_NoChild_DoesNothing`: a spawner with no profile gives a null `Engine`, no exception, and expects the spawner's error log via `LogAssert.Expect`.
  - EditMode `RoomHasComfortDriverWiredToRig`: there is exactly one `ComfortDriver`; its spawner is the Room's `ChildSpawner`; its head is the XR Origin camera; its hands are the two hand poke interactors.
- [ ] Run → fail. Implement. Run `autismvr_add_comfort_driver` headless. Both suites: PASS.
- [ ] Commit `feat: Drive the child's reactions in Room`.

### Task 9: Evidence, build, PR

- [ ] Add `[CliCommand("autismvr_comfort_scenario", …)]` in `Assets/_Project/Editor/ComfortScenarioCommands.cs`. It runs the Task 7 scenario for each of the three profiles and writes `docs/verification/f4-comfort-trigger-engine/scenario.md`: a per-second table of time, comfort, reaction and hits, plus each profile's outcome flags. Commit the output as evidence that the three children behave differently.
- [ ] Meta XR Simulator (Quest 3 profile, hands), Room in Play mode:
  - screenshot the child after the head is moved within 0.5 m (`room-child-too-close.png`);
  - screenshot the child after staring for over 4 s (`room-child-stared.png`).
  - If the simulator CLI cannot move the head, say so in the PR and rely on the scenario and the PlayMode tests.
- [ ] Android build from HEAD (`-o Builds/Quest/AutismVR-f4.apk`); the `aapt` flags are unchanged from F3.
- [ ] Commit `docs: Add F4 verification evidence`. Open the PR to `build` via `pr-author-context`.
