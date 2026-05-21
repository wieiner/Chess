# Chess3D P2J Hodge Projection Audit

P2J starts from the P2I runtime: RuleProfile loading, action history, reserve restore, CoreCell stacks, fusion descriptors, knockback/reserve, and Rubik layer turns are already implemented.

## Current Side / Face Mapping

The engine uses side ids `1..6` with fixed cube-face local frames:

| Side | Home face | Inward / forward axis | Local-to-global at home face |
| --- | --- | --- | --- |
| 1 | `Z0` | `+Z` | `(u,v,w) -> (u,v,w)` |
| 2 | `Z7` | `-Z` | `(u,v,w) -> (7-u,7-v,7-w)` |
| 3 | `Y0` | `+Y` | `(u,v,w) -> (u,w,v)` |
| 4 | `Y7` | `-Y` | `(u,v,w) -> (7-u,7-w,7-v)` |
| 5 | `X0` | `+X` | `(u,v,w) -> (w,u,v)` |
| 6 | `X7` | `-X` | `(u,v,w) -> (7-w,7-u,7-v)` |

The same convention drives `faceCenterSquare`, home-slot restore, six-side setup, and Hodge projection transforms.

## Action Boundaries

- `Chess3D_TryMakeMove` validates one legal move, applies it, recomputes anchors, and appends one `Move` action.
- `Chess3D_RotateLayer` is profile-gated by `layerTurnProfile`; successful Rubik turns append one `LayerTurn` action.
- `Chess3D_RestoreReservePiece` restores from side/type reserve to a free matching home slot and appends one `ReserveRestore` action.
- Debug/setup helpers such as `SetPiece`, `Clear`, explicit stack push/clear/remove, and profile load are not turn-history actions.

## Safe P2J Extension Point

`projectionProfile` is a new RuleProfile section. It is separate from:

- `goalProfile`: Hodge v0.1 uses `sandbox`, not centerAssembly.
- `occupancyProfile`: Hodge v0.1 uses `exclusive`, not CoreCell stacks.
- `fusionProfile`: Hodge v0.1 uses `none`.
- `layerTurnProfile`: Hodge v0.1 uses `disabled`.

This keeps Hodge Projection Duel independent from Asgard/Meru convergence and Rubik convergence.

## Composite Turn Risks

Composite projected turns can fail halfway if implemented naively. P2J mitigates that by:

- validating all three child moves before mutation;
- rejecting child destination/source collisions;
- appending only one composite action after all children succeed;
- keeping failed composite turns out of action history;
- leaving reserve/fusion/layer side effects unused in the Hodge v0.1 profile.

Future hybrid profiles that combine projection with stacks, fusion, reserve, or Rubik turns should add their own tests before enabling that combination.
