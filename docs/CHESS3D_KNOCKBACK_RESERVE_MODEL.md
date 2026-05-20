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

This intentionally avoids unique piece ids. A future stage can add restore actions and notation once piece identity and UI inventory are designed.

## Runtime Status

Implemented in P2G:

- profile-gated reserve enablement;
- profile-gated knockback enablement;
- home-slot fallback;
- reserve fallback;
- last knockback telemetry;
- C ABI and C# status access.

Deferred:

- restoring pieces from reserve;
- visual reserve inventory;
- online serialization;
- Rubik layer turns moving reserve or stacks.
