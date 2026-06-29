# P4J Server Resume Match

Date: 2026-06-29

Scope: P4J Phase 08. This phase adds an append-only active-match resume hub path. It does not implement room/table/native-session rehydration after server restart, does not change Chess3D rules, and does not touch Hetzner network/service configuration.

## Hub Method

`Chess3DRelayHub` now exposes:

```text
RequestResumeMatch(OnlineProtocolMessage message)
```

The method consumes `message.ResumeRequest` and returns an `OnlineProtocolMessage` with:

- `Envelope.MessageType = ResumeMatchResult`;
- `ResumeResult.Success`;
- current authoritative `Snapshot` when resume succeeds;
- action-log tail from the requested `LastKnownServerSeq`;
- clear `FailureReason` when resume fails.

On success, the current SignalR connection is re-added to the room/table groups and the authenticated session last-known membership is updated.

## Supported Now

Active in-memory match resume:

1. The room/table exists in `OnlineRoomRegistry`.
2. The table is `InGame`.
3. The table still has an active native `OnlineGameSession`.
4. The authenticated/requesting player owns a seat at the table.
5. Optional ruleset hint matches the active table.

The method returns snapshot/action-log data without applying a move and without mutating board state.

## Failure Reasons

The active method can return:

- `tableNotFound`
- `playerNotInTable`
- `rulesetMismatch`
- `tableNotActive`
- `cannotResumeAfterServerRestartYet`

`cannotResumeAfterServerRestartYet` is reserved for the case where table metadata exists but the native runtime session is not active. Full server-restart restore still requires `RestoreRoomsOnStartup` rehydration.

## Diagnostics

Diagnostics now report:

```json
"resumeMatch": true
```

`OnlineDiagnostics.ResumeMatchSupported` is true, and `RequestResumeMatch` appears in `SupportedHubMethods`.

## Not Implemented Yet

- App-restart client UI resume button.
- Persisted room/table/native-session rehydration after server restart.
- Lobby-based resume discovery.
- Token persistence in the client.

Those are separate P4J phases and must keep token/password storage opt-in or absent.

## Verification

Phase 08 tests cover:

- active match resume returns snapshot and action-log tail;
- resume does not mutate state hash;
- wrong player/seat is rejected;
- missing table is rejected cleanly;
- diagnostics capability flips to supported.
