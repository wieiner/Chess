# Rubik State File Guide

## State files

Rubik Studio lets the user choose the save location. `Save State` reuses the current path and `Save As` opens a standard Windows file dialog. The portable extension is `.rubik.json`.

The `rubik.state` version 1 document contains:

- size N (2..32);
- six U/R/F/D/L/B row-major facelet arrays;
- canonical white/red/green/yellow/orange/blue color IDs;
- optional non-executable metadata;
- a deterministic lowercase SHA-256 state hash.

It intentionally excludes native cubie IDs, trusted history, UI/camera state, and local paths. Solver moves are a separate `.rubikmoves` artifact.

## Save semantics

Saving validates/normalizes the document, calculates the canonical hash, writes a unique sibling temporary file, flushes it, re-reads it through the strict parser, and then performs a same-directory replace/move. Replacing an existing state retains a `.bak` backup. A failure does not delete or partially replace the destination.

The header shows filename, validation state, full hash, and a `*` dirty marker. Recent paths are memory-only.

## Load semantics

1. Press `Load State` and select `.rubik.json`.
2. The bounded reader rejects oversized, malformed, duplicate-property, unsupported-version, count-invalid, or hash-invalid data.
3. A separate native engine is created and receives the validated size/facelets.
4. Only after native acceptance does the app swap handles and rebuild the scene.

Invalid load leaves the current cube, hash, scene, and selected path unchanged. Facelet-only imports preserve exact visible stickers but do not invent trusted move history or native cubie orientation.

## Solution and checkpoint files

- Verified solver output uses `.rubikmoves` with input hash, solver ID, moves, complete/verified flags, and final hash.
- The UI loads a solution only when its size/input hash match the current cube; unverified artifacts cannot be played as solutions.
- NxN reduction checkpoints use `rubik.reduction-checkpoint` version 1 and resume only against the exact solver ID, size, and input hash.
- The current compact UI displays checkpoint status; checkpoint save/load APIs are implemented and contract-tested, while dedicated checkpoint buttons remain future UI work.

Schema: `assets/rules/rubik/rubik-state-v1.schema.json`.

