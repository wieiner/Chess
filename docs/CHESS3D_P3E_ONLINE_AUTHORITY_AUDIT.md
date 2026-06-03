# Chess3D P3E Online Authority Audit

P3E adds a reproducible online authority contract without changing any Chess3D rules.

## Existing Boundary

- `Chess3DEngine.dll` remains the source of truth for profiles, legal actions, action history, save/load/replay, state hash, AI candidates, and outcomes.
- `ChessOnlineApp.exe` was previously an integrations shell. It now has a local authority panel for protocol smoke testing.
- `src/ChessOnlineProtocol` is a managed protocol/domain layer that hosts an in-process authoritative room/table registry.

## Five Profiles

The online catalog exposes exactly the same five Chess3D RuleProfiles:

- `classic-six-side-3d-8x8x8-v0.1`
- `single-side-3d-8x8x8-v0.1`
- `asgard-convergence-3d-8x8x8-v0.1`
- `rubik-convergence-3d-8x8x8-v0.1`
- `hodge-projection-duel-3d-8x8x8-v0.1`

Scenario, playthrough, regression, and online fixture JSON files are not game modes.

## Safe P3E Scope

Implemented:

- JSON protocol envelope and DTOs.
- In-process room/table/seat state.
- Server-authoritative action validation through the existing engine entry points.
- Snapshot/resync and action-log chunks.
- Online contract tests and fixture JSON.
- Minimal ChessOnlineApp UI panel for local authority exercises.

Deferred:

- Hosted SignalR service.
- Production authentication and accounts.
- Public matchmaking.
- Database persistence.
- Cryptographic anti-cheat.
- Binary/UDP protocol.
- Online serialization in the native ABI.

## Authority Risks

The client must not decide legality. P3E treats client commands as requests and applies them only after the authoritative registry verifies room/table/seat, actor, profile, state hash, and engine legality. Failed commands do not mutate board, history, reserve, stacks, fusion, replay cursor, or server sequence.
