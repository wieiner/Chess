# P4G2 Legal Preview DTOs

Date: 2026-06-27

Scope: P4G2 Phase 03. This document records the append-only protocol DTOs added for online legal preview. It does not add a hub method yet and does not change game rules.

## Message Types

Two message types were added to `OnlineMessageTypes`:

- `RequestLegalPreview`
- `LegalPreviewResult`

Both are append-only. Existing clients that do not know about legal preview can continue using `SubmitAction`, `RequestSnapshot`, and `RequestActionLog`.

## OnlineProtocolMessage Payloads

Two nullable payload properties were added:

- `LegalPreviewRequest`
- `LegalPreview`

They are optional and do not affect existing messages.

## Request DTO

`OnlineLegalPreviewRequest` contains:

- `PlayerId`
- `RoomId`
- `TableId`
- `SourceX`, `SourceY`, `SourceZ`
- `ActorSide`
- `MacroPlayer`
- `ExpectedStateHash`
- `ActionKindFilter`

The envelope remains the source of client/session identity. The request fields exist so UI tools, logs, and tests can inspect the requested source cell without unpacking every envelope.

## Result DTO

`OnlineLegalPreviewResult` contains:

- `RoomId`
- `TableId`
- `RulesetId`
- `StateHash`
- `ServerSeq`
- source coordinates;
- actor side / macro-player;
- `IsStale`
- `NoLegalActionReason`
- `Options`
- optional `Error`

`ServerSeq` and `StateHash` are diagnostic and resync hints. A legal preview response must not advance the authoritative game state.

## Option DTO

`OnlineLegalActionOption` can represent:

- normal moves;
- captures;
- reserve restore;
- Rubik layer turn;
- Hodge projected move;
- future explicitly-supported action kinds.

Fields include:

- `ActionKind`
- `ActorSide`
- `MacroPlayer`
- `From`
- `To`
- `PromotionType`
- reserve fields: `Side`, `PieceType`, `ReserveTarget`
- Rubik fields: `Axis`, `Layer`, `QuarterTurns`
- Hodge field: `PrimarySide`
- UI text: `Notation`, `DisplayLabel`, `Reason`, `Capability`
- native preview data: `Flags`, `PieceCode`, `CapturedPieceCode`, `ReasonCode`
- booleans: `IsCapture`, `IsSpecial`, `IsRecommendedSafeTestAction`

The UI should dispatch the exact selected option through `SubmitAction`. Preview is not permission to mutate state by itself.

## Target DTO

`OnlineLegalTarget` is a simple coordinate object:

- `X`
- `Y`
- `Z`

It is used for move sources/targets and reserve targets to keep the preview option readable in JSON.

## Error DTO

`OnlineLegalPreviewError` contains:

- `ReasonCode`
- `ReasonText`
- `RequiresResync`

Errors are shaped for UI display and resync decisions. Raw exceptions, tokens, and secrets must not be placed in this DTO.

## Security Boundary

The legal preview payload deliberately excludes:

- access tokens;
- refresh tokens;
- passwords;
- auth headers;
- private keys;
- runtime keyrings or stores.

Public HTTP 80 remains diagnostic/dev only. Temporary users are still the only acceptable remote smoke accounts until TLS/domain work is handled separately.

## Tests

`ChessOnlineContractTests` now covers:

- `RequestLegalPreview` JSON roundtrip;
- empty legal preview result;
- normal move option serialization;
- Rubik layer-turn option serialization;
- Hodge projected-move option serialization;
- reserve-restore option serialization.

Future phases will add server no-mutation and UI highlight tests after the hub method exists.
