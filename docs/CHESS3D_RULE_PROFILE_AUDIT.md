# Chess3D Rule Profile Audit

P2B audit target: configurable Chess3D rule profiles for classic six-side chess, Asgard/Meru convergence, and Rubik layer-turn variants.

## Existing Rules JSON

Runtime rules currently exist under:

- `src/ChessApp/Assets/Rules3D/cube8x8x8_draft.json`
- `src/ChessApp/Assets/Rules3D/single_side_3d_chess_8x8x8_v0_1.json`

P2B adds data/spec profiles under:

- `assets/rules/profiles/*.json`

The `src/ChessApp/Assets/Rules3D` files are runtime assets copied by `Chess3DApp`. The root `assets/rules/profiles` files are design/contract assets used by tests and documentation, not yet full runtime behavior definitions.

## Hardcoded Today

The native `Chess3DEngine` still hardcodes several behaviors:

- board dimensions are fixed at 8x8x8;
- board indexing is `z * 64 + y * 8 + x`;
- piece codes are `side * 10 + type`;
- classic piece type ids are fixed;
- face-centered setup is generated procedurally;
- movement vectors are implemented in C++;
- captures are classic remove-from-board captures;
- pawn promotion defaults to queen;
- turn order cycles through active side ids;
- `RotateLayer` is always exposed as a callable board transform.

## Configurable Today

The existing tolerant JSON loader can read:

- width/height/depth, clamped to the fixed 8x8x8 engine maximum;
- `activeSideCount`;
- `maxPiecesPerSide`;
- `movementProfile` as setup-only vs draft movement;
- `kingSafety`;
- side forward vectors.

Unknown metadata fields are ignored, which makes profile JSON safe to introduce as documentation and future runtime contracts.

## Not Configurable Yet

The following are not implemented as runtime switches yet:

- goal profile;
- victory detection profile;
- center-core anchoring state;
- target-slot derivation;
- knockback/reserve capture;
- layer turns as legal turn actions;
- randomization profile;
- JSON-driven setup list parsing;
- custom/fairy piece movement.

## Goal Location

There is no formal goal/victory engine for Chess3D yet. The existing engine can generate moves, make moves, rotate layers, and run a shallow material/center evaluation, but it does not determine final 3D victory conditions.

P2B therefore treats `goalProfile` and `victoryProfile` as contract data, not completed gameplay logic.

## Movement Location

Movement lives in `src/Chess3DEngine/Chess3DEngine.cpp`:

- rook, bishop/officer, queen, and king directions are generated from 3D direction components;
- knight directions are 3D L vectors;
- pawn movement uses each side's forward vector;
- line-piece blocking is enforced in move generation.

P2B does not rewrite movement. It documents movement profile names and keeps P2A contract tests as the executable proof for the current local rule core.

## Rubik / Layer-Turn Location

Layer rotation lives in `Chess3D_RotateLayer` and internal `rotateLayer` helpers. The WPF UI exposes it as debug/sandbox Rubik controls, and network messages can broadcast `rotate3d`.

Today a layer turn is a direct board transform, not a legal chess action governed by turn cost, check interaction, or anchor rules. P2B makes this distinction explicit through `layerTurnProfile`.

## Safe P2B Changes

Safe changes for this stage:

- add profile JSON files under `assets/rules/profiles`;
- add a lightweight schema/data contract;
- validate profile files in headless contract tests;
- document Asgard/Meru convergence and Rubik layer-turn semantics;
- update roadmap/status/architecture/testing docs.

Risky changes intentionally deferred:

- implementing centerAssembly victory;
- implementing anchor state;
- implementing knockback/reserve;
- making layer turns legal actions;
- changing existing public ABI;
- changing Chess2D, RubikApp, OnlineApp, or CUDA requirements.
