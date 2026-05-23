# Chess3D Hodge Projection Duel

`hodge-projection-duel-3d-8x8x8-v0.1` is a separate Chess3D RuleProfile. It is not Asgard, not a Forbidden Core mode, and not a fusion mode by default.

## Concept

Two macro-players play on the same 8x8x8 cube. Each macro-player is represented by three cube-face projections:

- Macro-player 1, the positive triad: sides `1,3,5`, using inward axes `+Z,+Y,+X`.
- Macro-player 2, the negative triad: sides `2,4,6`, using inward axes `-Z,-Y,-X`.

When a player chooses a primary move on one projection, the engine maps the same local move through the other two projections in that macro-player. The resulting turn is one composite action: one primary move plus two mirror moves.

## Mathematical Metaphor

The name is Hodge-inspired. Hodge star and duality motivate the idea that a direction can be paired with complementary projections. The game does not claim to implement exterior algebra or algebraic geometry directly; it uses the idea as a deterministic gameplay transform over cube-face local frames.

## Profile Defaults

- Board: 8x8x8.
- Capture: `classicCapture`.
- Occupancy: `exclusive`.
- Fusion: `none`.
- Core physics: `none`.
- Goal: `sandbox` while full 3D checkmate remains draft.
- Layer turns: `disabled`.
- Projection: `hodgeTriuneProjection`.
- Mirror policy: `allOrNothing`.

## Composite Turn

The default policy is all-or-nothing:

- all three child moves must be legal;
- if any mirror move is illegal, the board and action history are unchanged;
- successful composite turns append one `projectionCompositeMove` action with `HPD` notation.

Example notation:

```text
#7 M1 HPD primary=S1 P (3,3,0)->(3,3,1); mirrors=[S3 P (3,0,3)->(3,1,3), S5 P (0,3,3)->(1,3,3)]
```

## Known Limits

- Full six-side checkmate and king safety are still draft.
- UI visualization of the projection groups is minimal.
- Replay/import/export, online serialization, AI/search integration, and notation standardization are later stages.
- Hodge v0.1 intentionally disables Asgard core stacks, fusion, implosion, reserve/knockback, and Rubik layer turns.

## P2K UI

`Chess3DApp` now exposes Hodge Projection Duel through a mode-aware panel:

- projection enabled/off;
- macro-player groups;
- primary-side selector;
- from/to coordinate inputs;
- mirror preview using the transform ABI;
- all-or-nothing apply through `Chess3D_TryMakeProjectedMove`;
- last projection error and last action notation.

This is still not a full Hodge editor, replay system, or AI/search integration.
