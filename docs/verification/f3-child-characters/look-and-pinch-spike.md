# Look-and-pinch spike (F3 Task 6)

Throwaway spike, 2026-10-22, to find which XRI interactor a Meta VR Glasses look-and-pinch
uses, so `PokeButton` can accept it.

**Setup:** Meta XR Simulator 207.1 as the OpenXR runtime (`metavr xrsim runtime activate`),
device **Meta VR Glasses** (90 Hz), both hands on input source **look-and-pinch**
(`metavr xrsim runtime status` → `L=look-and-pinch R=look-and-pinch`). `Chooser.unity` in
Editor Play mode through OpenXR (`XRSettings.isDeviceActive = true`).

**Interactors active in that mode** (from `XRBaseInteractor` enumeration in Play):

| Interactor | Path | Active |
|---|---|---|
| `NearFarInteractor` | `Camera Offset/Right Hand/Near-Far Interactor` | yes; aim almost parallel to head forward |
| `NearFarInteractor` | `Camera Offset/Left Hand/Near-Far Interactor` | yes; aim almost parallel to head forward |
| `XRPokeInteractor` | `Camera Offset/{Left,Right} Hand/Poke Interactor` | yes |
| `XRGazeInteractor` | `Camera Offset/Gaze Interactor` | **no** |
| controller interactors, teleport `XRRayInteractor` | `{Left,Right} Controller/...` | no |

**Input devices:** `OpenXRHmd`, `XRHandDevice` ×2, `HandInteraction` ×2. No eye-gaze device.
Runtime name `Meta XR Simulator`; HMD name `Head Tracking - OpenXR`.

**Findings**

- In the simulator, a look-and-pinch reaches the app as the hand's `NearFarInteractor`, the
  same interactor that carries the hand ray on Quest.
- Nothing seen at runtime distinguishes Meta VR Glasses from a Quest: no eye-gaze device, and
  generic runtime and HMD names.
- A pinch selecting a chooser button was **not** captured: the simulator CLI cannot drive a
  pinch. Real Meta VR Glasses were not available.

**Consequence for `PokeButton`:** accepting `NearFarInteractor` selects lets Glasses users press
buttons, but also lets Quest users press with a far hand-ray pinch. The owner decides which
behaviour to keep: the owner accepted far-pinch presses on Quest (F3).
