# Chess3D Action Flashes

Action flashes are short UI-only hints after successful actions or failed Hodge projection attempts.

- Normal move: source-to-target dotted path.
- Capture: same path plus capture target color from legal preview.
- Core/fusion action: core overlays and stack/fusion markers refresh after the action.
- Rubik: selected layer highlight before commit.
- Hodge: primary and mirror paths.
- Replay step: path flash when the replayed action exposes from/to coordinates.

The flash never mutates engine state and is cleared after a short delay.
