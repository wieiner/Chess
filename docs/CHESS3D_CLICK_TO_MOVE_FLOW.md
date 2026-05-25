# Chess3D Click-To-Move Flow

## Player Flow

1. Select a current-side piece.
2. Inspect highlighted targets and the legal action list.
3. Click a highlighted target for normal move/capture/projection entries.
4. Use the Rubik panel for layer turns.
5. Use the Asgard panel for reserve restore.
6. Use the Hodge panel when entering explicit projected coordinates.

## Runtime Flow

- Selection builds a non-mutating legal preview.
- Target click must match a preview entry exactly.
- Failed target clicks do not mutate board, stacks, reserve, fusion, anchors, victory, or action history.
- Invalid reasons are shown in the common panel and visual diagnostics.

## Important Mode Rules

- Classic and Single-Side use ordinary move/capture actions.
- Asgard uses ordinary moves plus core stack/fusion/reserve actions where the profile allows them.
- Rubik layer turns are profile-gated and panel-driven.
- Hodge projected moves are composite all-or-nothing actions, not ordinary single moves.
