# Chess3D Simple Anchor Projection

P2D implemented a temporary runtime projection for Asgard/Meru centerAssembly progress. P2E supersedes the cell scan with stack-aware anchors while keeping the same target-slot vocabulary.

It is intentionally not the final Forbidden Core fusion model.

## Why projection is needed

P2C specifies future core physics where multiple pieces can share a core cell and produce stack/fusion/resonance states. The current runtime board still stores exactly one integer per cell.

P2D bridged that gap with a compatibility layer:

- keep the current single-occupancy board;
- compute typed target slots over that board;
- count anchors when current cells match target requirements;
- detect simple centerAssembly victory.

P2E keeps the old projected board but also searches CoreCell stack entries for anchors.

## Anchor rule

An anchor is active when all conditions are true:

1. `goalProfile.type` is `centerAssembly` or `centerAssemblyTraining`;
2. the cell is a target slot for the side;
3. the cell contains a piece of that side;
4. the piece type matches the expected slot type;
5. if stacks are enabled, any stack entry in that cell can satisfy the target.

## Anchor count

`Chess3D_GetAnchorCount(side)` returns the number of matching typed target slots for that side. A slot counts once even if multiple matching entries are present.

`Chess3D_GetRequiredAnchorCount(side)` currently returns `victoryProfile.requiredPieceCount` when available, otherwise 16.

## Victory rule

For `centerAssembly` / `centerAssemblyTraining` profiles with `victoryProfile.type = allPiecesAnchored`, victory triggers when:

```text
anchorCount(side) >= requiredAnchorCount(side)
```

Then:

- `Chess3D_IsGameOver()` returns true;
- `Chess3D_GetWinnerSide()` returns that side.

Classic and sandbox profiles do not trigger centerAssembly victory.

## What P2E still does not implement

P2E still does not implement:

- fusion entities;
- implosion/resonance/color-permutation;
- contested anchors;
- knockback/reserve;
- dislodging anchored pieces;
- Rubik layer turns as legal turn actions;
- Volume-Surface 216 victory.

Those remain staged work for P2F and later.

## 96 pieces vs 64 core cells

Six sides have 96 total pieces, but the current tactical core has 64 cells. P2E solves the storage problem for stack-enabled profiles by letting multiple entries occupy one Forbidden Core cell. It still does not solve the game-design problem of how contested stacks, fusion, and dislodging should score.
