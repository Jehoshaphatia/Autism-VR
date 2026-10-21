# Verification screenshots

Each feature branch saves Unity CLI screenshots of its work in
`docs/verification/<feature>/` and links them from its PR. To capture one, open
the project with `unity open /Volumes/DevDrive/Dev/Autism-VR`, wait for
`unity status` to report `ready`, list the project's commands with
`unity list | grep autismvr_`, stage the view with
`unity command autismvr_open_scene --path <scene>` and
`unity command autismvr_frame --target <GameObject>`, then run
`unity command screenshot --output docs/verification/<feature>/<name>.png --width 1920 --height 1080`.
`unity command autismvr_missing_scripts` reports how many components in the open
scenes point at deleted scripts.
