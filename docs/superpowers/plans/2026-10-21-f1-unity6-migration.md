# F1 — Unity 6 Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the Unity 2017.2 project into a clean Unity 6000.6.0f1 URP project with all legacy SDKs removed, text-serialized assets, sane Git/LFS rules, an EditMode test assembly, and Unity CLI commands that let every later feature be verified with screenshots.

**Architecture:** Delete legacy SDK folders *before* the first Unity 6 open so the upgrade compiles cleanly. Upgrade headless with the Unity CLI, then drive the Editor through the `com.unity.pipeline` package: project-owned `[CliCommand]` methods apply settings, open scenes and frame the Scene view, and the built-in `screenshot` command captures evidence. Project-owned code lives under `Assets/_Project/`.

**Tech Stack:** Unity 6000.6.0f1 (arm64, `/Volumes/DevDrive/Applications/Unity/Hub/Editor/6000.6.0f1`), Unity CLI 1.0.0-beta.8 (`unity`), `com.unity.pipeline`, Universal Render Pipeline, Unity Test Framework (NUnit), Git LFS 3.8.0.

**Spec:** `docs/superpowers/specs/2026-10-21-autism-vr-rebuild-design.md` (§7 migration scope; §8a; roadmap `docs/superpowers/plans/2026-10-21-autism-vr-roadmap.md`)

## Global Constraints

- Editor version: exactly `6000.6.0f1`.
- Remove: GoogleVR, legacy Oculus integration, VRTK, SteamVR, IBM Watson SDK, IBM SpeechSandbox, MORPH3D (spec §7, §4).
- Keep the three child models untouched in this feature: `Assets/Aj.fbx`, `Assets/Ch12_nonPBR.fbx`, `Assets/Ch46_nonPBR.fbx` (import configuration is F3).
- Keep `Assets/_Scenes/Autism1.unity` (reference scene for F2) and its environment assets (`HomeStuff/`, `intireor bed room/`, root-level room models, materials and textures, `Animation/`, `Raw Mocap Data/`, `Tunde Intro/`, `Images/`, `Videos/`).
- No OpenAI or Supabase secret anywhere in the repo.
- Project-owned code under `Assets/_Project/`; Editor code in `Assets/_Project/Editor/`; tests in `Assets/_Project/Tests/EditMode/`.
- Commits: Udacity style (`type: Subject`, ≤50 chars, imperative, no trailing period); no `Co-Authored-By` trailer (owner's global rule).
- Screenshots: `docs/verification/f1-unity6-migration/*.png`.
- Project path for every CLI call: `/Volumes/DevDrive/Dev/Autism-VR` (referred to as `$P`).

## Review Focus

1. **Leftover scripts referencing removed SDKs** → Unity enters Safe Mode and the Pipeline server never loads. Expected: zero `error CS` lines in the upgrade log. Pinned by Task 3 Step 4 and `NoScriptReferencesRemovedSdks` (Task 3).
2. **Materials still on Built-in or legacy shaders** render pink in URP. Expected: every material asset under `Assets/` uses a URP, Shader Graph or Hidden shader. Pinned by `NoProjectMaterialUsesBuiltInShaders` (Task 4).
3. **Text assets swallowed by LFS** (`.meta`, `.unity`, `.cs` stored as LFS pointers) break merges and diffs. Expected: only binary media types go to LFS. Pinned by the `git check-attr` checks (Task 1).
4. **Generated or junk files committed** (`Library/`, `Logs/`, `UserSettings/`, `.DS_Store`). Expected: `git status` never shows them. Pinned by Task 1 Step 3 and re-checked before every commit in Task 5.
5. **`Autism1` fails to open after its SDK components are removed** (missing-script components). Expected: the scene opens; missing scripts are reported, not fatal. Pinned by `OpenScene_Autism1_ReturnsSceneName` (Task 5).

---

### Task 1: Feature branch and repository hygiene

**Files:**
- Modify: `.gitignore`
- Modify: `.gitattributes`

**Interfaces:**
- Produces: LFS rules that later features rely on (binary media in LFS, text assets in Git).

- [ ] **Step 1: Cut the feature branch from `build`**

```bash
cd /Volumes/DevDrive/Dev/Autism-VR
git switch build && git pull --ff-only origin build 2>/dev/null || true
git switch -c feature/unity6-migration
```
Expected: `git branch --show-current` prints `feature/unity6-migration`.

- [ ] **Step 2: Replace `.gitignore` with a Unity 6 ignore set**

Must ignore: `/[Ll]ibrary/`, `/[Tt]emp/`, `/[Oo]bj/`, `/[Bb]uild/`, `/[Bb]uilds/`, `/[Ll]ogs/`, `/[Uu]ser[Ss]ettings/`, `/[Mm]emoryCaptures/`, `/[Rr]ecordings/`, `*.csproj`, `*.sln`, `*.slnx`, `*.user`, `*.userprefs`, `*.pidb`, `*.booproj`, `*.svd`, `*.pdb`, `*.mdb`, `*.opendb`, `*.VC.db`, `*.apk`, `*.aab`, `*.unitypackage`, `/.vs/`, `/.idea/`, `.DS_Store`, `sysinfo.txt`, `crashlytics-build.properties`, `/supabase/.temp/`, `test-results*.xml`.

- [ ] **Step 3: Verify junk is ignored**

Run: `git status --porcelain | grep -E "\.DS_Store|Library/|Logs/"`
Expected: no output.

- [ ] **Step 4: Replace `.gitattributes` so only binary media goes to LFS**

Remove the line `Assets/ filter=lfs diff=lfs merge=lfs -text`. Add `filter=lfs diff=lfs merge=lfs -text` rules for: `*.fbx *.FBX *.obj *.blend *.png *.PNG *.jpg *.jpeg *.tga *.psd *.exr *.hdr *.tif *.wav *.mp3 *.m4a *.ogg *.mp4 *.flv *.mov *.ttf *.otf *.a *.so *.aar *.bundle`. Add `*.cs text eol=lf`, `*.unity text merge=unityyamlmerge eol=lf`, `*.prefab text merge=unityyamlmerge eol=lf`, `*.asset text merge=unityyamlmerge eol=lf`, `*.meta text eol=lf`, `*.mat text merge=unityyamlmerge eol=lf`, `*.controller text eol=lf`, `*.anim text eol=lf`.

- [ ] **Step 5: Verify attributes**

Run: `git check-attr filter -- Assets/Aj.fbx Assets/floor.mat Assets/_Scenes/Autism1.unity Assets/CharacterInteractionScripts/LookAt.cs`
Expected: `Assets/Aj.fbx: filter: lfs`; the other three `filter: unspecified`.

- [ ] **Step 6: Commit**

```bash
git add .gitignore .gitattributes
git commit -m "chore: Update ignore and LFS rules for Unity 6"
```

---

### Task 2: Remove legacy SDKs and dead content

**Files (delete, each with its `.meta`):**
- `Assets/GoogleVR/`, `Assets/Oculus/`, `Assets/SpeechSandbox/`, `Assets/Watson/`, `Assets/MORPH3D/`, `Assets/StreamingAssets/`
- `Assets/Plugins/OVRGamepad.bundle`, `Assets/Plugins/Android/`, `Assets/Plugins/Pixelplacement/` (iTween; unused outside removed SDKs), then `Assets/Plugins/` if empty
- `Assets/TextMesh Pro/` (2017 copy; unused outside removed SDKs; Unity 6 ships TMP in uGUI)
- `Assets/Scripts/` (tutorial game template, not in the design; uses removed APIs)
- `Assets/InteractionScript.cs` (empty), `Assets/CharacterInteractionScripts/MicInput.cs` (uses the removed `new AudioClip()`; loudness is rebuilt in F4)
- Scenes not used by the design: `Assets/mosquito_vr(remake).unity`, `Assets/room.unity`, `Assets/HomeStuff/Demo.unity`, `Assets/_Scenes/Autism01.unity` (near-duplicate of Autism1), `Assets/_Scenes/Autism1/` (lighting data folder for the 2017 bake; regenerated later)
- `UnityPackageManager/` (2017 manifest location; Unity 6 creates `Packages/`)

**Interfaces:**
- Produces: a tree with no legacy SDK code, so the Task 3 upgrade compiles.

- [ ] **Step 1: Confirm nothing kept depends on the folders being removed**

Run: `bash -c 'grep -rlE "\b(Gvr[A-Z]\w*|GoogleVR|OVR[A-Z]\w*|VRTK|Valve\.VR|IBM\.Watson|MORPH3D|MCS\.|Pixelplacement|iTween|TMPro)\b" --include=*.cs Assets | grep -vE "^Assets/(GoogleVR|Oculus|SpeechSandbox|Watson|MORPH3D|Plugins|TextMesh Pro|Scripts)/"'`
Expected: no output.

- [ ] **Step 2: Delete the listed paths with `git rm -r`** (include each `.meta`).

- [ ] **Step 3: Verify the kept scripts are only the three interaction scripts**

Run: `find Assets -name "*.cs"`
Expected exactly: `Assets/CharacterInteractionScripts/LookAt.cs`, `Assets/CharacterInteractionScripts/NodDetector.cs`, `Assets/CharacterInteractionScripts/PlayAudio.cs`.

- [ ] **Step 4: Commit**

```bash
git commit -m "chore: Remove legacy VR SDKs and unused content" -m "Removes GoogleVR, Oculus, VRTK/SteamVR (SpeechSandbox), Watson,
MORPH3D, old TextMesh Pro, iTween and the tutorial scripts. Also
removes the hardcoded Watson credentials with SpeechSandbox."
```

---

### Task 3: Upgrade to Unity 6000.6.0f1 with a test baseline

**Files:**
- Create: `Assets/_Project/Editor/AutismVR.Editor.asmdef` (Editor-only; references `Unity.Pipeline`)
- Create: `Assets/_Project/Editor/ProjectSetupCommands.cs`
- Create: `Assets/_Project/Tests/EditMode/AutismVR.Tests.EditMode.asmdef` (Editor-only; `UNITY_INCLUDE_TESTS`; references `AutismVR.Editor`, `nunit.framework.dll`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`)
- Create: `Assets/_Project/Tests/EditMode/ProjectBaselineTests.cs`
- Generated: `Packages/manifest.json`, `Packages/packages-lock.json`, upgraded `ProjectSettings/*`, `.meta` files for the three child FBX files

**Interfaces:**
- Produces: `AutismVR.Editor.ProjectSetupCommands.ApplyProjectBaseline() : string`, registered as CLI command `autismvr_apply_project_baseline`.
- Produces: assemblies `AutismVR.Editor` and `AutismVR.Tests.EditMode` that later features add to.

- [ ] **Step 1: Confirm licence and editor**

Run: `unity license status --format json` and `unity editors --installed --format json`
Expected: an active licence; `6000.6.0f1` listed. If no licence, stop and ask the owner to run `unity license activate`.

- [ ] **Step 2: Upgrade-import headless**

```bash
mkdir -p Logs
unity run /Volumes/DevDrive/Dev/Autism-VR --editor-version 6000.6.0f1 --timeout 5400 -- -logFile Logs/upgrade-6000.6.log
```
Expected: exit 0; `Packages/manifest.json` exists; `ProjectSettings/ProjectVersion.txt` contains `m_EditorVersion: 6000.6.0f1`.

- [ ] **Step 3: Check for compile errors**

Run: `grep -c "error CS" Logs/upgrade-6000.6.log`
Expected: `0`. If not, fix or delete the offending kept script and repeat Step 2 (Review Focus 1).

- [ ] **Step 4: Install the Pipeline package**

Run: `unity pipeline install --project-path /Volumes/DevDrive/Dev/Autism-VR`
Expected: `com.unity.pipeline` appears in `Packages/manifest.json`.

- [ ] **Step 5: Write the failing baseline tests** in `ProjectBaselineTests.cs` (namespace `AutismVR.Tests.EditMode`)

```csharp
[Test] public void EditorIsUnity6000_6()
    => Assert.That(Application.unityVersion, Does.StartWith("6000.6."));

[Test] public void AssetsAreSerializedAsText()
    => Assert.That(EditorSettings.serializationMode, Is.EqualTo(SerializationMode.ForceText));

[Test] public void MetaFilesAreVisible()
    => Assert.That(VersionControlSettings.mode, Is.EqualTo("Visible Meta Files"));

[TestCase("Assets/GoogleVR")] [TestCase("Assets/Oculus")] [TestCase("Assets/SpeechSandbox")]
[TestCase("Assets/Watson")] [TestCase("Assets/MORPH3D")] [TestCase("Assets/TextMesh Pro")]
public void LegacySdkFolderIsGone(string folder)
    => Assert.That(AssetDatabase.IsValidFolder(folder), Is.False);

[Test] public void NoScriptReferencesRemovedSdks()
{
    // Regex over every .cs file under Assets/:
    // \b(Gvr[A-Z]\w*|GoogleVR|OVR[A-Z]\w*|VRTK|Valve\.VR|IBM\.Watson|MORPH3D)\b
    Assert.That(offendingPaths, Is.Empty, string.Join("\n", offendingPaths));
}
```

- [ ] **Step 6: Run tests to see the serialization test fail**

Run: `unity test /Volumes/DevDrive/Dev/Autism-VR --mode EditMode --filter "AutismVR.Tests.EditMode.ProjectBaselineTests" --output Logs/f1-task3.xml`
Expected: exit 8; `AssetsAreSerializedAsText` fails (the 2017 project used Force Binary); the others pass.

- [ ] **Step 7: Implement `ApplyProjectBaseline()`** in `ProjectSetupCommands.cs`

`[CliCommand("autismvr_apply_project_baseline", "Force text serialization and visible meta files, then reserialize all assets")]`. Sets `EditorSettings.serializationMode = SerializationMode.ForceText`, `VersionControlSettings.mode = "Visible Meta Files"`, calls `AssetDatabase.ForceReserializeAssets()`, then `AssetDatabase.SaveAssets()`. Returns `"ForceText"`.

- [ ] **Step 8: Apply the baseline headless**

Run: `unity run /Volumes/DevDrive/Dev/Autism-VR --command autismvr_apply_project_baseline --timeout 3600`
Expected: exit 0; `head -2 Assets/_Scenes/Autism1.unity` starts with `%YAML 1.1`.

- [ ] **Step 9: Run the tests to see them pass**

Run: the Step 6 command.
Expected: exit 0, all `ProjectBaselineTests` pass.

- [ ] **Step 10: Commit** (check `git status` shows no `Library/`, `Logs/`, `UserSettings/`)

```bash
git add Packages ProjectSettings Assets
git commit -m "chore: Upgrade project to Unity 6000.6.0f1" -m "Upgrades assets and settings, switches to text serialization, adds
the Pipeline package and an EditMode baseline test suite."
```

---

### Task 4: Convert to the Universal Render Pipeline

**Files:**
- Create: `Assets/_Project/Editor/RenderPipelineCommands.cs`
- Create: `Assets/_Project/Settings/URP-Quest.asset`, `Assets/_Project/Settings/URP-Quest_Renderer.asset` (generated by the command)
- Create: `Assets/_Project/Tests/EditMode/UrpBaselineTests.cs`
- Modify: `Packages/manifest.json` (adds `com.unity.render-pipelines.universal`), `ProjectSettings/GraphicsSettings.asset`, `ProjectSettings/QualitySettings.asset`, converted material assets
- Modify: `Assets/_Project/Editor/AutismVR.Editor.asmdef`, `Assets/_Project/Tests/EditMode/AutismVR.Tests.EditMode.asmdef` (reference `Unity.RenderPipelines.Universal.Runtime`, and `.Editor` for the Editor asmdef)

**Interfaces:**
- Consumes: `AutismVR.Editor` assembly from Task 3.
- Produces: `AutismVR.Editor.RenderPipelineCommands.ApplyUrp() : string` (CLI `autismvr_apply_urp`); the URP asset path `Assets/_Project/Settings/URP-Quest.asset` that F2 tunes for Quest.

- [ ] **Step 1: Add URP** with the editor-matched version (use the `unity:unity-package-management` skill; `Client.Add("com.unity.render-pipelines.universal")`).

- [ ] **Step 2: Write the failing tests** in `UrpBaselineTests.cs`

```csharp
[Test] public void DefaultRenderPipelineIsUrp()
    => Assert.That(GraphicsSettings.defaultRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());

[Test] public void EveryQualityLevelUsesUrpOrDefault()
{
    // for i in 0..QualitySettings.count-1: GetRenderPipelineAssetAt(i) is null or UniversalRenderPipelineAsset
}

[Test] public void NoProjectMaterialUsesBuiltInShaders()
{
    // AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
    // offender = shader name does not start with "Universal Render Pipeline/", "Shader Graphs/" or "Hidden/"
    Assert.That(offenders, Is.Empty, string.Join("\n", offenders)); // "path -> shader name"
}
```

- [ ] **Step 3: Run tests to see them fail**

Run: `unity test /Volumes/DevDrive/Dev/Autism-VR --mode EditMode --filter "AutismVR.Tests.EditMode.UrpBaselineTests" --output Logs/f1-task4.xml`
Expected: exit 8; all three fail.

- [ ] **Step 4: Implement `ApplyUrp()`** in `RenderPipelineCommands.cs`

`[CliCommand("autismvr_apply_urp", "Create the Quest URP asset, assign it everywhere and convert Built-in materials")]`. Creates the URP asset and renderer at the paths above if missing (`UniversalRenderPipelineAsset.Create` + `ScriptableRendererData`), with MSAA 4x, HDR off, and main light shadows on; assigns `GraphicsSettings.defaultRenderPipeline` and every quality level; runs the Built-in → URP converters in batch mode (Material Upgrade and Read-only Material converters; see the `unity:migrate-birp-to-urp` skill for the API). Returns the number of materials converted as a string.

- [ ] **Step 5: Apply headless**

Run: `unity run /Volumes/DevDrive/Dev/Autism-VR --command autismvr_apply_urp --timeout 3600`
Expected: exit 0.

- [ ] **Step 6: Run tests to see them pass**

Run: the Step 3 command. Expected: exit 0. If `NoProjectMaterialUsesBuiltInShaders` lists offenders the converter skipped (custom or legacy shaders), switch each to `Universal Render Pipeline/Lit` keeping its main texture and colour, then re-run.

- [ ] **Step 7: Commit**

```bash
git add Packages ProjectSettings Assets
git commit -m "feat: Convert project to Universal Render Pipeline"
```

---

### Task 5: Verification commands and screenshot evidence

**Files:**
- Create: `Assets/_Project/Editor/VerificationCommands.cs`
- Create: `Assets/_Project/Tests/EditMode/VerificationCommandsTests.cs`
- Create: `docs/verification/f1-unity6-migration/autism1-sceneview.png`
- Create: `docs/verification/README.md`

**Interfaces:**
- Produces (used by every later feature for screenshots):
  - `VerificationCommands.OpenScene(string path) : string` — CLI `autismvr_open_scene --path <asset path>`; opens single, returns `scene.name`; throws `ArgumentException` when no scene asset exists at `path`.
  - `VerificationCommands.FrameSceneView(string target) : string` — CLI `autismvr_frame --target <GameObject name>`; frames `SceneView.lastActiveSceneView` on the named root-or-child object's renderer bounds, returns the object name; throws `ArgumentException` when no object has that name in the open scenes.
  - `VerificationCommands.MissingScriptCount() : int` — CLI `autismvr_missing_scripts`; counts missing-script components in the open scenes.

- [ ] **Step 1: Write the failing tests** in `VerificationCommandsTests.cs`

```csharp
[Test] public void OpenScene_Autism1_ReturnsSceneName()
    => Assert.That(VerificationCommands.OpenScene("Assets/_Scenes/Autism1.unity"), Is.EqualTo("Autism1"));

[Test] public void OpenScene_MissingPath_Throws()
    => Assert.Throws<ArgumentException>(() => VerificationCommands.OpenScene("Assets/_Scenes/DoesNotExist.unity"));

[Test] public void FrameSceneView_KnownObject_ReturnsName()
{
    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    GameObject.CreatePrimitive(PrimitiveType.Cube).name = "FrameTarget";
    Assert.That(VerificationCommands.FrameSceneView("FrameTarget"), Is.EqualTo("FrameTarget"));
}

[Test] public void FrameSceneView_UnknownObject_Throws()
{
    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    Assert.Throws<ArgumentException>(() => VerificationCommands.FrameSceneView("Nope"));
}

[Test] public void MissingScriptCount_EmptyScene_IsZero()
{
    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    Assert.That(VerificationCommands.MissingScriptCount(), Is.EqualTo(0));
}
```
`FrameSceneView_KnownObject_ReturnsName` needs a Scene view: start it with `Assume.That(SceneView.lastActiveSceneView, Is.Not.Null)` so a batch run without one reports *inconclusive*, not failed.

- [ ] **Step 2: Run tests to see them fail**

Run: `unity test /Volumes/DevDrive/Dev/Autism-VR --mode EditMode --filter "AutismVR.Tests.EditMode.VerificationCommandsTests" --output Logs/f1-task5.xml`
Expected: compile failure (exit 6) — `VerificationCommands` does not exist.

- [ ] **Step 3: Implement the three methods** in `VerificationCommands.cs` with the `[CliCommand]` names above (`MainThreadRequired = true`). `MissingScriptCount` uses `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount` over all objects in loaded scenes.

- [ ] **Step 4: Run tests to see them pass**

Run: the Step 2 command. Expected: exit 0 (Scene-view tests may be inconclusive in batch).

- [ ] **Step 5: Capture the screenshot from a live Editor**

```bash
unity open /Volumes/DevDrive/Dev/Autism-VR
unity status --format json                      # wait for state "ready"
unity command recompile --project-path /Volumes/DevDrive/Dev/Autism-VR
unity list --project-path /Volumes/DevDrive/Dev/Autism-VR | grep autismvr_   # all four commands listed
unity command autismvr_open_scene --path "Assets/_Scenes/Autism1.unity" --project-path /Volumes/DevDrive/Dev/Autism-VR
unity command autismvr_missing_scripts --project-path /Volumes/DevDrive/Dev/Autism-VR   # record the number
unity command screenshot --help --project-path /Volumes/DevDrive/Dev/Autism-VR          # confirm the view option
mkdir -p docs/verification/f1-unity6-migration
unity command screenshot --output docs/verification/f1-unity6-migration/autism1-sceneview.png --width 1920 --height 1080 --project-path /Volumes/DevDrive/Dev/Autism-VR
```
Expected: the PNG exists; opening it shows the furnished room with textured (not pink) materials. Frame the room first with `autismvr_frame` on a room object named in the hierarchy if the default view is empty.

- [ ] **Step 6: Write `docs/verification/README.md`** — one paragraph: how to start the Editor, list the `autismvr_*` commands, and capture a screenshot with the Step 5 commands; screenshots live in `docs/verification/<feature>/`.

- [ ] **Step 7: Run the whole EditMode suite**

Run: `unity close /Volumes/DevDrive/Dev/Autism-VR && unity test /Volumes/DevDrive/Dev/Autism-VR --mode EditMode --output Logs/f1-all.xml`
Expected: exit 0.

- [ ] **Step 8: Commit**

```bash
git status --porcelain | grep -E "Library/|Logs/|UserSettings/|\.DS_Store"   # must print nothing
git add Assets/_Project docs/verification
git commit -m "feat: Add Unity CLI verification commands"
```

---

### Task 6: Open the PR to `build`

- [ ] **Step 1: Publish `build`** (first feature only): `git push -u origin build`
- [ ] **Step 2: Push the branch**: `git push -u origin feature/unity6-migration`
- [ ] **Step 3: Create the PR to `build` using the `pr-author-context` skill** (adversarial review first, then Why / What changed / Validation and proof). Validation lists: upgrade log has 0 `error CS`; EditMode suite exit 0 with test counts; missing-script count for Autism1; the screenshot embedded from `docs/verification/f1-unity6-migration/autism1-sceneview.png`. Base branch: `build`.
