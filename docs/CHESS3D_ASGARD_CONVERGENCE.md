# Chess3D Asgard / Meru Convergence

Ruleset id: `asgard-convergence-3d-8x8x8-v0.1`

## Idea

At the center of the 8x8x8 cube is a forbidden city: Asgard, Meru, or the sacred central core. Six sides begin on the six faces of the cube and converge toward that center.

The goal is not necessarily checkmate. In Convergence mode, players try to bring pieces into central target slots and anchor them into a central formation.

## Myth Profile

The profile uses:

- theme: `asgard-meru`;
- center name: `Forbidden Core / Asgard / Meru`;
- six gate names for the six cube faces.

This is narrative/visual metadata. It must not affect headless tests by itself.

## Gameplay Profiles

Gameplay comes from these profiles:

- `goalProfile.type = centerAssembly`;
- `coreProfile.type = asgardMeruCore`;
- `captureProfile.type = knockbackCapture`;
- `knockbackProfile.type = homeOrReserve`;
- `reserveProfile.type = sidePieceTypeCounts`;
- `occupancyProfile.type = coreStack`;
- `fusionProfile.type = stackFusion`;
- `corePhysicsProfile.type = asgardCorePhysics`;
- `victoryProfile.type = allPiecesAnchored`;
- `layerTurnProfile.type = disabled` in the base Asgard profile.
- Rubik-style ritual turns are implemented only in the separate Rubik convergence profile.

## Core Cube

The sacred center is:

- `x = 2..5`
- `y = 2..5`
- `z = 2..5`

This 4x4x4 volume is the `coreCube`.

## Core Physics

Outside the core, normal chess occupancy applies: one cell contains at most one piece.

Inside the Forbidden Core, Asgard profiles allow `coreStack` model:

- multiple pieces may share one core-cell;
- ordinary capture in the core can be disabled;
- friendly co-occupancy may form assembly/fusion progress;
- enemy co-occupancy may become contested fusion;
- P2F reports friendly, royal, and contested fusion descriptors;
- resonance, color/permutation, destructive implosion, and dislodging ideas are reserved for later rules.

The runtime board still stores one projected integer per cell for compatibility, but stack/fusion state lives beside it.

## Volume-Surface 216 Principle

The Volume-Surface 216 Principle is recorded as an authorial mathematical-mythological concept:

- a conceptual 6x6x6 cube has volume 216;
- six 6x6 faces also total 216 unit cells;
- future rules may use this as symbolic surface/volume balance.

It is disabled in the current profile and is not asserted as physical fact.

## Target Slots

Target slots are derived from each side's home-face projection into CoreCube `2..5`.

P2D implements this as a typed runtime projection:

- each side has 16 logical target slots;
- the P2A 4x4 pattern is projected onto that side's core plane;
- slots match by side and piece type, not by unique piece id;
- physical target cells can overlap between sides.

P2E adds CoreCell stacks inside the Forbidden Core, so overlapping target regions can now contain multiple entries at runtime. The old board projection still shows only the top entry.

## Anchoring

Initial anchor mode is `softAnchor`:

- a piece that reaches a valid target slot can become anchored;
- anchored pieces are intended to become part of the central formation;
- contested anchors are deferred.

P2E makes anchors stack-aware. A target slot is anchored when any entry in that core-cell stack has the expected side and type. P2F adds fusion descriptors over the same stack. If a matching target entry shares a friendly fusion cell, the descriptor can carry anchored-fusion and implosion-seed flags.

## Knockback Capture

Convergence uses `knockbackCapture`:

- captured piece returns to the first matching free home slot if possible;
- otherwise it goes to reserve;
- P2I can restore reserve pieces to matching free home slots.

P2G implements runtimePartial knockback/reserve for ordinary outer-field captures and core-to-outside captures. Entering the Forbidden Core does not knock back occupants; it creates stack co-occupancy and possible contested fusion state. P2I records these actions in unified action history and adds reserve restore notation.

## Implemented Now

Implemented now:

- profile JSON contract;
- schema/data documentation;
- headless validation tests for profile files;
- occupancy/core-physics fields and runtimePartial fusion profile data;
- runtime RuleProfile loading and profile summary ABI;
- typed target-slot projection for sides 1..6;
- CoreCell stack runtime in the Forbidden Core;
- stack-aware anchor count and centerAssembly victory;
- fusion descriptors for single, friendlyPair, friendlyStack, royalPair, and contested cells;
- implosion progress state;
- knockback/home-or-reserve capture routing for ordinary outer-field captures;
- action history and deterministic notation for moves, captures, layer turns, and reserve restores;
- reserve restore action to matching free home slots;
- P2K Chess3D control-center visibility for selected core cell, stack count, projected piece, fusion kind, contested state, anchor count, reserve total, last capture destination, and auto restore;
- P2A movement/setup contracts remain executable through `Chess3DEngineContractTests`.

Deferred:

- destructive fusion/implosion event model;
- contested anchor scoring/dislodging;
- rich reserve inventory UI, restore into core, and restore captures;
- Rubik layer-turn animation/full replay/online serialization;
- checkmate/hybrid hardening.
## P2L Playability Note

Asgard remains one profile, not the default meaning of all Chess3D. In P2L the UI exposes Asgard-specific core stack, fusion, anchor, reserve, and invalid-reason state only when the active RuleProfile enables those capabilities. Classic, Single-Side, Rubik, and Hodge remain separate modes.

## P2O Rule-Gate Note

P2O keeps Asgard's outcome tied to `centerAssembly`/anchor completion, not Classic checkmate. The rule summary reports king safety as not applicable/deferred for this profile. Action perft/divide can enumerate shallow Asgard legal actions for diagnostics, including profile-enabled reserve restore candidates, but it does not implement destructive implosion or contested-anchor scoring.
