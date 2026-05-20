# Chess3D CoreCell Stack Model

P2E introduces the first runtime CoreCell stack model for the Asgard / Forbidden Core.

## 1. Board Remains Compatible

The public board remains:

```text
8 x 8 x 8
512 integer cells
pieceCode = side * 10 + pieceType
```

Old APIs still work:

- `Chess3D_GetBoard` returns the projected 512-int board;
- `Chess3D_SetBoard` accepts a projected 512-int board;
- `Chess3D_GetPiece` returns one projected piece;
- `Chess3D_SetPiece` remains deterministic.

## 2. Core Stack Overlay

The stack model is an overlay only for Forbidden Core cells.

Stacks are enabled when the loaded profile declares:

```text
occupancyProfile.type = coreStack
```

or:

```text
corePhysicsProfile.type = asgardCorePhysics
```

Outside the core, the old one-piece-per-cell model remains authoritative.

## 3. CoreStackEntry

Each stack entry stores:

```text
side
pieceType
pieceCode
flags
```

`flags` is currently `0` and reserved for future states:

- anchored;
- fused;
- contested;
- reserve/future;
- color/permutation/future.

Fusion and implosion are not active mechanics in P2E.

## 4. Projection Semantics

For old API compatibility:

```text
projected piece = top stack entry = last pushed entry
```

If the stack is empty, the projected piece is `0`.

The engine keeps `Position::board[index]` synchronized with this projection so old UI, snapshots, tests, and GPU smoke paths continue to see a normal board.

## 5. SetPiece Semantics

Outside the core:

- `SetPiece` keeps old behavior.

Inside the core when stacks are disabled:

- `SetPiece` keeps old behavior.

Inside the core when stacks are enabled:

- `SetPiece(x,y,z, side, type)` replaces the stack with exactly one entry;
- `SetPiece(x,y,z, 0, 0)` clears the stack.

This is intentional. Old setup tools remain predictable and do not silently append.

## 6. Stack APIs

P2E adds append-only ABI functions for:

- checking whether core stacks are enabled;
- reading stack count;
- reading a stack entry;
- pushing a piece into a core stack;
- clearing a core stack;
- removing a stack entry;
- reading the projected piece explicitly.

Invalid coordinates, outside-core pushes, disabled-stack pushes, invalid indices, and null output pointers fail cleanly.

## 7. Profile Isolation

Classic and single-side profiles use `occupancyProfile.type = exclusive`. Their stack APIs report disabled behavior and do not produce centerAssembly victories.

Asgard and Rubik convergence profiles enable stacks, but Rubik layer turns still do not move stacks in P2E.
