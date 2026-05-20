# Chess3D Rule Profile Architecture

P2B introduces a configurable rule-profile contract. P2D adds the first runtime bridge for selected profile fields. The goal is still to avoid hardcoded mode branches while keeping the current stable ABI intact.

## RuleSet

A rule profile is a JSON `RuleSet` with these top-level fields:

- `rulesetId`: stable id, for example `asgard-convergence-3d-8x8x8-v0.1`.
- `version`: profile version.
- `displayName`: user-facing name.
- `description`: short explanation.
- `tags`: search/filter labels.
- `boardProfile`: board dimensions and coordinate conventions.
- `mythProfile`: optional narrative/decorative theme.
- `setupProfile`: initial army placement and randomization hooks.
- `movementProfile`: piece movement family.
- `captureProfile`: capture semantics.
- `knockbackProfile`: optional home-or-reserve routing for captured pieces.
- `reserveProfile`: optional reserve storage and future restore-action policy.
- `goalProfile`: what the game is trying to achieve.
- `coreProfile`: central-core target and anchoring data.
- `occupancyProfile`: board-cell occupancy semantics.
- `fusionProfile`: transformation/progress rules for co-occupied core cells.
- `corePhysicsProfile`: binding between core zone, occupancy, fusion, anchor, and symbolic laws.
- `implosionProfile`: optional non-destructive or future destructive completion behavior.
- `layerTurnProfile`: Rubik-like layer-turn behavior.
- `turnProfile`: side order and action model.
- `victoryProfile`: concrete victory detection rule.
- `randomizationProfile`: seed-based variation controls.
- `knownLimitations`: honest implementation notes.

## boardProfile

Current board profile:

- width: 8
- height: 8
- depth: 8
- coordinates: `x,y,z` in `0..7`

The native engine is still fixed to 8x8x8. Future engines can generalize this field, but current profile validation intentionally requires 8.

## mythProfile

`mythProfile` is narrative and visual metadata. It must not affect headless tests or engine correctness.

Fields:

- `theme`: `none`, `asgard`, `meru`, `asgard-meru`, or `custom`.
- `centerName`: for example `Asgard`, `Meru`, or `Forbidden City`.
- `sideNames`: names for sides/gates.
- `lore`: optional flavor text.

The Asgard/Meru idea belongs here when it is decorative. Gameplay-affecting central slots, occupancy, fusion, anchoring, and victory belong in `coreProfile`, `occupancyProfile`, `fusionProfile`, `corePhysicsProfile`, `goalProfile`, and `victoryProfile`.

## setupProfile

Setup profile describes how armies are placed:

- `singleSideCentral4x4`: P2A one-side setup.
- `sixSideProjectedFromSingleSide`: project the P2A local setup to all six home faces.
- `baseSetup`: `central4x4`.
- `homeFace` or `homeFaces`.
- `piecesPerSide`.
- `randomizationProfile`.

Optional minor randomization:

- rooks and pawns stay fixed;
- king and queen stay fixed by default;
- two knights and two bishops/officers may be permuted through seed-based randomization.

## movementProfile

Movement profiles name reusable movement families:

- `rook3d`: one coordinate changes, line path clear.
- `bishop3d` / `officer3d`: two or three coordinates change by equal absolute distance, line path clear.
- `queen3d`: rook3d plus bishop3d.
- `king3d`: one step in any of 26 neighboring cells.
- `knight3d`: leaper using coordinate permutations of `(+-2,+-1,0)`.
- `pawn3d`: side-local forward move, initial double move, forward-layer captures, promotion.
- custom/fairy pieces: later extension point.

## captureProfile

Supported contract values:

- `classicCapture`: captured piece is removed from the board.
- `knockbackCapture`: captured piece returns to its home slot if free; otherwise it goes to reserve.

P2G implements the runtimePartial home-or-reserve subset for ordinary outer-field captures.

## knockbackProfile

Supported contract values:

- `none`: no knockback behavior.
- `homeOrReserve`: captured outer-field pieces first try a matching free home slot, otherwise reserve.

Current runtime fields:

- `homeSlotPolicy = firstMatchingFreeHomeSlot`;
- `fallback = reserve`;
- `appliesTo = outerFieldCaptures`;
- `coreCapturePolicy = coOccupancyContested`;
- `destructiveCoreCapture = false`.

## reserveProfile

Supported contract values:

- `none` / `disabled`: no reserve.
- `sidePieceTypeCounts`: reserve stores counts by side and piece type.

P2G implements side/type counts and last-capture telemetry. Restore actions remain deferred.

## goalProfile

Supported contract values:

- `classicCheckmate`: chess-like objective. Full 3D mate remains later work.
- `centerAssembly`: pieces converge on central target slots and become anchored.
- `hybrid`: either checkmate or center assembly can win.
- `sandbox`: no automatic victory; useful for editor/testing.
- `centerAssemblyTraining`: training-only center assembly variant.

## coreProfile

Asgard/Meru convergence uses a central core:

- `coreCube`: `x=2..5`, `y=2..5`, `z=2..5`.
- `targetSlots`: derived from side home-face projection.
- `anchorMode`: `softAnchor` initially.
- `contestedAnchor`: future rule for contested central slots.

Anchoring is gameplay, not narrative. It belongs here and in `victoryProfile`.

## occupancyProfile

Occupancy profile affects board-cell semantics.

Supported contract values:

- `exclusive`: every cell contains at most one piece.
- `coreStack`: outside the core every cell is exclusive; inside the core a stack may contain multiple pieces.
- `quantumCore`: future mode where core occupants can carry state/color/layer/permutation data.

The old projected board ABI remains 512 integer cells. P2E adds `coreStack` as a runtime overlay for Asgard/Rubik profiles while classic and single-side profiles remain exclusive.

## fusionProfile

Fusion profile affects transformation and victory progress inside the core.

Supported contract values:

- `none`: no fusion.
- `anchorOnly`: a piece reaches a target slot and anchors.
- `pairFusion`: two compatible pieces form a fusion entity.
- `stackFusion`: several pieces form a stack/fusion state.
- `colorPermutation`: future color/permutation state.
- `volumeSurface216`: future surface/volume symbolic completion mode.

A fusion entity may be a virtual state, stack descriptor, transformed piece, victory progress marker, or ritual state. It is not necessarily a new classic chess piece.

P2F implements the stack-descriptor subset. Supported runtime fusion kinds are `none`, `single`, `friendlyPair`, `friendlyStack`, `royalPair`, and `contested`. `implosionSeed` and `implosionReady` remain reserved names; P2F exposes seed/readiness through flags and side progress.

## implosionProfile

Implosion profile describes central completion behavior.

Current runtime support:

- `type = centerCompletion`;
- `mode = progressState`;
- `destructive = false`.

P2F progress does not remove or transform pieces. Volume-Surface 216 remains disabled/future metadata.

## corePhysicsProfile

Core physics profile binds `coreProfile`, `occupancyProfile`, `fusionProfile`, anchor behavior, and victory interpretation.

For Asgard/Meru:

- `type`: `asgardCorePhysics`;
- `zoneModel`: `outerExclusive_coreStack`;
- `implementationStage`: `specOnly`;
- `volumeSurface216Principle.enabled`: `false` for now.

The Volume-Surface 216 Principle is an authorial mathematical-mythological concept: a 6x6x6 cube has volume 216, while six 6x6 faces also total 216 unit cells. It is documented as future symbolic game law, not as a factual physics claim.

## layerTurnProfile

Supported contract values:

- `disabled`: no Rubik layer turns.
- `ritualTurn`: `rotateLayer` is a legal action instead of a normal move.
- `globalEvent`: automatic layer rotations later.
- `sandbox`: UI/debug may rotate layers without normal turn semantics.

For ritual turns:

- axes: `X`, `Y`, `Z`;
- layers: `0..7`;
- quarter turns: `-1`, `+1`;
- action cost: `oneTurn`.

## turnProfile

Turn profile defines side order and action kinds:

- `singleSideLoop`: used by one-side tests/training.
- `roundRobin`: sides act in order.
- `roundRobinWithLayerActions`: sides can choose a piece move or a legal layer-turn action.

## victoryProfile

Supported contract values:

- `allPiecesAnchored`;
- `requiredPieceCount`;
- `kingOnly`;
- `percentageThreshold`;
- `checkmate`;
- `hybrid`;
- `sandbox`.

P2D implements a narrow subset at runtime: `centerAssembly` / `centerAssemblyTraining` can win through simple typed target-slot anchors when `victoryProfile.type` is `allPiecesAnchored`, `requiredPieceCount`, or `hybrid`. Other victory modes remain contracts for later stages.

## randomizationProfile

Supported contract values:

- `none`;
- `minorRandom`;
- `fullSymmetricRandom` later.

Randomized profiles must include a seed when used in a reproducible match.

## P2D Runtime Projection

`Chess3D_LoadRuleProfileJson` loads a selected RuleProfile into the engine. The engine stores profile summary fields, core cube bounds, anchor mode, and required anchor count. It also exposes append-only ABI getters so apps and tests can inspect the selected profile without parsing JSON themselves.

Target slots are computed from the P2A central 4x4 pattern projected onto the six core planes. Matching is type-based because the current board stores only side/type integer codes, not unique piece ids.

P2E replaces the purely single-cell anchor scan with a CoreCell stack overlay:

- the old board remains one projected integer per cell;
- stack-enabled core cells can store multiple side/type entries;
- a target is anchored when any stack entry has the matching side and piece type;
- overlapping target regions can now share a physical core cell at runtime;
- destructive fusion/implosion events, reserve restore actions, and ritual Rubik turns moving stacks remain later runtime work.

## P2F Runtime Fusion

P2F adds `CoreFusionState` descriptors over CoreCell stacks:

- friendly same-side pairs/stacks are counted per side;
- king+queen in one same-side core stack reports `royalPair`;
- multi-side stacks report `contested`;
- side implosion progress is `anchorCount + friendlyFusionCount + royalPairBonusCount`;
- default `allPiecesAnchored` victory remains compatible and is not replaced by fusion victory.

## P2G Runtime Knockback / Reserve

P2G adds profile-gated capture routing:

- classic and single-side profiles keep reserve/knockback disabled;
- Asgard and Rubik convergence profiles enable `knockbackCapture`;
- outer-field enemy captures route the captured piece to home or reserve;
- entering the Forbidden Core appends to stacks and does not knock back occupants;
- core-to-outside captures route the outside captured piece through the same home-or-reserve policy.
