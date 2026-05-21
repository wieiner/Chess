# Chess3D Action History And Notation

P2I notation is deterministic and human-readable. It is not final PGN and is deliberately simple so tests, future replay, and online protocols can use it as a stable foundation.

## Format

Every notation line starts with `#<actionIndex>`.

Examples:

```text
#1 S1 MOVE P (3,3,0)->(3,3,1)
#2 S1 MOVE R (0,0,0)x(0,0,3) captured=2P capture=reserve
#3 S2 RESTORE P reserve->(5,5,7)
#4 LAYER Z[2]+
```

## Capture Destination

- `none`
- `removed`
- `home`
- `reserve`
- `coreCoOccupancy`

## Flags

Action flags mark important derived facts:

- capture;
- knockback;
- entered core;
- left core;
- layer turn;
- reserve restore;
- fusion/anchor changes;
- game over after action.

## Replay Foundation

P2I does not implement replay or undo. The action record now carries enough information for later export/import/replay stages to reconstruct intent and display a clear move log.
