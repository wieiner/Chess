# P4M Chess3D Profile Model Integration

Phase 39 connects Chess3DApp to the shared semantic model catalog. The asset
layer remains entirely in managed presentation code and does not modify native
board state, legal actions, history, profiles, save/replay, or outcomes.

## Profile plans

All five existing profiles use the six `chess3d.common.*` piece roles when a
selected package supplies them. The legacy v1 set remains compatible through
its white/black piece roles.

Optional roles are isolated:

| Profile | Optional visual roles |
| --- | --- |
| Classic Six-Side | none |
| Single-Side Training | none |
| Asgard Convergence | core, anchor, reserve slot, fusion marker |
| Rubik Convergence | Asgard roles plus convergence core/layer/turn markers |
| Hodge Projection Duel | primary marker, mirror marker, projection arrow |

The planner recognizes only these existing rule IDs. It does not create a rule
profile and is not consulted by the native engine.

## Runtime behavior

The model selector discovers v2 manifests and the legacy v1 catalog by stable
`setId`. GLB parsing uses the bounded asynchronous loader and stale selection
loads are cancelled. OBJ uses the existing mesh/MTL path. A missing common or
special role retains the current procedural piece or overlay.

Core, anchor, fusion, selected Rubik layer, and Hodge endpoint models are
additive presentation hints. Existing overlays remain visible and authoritative,
so a corrupt or absent optional model cannot hide gameplay state. Overlay models
are not inserted into the hit map; piece and legal-target hit testing remains
unchanged.

Diagnostics report catalog type, loaded GLB role count, OBJ/material status,
and procedural fallback count.

## Verification

- shared contracts prove distinct plans for exactly five rule profile IDs;
- Classic and Single have no special-role inheritance;
- Hodge has no Asgard role inheritance;
- existing GLB corruption and fallback contracts remain green;
- Chess3DApp x64 Release build verifies linked WPF and native boundaries;
- no rule profile or scenario JSON is changed.
