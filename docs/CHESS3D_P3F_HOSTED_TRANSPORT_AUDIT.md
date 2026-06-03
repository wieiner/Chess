# Chess3D P3F Hosted Transport Audit

P3F starts from the P3E authority contract and adds a hosted local SignalR transport. It does not add a RuleProfile or change profile rules.

## Current Boundary

- `src/ChessOnlineProtocol` owns protocol DTOs, JSON envelope validation, rooms, tables, seats, authoritative game sessions, snapshots, action logs, and diagnostics.
- `OnlineRoomRegistry` remains the source of truth for room/table membership and action authority.
- `ChessOnlineApp` already had a local P3E authority panel, but no hosted transport.
- `tests/ChessOnlineContractTests` covered the in-process authority layer.
- `assets/rules/scenarios/chess3d/online` covered P3E regression descriptors.

## Safe P3F Additions

- Add `src/ChessOnlineServer` as a local ASP.NET Core/SignalR host.
- Fan out the existing P3E DTOs through `/chess3d/relay`.
- Keep SignalR groups as delivery helpers only.
- Keep authorization and durable state in `OnlineRoomRegistry`.
- Add a local-dev session token for reconnect smoke tests.
- Add hosted diagnostics without exposing session tokens.
- Add `ChessOnlineSignalRContractTests` with in-process Kestrel startup and clean shutdown.

## Risks

- SignalR connection id and group membership are not durable authority.
- Session token is a development reconnect token, not production auth.
- Hosted tests must not leave background server processes.
- Large transport messages can be rejected by SignalR before hub method dispatch.
- Registry concurrency is currently protected by coarse locks, which is correct for P3F but not a high-scale design.

## Non-Goals

- Production authentication.
- Public matchmaking.
- Database persistence.
- Redis backplane or Azure SignalR.
- UDP or binary protocol.
- Complete anti-cheat.
- Online rule changes.

