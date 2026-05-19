# Chess3D Asgard Core Physics Audit

P2C audit target: current center/core model, board occupancy, and safe path toward Asgard multi-occupancy/fusion physics.

## 1. Current Board Cell Storage

`Chess3DEngine` stores the board as:

```cpp
std::array<int, 512> board;
```

Each cell contains one integer piece code:

```text
piece = side * 10 + type
```

Zero means empty. Nonzero means exactly one piece.

The index layout is:

```text
index = z * 64 + y * 8 + x
```

## 2. Multi-Occupancy Support

The current runtime model does not support multiple pieces in one cell.

`Chess3D_SetPiece` overwrites the integer at a cell. `Chess3D_GetPiece` returns a single integer. `Chess3D_GetBoard` and `Chess3D_SetBoard` exchange exactly 512 integers. This ABI shape is intentionally simple and stable, but it means core stacks cannot be represented directly yet.

## 3. Existing Single-Occupancy Assumptions

Single occupancy is assumed in:

- `Position::board`;
- move generation target lookup;
- own-piece blocking;
- capture flags and captured-piece field;
- `SetPiece`/`GetPiece`;
- `SetBoard`/`GetBoard`;
- `RotateLayer`, which moves one integer per cell;
- GPU 3D evaluation and Rubik board generation, which expect 512 integer cells;
- WPF rendering, which maps one piece code to one visual model per cell.

## 4. Existing coreProfile / anchorMode

The P2B profiles already contain:

- `coreProfile.type = asgardMeruCore`;
- `coreCube = x/y/z 2..5`;
- `targetSlots.derivation = derivedFromSideHomeFaceProjection`;
- `anchorMode = softAnchor`;
- `contestedAnchor = deferred`.

These are data contracts. There is no runtime anchor state, target-slot derivation, or victory detection yet.

## 5. Safe Changes Without Rewrite

Safe P2C changes:

- add `occupancyProfile`, `fusionProfile`, and `corePhysicsProfile` to JSON profiles;
- document two-zone physics: outer field single occupancy, core special occupancy;
- validate the new profile fields in headless tests;
- keep runtime board storage and public ABI unchanged;
- explicitly mark multi-occupancy as `specOnly`.

## 6. Changes Requiring Deep Refactor

These require a separate design/implementation pass:

- replacing `int board[512]` with a richer `Cell` model;
- preserving old `GetBoard`/`SetBoard` while exposing new stack-aware ABI;
- rendering multiple models or fusion states in one 3D cell;
- making move generation understand stack/co-occupancy;
- making `RotateLayer` move cell stacks instead of single integers;
- updating GPU ABI for stack/fusion-aware evaluation;
- synchronizing stacks through network messages.

## 7. Recommended Staging

Stage 1: P2C data/spec/tests only. Engine remains single-occupancy.

Stage 2: parse profile metadata at runtime and expose profile summary safely.

Stage 3: add a new `CoreCell` stack model internally while preserving old ABI projections.

Stage 4: implement fusion, anchor, and victory logic.

Stage 5: add UI visualization for stacks, resonance, color/permutation state, and implosion/fusion markers.

This staged path avoids breaking Chess2D, Rubik, Online, CUDA optional behavior, and the existing Chess3D ABI.
