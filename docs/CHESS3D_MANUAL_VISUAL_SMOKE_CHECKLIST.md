# Chess3D Manual Visual Smoke Checklist

Run this checklist after a Release build.

## Classic

- Select Classic profile.
- Confirm pieces are readable and black pieces are not lost against the background.
- Select a current-side piece and verify legal targets are visible.
- Create or load a check fixture and confirm the checked king is highlighted.

## Single-Side

- Select Single-Side Training.
- Confirm the central setup is readable and normal legal targets appear.

## Asgard

- Select Asgard Convergence.
- Confirm CoreCube cells show core overlay.
- Create a multi-entry stack and confirm stack bars appear.
- Confirm friendly/royal/contested fusion overlays are distinguishable.

## Rubik

- Select Rubik Convergence.
- Use the layer-turn panel.
- Confirm selected layer highlight appears before the turn.
- Four identical turns should visually and state-wise return to the original position.

## Hodge

- Select Hodge Projection Duel.
- Preview mirrors and confirm primary plus two mirror dotted arrows.
- Block a mirror and confirm blocked arrows/error text.

## Replay

- Import or create a replay.
- Use Replay Step and confirm a short source-to-target flash where the action has move coordinates.

## P3C RC Addendum

- Try camera presets: isometric, top, side, reset.
- Toggle neutral/light/dark backgrounds and confirm black pieces remain visible.
- Toggle high-contrast pieces and confirm the board is still readable.
- Toggle CoreCube, Hodge arrows, and Rubik layer overlays and confirm only UI decoration changes.
- Copy visual diagnostics and confirm the text includes visual mode, selection state, options, model diagnostics, overlay count, animation state, and last invalid reason.
