# Chess3D Turn Controller

P2L adds a lightweight runtime turn/capability contract without replacing older move APIs.

## ABI

- `Chess3D_GetCurrentTurnKind`
- `Chess3D_GetCurrentSide`
- `Chess3D_GetCurrentMacroPlayer`
- `Chess3D_GetAllowedActionMask`
- `Chess3D_GetTurnSummary`

## Turn Kinds

- Classic: side `1..6`, normal moves/captures only.
- Single-Side: side `1`, training/sandbox movement.
- Asgard: side `1..6`, normal moves plus core/fusion/reserve capabilities.
- Rubik: side `1..6`, Asgard-style capabilities plus legal layer turns.
- Hodge: macro-player `1..2`, projected composite moves through profile-defined side groups.

The action mask is capability based; it does not by itself prove every concrete action is legal. Concrete legality still comes from preview, `TryMakeMove`, `CanRotateLayer`, reserve restore checks, and Hodge projected-move validation.
