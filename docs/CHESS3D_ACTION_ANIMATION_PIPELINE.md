# Chess3D Action Animation Pipeline

P3B introduces small, safe visual action hints.

## Implemented

- normal move flash from source to target;
- replay-step flash using the engine last-move coordinates;
- Rubik layer pre-highlight before the engine turn;
- Hodge primary/mirror path flash after composite moves.

## Rules

The engine remains the source of truth. The UI does not commit speculative board state during animations. After every action, visuals are rebuilt from the engine board, stacks, fusion descriptors, anchors, and status getters.

## Deferred

Smooth mesh glide, capture dissolve, reserve inventory motion, cinematic fusion effects, and timeline replay controls remain later UI work.
