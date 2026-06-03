# Chess3D AI Evaluation

P3D evaluation is deterministic and intentionally simple. P3D.1 keeps the same profile-aware evaluation surface but adds deterministic move ordering, alpha-beta search, iterative deepening, and bounded quiescence-lite around it.

## Common Signals

- material over the projected board;
- material inside CoreCell stacks when stack mode is enabled;
- center proximity;
- legal-action mobility;
- terminal outcome bonus or penalty where the active profile has an engine-backed outcome.

## Asgard / Rubik Signals

For profiles with core mechanics, evaluation also considers:

- anchor counts;
- fusion counts;
- royal-pair counts;
- implosion progress;
- reserve counts;
- contested cells as a small risk.

Fusion remains a descriptor. Evaluation does not destroy or merge stack entries.

## Hodge Signals

Hodge scores material and mobility by macro-player group. A projected composite move is evaluated as one action after all child moves are applied to a copied state.

## P3D.1 Search Behavior

- Depth 1 keeps a fast static ordered root result so old short smoke tests remain stable.
- Deeper searches use copy-and-apply alpha-beta and return only completed iterative depths.
- Quiescence-lite is budget-gated and considers bounded tactical normal captures plus simple reserve restores.
- Summary JSON reports qnodes and cutoffs for diagnostics.

## Deferred

No opening book, neural evaluation, GPU search, external engine, or transposition-table implementation is included. TT is documented as a future search-local feature once Chess3D state identity is hardened enough.
