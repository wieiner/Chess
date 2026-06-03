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

The current implementation is in-process. A SignalR hub can fan out the same DTOs later, but SignalR groups must not become rule authority. The authoritative registry remains the validator.
