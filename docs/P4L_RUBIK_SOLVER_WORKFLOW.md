# P4L Rubik Solver Workflow

## Product boundary

The Rubik Studio `Solver` tab is a capability-aware front end over the managed solver contracts. It does not rename reverse-history inversion as arbitrary solving and does not claim that arbitrary 3x3 or 11x11 solving is available.

| Size | Implementation | Result |
| --- | --- | --- |
| 2x2 | `owned-bounded-2x2-iddfs-v1` | Bounded arbitrary-state search followed by independent replay on a fresh native engine. |
| 3x3 | No approved search backend | Physical solvability validation is available; arbitrary search is deferred. |
| 4x4..32x32 | `owned-nxn-reduction-framework-v1` | Level A validation/decomposition guidance and a resumable checkpoint. Center/wing move generation is not implemented. |

The existing Scramble tab retains the trusted reverse-history workflow for an in-session move history.

## Controls

- `Validate` runs structured physical state and solvability validation.
- `Solve` selects the implementation by cube size and runs long work away from the WPF UI thread.
- `Cancel` requests cooperative cancellation.
- `Pause` and `Resume` are visible but disabled because the current backends advertise `SupportsPauseResume=false`.
- `Save Solution` writes an atomic `.rubikmoves` document only after independent replay verification.
- `Load Solution` accepts the versioned move document only when its size and input hash match the current cube.
- `Play Solution`, `Step`, and `Previous Step` use the existing animated native layer-turn path.

## Safety and verification

The bounded 2x2 solver returns candidate moves. `RubikSolutionVerifier` then creates a fresh `NativeRubikEngine`, imports the exact input facelets, replays every structured move, validates intermediate states, and requires the canonical solved state and final hash. Only this verified result enables save/playback.

Playback tracks a cursor and expected state hash. A manual change that diverges from the cursor blocks the next step rather than applying a move to the wrong position. `Previous Step` applies the inverse of the prior structured move and does not depend on native trusted history.

Closing the window requests cancellation before disposing the live native engine. Progress updates are throttled to short UI assignments; solver search and independent verification are not performed on the UI thread.

## Known limitations

- Bounded 2x2 search has explicit 30-second, depth-14, and memory-derived node limits in the UI workflow.
- Arbitrary 3x3 solving remains deferred pending an approved backend.
- NxN reduction currently emits no solving moves. Its checkpoint is useful for honest workflow continuity, not proof of a complete solve.
- A checkpoint can be saved/loaded through the RubikState API; dedicated checkpoint file buttons are not part of this compact UI iteration.
