# Chess3D P2I Action System Audit

P2I starts from the P2H runtime where normal moves, knockback captures, reserve counts, CoreCell stacks, fusion descriptors, anchors, victory checks, and Rubik layer turns already exist as separate operations.

## Existing Action Boundaries

- Ordinary moves enter through `Chess3D_TryMakeMove` and the internal `applyMove`.
- AI moves enter through `Chess3D_MakeBestMove` and also use `applyMove`.
- Capture routing happens inside `applyMove` through classic removal or `knockbackCapture` home/reserve routing.
- Forbidden Core stack transitions are handled by the same move path for outside-to-core, core-to-core, and core-to-outside moves.
- Rubik layer turns enter through `Chess3D_RotateLayer`.
- Stack, fusion, anchor, implosion, reserve, and victory recalculation happens after successful structural changes.

## Existing Telemetry

- P2G already exposed last knockback/captured-piece information.
- P2H already exposed last layer-turn axis/layer/quarter/result information.
- There was no unified action list, stable notation, replay foundation, or reserve restore action.

## Safe P2I Additions

- Add an internal append-only `ActionRecord` vector inside the Chess3D game/session.
- Record only successful game actions: moves, layer turns, and reserve restores.
- Do not record `Reset`, `LoadRuleProfileJson`, `SetPiece`, direct stack debug helpers, or manual setup edits.
- Keep all previous ABI functions unchanged and add only new exports.

## Deferred

- Undo/replay/import/export.
- Online serialization.
- Full UI inventory and drag/drop restore.
- Notation as final PGN equivalent.
