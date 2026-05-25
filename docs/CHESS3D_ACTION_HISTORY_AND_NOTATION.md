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
#5 M1 HPD primary=S1 P (3,3,0)->(3,3,1); mirrors=[S3 P (3,0,3)->(3,1,3), S5 P (0,3,3)->(1,3,3)]
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
- projection composite move.

## Replay Foundation

P2I does not implement replay or undo. P2J adds Hodge Projection Duel composite actions to the same history stream. The action record now carries enough information for later export/import/replay stages to reconstruct intent and display a clear move log.

P2L legal action preview is intentionally not action history. Preview calls do not append actions and do not mutate board, stacks, fusion, reserve, anchors, or victory. Only successful moves, reserve restores, layer turns, and projected composite moves are recorded.

## P2K UI Exposure

`Chess3DApp` now shows the action stream in the control center. The UI can refresh, copy, and save a `.ch3dlog` text file. The file starts with the active `rulesetId`, followed by deterministic notation lines. Import/replay is still deferred to P2L.
