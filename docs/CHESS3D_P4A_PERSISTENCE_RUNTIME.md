# Chess3D P4A Persistence Runtime

`ChessOnlinePersistence` adds provider-style interfaces:

- `IOnlineIdentityStore`
- `IOnlineSessionStore`
- `IOnlineRoomPersistenceStore`

The P4A implementation is `JsonOnlineStore`, a local atomic JSON document store under `%LOCALAPPDATA%\Chess3D\online-dev` by default.

Persisted records:

- player accounts;
- durable sessions;
- rooms;
- tables;
- seats;
- accepted action log events.

Only accepted authoritative results are persisted. Rejected commands do not become action-log truth. The P3E authority registry remains the runtime source of truth for rules and action validation.

Action log persistence includes a simple SHA-256 event hash chained from the previous event for the same persisted table key. This is a diagnostic integrity fingerprint, not a cryptographic anti-cheat guarantee.

Generated stores and Data Protection keys are runtime artifacts and must not be committed or shipped in portable `ProductionOutput`.
