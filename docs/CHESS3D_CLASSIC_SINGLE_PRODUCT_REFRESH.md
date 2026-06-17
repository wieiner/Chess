# Chess3D Classic / Single-Side Product Refresh

Phase: P4C phase 09.

## Classic Six-Side

Classic Six-Side is the baseline normal Chess3D profile:

- file: `assets/rules/profiles/classic_six_side_3d_v0_1.json`
- rulesetId: `classic-six-side-3d-8x8x8-v0.1`
- product role: king-safe 3D chess profile without Asgard, Rubik, Hodge, reserve, fusion, or projection

Runtime status:

- classic 3D movement and capture;
- king safety, check, checkmate, and stalemate are runtime-backed for this profile;
- legal preview and TryMakeMove use the same legality layer;
- online authority accepts normal moves and rejects profile-only actions;
- exact-profile matchmaking smoke exists.

## Single-Side Training

Single-Side is a training/debug profile:

- file: `assets/rules/profiles/single_side_3d_v0_1.json`
- rulesetId: `single-side-3d-8x8x8-v0.1`
- product role: one-side movement, UI, and rule-debug training surface

Runtime status:

- one active training side;
- no core stacks, fusion, reserve, layer turns, or Hodge projection;
- profile loads under online authority and snapshot/action-log infrastructure;
- matchmaking policy treats it as one-player/training, not as a public competitive queue.

## Shared Product Rules

- Classic and Single-Side must remain first-class products, not footnotes under Asgard.
- Classic must not expose Asgard/Rubik/Hodge action panels except as disabled capabilities.
- Single-Side must not be marketed as full multiplayer competition.
- Scenario/playthrough/regression JSON files are not additional modes.

## Known Gaps

- Classic AI/search is smoke-level/profile-aware rather than a strong engine.
- Full public multiplayer UX, ratings, spectators, and anti-cheat are future work.
- Single-Side is intentionally training/sandbox.
