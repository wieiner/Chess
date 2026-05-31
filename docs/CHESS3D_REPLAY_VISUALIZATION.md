# Chess3D Replay Visualization

Replay remains engine-owned. The UI calls replay ABI and then rebuilds visuals from the resulting state.

P3C behavior:

- `Replay Step` shows a short flash for move-like replay actions.
- `Replay All` stays fast and rebuilds the final state.
- Replay errors are shown in the save/replay panel and visual diagnostics.
- Replay import/reset clears stale selections, arrows, and flashes.

Future work may add a timeline and slower animated replay, but that is not required for the P3C release-candidate polish.
