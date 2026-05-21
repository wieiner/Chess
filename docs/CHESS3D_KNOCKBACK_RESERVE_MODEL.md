# Chess3D Knockback / Reserve Model

P2G adds runtime capture semantics for Asgard/Meru convergence profiles.

## Outer Field

The outer field stays single-occupancy:

- empty destination: normal move;
- own occupied destination: illegal;
- enemy occupied destination:
  - `classicCapture`: captured piece is removed;
  - `knockbackCapture`: captured piece is routed to home or reserve.

## Home-Or-Reserve

For `knockbackCapture`:

1. Find the first free home slot for the captured side and piece type.
2. If found, place the captured piece there.
3. If no matching home slot is free, increment reserve count for `side + pieceType`.
4. The moving piece occupies the capture destination.

The current policy is `firstMatchingFreeHomeSlot`.

## Forbidden Core

The Forbidden Core keeps P2E/P2F semantics:

- entering a core cell appends to its stack;
- existing occupants are not removed;
- enemy co-occupancy becomes contested fusion state;
- ordinary destructive core capture remains disabled.

## Reserve

Reserve is stored as counts:

```text
reserveCounts[side][pieceType]
```

This intentionally avoids unique piece ids. P2I adds the first restore action without changing that model: a side/type count can return one compatible piece to a matching free home slot.

## Runtime Status

Implemented in P2G:

- profile-gated reserve enablement;
- profile-gated knockback enablement;
- home-slot fallback;
- reserve fallback;
- last knockback telemetry;
- C ABI and C# status access.
- P2I restore-from-reserve to matching free home slots;
- P2I deterministic restore notation and action-history records.

Deferred:

- visual reserve inventory;
- restore into Forbidden Core;
- restore captures;
- online serialization;
- moving reserve during Rubik layer turns.

P2H moves projected board cells and whole CoreCell stacks for Rubik convergence. Reserve counts remain outside the board and are unaffected by layer turns.
