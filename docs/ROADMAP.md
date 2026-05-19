# Roadmap

## P0 - Repo, Build, Package Stabilization

- Keep GitHub import reproducible.
- Ensure normal `Release|x64` builds without CUDA Toolkit MSBuild integration.
- Keep `rude-resource/` ignored and document asset boundaries.
- Keep release packaging centralized and repeatable.

## P1 - Tests and Rule Contracts

- Native contract smoke tests now cover ChessEngine, Chess3DEngine, RubikEngine, and ChessGpuBackend.
- `tests/run-tests.ps1` builds and runs the contract suite plus `Chess2DBenchmark --quick`.
- `scripts/verify.ps1` now includes contract tests after packaging.
- Next P1 work: expand assertions into deeper rule-contract suites and add UI smoke automation.

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

- Keep GitHub Actions Windows build verification green.
- Produce zipped portable artifacts.
- Optionally add installer packaging later.
