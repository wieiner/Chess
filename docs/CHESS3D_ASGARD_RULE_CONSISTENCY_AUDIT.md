# Chess3D Asgard Rule Consistency Audit

Phase: P4C phase 08.

## Scope

This audit checks the current Asgard / Meru Convergence profile against runtime behavior. It does not introduce final fusion physics, destructive implosion, new victory rules, or a sixth Chess3D mode.

Real Asgard profile:

- file: `assets/rules/profiles/asgard_convergence_3d_v0_1.json`
- rulesetId: `asgard-convergence-3d-8x8x8-v0.1`
- goal: `centerAssembly`
- layer turns: disabled
- projection: none

## Implemented Runtime

- RuleProfile loading and profile summaries.
- CoreCube `x/y/z = 2..5`.
- CoreCell stack overlay inside the Forbidden Core.
- Projected board compatibility for old piece APIs.
- Stack-aware anchors and `allPiecesAnchored` centerAssembly victory.
- Fusion descriptors over stacks: single, friendly pair/stack, royal pair, contested/mixed, implosion progress flags.
- Knockback capture for outer-field captures and core-to-outside captures.
- Reserve counts by side and piece type.
- Reserve restore to matching free home slots.
- Action history, notation, save/load/replay state hash, and shallow action diagnostics.
- Online authority startup, snapshot, action validation, and matchmaking smoke through the existing protocol surface.
- AI/search smoke over Asgard legal candidates, including reserve restore where available.

## RuntimePartial / Deferred

- Fusion is a descriptor, not destructive merge.
- Implosion is progress state, not a destructive board event.
- Contested anchor scoring remains deferred.
- Dislodge, knockback from core stacks, and reserve restore into core remain deferred.
- Rich reserve inventory UI remains deferred.
- Volume-Surface 216 remains disabled/future.
- Classic checkmate is not the Asgard outcome.
- Rubik layer turns belong to `rubik_convergence_3d_v0_1.json`, not base Asgard.
- Hodge projected moves belong to `hodge_projection_duel_3d_v0_1.json`, not Asgard.

## Metadata Drift Found

The profile known limitations still said reserve restore was not implemented. That is stale after P2I. The accurate statement is:

- reserve restore is implemented only to matching free home slots;
- restore into core, restore-capture, and rich inventory UI remain deferred.

## Consistency Rules

- Asgard may expose reserve restore and center/core actions.
- Asgard must reject Rubik layer-turn commands.
- Asgard must reject Hodge projected move commands.
- Asgard save/replay must preserve enough projected board, stack, reserve, fusion, anchor, and action history state to rebuild descriptors.
- Online authority must remain the only gameplay authority for remote Asgard tables.

## Risks

- Treating descriptor fusion as final rules would make tests pass for the wrong product promise.
- Treating scenario JSON as modes would inflate the five-profile catalog.
- Treating Asgard as the default 3D game would hide Classic, Single-Side, Rubik, and Hodge boundaries.
- Online snapshots must not omit stack/reserve/action-history state required for replay or reconnect.
