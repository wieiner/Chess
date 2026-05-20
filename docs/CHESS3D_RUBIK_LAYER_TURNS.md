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

`Chess3D_RotateLayer` transforms the projected board for non-stack draft profiles and is used by the UI and network messages. P2D can load the Rubik convergence profile and expose `layerTurnProfile.type = ritualTurn` through profile summary ABI.

P2E adds CoreCell stacks to the Forbidden Core. To avoid corrupting stacked cells, `Chess3D_RotateLayer` now fails cleanly when core stacks are enabled. Ritual layer turns are still profile data, not legal chess actions.

Deferred:

- legality checking for ritual turns;
- turn-cost enforcement;
- king-safety after layer rotation;
- anchor interaction after layer rotation;
- stack/fusion movement when a layer contains CoreCell stacks;
- notation and replay semantics.

These belong to P2H on top of the P2E CoreCell stack model.
