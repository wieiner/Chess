# Chess3D Simple Anchor Projection

P2D implements a temporary runtime projection for Asgard/Meru centerAssembly progress.

It is intentionally not the final Forbidden Core fusion model.

## Why projection is needed

P2C specifies future core physics where multiple pieces can share a core cell and produce stack/fusion/resonance states. The current runtime board still stores exactly one integer per cell.

P2D bridges that gap with a compatibility layer:

- keep the current single-occupancy board;
- compute typed target slots over that board;
- count anchors when current cells match target requirements;
- detect simple centerAssembly victory.

## Anchor rule

An anchor is active when all conditions are true:

1. `goalProfile.type` is `centerAssembly` or `centerAssemblyTraining`;
2. the cell is a target slot for the side;
3. the cell contains a piece of that side;
4. the piece type matches the expected slot type;
5. the cell contains one piece, because P2D still uses the old single-occupancy board.

## Anchor count

`Chess3D_GetAnchorCount(side)` returns the number of matching typed target slots for that side.

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

## What P2D does not implement

P2D does not implement:

- CoreCell stacks;
- multi-piece cells;
- fusion entities;
- implosion/resonance/color-permutation;
- contested anchors;
- knockback/reserve;
- dislodging anchored pieces;
- Rubik layer turns as legal turn actions;
- Volume-Surface 216 victory.

Those remain staged work for P2E and later.

## 96 pieces vs 64 core cells

Six sides have 96 total pieces, but the current tactical core has 64 cells. P2D can still count target progress as a projection, but it cannot solve true six-side co-occupancy. That is the point of the future CoreCell Stack Board Model.
