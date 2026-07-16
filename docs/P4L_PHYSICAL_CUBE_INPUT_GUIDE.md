# Physical Rubik Cube Input Guide

## Before entering stickers

Choose the exact cube size (2..32) in Rubik Studio. Use the canonical face labels and color scheme:

| Face | Solved color |
| --- | --- |
| U | white |
| R | red |
| F | green |
| D | yellow |
| L | orange |
| B | blue |

Keep the physical cube orientation fixed while copying all six faces. Each grid is row-major as viewed directly from that labelled face.

## Enter the cube

1. Press `Physical Editor`.
2. Select a color and paint/click/drag cells in the U/R/F/D/L/B tabs.
3. Use fill, rotate, copy/paste, undo, and redo only inside the draft. The live native cube does not change while editing.
4. Run validation and select an issue to navigate to its face/cell.
5. Correct all errors and apply the accepted draft.
6. Save the accepted state with `Save As` to a `.rubik.json` file.

Odd cubes can use their fixed face centers as orientation guidance when all six centers are distinct. Even cubes have no single fixed center; their U/R/F/D/L/B labels must be declared explicitly and are not silently inferred.

Incomplete work can use the separate editor-draft artifact. It is not a valid `.rubik.json` physical state until all `6*N*N` stickers are present.

## Validation diagnostics

The editor/state pipeline detects:

- missing or unknown stickers;
- wrong per-color counts;
- malformed dimensions/face lengths;
- impossible corner or wing inventory;
- a single twisted corner (2x2/3x3 proof boundary);
- a single flipped 3x3 edge;
- incompatible 3x3 corner/edge permutation parity;
- hash or format/version mismatch when reading a file.

For N>3, inventory/orbit checks are implemented, but full arbitrary-N orientation/permutation/parity proof is not. A valid 11x11 import therefore reports inventory-valid with orientation/parity unproven and `solverReady=false`; this is an honest accepted input boundary, not a full solvability proof.

Applying an accepted physical state creates a candidate native engine and swaps it into the app only after native acceptance. A rejection leaves the previous cube unchanged.

