# Chess3D Visual State Machine

P3C adds a UI-only visual state layer. It is rebuilt from native engine ABI after profile load, reset, click selection, successful or failed action, layer turn, projected move, reserve restore, save/load, replay, and visual option changes.

## Selection State

- `None`: no selected cell.
- `CellSelected`: a cell is selected but has no active preview.
- `PieceSelected`: a piece is selected.
- `ActionPreview`: legal preview entries exist for the selected piece.
- `InvalidTarget`: the last click/action produced a readable rejection reason.
- `AnimationLocked`: a short UI animation is running and input is temporarily blocked.
- `ReplayStepping`: the UI is applying a replay step.

## Mode State

- `Classic`
- `SingleSide`
- `Asgard`
- `Rubik`
- `Hodge`

The state is inferred from the active ruleset and profile-gated capabilities. Scenario and regression JSON are not modes.

## Turn State

The visual layer records current side, current macro-player, turn summary, allowed action mask, game phase, game outcome, and check summary. These values are read from engine ABI.

## Action State

The visual layer records last notation, invalid reason, replay error, layer-turn info, projection error, capture/knockback info, and reserve-restore info. It does not write history.

## Cell State

Cells are rendered from projected board and profile features:

- selected;
- legal target;
- capture target;
- checked king;
- core cell;
- anchor;
- stack count;
- fusion kind;
- contested;
- Rubik layer membership;
- Hodge/action flash paths.

## Authority Boundary

The UI can call official native ABI actions, then rebuild visuals from engine state. It must not mutate board cells, CoreCell stacks, fusion, reserve, anchors, action history, or replay state by itself.
