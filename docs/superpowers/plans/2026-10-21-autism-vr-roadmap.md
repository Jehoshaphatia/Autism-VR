# AutismVR Rebuild — Roadmap

**Spec:** `docs/superpowers/specs/2026-10-21-autism-vr-rebuild-design.md`

This roadmap splits milestone 1 (spec §8b) into feature branches. Each feature
gets its own detailed plan, `docs/superpowers/plans/YYYY-MM-DD-fN-<name>.md`,
written when its branch is cut, against the code as it exists on `build` then.
Only F1's detailed plan exists today.

## Workflow (owner, spec §11 #34)

1. Cut `feature/<name>` from the latest `build`.
2. Implement the feature's plan task by task.
3. **Green** means all of:
   - the Unity project compiles with zero errors (no Safe Mode);
   - `unity test <project> --mode EditMode` exits 0 (and `--mode PlayMode` where
     the feature has PlayMode tests);
   - for backend/web features, their own test suites pass (`supabase test db`,
     web unit tests);
   - Unity CLI screenshots of the work are saved under
     `docs/verification/<feature>/` and committed.
4. Open a PR to `build` (via the `pr-author-context` skill), screenshots
   included as validation evidence.
5. After merge, cut the next feature branch from `build`.
6. When milestone 1 is complete and tested on a Quest 3, PR `build` → `master`.

## Feature branches

| # | Branch | Delivers | Depends on | Can run alongside |
|---|---|---|---|---|
| F1 | `feature/unity6-migration` | Legacy SDKs removed; project on Unity 6000.6.0f1; text serialization; URP; LFS/gitignore fixed; Unity CLI verification commands; EditMode test assembly | — | F5 |
| F2 | `feature/xri-hands-rig` | OpenXR + Unity OpenXR: Meta + XRI + XR Hands; Meta Quest build profile (IL2CPP, ARM64); new `Room` scene built from `_Scenes/Autism1` with a hands-only XR Origin; poke button; Android build succeeds | F1 | F5 |
| F3 | `feature/child-characters` | `Aj`, `Ch12_nonPBR`, `Ch46_nonPBR` imported as Humanoid; one shared Animator controller with the §4 reaction states and placeholder clips; `CharacterProfile` data with three provisional profiles; character chooser room → `Room` | F2 | F4, F5 |
| F4 | `feature/comfort-trigger-engine` | Pure C# comfort model, trigger catalog model, detection channels (loudness, stare, proximity, hand reach), reaction selection; heavy EditMode tests; no network | F1 | F2, F3, F5 |
| F5 | `feature/supabase-backend` | Local Supabase (CLI on OrbStack): schema for orgs, members/roles, trainer assignments, trigger catalog, character profiles, sessions, trigger events, device codes; RLS; seed data; pgTAP tests; Edge Functions `live-session` (GPT-Live SDP exchange, signed-in users only) and `device-code` pairing | — | F1–F4 |
| F6 | `feature/gpt-live-voice` | Unity WebRTC spike on Quest (go/no-go; fallback relay allowed, spec §8b); GPT-Live client; Decisions transcript triggers wired into F4; mic mute during greeting; echo check | F3, F4, F5 | F8 |
| F7 | `feature/headset-auth-session` | Device-code sign-in in VR; consent screen; catalog/profile fetch + cache; session + trigger-event upload; 10-minute limit; quit from VR; in-headset feedback summary | F5, F6 | F8 |
| F8 | `feature/web-dashboard` | Web app: device-code confirm page; trainee / trainer (assigned trainees only) / org-admin views; platform-admin editors for trigger catalog and character profiles; invites | F5 | F6, F7 |
| R | `build` → `master` | Milestone 1 release PR after an on-device pass on Quest 3 | F1–F8 | — |

Web stack for F8 *(assistant's call, revisit in F8's plan)*: Next.js App Router
with `@supabase/ssr`.

## Later milestones (out of milestone 1)

VR dashboard, mobile app, Horizon Store listing, Meta VR Glasses hardware pass,
facial expressions (needs a face rig).
