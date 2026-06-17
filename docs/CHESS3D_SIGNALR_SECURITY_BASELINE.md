# Chess3D SignalR Security Baseline

P3F is a local hosted prototype. P4A adds a production-oriented local identity/session/persistence baseline, but the server is still not a public production service.

## Implemented

- Server-authoritative action validation through the P3E registry.
- Local CORS policy for development origins.
- SignalR detailed errors disabled by default.
- Configurable maximum receive message size.
- Lightweight per-connection command throttling.
- Diagnostics avoid exposing session tokens.
- Wrong protocol, unsupported version, unknown message type, wrong actor, duplicate seat, stale hash, and malformed commands reject cleanly.
- P4A optional authenticated mode using hashed passwords, Data Protection protected tokens, server-derived player identity, and durable local sessions.
- P4A persistence baseline for rooms, tables, seats, and accepted action logs.

## Not Implemented

- Public matchmaking.
- Redis/Azure SignalR backplane.
- Complete anti-cheat.
- Encrypted transport policy enforcement.
- Public internet hardening.
- OAuth/external login and real email confirmation.

## Rule Safety

Transport safety does not replace rule safety. The registry and engine remain authoritative for Classic, Single-Side, Asgard, Rubik, and Hodge semantics.
