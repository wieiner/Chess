# P4G2 Server Legal Preview

Date: 2026-06-27

Scope: P4G2 Phase 04. This phase adds a server-authoritative, read-only legal preview path for the online client. It does not change Chess3D rules, add a profile, or mutate game state.

## Added Hub Method

`Chess3DRelayHub` now exposes:

```csharp
RequestLegalPreview(OnlineProtocolMessage message)
```

The method uses the same hub validation boundary as other table commands:

- protocol envelope validation;
- authentication validation when hosted auth is enabled;
- per-connection rate limiting;
- current session envelope normalization.

The result is sent only to the caller through:

```text
ReceiveLegalPreviewResult
```

Preview is not broadcast to the whole table. Accepted/rejected gameplay actions remain the only table-wide mutation events.

## Registry Flow

`OnlineRoomRegistry.RequestLegalPreview` checks:

- player is seated at the table;
- table is in game;
- session exists;
- requested actor matches the caller's seat or macro-player;
- optional expected state hash is current.

If the expected state hash is stale, the server returns `LegalPreviewResult` with:

- `IsStale = true`;
- `Error.ReasonCode = staleStateHash`;
- `Error.RequiresResync = true`;
- current authoritative `StateHash`;
- current `ServerSeq`.

This gives the UI enough information to request snapshot/action log without submitting a doomed action.

## Native Session Flow

`OnlineGameSession.BuildLegalPreview` wraps the existing native engine preview:

- `BuildLegalActionPreviewForCell`
- `GetLegalActionPreview`
- `GetPreviewEntryReason`

The resulting `OnlineLegalActionOption` maps native preview entries into online action kinds:

- native move/capture -> `NormalMove`;
- reserve restore -> `ReserveRestore`;
- layer turn -> `RubikLayerTurn`;
- projection composite -> `HodgeProjectedMove`.

The first online UI consumer should initially focus on normal move options. Special actions remain visible as structured options but can stay in dedicated mode panels until their UX is complete.

## No-Mutation Guarantee

Legal preview must not mutate:

- authoritative board;
- reserve/core/fusion state;
- action log;
- table server sequence;
- state hash.

The implementation checks the session state hash before and after preview. If preview ever mutates state, it clears options and returns an internal preview error requiring resync.

## Profile Boundary

Classic:

- normal move/capture preview is expected.

Single-Side:

- training legal move preview is expected.

Asgard:

- normal/core-aware preview can be returned when represented by native preview flags.

Rubik:

- Rubik layer turns are represented as special options only when available; normal move UI must not fake layer turns.

Hodge:

- projected moves are represented as special options only when available; normal move UI must not fake Hodge composite turns.

Exactly five Chess3D RuleProfiles remain in scope.

## Tests

`ChessOnlineContractTests` now verifies:

- Classic selected source returns a matching normal move option;
- Classic legal preview does not mutate state hash;
- stale expected hash returns explicit preview resync;
- empty source returns a no-action reason;
- Asgard selected source returns a normal move option;
- Asgard legal preview does not mutate state hash.

Future phases will add the client SDK method and WPF legal target highlights.
