# P4G Online Board Renderer

Date: 2026-06-27

`ChessOnlineApp` now includes a first visible online Chess3D board surface under **P4G Realtime Board Snapshot**.

## What It Shows

The renderer is built from the authoritative `OnlineSnapshot.SaveGameJson` through `OnlineChess3DBoardSnapshotParser`.

Visible UI:

- selected `Layer Z` selector for slices `0..7`;
- 8x8 grid for the selected layer;
- occupied cells rendered as compact piece labels such as `S1P`, `S1K`, `S2Q`;
- empty cells rendered as `.`;
- status line with ruleset, server sequence, occupied count, and state hash;
- selected cell line with coordinate, piece label, and cell index.

Piece labels use the native piece type convention:

- `P`: pawn;
- `N`: knight;
- `B`: bishop;
- `R`: rook;
- `Q`: queen;
- `K`: king.

## Refresh Flow

The board refreshes when `RememberP4FSnapshot(...)` receives an authoritative snapshot from:

- `Start Game`;
- `Request Snapshot`;
- the post-acceptance refresh after `Submit Safe Asgard Test Action`.

After a safe Asgard action is accepted, `ChessOnlineApp` immediately requests a fresh snapshot and rebuilds the board from server state. The client does not locally mutate the board as the source of truth.

## Selection Flow

Clicking a rendered board cell selects it and updates the selected-cell status. P4G Phase 03 does not submit arbitrary clicked moves yet. This is intentional: server-backed legal preview and exact action dispatch are separate follow-up steps.

## Safety Boundary

The renderer:

- does not change server deployment;
- does not change Chess3D rules;
- does not add a sixth profile;
- does not expose tokens or passwords;
- does not mutate local native engine state;
- does not claim legal target preview before the server exposes it.

## Known Limitations

- Stack/fusion/core overlays are not rendered yet.
- Hodge mirror arrows are not rendered yet.
- Rubik layer-turn controls remain outside the board grid.
- Arbitrary click-to-move is still future work and should be backed by authoritative legal preview.
- The public HTTP endpoint remains diagnostic/dev only.
