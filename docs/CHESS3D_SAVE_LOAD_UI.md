# Chess3D Save / Load UI

P2N adds a `Save / Replay` panel to the Chess3D control center.

Controls:

- `Save Game`: writes `.ch3dsave`.
- `Load Game`: reads `.ch3dsave` transactionally.
- `Export Replay`: writes `.ch3dreplay`.
- `Import Replay`: loads replay queue.
- `Replay Step`: applies the next queued action.
- `Replay All`: applies all queued actions.
- `Reset Cursor`: resets replay playback to the replay start.
- `State Hash`: shows the current diagnostic hash.

After load or replay, the UI refreshes board, selected cell, legal preview, status text, and action log. Errors are shown through message boxes and `lastReplayError`; they should not crash the app.
