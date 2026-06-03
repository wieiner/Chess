# Chess3D Online Security Baseline

P3E is a contract baseline, not a production security layer.

## Implemented Safety

- Server authority validates all actions.
- Duplicate seats are rejected.
- Wrong actor commands are rejected.
- Stale state hashes trigger resync.
- Malformed, wrong-protocol, unsupported-version, and oversized messages are rejected.
- Unknown future fields are tolerated without changing semantics.
- P3F SignalR detailed errors are disabled by default.
- P3F diagnostics avoid session-token exposure.
- P3F adds local-dev CORS and lightweight command throttling.

## Not Implemented

- Production authentication.
- Public matchmaking.
- Production-grade rate limiting.
- Cryptographic anti-cheat.
- Server persistence.
- Replay tamper detection.
- Encrypted transport.
- Redis/Azure SignalR backplane.

## Rule Safety

The authoritative server does not trust client move legality. It calls the same engine pathways as the UI and headless tests, preserving profile-specific Classic, Single-Side, Asgard, Rubik, and Hodge semantics.
