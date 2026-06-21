# Next Era Mode Incubator

Date: 2026-06-21

Status: concept incubator only.

This document does not create a runtime mode. It does not add JSON RuleProfile files, schema enum values, test expectations, engine hooks, UI panels, or online protocol actions.

The runtime Chess3D RuleProfile count remains exactly five:

1. `classic-six-side-3d-8x8x8-v0.1`
2. `single-side-3d-8x8x8-v0.1`
3. `asgard-convergence-3d-8x8x8-v0.1`
4. `rubik-convergence-3d-8x8x8-v0.1`
5. `hodge-projection-duel-3d-8x8x8-v0.1`

## Incubation Rules

- Do not create JSON profiles.
- Do not add RuleProfile files.
- Do not update the rule-profile schema.
- Do not add tests that count these as modes.
- Do not change native engine behavior.
- Do not change save/replay/action history formats.
- Keep Asgard, Rubik, and Hodge meanings stable.
- Use this document to compare concepts before any future implementation phase exists.

## Candidate 1 - Timefold 3D Chess

Core idea:

Timefold pieces create delayed echoes of prior legal moves. An echo is not a new army; it is a scheduled replay artifact that may affect selected future turns under strict rules.

How it differs from existing modes:

- Unlike Asgard, it is not about the Forbidden Core, fusion, or centerAssembly.
- Unlike Rubik, it does not rotate full layers.
- Unlike Hodge, it is not a triune mirror projection from one primary move.
- It would be a temporal/replay mechanic layered over ordinary board actions.

Required engine primitives:

- scheduled action queue;
- replay-safe delayed effects;
- deterministic conflict policy when an echo target is occupied;
- state hash that includes pending echoes;
- save/load/replay support for pending temporal actions.

UI requirements:

- timeline strip;
- pending echo indicators;
- source/target ghost highlights;
- clear invalidation messaging when an echo fizzles or conflicts.

AI/search implications:

- branching factor increases because future board state depends on scheduled effects;
- search must reason over pending echo queues;
- perft/divide must include delayed-action state.

Online implications:

- server authority must own echo scheduling;
- action log must expose both original action and resolved echo;
- reconnect snapshots must include pending echoes.

Why not implement now:

- P4/Next Era priorities are deployment hardening, documentation consistency, and portal foundations.
- The replay/save system would need a new scheduled-effect layer.

## Candidate 2 - Portal / Gate Chess

Core idea:

Fixed portals connect selected cells or cube faces. A move entering one gate exits another according to a deterministic transform.

How it differs from existing modes:

- Unlike Rubik, portals move pieces through fixed topological links rather than rotating a whole layer.
- Unlike Hodge, portal transforms are board features, not macro-player mirror moves.
- Unlike Asgard, the central core is not the default goal.

Required engine primitives:

- portal graph descriptor;
- move generator awareness of portal exits;
- no-cycle path policy;
- collision/capture policy at portal exit;
- profile-gated portal legality.

UI requirements:

- visible gate markers;
- preview lines between linked gates;
- clear "exit occupied" or "portal blocked" invalid reasons.

AI/search implications:

- pseudo/legal move generation must expand portal transitions;
- pathfinding/cycle prevention becomes part of move generation;
- divide diagnostics must show portal-expanded actions.

Online implications:

- portal graph must be in snapshot/savegame;
- server must validate portal actions from raw source/target input.

Why not implement now:

- It needs a new rule-profile schema branch and UI overlay language.
- The existing five modes are still being hardened for deployment and playability.

## Candidate 3 - Gravity Well Chess

Core idea:

Certain cells exert directional pull. Pieces may be constrained, accelerated, or redirected near gravity wells.

How it differs from existing modes:

- Unlike Asgard, the center is not necessarily an assembly/victory target.
- Unlike Rubik, the board does not rotate.
- Unlike Hodge, there are no linked projection sides.

Required engine primitives:

- field map over 8x8x8 coordinates;
- movement modifier policy;
- deterministic forced-move or drift resolution;
- state hash and save/replay support for dynamic fields if fields can change.

UI requirements:

- field strength visualization;
- affected-path preview;
- blocked/forced movement explanation.

AI/search implications:

- legal move generation must distinguish chosen movement from forced field effects;
- search must simulate follow-up forced movement without mutation leaks.

Online implications:

- server action validation must include field effects;
- snapshots must expose field state.

Why not implement now:

- It changes core movement semantics more deeply than the current deployment-focused roadmap allows.

## Candidate 4 - Orbit Chess

Core idea:

Selected rings, shells, or orbital bands can rotate pieces around a center without full Rubik layer semantics.

How it differs from existing modes:

- Rubik rotates complete 8x8 layers around X/Y/Z.
- Orbit Chess would rotate predefined rings/bands and may leave most of the layer untouched.
- It should not reuse Rubik action names unless transforms are identical.

Required engine primitives:

- orbit set descriptor;
- orbit transform map;
- collision/no-overwrite policy;
- action history notation distinct from Rubik `LAYER`.

UI requirements:

- orbit band selector;
- pre-highlighted ring cells;
- rotation animation for ring subset.

AI/search implications:

- action generator needs orbit actions as first-class candidates;
- perft/divide must count orbit actions separately from layer turns.

Online implications:

- protocol needs distinct orbit action kind;
- action log/replay must serialize orbit id and direction.

Why not implement now:

- Rubik layer turns already cover the current rotation mechanic.
- Adding a near-neighbor mechanic now risks confusing profile boundaries.

## Candidate 5 - Team Cathedral Chess

Core idea:

Six cube sides are grouped into macro alliances such as 2v2 or 3v3. The game emphasizes coordinated side turns and shared objectives.

How it differs from existing modes:

- Hodge has two macro-players with three mirrored projections and all-or-nothing composite moves.
- Team Cathedral would be alliances of independently moving sides, not mirror projections.
- Asgard may have center objectives, but Team Cathedral would not default to fusion/core physics.

Required engine primitives:

- alliance/team descriptor;
- turn order policy for allies;
- shared victory and resignation/draw rules;
- optional friendly-fire policy.

UI requirements:

- team color/labeling;
- current ally turn banner;
- shared objective and score panels.

AI/search implications:

- multi-agent evaluation and side-to-team aggregation;
- possible cooperative action planning.

Online implications:

- matchmaking must seat teams;
- reconnect/spectator views must distinguish side seats from team identities.

Why not implement now:

- Public online authority is still single-server MVP.
- Team seating and reconnect semantics should be hardened before team variants.

## Candidate 6 - Shadow Mirror Chess

Core idea:

Shadow Mirror is an asymmetric cousin of Hodge: one side creates a shadow response through a transform, but the response may differ by piece, side, or phase.

How it differs from existing modes:

- Hodge is symmetric triune projection with one primary plus two mirrors.
- Shadow Mirror would be asymmetric and possibly two-action rather than three-action.
- It must not be folded into Hodge unless it preserves Hodge's macro-player contract.

Required engine primitives:

- transform descriptor with asymmetry;
- shadow legality and rollback policy;
- action history metadata for primary/shadow action pairs;
- conflict policy for partial or blocked shadow responses.

UI requirements:

- primary action arrow;
- shadow response arrow;
- blocked-shadow status.

AI/search implications:

- composite action generator similar to Hodge but with distinct transforms;
- evaluation must account for asymmetric shadow effects.

Online implications:

- server authority must enforce all-or-nothing or explicitly partial policy;
- replay and snapshots must serialize the shadow pair.

Why not implement now:

- Hodge already exercises the projection/composite-move family.
- A second mirror mode should wait until Hodge UX, replay, and online play are production-stable.

## Incubator Ranking

| Candidate | Implementation risk | Reuse from current engine | Best future prerequisite |
| --- | --- | --- | --- |
| Portal / Gate Chess | medium | legal preview, action history, transforms | PGN/replay/docs cleanup complete |
| Team Cathedral Chess | medium | side/team metadata, online seats | reconnect/team matchmaking hardening |
| Orbit Chess | medium-high | Rubik transform/action pipeline | Rubik animation and notation polish |
| Shadow Mirror Chess | medium-high | Hodge transform/composite pipeline | Hodge online/replay hardening |
| Timefold 3D Chess | high | replay/action history | scheduled-effect save/replay layer |
| Gravity Well Chess | high | coordinate board/action preview | movement-law refactor budget |

Near-term recommendation:

- Do not implement any incubator mode in the next deployment phase.
- Finish TLS/domain, public HTTPS auth, rollback/backups/log rotation, documentation reconciliation, and Chess2D PGN/UCI foundations first.
