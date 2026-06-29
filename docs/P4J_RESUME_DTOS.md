# P4J Resume DTOs

Date: 2026-06-29

Scope: P4J Phase 07. This phase adds append-only protocol DTOs and diagnostics capability reporting for future online match resume. It does not add the server hub method yet and does not claim resume is playable.

## Added DTOs

`OnlineResumeRequest`

- `PlayerId`
- `RoomId`
- `TableId`
- `SeatIndex`
- `ExpectedRulesetId`
- `LastKnownStateHash`
- `LastKnownServerSeq`

`OnlineResumeResult`

- `Success`
- `FailureReason`
- `FailureText`
- `RoomId`
- `TableId`
- `SeatIndex`
- `RulesetId`
- optional `Snapshot`
- optional `ActionLog`
- `Candidates`

`OnlineResumeCandidate`

- `RoomId`
- `TableId`
- `SeatIndex`
- `RulesetId`
- `StateHash`
- `ServerSeq`
- `TableState`
- `UpdatedAtUtc`

`OnlineResumeFailureReasons`

- `none`
- `notAuthenticated`
- `tableNotFound`
- `playerNotInTable`
- `rulesetMismatch`
- `staleState`
- `tableNotActive`
- `cannotResumeAfterServerRestartYet`

## Message Constants

Two message constants are reserved for Phase 08:

- `RequestResumeMatch`
- `ResumeMatchResult`

They are accepted by the protocol JSON validator so future clients and tests can roundtrip payloads, but the server hub method is intentionally not listed in `SupportedHubMethods` yet.

## Diagnostics

`OnlineDiagnostics` now includes:

```json
"resumeMatchSupported": false
```

The HTTP diagnostics endpoint exposes:

```json
"resumeMatch": false
```

This is intentionally false until `Chess3DRelayHub.RequestResumeMatch` is implemented. Clients should not enable resume UI as a working server feature until the flag becomes true.

## Security Boundary

Resume DTOs do not contain:

- access tokens;
- refresh tokens;
- passwords;
- Authorization headers.

Future client-side resume context should persist only non-secret metadata such as room/table/ruleset/seat/hash/seq.

## Verification

Phase 07 verifies:

- resume request JSON roundtrip;
- resume result JSON roundtrip with a deferred failure reason;
- no token field names in serialized resume DTOs;
- diagnostics exposes `ResumeMatchSupported == false`;
- diagnostics does not list `RequestResumeMatch` before the hub method exists.
