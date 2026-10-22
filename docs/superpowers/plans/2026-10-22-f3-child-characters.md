# F3 — Child Characters and Chooser Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The trainee starts in a chooser room, picks one of three child characters with a fingertip (or look-and-pinch on Meta VR Glasses), and that child appears at `ChildSpawn` in `Room`, idling in a reaction state driven through one shared Animator controller, with a provisional personality profile attached.

**Architecture:** Data first: a `CharacterProfile` ScriptableObject per child (spec §4.1) and a `ChildReaction` enum that names the spec §4 reaction states. A `ChildAnimator` component maps a reaction to a state in one generated `ChildReactions.controller` (placeholder clips already in the project). Selection flows through a tiny static `SelectedCharacter` holder: the `Chooser` scene sets it, `Room`'s `ChildSpawner` reads it. Scenes and import settings are produced by idempotent `[CliCommand]`s and pinned by EditMode tests; runtime behaviour has PlayMode tests. Meta XR Simulator (installed) provides headset evidence and the look-and-pinch spike.

**Tech Stack:** Unity 6000.6.0f1, URP, XRI 3.6.1, XR Hands 1.9.0, OpenXR 1.18.0, Unity CLI + `com.unity.pipeline`, Meta XR Simulator 207.1 (`metavr xrsim`).

**Spec:** `docs/superpowers/specs/2026-10-21-autism-vr-rebuild-design.md` (§4 the child, §4.1 chooser and profiles, §3 interaction); roadmap F3 row.

## Global Constraints

- Children: `Assets/Aj.fbx`, `Assets/Ch12_nonPBR.fbx`, `Assets/Ch46_nonPBR.fbx`; import as **Humanoid** so they share **one Animator controller**; no facial blendshapes (body, head and gaze only) (spec §4).
- Reaction states (spec §4, initial list): calm, uneasy, looks away, fidgets, covers ears / withdraws, tantrum, ready to greet → enum `ChildReaction { Calm, Uneasy, LooksAway, Fidgets, Withdrawn, Tantrum, Greeting }`.
- Placeholder clips (spec §4): tantrum `Assets/Animation/stefani@Yelling.fbx`, withdrawn `Assets/Animation/stefani@Sad Idle.fbx`, looks away `Assets/Animation/jasper@Right Turn.fbx`, greeting `Assets/Animation/Standing Greeting.fbx`, calm `Assets/Raw Mocap Data/Animations/Idle/Idle_Neutral_1.fbx`; missing (uneasy, fidgets) use `Idle_IdleToIdlesR.fbx` and `IdleR_To_IdleL_1.fbx` as stand-ins until proper clips arrive.
- Profile fields (spec §4.1): display name, model, greeting voice and line, starting comfort, greeting threshold, per-trigger sensitivity multipliers and which triggers apply, recovery speed, personality description. Starting values are **provisional**.
- Greeting voice placeholder: `Assets/Tunde Intro/Hello_Uncle.wav`.
- Interaction must work with hands only on Quest and with look-and-pinch on Meta VR Glasses (spec §3).
- Project code under `Assets/_Project/`; commits Udacity style, **no Co-Authored-By**; PR body **no "Generated with Claude Code" line**.
- Screenshots: `docs/verification/f3-child-characters/*.png`. `$P` = repo root.
- Green = EditMode + PlayMode exit 0; Android build exit 0; screenshots committed.

## Review Focus

1. **A child stays Generic or its avatar is invalid** → shared Humanoid clips play as a T-pose. Expected: all three models `Human` with a valid humanoid avatar. Pinned by `ChildModelIsHumanoidWithValidAvatar` (Task 1).
2. **A reaction with no animator state, or a state with no clip** → the child freezes when that reaction fires in F4. Expected: one state per `ChildReaction`, each with a motion. Pinned by `ControllerHasStateWithClipForEveryReaction` (Task 2).
3. **Room entered without going through the chooser** (Editor play of Room, or a build that boots into Room) → no child at all. Expected: the default profile spawns. Pinned by `ChildSpawner_NoSelection_SpawnsDefaultProfile` (Task 4).
4. **Look-and-pinch cannot press buttons on Meta VR Glasses** → Glasses users are stuck in the chooser. Expected: the interactor the Glasses look-and-pinch actually uses can press `PokeButton`. Pinned by the Task 6 spike result and `PokeButton_LookAndPinchSelect_RaisesPressed` (Task 6).
5. **The chooser boots into the wrong scene** → trainees skip choosing. Expected: build scenes are `[Chooser, Room]` in that order. Pinned by `BuildScenesAreChooserThenRoom` (Task 5).

---

### Task 1: Import the three children as Humanoid

**Files:** Create `Assets/_Project/Editor/CharacterImportCommands.cs`, `Assets/_Project/Tests/EditMode/CharacterImportTests.cs`; modify the three `.fbx.meta`.

**Interfaces:** Produces `CharacterImportCommands.ChildModelPaths : string[]` and CLI `autismvr_configure_characters`.

- [x] Write failing tests: `[TestCase]` per model path — `ModelImporter.animationType == ModelImporterAnimationType.Human`; the imported `Avatar` sub-asset `isValid && isHuman`; materials import as URP-compatible (`RoomSceneCommands.IsUrpCompatible` on every embedded material).
- [x] Run → fail (Generic).
- [x] Implement `ConfigureCharacters()`: `animationType = Human`, `avatarSetup = CreateFromThisModel`, `materialImportMode = ImportViaMaterialDescription`, `SaveAndReimport()`; returns count.
- [x] Apply headless; tests pass; full EditMode green. Commit `feat: Import child characters as Humanoid`.

### Task 2: Reaction states and shared Animator controller

**Files:** Create `Assets/_Project/Runtime/Characters/ChildReaction.cs`, `ChildAnimator.cs`; `Assets/_Project/Editor/ChildAnimatorCommands.cs`; generated `Assets/_Project/Characters/ChildReactions.controller`; tests `Assets/_Project/Tests/EditMode/ChildAnimatorControllerTests.cs`, `Assets/_Project/Tests/PlayMode/ChildAnimatorTests.cs`.

**Interfaces:**
- `enum ChildReaction { Calm, Uneasy, LooksAway, Fidgets, Withdrawn, Tantrum, Greeting }` (namespace `AutismVR.Characters`).
- `ChildAnimator : MonoBehaviour` — `[RequireComponent(typeof(Animator))]`; `public ChildReaction Current { get; }`; `public void SetReaction(ChildReaction reaction)` sets int parameter `Reaction` = (int)reaction; F4 calls `SetReaction`.
- Constant `ChildAnimatorCommands.ControllerPath = "Assets/_Project/Characters/ChildReactions.controller"`; CLI `autismvr_build_child_controller`.

- [x] Failing EditMode tests: `ControllerHasStateWithClipForEveryReaction` (a state named after every enum value with non-null motion); `ControllerHasReactionIntParameter`; `EveryStateIsReachableFromAnyState` (an AnyState transition per state on `Reaction == value`, no exit time).
- [x] Failing PlayMode test `ChildAnimator_SetReaction_EntersMatchingState`: instantiate `Aj` with the controller, `SetReaction(Tantrum)`, wait ≤ 0.5 s, `animator.GetCurrentAnimatorStateInfo(0).IsName("Tantrum")`.
- [x] Implement the enum, `ChildAnimator`, and `BuildChildController()` using the placeholder clip map in Global Constraints (loop on Calm/Uneasy/Fidgets/Withdrawn/Tantrum; transitions 0.2 s).
- [x] Apply; suites green. Commit `feat: Add child reaction animator`.

### Task 3: Character profiles

**Files:** Create `Assets/_Project/Runtime/Characters/CharacterProfile.cs`, `TriggerIds.cs`; `Assets/_Project/Editor/CharacterProfileCommands.cs`; generated `Assets/_Project/Characters/Profiles/{Aj,Ch12,Ch46}.asset`; tests `Assets/_Project/Tests/EditMode/CharacterProfileTests.cs`.

**Interfaces:**
- `static class TriggerIds` — string constants for the spec §5.1 triggers: `Shouting`, `DemandingEyeContact`, `Staring`, `RapidQuestions`, `MultiStepInstructions`, `Sarcasm`, `Anger`, `PressuringSpeech`, `ApproachTooClose`, `ReachingOrTouch`, `DemandsDuringMeltdown` (values snake_case, e.g. `"shouting"`). F4's catalog uses these ids.
- `CharacterProfile : ScriptableObject` — `Id`, `DisplayName`, `ModelPrefab` (GameObject), `GreetingClip` (AudioClip), `GreetingLine`, `Personality` (multi-line), `StartingComfort` [0,1], `GreetingThreshold` [0,1] (> StartingComfort), `RecoveryPerSecond` ≥ 0, `float GetSensitivity(string triggerId)` (default 1, 0 if excluded), `bool Applies(string triggerId)`. Serialized `List<TriggerSensitivity { string TriggerId; float Multiplier; }>` and `List<string> ExcludedTriggers`.
- Constant folder `CharacterProfileCommands.ProfilesFolder`; `CharacterProfileCommands.DefaultProfileId = "aj"`; CLI `autismvr_create_profiles`.

- [x] Failing tests: three profiles exist with distinct ids `aj`, `ch12`, `ch46`; each has a model, greeting clip and non-empty line/personality; `StartingComfort < GreetingThreshold`; `GetSensitivity` returns 1 for an unlisted trigger, the multiplier for a listed one, 0 for an excluded one; every id used in a profile is a `TriggerIds` constant.
- [x] Implement with **provisional** personalities *(assistant's call; owner/admins tune in F8)*: `aj` — very sensitive to loud voices (`shouting` 1.5, `anger` 1.3), start 0.35, threshold 0.75, recovery 0.02/s; `ch12` — sensitive to space (`approach_too_close` 1.5, `reaching_or_touch` 1.6), start 0.4, threshold 0.75, recovery 0.03/s; `ch46` — sensitive to demands (`rapid_questions` 1.5, `pressuring_speech` 1.4, `demanding_eye_contact` 1.3), start 0.3, threshold 0.8, recovery 0.015/s. Display names = model names until the owner names them; greeting line "Hello!"; greeting clip `Hello_Uncle.wav`. Each model prefab = the model asset with `Animator` (shared controller) + `ChildAnimator`.
- [x] Apply; tests green. Commit `feat: Add provisional character profiles`.

### Task 4: Spawn the chosen child in Room

**Files:** Create `Assets/_Project/Runtime/Characters/SelectedCharacter.cs`, `ChildSpawner.cs`; modify Room via `RoomSceneCommands.AddChildSpawner()` (CLI `autismvr_add_child_spawner`); tests `Assets/_Project/Tests/PlayMode/ChildSpawnerTests.cs`, additions to `RoomSceneTests`.

**Interfaces:** `static class SelectedCharacter { CharacterProfile Current; void Choose(CharacterProfile); void Clear(); }`; `ChildSpawner : MonoBehaviour` — serialized `CharacterProfile m_DefaultProfile`, `Transform m_SpawnPoint`, `Transform m_FaceTowards`; `public ChildAnimator Spawned { get; }`; spawns in `Start`.

- [x] Failing PlayMode tests: `ChildSpawner_WithSelection_SpawnsSelectedProfileAtSpawnPoint` (position within 1 cm, facing the target ±5°); `ChildSpawner_NoSelection_SpawnsDefaultProfile`. EditMode `RoomHasChildSpawnerWiredToSpawns` (spawn = ChildSpawn, face = TrainerSpawn, default profile = `aj`).
- [x] Implement; apply to Room; suites green. Commit `feat: Spawn the chosen child in Room`.

### Task 5: Chooser scene

**Files:** Create `Assets/_Project/Runtime/Characters/CharacterChoice.cs`; `Assets/_Project/Editor/ChooserSceneCommands.cs`; generated `Assets/_Project/Scenes/Chooser.unity`; tests `Assets/_Project/Tests/EditMode/ChooserSceneTests.cs`, `Assets/_Project/Tests/PlayMode/CharacterChoiceTests.cs`.

**Interfaces:** `CharacterChoice : MonoBehaviour` — serialized `CharacterProfile m_Profile`, `PokeButton m_Button`, `string m_NextScene = "Room"`; on `Pressed` → `SelectedCharacter.Choose(m_Profile)` then `SceneManager.LoadScene(m_NextScene)`. CLI `autismvr_build_chooser`; `ChooserSceneCommands.ChooserScenePath = "Assets/_Project/Scenes/Chooser.unity"`.

- [x] Failing tests: Chooser has exactly one XR Origin (hands rig, Floor tracking) and one neutral light; three `CharacterChoice` objects, one per profile, each with a preview of that profile's model and a `PokeButton` on a pedestal in front of it within reach (0.5–0.7 m ahead of the trainer, 1.0–1.2 m high, within 35° of eye line so it is in view); `BuildScenesAreChooserThenRoom`. PlayMode `CharacterChoice_Pressed_ChoosesProfile` (assert `SelectedCharacter.Current` and that the scene load was requested — inject the loader as a `System.Action<string>` for the test).
- [x] Implement `BuildChooserScene()`: floor plane + neutral light (same as Room), hands rig, three previews 1.6 m ahead in an arc, each with its own button; build scenes `[Chooser, Room]`.
- [x] Also move Room's `StartButton` into view with the same height/angle rule and seat it on its pedestal (fixes the deferred 3 cm gap); update `RoomHasStartButtonInFrontOfSpawn`.
- [x] Apply; suites green. Commit `feat: Add character chooser scene`.

### Task 6: Look-and-pinch on Meta VR Glasses (spike, then test)

- [x] **Spike (throwaway, findings to ledger):** Editor Play of `Chooser` through Meta XR Simulator as **Meta VR Glasses** with `look-and-pinch` input; log every `selectEntered` on a button (interactor type) while issuing a pinch (simulator pinch control). Record which interactor type selects (expected: `NearFarInteractor` with gaze-driven aim, or `XRGazeInteractor`).
- [x] Write the failing PlayMode test `PokeButton_LookAndPinchSelect_RaisesPressed` using that interactor type via `SelectEnterUnconditionally`, keeping `PokeButton_RaySelect_DoesNotRaisePressed` (a hand ray on Quest must still not press).
- [x] Implement: `PokeButton` accepts poke interactors, plus the look-and-pinch interactor only when the session is gaze-driven (the interactor type and how to tell it apart from a Quest hand ray come from the spike).
- [x] Suites green. Commit `feat: Let look-and-pinch press buttons`.

> **Deviation (recorded after the spike):** the spike
> ([look-and-pinch-spike.md](../../verification/f3-child-characters/look-and-pinch-spike.md))
> found that look-and-pinch arrives as the hand's `NearFarInteractor`, the same interactor as the
> Quest hand ray, and that nothing at runtime tells Meta VR Glasses apart from a Quest. "Only when
> the session is gaze-driven" could not be implemented. `PokeButton` instead accepts every
> `NearFarInteractor` select, so on Quest a far hand-ray pinch also presses buttons. The owner
> accepted this; `PokeButton_FarHandRayPinch_RaisesPressed` pins it. `PokeButton_RaySelect_DoesNotRaisePressed` pins that the teleport
> `XRRayInteractor` does not press. Hands only (owner ruling): the starter rig's controllers carry their
> own `NearFarInteractor` and poke, so `PokeButton` ignores interactors under the
> `XRInputModalityManager`'s controller objects (`PokeButton_Controller*` tests,
> `HandsOnlyRigTests` for the scene wiring).

### Task 7: Build, simulator evidence, PR

- [x] Android build from HEAD (`-o Builds/Quest/AutismVR-f3.apk`), `aapt` check unchanged flags.
- [x] Meta XR Simulator (Quest 3, hands): screenshots of the chooser (`chooser-quest3.png`) and of Room with the chosen child at `ChildSpawn` (`room-child-quest3.png`); one of each child previewed in the chooser (`chooser-children.png`, Scene view).
- [ ] Commit `docs: Add F3 verification screenshots`; PR to `build` via `pr-author-context`.
