# Chess3D SignalR Security Baseline

P3F is a local hosted prototype, not a production security system.

## Implemented

- Server-authoritative action validation through the P3E registry.
- Local CORS policy for development origins.
- SignalR detailed errors disabled by default.
- Configurable maximum receive message size.
- Lightweight per-connection command throttling.
- Diagnostics avoid exposing session tokens.
- Wrong protocol, unsupported version, unknown message type, wrong actor, duplicate seat, stale hash, and malformed commands reject cleanly.

## Not Implemented

- Production authentication.
- Account identity.
- Public matchmaking.
- Durable session or room persistence.
- Redis/Azure SignalR backplane.
- Complete anti-cheat.
- Encrypted transport policy enforcement.
- Public internet hardening.

## Rule Safety

Transport safety does not replace rule safety. The registry and engine remain authoritative for Classic, Single-Side, Asgard, Rubik, and Hodge semantics.

