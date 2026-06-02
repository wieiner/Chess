# Chess3D AI Evaluation

P3D evaluation is deterministic and intentionally simple.

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

## Deferred

No opening book, transposition table, quiescence search, neural evaluation, GPU search, or external engine is included in P3D.
