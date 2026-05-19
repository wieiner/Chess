# Single-Side 3D Chess Rules Spec

Ruleset id: `single-side-3d-chess-8x8x8-v0.1`

P2A scope: one side, one army, one home face. The purpose is to make the local rule core precise before generalizing it to six cube faces.

## 1. Board

- Board size: 8 x 8 x 8.
- Coordinates: `x,y,z` in `0..7`.
- Total cells: 512.
- P2A home face: `z = 0`.
- P2A forward direction: `+Z`.

## 2. Initial Setup

The initial army occupies the central 4x4 square:

- `x = 2..5`
- `y = 2..5`
- `z = 0`

Layout:

```text
y=5:  N  P  P  R
y=4:  P  Q  K  P
y=3:  P  B  B  P
y=2:  R  P  P  N
       x=2 3 4 5
```

Piece names:

- `P`: pawn
- `R`: rook
- `N`: knight
- `B`: bishop/officer
- `Q`: queen
- `K`: king

Formal coordinates:

| Piece | Coordinates |
| --- | --- |
| Pawn | `(3,2,0)`, `(4,2,0)`, `(2,3,0)`, `(5,3,0)`, `(2,4,0)`, `(5,4,0)`, `(3,5,0)`, `(4,5,0)` |
| Rook | `(2,2,0)`, `(5,5,0)` |
| Knight | `(5,2,0)`, `(2,5,0)` |
| Bishop/Officer | `(3,3,0)`, `(4,3,0)` |
| Queen | `(3,4,0)` |
| King | `(4,4,0)` |

## 3. Optional Minor Randomization

`minor-random` mode is specified for later implementation:

- rooks stay fixed;
- pawns stay fixed;
- king and queen stay fixed unless a future config explicitly opts in;
- two knights and two bishops may be randomly permuted between four minor slots:
  - corner knight slots: `(5,2,0)` and `(2,5,0)`;
  - central bishop slots: `(3,3,0)` and `(4,3,0)`;
- randomization must be seed-based and reproducible.

P2A does not require runtime randomization.

## 4. Movement Rules

### Rook

- Changes exactly one coordinate by a nonzero distance.
- Other two coordinates are unchanged.
- Line piece.
- Path must be clear.

### Bishop / Officer

- Diagonal line piece.
- Changes two or three coordinates by equal absolute distance.
- Direction examples:
  - `(+-n,+-n,0)`
  - `(+-n,0,+-n)`
  - `(0,+-n,+-n)`
  - `(+-n,+-n,+-n)`
- Path must be clear.

### Queen

- Rook plus bishop.
- Moves in all straight 3D directions with direction components in `{-1,0,+1}`, not all zero.
- Path must be clear.

### King

- One step to any of the 26 neighboring cells.
- Cannot move onto a friendly piece.
- Full king-safety enforcement is draft for P2A.

### Knight

- 3D L-leaper.
- All coordinate permutations of `(+-2,+-1,0)`.
- Ignores blockers.
- Cannot land on a friendly piece.

### Pawn

For the P2A side:

- Forward direction: `+Z`.
- Quiet move: `(0,0,+1)` if destination is empty.
- Initial double move from `z = 0`: `(0,0,+2)` if both intermediate and destination cells are empty.
- Captures: `(dx,dy,+1)`, where `dx,dy` are in `{-1,0,+1}` and not both zero.
- Cannot capture straight forward.
- Promotion at `z = 7`.
- En passant is out of scope for P2A.

## 5. Capture Rules

- No cell contains more than one piece.
- Own piece blocks and cannot be captured.
- Enemy piece can be captured if the destination is reachable by that piece.
- Line pieces cannot jump over any occupied cell.
- Knight jumps.
- Pawn captures only by pawn-capture vectors.

## 6. Attack Map / Check Draft

### Current Behavior

The engine currently generates pseudo-legal draft movement. It validates movement shape, board bounds, own-piece blocking, enemy captures, line blockers, knight jumps, pawn captures, and promotion.

It does not yet fully enforce 3D king safety, check, checkmate, or stalemate.

### P2A Target

Attack generation can be derived from the movement rules above. A king is in check when attacked by an enemy piece under those attack vectors.

Full checkmate and stalemate hardening remains later work.

## 7. Future Generalization

Six-sided chess should generalize this P2A rule core through local-to-world coordinate transforms:

- each side has a home face;
- each side has a forward direction into the cube;
- local P2A coordinates map to world coordinates for `PX`, `NX`, `PY`, `NY`, `PZ`, `NZ` or the existing side ids `1..6`;
- movement rules remain local and are transformed per side;
- P2B will formalize the six-face mapping;
- P2C will define Rubik layer turns as legal chess actions when appropriate.
