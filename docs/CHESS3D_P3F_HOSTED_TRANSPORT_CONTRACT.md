# Chess3D P3F Hosted Transport Contract

P3F is a hosted transport prototype for `chess3d.relay.v1` / `0.1`.

## Contract

- The hosted server accepts the same protocol envelope used by P3E.
- The hub returns `OnlineProtocolMessage` responses and emits named SignalR events.
- Every accepted gameplay action goes through `OnlineRoomRegistry`.
- The registry calls the existing Chess3D engine action paths.
- State hashes, snapshots, resync, and action-log chunks keep their P3E meaning.
- No client message can authoritatively replace board state.

## Server

Project:

```text
src/ChessOnlineServer
```

Default hub:

```text
/chess3d/relay
```

Health/diagnostic endpoints:

```text
/healthz/live
/healthz/ready
/chess3d/diagnostics
```

Local run example:

```powershell
.\ChessOnlineServer.exe --urls http://127.0.0.1:5077
```

## Profile Scope

The five real Chess3D RuleProfiles remain:

- Classic Six-Side
- Single-Side Training
- Asgard / Meru Convergence
- Rubik Convergence
- Hodge Projection Duel

P3F adds no sixth mode.

