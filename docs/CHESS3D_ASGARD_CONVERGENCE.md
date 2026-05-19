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
- `occupancyProfile.type = coreStack`;
- `fusionProfile.type = stackFusion`;
- `corePhysicsProfile.type = asgardCorePhysics`;
- `victoryProfile.type = allPiecesAnchored`;
- `layerTurnProfile.type = disabled` in the base Asgard profile.

## Core Cube

The sacred center is:

- `x = 2..5`
- `y = 2..5`
- `z = 2..5`

This 4x4x4 volume is the `coreCube`.

## Core Physics

Outside the core, normal chess occupancy applies: one cell contains at most one piece.

Inside the Forbidden Core, Asgard profiles allow a future `coreStack` model:

- multiple pieces may share one core-cell;
- ordinary capture in the core can be disabled;
- friendly co-occupancy may form assembly/fusion progress;
- enemy co-occupancy may become contested fusion;
- stack, resonance, color/permutation, and implosion ideas are reserved for later rules.

P2C documents this as profile data only. The runtime board still stores one integer piece per cell.

## Volume-Surface 216 Principle

The Volume-Surface 216 Principle is recorded as an authorial mathematical-mythological concept:

- a conceptual 6x6x6 cube has volume 216;
- six 6x6 faces also total 216 unit cells;
- future rules may use this as symbolic surface/volume balance.

It is disabled in the current profile and is not asserted as physical fact.

## Target Slots

Target slots are currently specified as derived from each side's home-face projection. The exact six-side target projection is draft and will be formalized in P2C/P3.

## Anchoring

Initial anchor mode is `softAnchor`:

- a piece that reaches a valid target slot can become anchored;
- anchored pieces are intended to become part of the central formation;
- contested anchors are deferred.

Runtime anchor state is not implemented in P2B.

## Knockback Capture

Convergence uses `knockbackCapture`:

- captured piece returns to its home slot if that slot is free;
- otherwise it goes to reserve;
- reserve restore is deferred.

Runtime knockback/reserve is not implemented in P2B.

## Implemented Now

Implemented now:

- profile JSON contract;
- schema/data documentation;
- headless validation tests for profile files;
- occupancy/fusion/core-physics fields as `specOnly`;
- P2A movement/setup contracts remain executable through `Chess3DEngineContractTests`.

Deferred:

- runtime centerAssembly victory;
- runtime core multi-occupancy;
- fusion entity model;
- anchor state;
- target-slot projection;
- knockback/reserve behavior;
- checkmate/hybrid hardening.
