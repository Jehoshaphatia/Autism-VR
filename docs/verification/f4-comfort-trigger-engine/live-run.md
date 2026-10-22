# F4 live run in Meta XR Simulator

`Room.unity` in Editor Play mode through Meta XR Simulator 207.1 (Meta Quest 3 profile, hand input).
Child: Aj, the default profile. The simulated headset and hands stay still unless the rig is moved
with `XROrigin.MoveCameraToWorldLocation`. There is no mic yet, so the loudness channel is idle.

What happened:

1. **0–15 s: standing at the trainer spawn, facing the child, not moving.** The head points at the
   child, so `staring` hits once a second from 5 s. Comfort falls from 0.35 to 0 and the child has
   a meltdown at about 14 s. Giving space (≥ 1.5 m) adds a little every 5 s, but not enough to offset.
2. **~16 s: rig moved to 0.5 m.** That fired `approach_too_close`. The simulator holds the hands out
   in front, which put them within 0.35 m of the child's head, so `reaching_or_touch` fired too.
3. **16–86 s: rig at 1.2 m, facing slightly down.** No hits, so the meltdown recovered at Aj's
   0.02/s and was de-escalated. `room-child-recovering-1m2.png` was taken in this phase.
4. **86 s: rig moved to 0.5 m again.** The too-close detector had re-armed beyond 1.0 m, so both
   triggers fired again and comfort fell to 0.03. The game view at 0.5 m points over the child's head (the simulated head is level), so no screenshot is kept for this step.

| t (s) | trigger | channel | comfort before | comfort after |
|---|---|---|---|---|
| 5.0 | staring | Gaze | 0.35 | 0.31 |
| 5.0 | gives_space | Proximity | 0.31 | 0.35 |
| 6.0 | staring | Gaze | 0.35 | 0.31 |
| 7.0 | staring | Gaze | 0.31 | 0.27 |
| 8.0 | staring | Gaze | 0.27 | 0.23 |
| 9.0 | staring | Gaze | 0.23 | 0.19 |
| 10.0 | staring | Gaze | 0.19 | 0.15 |
| 10.0 | gives_space | Proximity | 0.15 | 0.19 |
| 11.0 | staring | Gaze | 0.19 | 0.15 |
| 12.0 | staring | Gaze | 0.15 | 0.11 |
| 13.0 | staring | Gaze | 0.11 | 0.07 |
| 14.0 | staring | Gaze | 0.07 | 0.03 |
| 15.0 | staring | Gaze | 0.03 | 0.00 |
| 15.0 | gives_space | Proximity | 0.00 | 0.04 |
| 15.9 | approach_too_close | Proximity | 0.04 | 0.00 |
| 15.9 | reaching_or_touch | Hands | 0.00 | 0.00 |
| 86.1 | approach_too_close | Proximity | 0.30 | 0.18 |
| 86.1 | reaching_or_touch | Hands | 0.18 | 0.03 |

Outcome: meltdown yes, de-escalated yes, greeted no.

**Tuning risk this run shows:** on Quest, staring is judged from head direction. A trainee who simply
faces the child, as people do when talking to someone, counts as staring after 4 s, and the child
melts down in about 14 s from that alone. The simulated head is perfectly still, which exaggerates
this, but the problem is real. The staring values are provisional catalog rows (`hold_seconds` 4,
`cone_degrees` 15) and need tuning on a headset before a clinician review.
