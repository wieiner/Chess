# Chess3D Rubik Layer Turns

Ruleset id: `rubik-convergence-3d-8x8x8-v0.1`

## Operation

The existing engine exposes:

```text
rotateLayer(axis, layer, quarterTurns)
```

Profile contract:

- axes: `X`, `Y`, `Z`;
- layers: `0..7`;
- quarter turns: `-1`, `+1`;
- action cost: `oneTurn`.

## Layer Turn Profiles

Layer-turn behavior is deliberately separated from movement rules:

- `disabled`: no layer turns in normal play.
- `sandbox`: UI/debug turns are allowed without turn semantics.
- `ritualTurn`: rotating a layer is a legal action instead of a normal move.
- `globalEvent`: automatic layer rotations later.

This separation allows classic six-side chess and Asgard/Meru convergence to exist without mandatory cube rotations.

## Current Runtime Status

P2H implements `ritualTurn` for `rubik-convergence-3d-8x8x8-v0.1`.

`Chess3D_RotateLayer` now:

- rotates the projected 512-cell board;
- moves whole CoreCell stacks for cells inside the rotated layer;
- resynchronizes projected core cells from stack top entries;
- recomputes fusion descriptors;
- recomputes anchors, implosion progress, and compatible centerAssembly victory;
- leaves reserve counts untouched;
- records last layer-turn telemetry through append-only ABI.
- P2I records successful ritual turns in unified action history with notation such as `#4 LAYER Z[2]+`.
- P2K exposes basic layer-turn controls in Chess3DApp: axis, layer, quarter turn, capability check, last result, and action-log visibility.

Asgard convergence, classic six-side, and single-side profiles keep layer turns disabled and clean-fail. The legacy draft profile keeps non-stack debug rotation for compatibility.

Deferred:

- king-safety after layer rotation;
- animated UI controls;
- full replay/import/export semantics;
- online serialization;
- AI/search generation of layer-turn actions;
- GPU stack snapshots.

See also `docs/CHESS3D_RUBIK_LAYER_TURN_SEMANTICS.md` and `docs/CHESS3D_RUBIK_STACK_ROTATION.md`.
