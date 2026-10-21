# Verification screenshots

Each feature branch saves Unity CLI screenshots of its work in
`docs/verification/<feature>/` and links them from its PR.

## One-time setup

- Unity 6000.6.0f1 with Android Build Support, and the Unity CLI (`unity`).
- `git lfs install` before cloning or pulling: models, textures, audio and these
  screenshots are stored in Git LFS.
- Optional, for merging Unity YAML assets: point git at Unity's merge tool, for example
  `git config merge.unityyamlmerge.driver '"<Unity.app>/Contents/Tools/UnityYAMLMerge" merge -p %O %B %A %A'`.
  Without it git falls back to a plain text merge for `.unity`, `.prefab`, `.asset` and `.mat` files.

## Capturing a screenshot

From the project root (`P=$PWD`):

1. `unity open "$P"`, then wait for `unity status` to report `ready`.
2. `unity list --project-path "$P" | grep autismvr_` lists the project's commands.
3. Stage the view: `unity command autismvr_open_scene --path Assets/_Project/Scenes/Room.unity --project-path "$P"`
   (it refuses to discard unsaved edits unless `--discard true` is passed) and
   `unity command autismvr_frame --target "<GameObject>" --project-path "$P"`.
4. `unity command screenshot --view scene --output "$P/docs/verification/<feature>/<name>.png" --width 1920 --height 1080 --project-path "$P"`.
   Use `--view game` in Play mode (`unity command editor_play`) to capture the headset camera.

`unity command autismvr_missing_scripts` reports how many components in the open scenes
point at deleted scripts.
