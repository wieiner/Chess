# P4G2 Client Legal Preview

Date: 2026-06-27

Scope: P4G2 Phase 05. This phase adds client SDK support for requesting legal preview from the online server. It does not yet wire WPF board clicks to the preview method.

## Relay Client Method

`ChessOnlineRelayClient` now exposes:

```csharp
RequestLegalPreviewAsync(
    string clientId,
    string roomId,
    string tableId,
    OnlineLegalPreviewRequest request,
    CancellationToken cancellationToken = default)
```

The method:

- creates a `RequestLegalPreview` protocol message;
- fills room/table/player ids when they are missing from the request;
- invokes the server `RequestLegalPreview` hub method;
- stores the latest response in `LastLegalPreview`;
- receives pushed caller-only responses through `ReceiveLegalPreviewResult`.

## Client View State

`OnlineLegalPreviewState.cs` adds WPF-friendly client models:

- `LegalPreviewState`
- `LegalActionOptionViewModel`
- `LegalTargetMarker`

These models convert raw protocol DTOs into:

- target markers for board highlighting;
- display labels;
- stale/rejection reason text;
- an exact `OnlineActionCommand` that can later be submitted when the user clicks a legal target.

## Logging Boundary

Preview client logs use the existing `ChessOnlineClientEventLog` path. The event label contains:

- method/event label;
- message type;
- server sequence;
- error reason.

It does not log access tokens, refresh tokens, passwords, or auth headers.

## Tests

`ChessOnlineContractTests` covers:

- relay event list includes `ReceiveLegalPreviewResult`;
- legal preview state builds target markers;
- legal preview option maps to a submit command;
- stale preview state surfaces the server reason.

Network invocation is not exercised in CI. Remote Hetzner smoke remains manual/operator gated.

## Next Step

Phase 06 wires selected source cells in `ChessOnlineApp` to `RequestLegalPreviewAsync` and renders legal target highlights on the online board.
