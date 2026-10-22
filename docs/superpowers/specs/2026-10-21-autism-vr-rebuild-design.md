# AutismVR rebuild — design (DRAFT, in progress)

Status: **approved by the owner on 2026-10-21.** Items marked *(assistant's call)* were proposed by the
assistant and have not been explicitly approved.

Branch: `build` (from `master` @ d86c390).

---

## 1. Intent

AutismVR is a **training simulator** that teaches people how to interact with an
autistic child. The trainee meets a virtual autistic child in VR and talks to them
naturally. The child reacts the way a real autistic child would: things a real child
finds distressing make the virtual child distressed (up to a tantrum/meltdown);
calm, patient, respectful behaviour lets the child become comfortable enough to
greet the trainee. Greeting is the goal of a session.

The learning comes from consequences: the trainee sees the effect of what they say
and do, and gets a summary afterwards of which triggers they hit.

## 2. Goals, non-goals, anti-goals

**Goals**
- Natural spoken interaction with the child (no menus to "choose what to say").
- Child reactions grounded in real clinical guidance about autistic children.
- Triggers are data, not code: add, remove, enable, disable, and tune without a
  code change.
- Hands-only interaction (wave, press buttons) on Meta headsets.
- A feedback summary per session plus structured session data stored in a backend
  and viewable on dashboards (in VR, mobile app, or web app).

**Non-goals (for now)**
- Supporting non-Meta headsets.
- Keeping any of the legacy SDKs (see §7).

**Anti-goals** (would be a failure even if it technically works)
- Teaching the wrong lesson — e.g. penalising a trainee for ordinary, natural
  glances at the child, or rewarding pushing the child to make eye contact.
- A child that chatters. The child is mostly non-verbal (see §4).
- Shipping an OpenAI or Supabase secret key inside the headset app.

## 3. Platform and interaction

- **Devices (owner):** Meta Quest 2, Meta Quest 3, Meta VR Glasses.
- **Input (owner):** hands — wave at the child, press buttons with fingers, other
  hand gestures. No controller dependency.
- **Engine:** Unity **6000.6.0f1** (already installed on this machine, arm64, with
  Android build support). Meta VR Glasses require Unity 6000.0.66f2 or later.
- **Interaction toolkit (owner):** Unity **XR Interaction Toolkit (XRI)** with
  OpenXR and XR Hands.
- **XRI everywhere, including Meta VR Glasses (owner; confirmed).** Unity 6.6
  supports Meta VR Glasses through OpenXR with XRI and **XR Hands 1.9.0 or newer**
  for hand and eye interactions (Unity, Meta Connect 2026). Glasses have no
  controllers and use look-and-pinch (eye tracking + pinch), so every
  interaction must work with hands and gaze alone. XR Interaction Simulator can
  preview Glasses interactions in the Editor.
- Build: IL2CPP, ARM64 (required by Meta VR Glasses; Mono is 32-bit ARM only).

## 4. The child

- **Mostly non-verbal (owner).** The child reacts without words: looks away,
  fidgets, withdraws, throws a tantrum.
- **Speaks only to greet the trainee (owner)**, when it is the right time.
- **While the child greets, the mic is paused (owner)**; it resumes afterwards.
  GPT-Live supports this directly (`session.input_audio.mute` / `unmute`).
- **Comfort level** *(assistant's call)*: a hidden value that triggers lower and
  good behaviour slowly raises. Reaction state follows comfort; the greeting
  happens when comfort crosses a threshold.
- **Reaction states** *(assistant's call, initial list)*: calm, uneasy, looks away,
  fidgets, covers ears / withdraws, tantrum, ready to greet. Each maps to an
  Animator state, so animations can be swapped without code changes.
- **Placeholder animations already in the project:**
  - tantrum: `Assets/Animation/stefani@Yelling.fbx`
  - withdrawn: `Assets/Animation/stefani@Sad Idle.fbx`
  - turn away: `Assets/Animation/stefani@Walking Turn 180.fbx`
  - greeting: `Assets/Animation/Standing Greeting.fbx`
  - calm: idles in `Assets/Raw Mocap Data/Animations/Idle/`
  - missing (fidget, rocking, covering ears): placeholder clips such as free
    Mixamo clips until proper animations exist.
- Greeting voice audio already exists: `Assets/Tunde Intro/Hello_Uncle.*`.

## 5. Triggers

### 5.1 Sources
- **Clinician's list (owner, from a clinician the owner worked with):** shouting,
  yelling, demanding eye contact ("look at me"), staring intently.
- **Research additions** (National Autistic Society, NHS, peer-reviewed sources;
  see §10): rapid-fire questions without processing time, long multi-step
  instructions, sarcasm/idioms/vague language, anger or impatience, pressuring
  speech ("say hello!"), moving too close or too fast, reaching toward or touching
  the child, questions or instructions during a meltdown.

### 5.2 Eye contact rule (owner confirmed)
Penalise **demanding** eye contact and **sustained staring** — not ordinary eye
contact.

| Trainee behaviour | Effect on comfort | Detection |
|---|---|---|
| Demands eye contact ("look at me") | drops | words (Decisions) |
| Stares at the child's face continuously beyond a threshold | drops gradually while staring | head direction; eye direction on Meta VR Glasses |
| Natural glances, looking around | no change | — |
| Lets the child look away without remarking on it | rises slightly | words + child state |

Staring threshold: configurable, starting at 3–5 s *(assistant's call)*; to be
tuned with a clinician.

### 5.3 Detection channels
A trigger is detected through one or more channels:
- **Words / meaning** — OpenAI Decisions API over the live transcript.
- **Loudness** — mic level (shouting is volume, not words).
- **Gaze** — head direction (Quest) or eye direction (Meta VR Glasses) at the
  child's face, plus duration.
- **Proximity / speed** — headset position and approach speed.
- **Hands** — hand tracking: reaching toward the child, sudden movements, touch.

Calming behaviours that raise comfort come from the same sources: calm quiet
voice, short simple sentences, waiting, letting the child look away, keeping
distance, giving space during a meltdown.

### 5.4 Malleable trigger catalog (owner requirement)
Triggers live in one editable catalog. Each entry has:
- id / name
- plain-language description (fed to the Decisions question instructions)
- detection channel(s) and channel parameters (e.g. loudness threshold, staring
  seconds)
- effect on comfort (strength)
- resulting reaction (optional)
- enabled flag

Adding a row adds a trigger; disabling a row removes it. GPT-Live accepts updated
Decisions questions mid-session via `session.update`.
*(assistant's call)* Store the catalog in Supabase so it can be edited without a
new build; the app caches the last known catalog for offline use.

**Who edits it (owner):** the project owner and admins. *(assistant's call)* One
global catalog shared by all organisations, editable only by platform admins (the
owner plus anyone they make an admin); trainees, trainers and organisation admins
cannot change it. The editing tool is an admin screen in the web dashboard.

## 6. Conversation AI (OpenAI)

Researched 2026-10-21 against OpenAI's docs:

- **GPT-Live** (`gpt-live-1`, GA 2026-09-10): full-duplex voice model; listens and
  speaks simultaneously; $0.05/min billed per second. Connect over WebRTC
  (recommended) or WebSocket (`wss://api.openai.com/v1/live/sessions`). Audio is
  PCM16 mono at 24 kHz (or 16 kHz). Transcripts arrive as
  `session.input_transcript.delta` / `session.output_transcript.delta`.
- **Decisions API** (`POST /v1/decisions`, model `gpt-6-luna`, public beta): picks
  from developer-defined answers (`predicate`, `choice`, `score`) with
  probabilities and confidence; ~150 ms; $0.10 per 1M input tokens, no output
  charge.
- **Native integration:** GPT-Live can call Decisions directly via
  `session.triggers` on transcript deltas, so the child can react *while the
  trainee is still speaking*. Results arrive as `decision.event` with
  `decision.completed` answers. Questions can be changed mid-session with
  `session.update`.

Planned use *(assistant's call)*:
- Decisions answers drive the child's reaction (which triggers fired, which
  reaction, intensity). The app combines these with sensor channels (§5.3) to
  update comfort.
- GPT-Live provides the transcript and the child's greeting voice.
- Risk: GPT-Live is built to talk. Fallback: keep GPT-Live listening only and play
  the recorded `Hello_Uncle` greeting.

**Microphone:** no Meta voice package needed. Unity's microphone input with the
`android.permission.RECORD_AUDIO` permission feeds GPT-Live, which does its own
speech recognition. Meta XR Audio SDK is optional, for spatialising the child's
voice.

## 7. Migration scope

- Upgrade from Unity 2017.2.2f1 to 6000.6.0f1.
- **Remove (owner):** GoogleVR, legacy Oculus integration, VRTK, SteamVR, IBM
  Watson SDK, IBM SpeechSandbox.
- Remove the hardcoded Watson credentials in
  `Assets/SpeechSandbox/Scripts/SpeechSandboxStreaming.cs` (removed with the
  SpeechSandbox folder; they stay in git history).
- Rebuild `Assets/CharacterInteractionScripts/` behaviour (gaze IK `LookAt`, mic
  loudness `MicInput`, `NodDetector`, `PlayAudio`) on the new stack where still
  useful.
- **Main scene (assistant's call; owner delegated):** `Assets/_Scenes/Autism1.unity`.
  It is first in the build settings and contains the child character
  (`M3DMale`), the room, a score canvas and an intro canvas. `_Scenes/Autism01`
  is a near-duplicate; `Autism_Level_1` is a bare room with a camera.
  Approach: open the project in Unity 6.6 so the Editor upgrades the assets, then
  build one new scene from Autism1's room, with a fresh XRI rig instead of the old
  Vive/Oculus/Google rigs. The other legacy scenes are removed once the new scene
  works.
- **Done in F2:** `Assets/_Project/Scenes/Room.unity` is now the main scene and the
  source of truth; Autism1 and the other legacy scenes are deleted. Room keeps
  Autism1's room, a `TrainerSpawn` at the old player start and a `ChildSpawn` at
  the old child position. The old child's animation and setup did not carry over
  (it was the removed MORPH3D character); F3 places the new children at
  `ChildSpawn`.
- **Three child characters (owner):** `Assets/Aj.fbx`, `Assets/Ch12_nonPBR.fbx`,
  `Assets/Ch46_nonPBR.fbx` (added 2026-10-21), replacing the MORPH3D `M3DMale`
  child; MORPH3D is removed with the other legacy packages. All three are Mixamo
  characters (`mixamorig:` / `mixamorig4:` skeletons with finger bones, embedded
  textures). Import each as a Unity Humanoid so they share **one Animator
  controller** and Mixamo clips retarget directly; free Mixamo clips are the
  placeholder source for missing reactions. None has **facial blendshapes**, so
  reactions are body, head and gaze only; facial expression is out of scope
  unless a face rig is added later.

### 4.1 Character chooser and profiles (owner)

- At the start the trainee enters a **character stage / chooser room** and picks
  one of the three children. The chosen child then appears in the main room.
- **Each character has its own personality and trigger baseline (owner).**
  *(assistant's call)* A **character profile** holds:
  - display name, model, greeting voice and greeting line
  - starting comfort and greeting threshold
  - a per-trigger sensitivity multiplier (e.g. one child is very sensitive to
    loud voices, another to being approached), and which triggers apply at all
  - recovery speed after a meltdown
  - a short personality description, used in the GPT-Live instructions for the
    greeting and in the Decisions question text
- Profiles are data alongside the trigger catalog (§5.4): stored in Supabase,
  edited by platform admins on the web dashboard, cached in the app.
- Each session records which character was chosen, so dashboards can compare
  results across characters.
- Starting profiles are *provisional* (§8a) and reviewed like the trigger values.

## 8. Feedback and data (owner)

- At session end the trainee gets a **feedback summary showing which triggers they
  hit**.
- Session data is sent to a backend and shown on dashboards in VR, a mobile app,
  or a web app.
- **Backend: Supabase for everything backend-related (owner).**
- *(assistant's call)* Supabase roles:
  - **Auth** — trainee (and trainer) accounts.
  - **Postgres** — sessions, trigger events (timestamp, trigger id, channel,
    transcript excerpt, comfort before/after), outcome (greeted / meltdown /
    ended), trigger catalog. RLS on every table.
  - **Edge Functions** — the trusted server that holds the OpenAI key and starts
    GPT-Live sessions (WebRTC offer/answer exchange via `POST /v1/live/sessions`),
    so no secret ships in the APK.
- **Who sees the data (owner):**
  - **Trainees** see their own sessions.
  - **Trainers** and **organisations** review several trainees.
- *(assistant's call)* Access model: organisations have members with a role
  (trainee, trainer, org admin); Postgres RLS enforces it — a trainee reads only
  their own rows, a trainer reads the trainees they are allowed to review, an org
  admin reads the whole organisation. Roles are stored in data the user cannot
  edit (never `user_metadata`).
- **Trainers see only trainees assigned to them (owner).** Assignments are rows
  that an organisation admin manages.
- **Local development (owner):** Supabase runs locally via the Supabase CLI
  (v2.121.0) on Docker through OrbStack.

## 8b. Scope, accounts, sessions, privacy (owner approved the assistant's proposals)

- **Milestone 1:** the headset experience (chooser room, new scene, XRI hands rig, children,
  GPT-Live + Decisions loop, triggers, in-headset feedback summary) **plus** the
  Supabase backend **plus** the web dashboard (trainee / trainer / org admin
  views and the trigger catalog admin screen). The VR dashboard and the mobile app
  are later milestones that read the same tables. No fixed date.
- **Accounts:** invite-only. Platform admins create organisations; organisation
  admins invite trainees and trainers on the web dashboard. No self-signup and no
  anonymous sessions.
- **Headset sign-in:** device-code pairing. The headset shows a short code; the
  trainee confirms it on the web dashboard (phone or laptop) while signed in. The
  Edge Function that starts GPT-Live sessions requires a signed-in user, so
  strangers can't spend the OpenAI budget.
- **Session end and meltdowns:** a meltdown is **recoverable** — the child can
  calm down when the trainee de-escalates (gives space, lowers demands, stays
  quiet), and practising this is a core skill. A session ends when the child
  greets the trainee, when the trainee quits from inside VR, or at a time limit
  (default 10 minutes, configurable; also caps OpenAI cost). Stored outcomes:
  `greeted`, `quit`, `timed_out`, with whether a meltdown happened and whether it
  was de-escalated recorded separately.
- **Fallback relay:** if Unity WebRTC fails on Quest, one small service outside
  Supabase may run a WebSocket audio relay. Emergency fallback only.
- **Privacy** *(assistant's call — the owner approved "suggestions for all five",
  but this one had no explicit proposal; owner to confirm)*:
  - Trainees are adults (18+) for now.
  - A consent screen before the first session explains that voice goes to OpenAI
    and transcript excerpts are stored.
  - Transcript excerpts are deleted after 12 months; session summaries and
    trigger counts are kept. Users can request deletion of their data.
  - Design to UK GDPR / Nigeria NDPA-level rules: data minimisation, RLS, no
    secrets on device.
  - The hosted Supabase region is chosen at first deployment (local development
    needs none); it should match where most trainees are.

## 8a. Builder calls *(assistant's calls, from the builder check)*

- **Device order:** Quest 3 first (main development device); Quest 2 sets the
  performance budget; Meta VR Glasses (retail spring 2027) supported through the
  same XRI/OpenXR path but treated as unproven until tested on hardware —
  eye-direction staring falls back to head direction where eye tracking is absent.
- **Distribution:** testers via sideloading / Meta release channels first; a
  Horizon Store listing (entitlement check, privacy policy, permission
  justifications) is a later phase.
- **Language:** English first, with Nigerian English accents a primary test case
  (the existing `Hello_Uncle` greeting suggests this audience). Trigger question
  text and test phrases are written in English; other languages later.
- **Clinical sign-off:** starting trigger values are marked *provisional* in the
  catalog. The owner reviews them against the cited sources before rollout, and a
  clinician tunes them when available.
- **GPT-Live connection:** WebRTC. The Supabase Edge Function only does the
  offer/answer exchange (`POST /v1/live/sessions`) with the OpenAI key; audio
  then flows directly between the headset and OpenAI, so no long-lived relay is
  needed. The Unity WebRTC package on Quest is validated in an early spike.

## 9. Risks

| Risk | Notes / mitigation |
|---|---|
| Echo | GPT-Live listens continuously; the headset mic may hear the child's voice from the headset speakers. Validate early with a throwaway spike; mic is muted during the greeting anyway. |
| Child too chatty | GPT-Live is a conversational model; fallback in §6. |
| Unity WebRTC on Quest | Needs a spike to confirm the Unity WebRTC package works on Quest / Unity 6.6; WebSocket via a relay is the fallback (adds latency, and a long-lived relay does not suit Edge Functions). |
| Decisions API is beta | Interface may change before GA. |
| 2017 → 6.6 jump | Binary-serialised 2017 scenes and assets; legacy scripts use removed APIs. |

## 10. Sources

- OpenAI DevDay 2026 recap — https://openai.com/index/devday-2026-recap/
- Decisions guide — https://developers.openai.com/api/docs/guides/decisions
- Connect voice to Decisions — https://developers.openai.com/api/docs/guides/decisions-voice
- GPT-Live — https://developers.openai.com/api/docs/guides/live
- GPT-Live WebSockets — https://developers.openai.com/api/docs/guides/voice-websockets?api=live
- Meta: Support Meta VR Glasses — https://developers.meta.com/horizon/documentation/unity/unity-support-meta-vr-glasses
- Unity: Build for Meta VR Glasses — https://unity.com/blog/build-for-meta-vr-glasses-with-unity
- Meta: Horizon OS audio — https://developers.meta.com/horizon/essentials/horizon-os-audio
- NAS: Autism and communication — https://www.autism.org.uk/advice-and-guidance/about-autism/autism-and-communication
- NAS: Sensory processing — https://www.autism.org.uk/advice-and-guidance/about-autism/sensory-processing
- NHS Leicestershire: Meltdowns and shutdowns — https://www.leicspart.nhs.uk/autism-space/health-and-lifestyle/meltdowns-and-shutdowns/
- Reacting to autistic children: the danger of trying too hard (PubMed 1201443)
- Eye contact experiences of autistic adults and teens (PMC5705114)
- Communication during distress, scoping review (PMC13005890)

## 11. Decision log

| # | Decision | By |
|---|---|---|
| 1 | Own repo is the source of truth; Imisi3D upstream remote removed | owner |
| 2 | Work happens on branch `build` | owner |
| 3 | Target Quest 2, Quest 3, Meta VR Glasses; hands-only input | owner |
| 4 | Unity XR Interaction Toolkit | owner |
| 5 | Drop GoogleVR, legacy Oculus, VRTK, SteamVR, Watson | owner |
| 6 | OpenAI GPT-Live for voice; OpenAI Decisions for child reactions | owner |
| 7 | Skip Meta voice packages if GPT-Live + Unity mic suffice | owner |
| 8 | Child mostly non-verbal; speaks only to greet; mic paused while greeting | owner |
| 9 | Triggers from clinician's list plus research; editable catalog | owner |
| 10 | Penalise demanding/staring, not ordinary eye contact | owner |
| 11 | Feedback summary of triggers hit; data to backend; dashboards | owner |
| 12 | Supabase for all backend | owner |
| 13 | Unity 6000.6.0f1 | assistant's call (only installed editor) |
| 14 | Comfort-level model and initial reaction states | assistant's call |
| 15 | Catalog in Supabase; Edge Functions hold the OpenAI key | assistant's call |
| 16 | Data viewers: trainees (own), trainers and organisations (several trainees) | owner |
| 17 | Org / member-role access model enforced by RLS | assistant's call |
| 18 | Supabase developed locally with the CLI on OrbStack | owner |
| 19 | Trainers see only trainees assigned to them | owner |
| 20 | XRI on all three devices, including Meta VR Glasses (Unity 6.6 + XR Hands 1.9+) | owner (confirmed) |
| 21 | Main scene: `_Scenes/Autism1`'s room rebuilt as `_Project/Scenes/Room.unity` (F2); Autism1 deleted | assistant's call (delegated) |
| 22 | Trigger catalog edited by the owner and platform admins; one global catalog | owner / assistant's call (global) |
| 23 | Owner provides a new child character; MORPH3D removed | owner |
| 24 | Device order, distribution, language, provisional triggers, WebRTC via Edge Function (§8a) | assistant's calls |
| 25 | Milestone 1 = headset + Supabase + web dashboard; VR/mobile dashboards later | owner (approved proposal) |
| 26 | Invite-only accounts; device-code headset sign-in | owner (approved proposal) |
| 27 | Meltdown recoverable; session ends on greet / quit / 10-min limit | owner (approved proposal) |
| 28 | Fallback relay outside Supabase allowed if WebRTC fails | owner (approved proposal) |
| 29 | Privacy defaults: adults only, consent screen, 12-month transcript retention | assistant's call — confirm |
| 30 | Three children: `Aj`, `Ch12_nonPBR`, `Ch46_nonPBR` (Mixamo, no face blendshapes) | owner |
| 31 | Character chooser room at start; per-character personality and trigger baseline | owner |
| 32 | Character profile fields; profiles stored with the catalog in Supabase | assistant's call |
| 33 | Design approved | owner, 2026-10-21 |
| 34 | Workflow: feature branch from `build` → green + Unity CLI screenshot verification → PR to `build`; repeat; finally `build` → `master` | owner |
