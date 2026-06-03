# Chess3D Online Security Baseline

P3E is a contract baseline, not a production security layer.

## Implemented Safety

- Server authority validates all actions.
- Duplicate seats are rejected.
- Wrong actor commands are rejected.
- Stale state hashes trigger resync.
- Malformed, wrong-protocol, unsupported-version, and oversized messages are rejected.
- Unknown future fields are tolerated without changing semantics.

## Not Implemented

- Production authentication.
- Public matchmaking.
- Rate limiting.
- Cryptographic anti-cheat.
- Server persistence.
- Replay tamper detection.
- Encrypted transport.

## Rule Safety

The authoritative server does not trust client move legality. It calls the same engine pathways as the UI and headless tests, preserving profile-specific Classic, Single-Side, Asgard, Rubik, and Hodge semantics.
