# Chess3D Asgard Core Physics

P2C defines a richer center model for Asgard/Meru Convergence. P2D adds a small runtime bridge, but the native engine remains single-occupancy until a later board-model refactor.

## 1. Two-Zone Physics

### Outer Field

The outer field is every cell outside the Forbidden Core.

- One cell contains at most one piece.
- Normal movement, capture, and blocking rules apply.
- Existing P2A movement contracts remain valid here.

### Forbidden Core / Asgard

The Forbidden Core is a special zone.

- Co-occupancy may be allowed.
- Ordinary capture may be disabled.
- Multiple pieces may share one core-cell.
- Shared presence can form stack, resonance, color/permutation, implosion, or fusion state.

The key idea: outside the center the game behaves like chess; inside the center it can behave like a sacred assembly/fusion space.

## 2. CoreCube Variants

### A. Current Tactical Core

Current Asgard profiles use:

- `x = 2..5`
- `y = 2..5`
- `z = 2..5`

This is a 4x4x4 tactical core inside the 8x8x8 board. P2C does not change this current core.

### B. Volume-Surface 216 Core

The Volume-Surface 216 Principle is an authorial mathematical-mythological concept:

- a 6x6x6 cube has volume 216;
- six 6x6 faces have total surface area 216;
- therefore surface and volume can be symbolically balanced.

This is not treated as a factual physics claim. It is a future symbolic/game-law profile idea. P2C records it as disabled/future metadata.

## 3. OccupancyProfile

`occupancyProfile` controls cell occupancy semantics.

### exclusive

- Every cell contains at most one piece.
- This is the current runtime model.

### coreStack

- Outside the core: at most one piece.
- Inside the core: a stack of multiple pieces may occupy one cell.
- Stack size can be bounded later; current profile uses `unboundedDraft`.

### quantumCore

- Inside the core, pieces may carry state/color/layer/permutation data.
- Detailed implementation is deferred.

## 4. FusionProfile

`fusionProfile` controls transformation/progress when pieces share core space.

### none

No fusion behavior.

### anchorOnly

A piece reaches a target and becomes anchored. This is the old simple P2B/P2C idea.

### pairFusion

Two compatible pieces in one core-cell form a fusion entity.

### stackFusion

Several pieces form a stack. Count/state can matter more than a single piece identity.

### colorPermutation

Pieces gain color/permutation state. This is a metaphorical game-state layer, not a claim about real physics.

### volumeSurface216

Future mode where completion depends on surface/volume balance, not only a raw piece count.

## 5. Fusion Entity

A fusion entity is not necessarily a new chess piece. It can be:

- virtual state;
- stack descriptor;
- transformed piece;
- victory progress marker;
- ritual state.

The engine should not assume a fusion entity always has a single classic piece type.

## 6. Capture / Fusion Interaction

Possible rules:

- outside core: ordinary capture or knockback;
- entering core: capture may be disabled;
- in core: enemy and friendly pieces may coexist;
- enemy co-occupancy may create contested fusion;
- friendly co-occupancy may create assembly/fusion progress.

The exact contested-fusion and implosion rules are intentionally deferred.

## 7. Victory

Asgard Convergence can support victory profiles beyond `allPiecesAnchored`:

- `requiredFusionCount`;
- `requiredCoreStacks`;
- `kingQueenFusion`;
- `surfaceVolume216Completion`;
- `sixGateCoronation`;
- `hybrid`.

P2D implements only simple `allPiecesAnchored` / `requiredPieceCount` style centerAssembly victory over the current single-occupancy board. Fusion-based victory remains later work.

## 8. Implementation Staging

Stage 1: JSON/spec/tests only. Engine remains single-occupancy. Completed in P2C.

Stage 2: runtime parses profile metadata, exposes profile summary ABI, derives target slots, and computes simple anchor projection. Completed in P2D.

Stage 3: board model supports `CoreCell` stacks without breaking old ABI. Planned for P2E.

Stage 4: fusion and advanced victory logic.

Stage 5: UI visualization for stacks, resonance, color/permutation, and implosion/fusion states.
