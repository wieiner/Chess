# Chess3D Capture Semantics

P2G separates classic capture from Asgard/Meru knockback capture.

## Classic

Profiles with `captureProfile.type = classicCapture` keep legacy behavior:

- enemy destination is overwritten by the moving piece;
- captured piece is removed;
- reserve is disabled;
- knockback telemetry reports `classicRemoved` for a successful classic capture.

## Asgard / Rubik Convergence

Profiles with `captureProfile.type = knockbackCapture` use:

```text
knockbackProfile.type = homeOrReserve
reserveProfile.type = sidePieceTypeCounts
```

### Outside to Outside

Enemy captures route the captured piece:

1. first free matching home slot;
2. otherwise reserve.

### Outside to Core

No knockback. The moving piece enters the target core stack. Occupants remain and fusion/contested state is recomputed.

### Core to Core

No knockback. The projected/top core stack entry moves to another core stack.

### Core to Outside

If the outside target has an enemy piece, that target piece is routed through the same home-or-reserve policy. The moving stack entry exits the core and occupies the target.

## Deferred

P2G does not implement:

- reserve restore action;
- dislodging pieces from contested core stacks;
- destructive implosion;
- reserve movement during Rubik layer turns;
- full six-side checkmate.

P2H layer turns move projected board cells and whole CoreCell stacks for Rubik convergence. They do not perform capture and do not change reserve counts.
