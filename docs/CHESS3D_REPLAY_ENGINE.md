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
