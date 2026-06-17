# Chess3D P4A Identity / Persistence Audit

P4A starts from P3F commit `6e20cdd`, where the hosted SignalR server was a local transport prototype over the P3E authority registry.

## Current P3F Boundary

`Chess3DRelayHub.Hello` accepted a client-supplied `clientId`, optional `playerId`, and dev `sessionToken`. `OnlineHubConnectionRegistry` created `player-{guid}` when no player was supplied, generated an in-memory `SessionToken`, and allowed reconnect if the token was still present in the process.

The SignalR `connectionId` was transport state only, but the reconnect path could still be confused with identity because the in-memory registry trusted envelope player/session claims in development flow.

## Volatile State Before P4A

Server restart lost:

- player/session tokens;
- room membership;
- table/seat assignment;
- action log and server sequence;
- reconnect context.

The P3E `OnlineRoomRegistry` remains the source of truth for game rules and authoritative actions. P4A must not move legal move validation, save/load/replay, or state hash decisions into persistence.

## Authentication Boundary

Anonymous methods that can remain local/dev-only:

- `Ping`;
- diagnostics;
- `Hello` only when explicit development anonymous sessions are enabled.

Production-like mode must authenticate mutating hub commands:

- room/table creation and joins;
- ready/start;
- submit action;
- snapshot/action-log requests tied to table membership.

## Persistent Data Needed

P4A persists:

- player account metadata and password hash;
- durable sessions with refresh-token hash and last-known room/table;
- room/table/seat metadata;
- accepted authoritative action log events.

P4A does not persist client-claimed actions as truth. Only successful authoritative registry results are mirrored.

## Risks

- client-supplied `playerId` must not override authenticated player identity;
- diagnostics must not expose tokens, password hashes, or key material;
- local JSON store is a baseline provider, not cloud production storage;
- Data Protection key rings must be operator-managed and are not committed.

## Out of Scope

No OAuth, public matchmaking, email confirmation, payment/account economy, Redis/Azure SignalR backplane, full anti-cheat, binary protocol, cloud deployment, or new Chess3D RuleProfile is introduced in P4A.
