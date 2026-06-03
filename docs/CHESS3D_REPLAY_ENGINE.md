# Chess3D Replay Engine

P2N adds an engine-level replay queue. The UI calls ABI functions; replay does not depend on WPF.

ABI:

- `Chess3D_ExportReplayJson`
- `Chess3D_LoadReplayJson`
- `Chess3D_ReplayAction`
- `Chess3D_ReplayAll`
- `Chess3D_ResetReplayCursor`
- `Chess3D_GetReplayActionCount`
- `Chess3D_GetReplayCursor`
- `Chess3D_GetLastReplayError`

Replay loading resets to embedded `initialRulesJson` or to optional `initialSaveJson`. The cursor starts at zero. `ReplayAction(0)` or `ReplayAction(cursor + 1)` applies the next action. Out-of-order replay is rejected in v0.1.

Supported runtime actions:

- normal/core move;
- Hodge projected move;
- Rubik layer turn;
- reserve restore.

Errors are reported as readable `lastReplayError` strings. The engine restores pre-action state if a replay action fails.

## P3A King Safety

Replay uses the same `TryMakeMove` path as live play. Classic/Single-Side replay actions are rejected if they leave the own king in check, move the king into attack, or try to continue after a game-over outcome. Checkmate/stalemate positions replay to the same state hash as the original game when the action sequence is valid.

## P3E Online Replay

The online authority exposes accepted `OnlineActionEvent` records in server-sequence order. P3E tests replay those events by applying the same command fields to a clean authoritative engine session and comparing the final state hash with the snapshot hash.

This is not a new replay file format. It is a multiplayer wrapper around the existing Chess3D replay/save/hash contract.
