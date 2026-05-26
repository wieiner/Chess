# Chess3D P2N Save / Replay Audit

P2N starts from the P2M runtime on commit `11252e2`: five Chess3D RuleProfiles, legal-action preview, action history, reserve restore, Rubik layer turns, Hodge projected moves, and visual asset packaging are already present.

## State Storage

- Projected board state is still `Position::board`, a 512-int array.
- `sideToMove` lives in `Position`.
- CoreCell stacks live in `Game::coreStacks` as vectors of side/type/piece/flags entries.
- Fusion states live in `Game::fusionStates` and are recomputed from stacks.
- Reserve counts live in `reserveCounts[side][pieceType]`.
- Anchors and victory are derived from board/stacks/profile and are recomputed after structural load/replay.
- Hodge macro-player state is derived from the active profile and `sideToMove`.

## Action History

Successful runtime actions already append `ActionRecord` entries:

- normal/core moves through `Chess3D_TryMakeMove`;
- Rubik layer turns through `Chess3D_RotateLayer`;
- reserve restore through `Chess3D_RestoreReservePiece` / auto restore;
- Hodge projected composite moves through `Chess3D_TryMakeProjectedMove`.

Debug/setup operations are intentionally not turn actions:

- `LoadRuleProfileJson`;
- `Reset`;
- `Clear`;
- `SetPiece`;
- explicit stack push/remove/clear helpers.

## UI Boundary

Before P2N the UI could save a plain `.ch3dlog` notation file from action history. It could not reload that log, restore a full game snapshot, step replay actions, or show a deterministic state hash.

## Safe P2N Scope

Safe changes are append-only:

- JSON savegame export/import;
- JSON replay export/import;
- replay cursor and error ABI;
- deterministic state hash;
- minimal Save/Replay UI panel;
- runnable scenario playthrough tests.

Deferred:

- stable online serialization protocol;
- undo/branching timeline;
- binary save format;
- replay UI timeline/animation;
- import of old free-form `.ch3dlog` beyond legacy export display.
