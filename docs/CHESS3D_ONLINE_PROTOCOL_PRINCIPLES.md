# Chess3D Online Protocol Principles

Protocol id: `chess3d.relay.v1`

Protocol version: `0.1`

P3E is a JSON relay contract. It is meant to be readable, deterministic, and easy to replay in tests. It is not a high-performance wire format.

## Principles

- Server authority is mandatory.
- The client sends commands, never authoritative board state.
- The server validates every command through `Chess3DEngine`.
- Every accepted action receives a monotonic `serverSeq`.
- Snapshots contain authoritative savegame JSON and state hash.
- Stale clients receive `ResyncRequired`.
- Unknown future JSON fields are tolerated.
- Unknown message types, wrong protocol ids, unsupported versions, malformed JSON, and oversized messages are rejected cleanly.

## Message Size

`MaxMessageBytes` is `65536` for P3E contract tests. Larger messages are rejected before dispatch.

## Transport

P3E introduced the in-process authority contract. P3F adds a hosted local SignalR prototype over the same DTOs and registry.

SignalR is only transport/fanout. SignalR groups are not authorization, not durable room state, and not the source of truth. `OnlineRoomRegistry` remains the validator for seats, ready/start, actions, snapshots, resync, and action logs.

P3F is still not public production multiplayer. Production auth, matchmaking, persistence, complete anti-cheat, Redis/Azure SignalR backplane, and binary/UDP protocols remain future work.
