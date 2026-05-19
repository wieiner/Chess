# Roadmap

## P0 - Repo, Build, Package Stabilization

- Keep GitHub import reproducible.
- Ensure normal `Release|x64` builds without CUDA Toolkit MSBuild integration.
- Keep `rude-resource/` ignored and document asset boundaries.
- Keep release packaging centralized and repeatable.

## P1 - Tests and Rule Contracts

- Add smoke tests around native exports.
- Add rule contract tests for 2D legal moves, draw rules, FEN, and search telemetry.
- Add package verification to CI once the Windows build agent is ready.

## P2 - Formal 3D Chess Rules

- Replace draft movement semantics with explicit, versioned rule contracts.
- Define six-side king safety, check/mate semantics, pawn rules, and turn order.
- Keep rules data-driven where practical.

## P3 - Online Relay Server for `chess3d.relay.v1`

- Build the hosted room/relay service for 3D chess.
- Support six clients per table and bridge groups between six-player tables.
- Formalize sync, move, rotate, chat, reconnect, and authority messages.

## P4 - Asset Pipeline

- Keep OBJ loading now.
- Add future glTF/GLB support.
- Add manifests, validation, scale/origin checks, and missing-asset reports.

## P5 - GPU Benchmark, Parity, Frontier Evaluation

- Expand CPU/Direct3D/CUDA benchmarks.
- Add correctness parity checks across backends.
- Identify where GPU batching really wins and where CPU remains better.

## P6 - Release Packaging and GitHub Actions

- Add GitHub Actions for Windows build verification.
- Produce zipped portable artifacts.
- Optionally add installer packaging later.
