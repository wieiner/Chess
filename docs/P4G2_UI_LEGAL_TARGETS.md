# P4G2 UI Legal Target Highlights

Date: 2026-06-27

Scope: P4G2 Phase 06. This phase wires the online board source-cell click path to the server legal preview API and renders legal target markers in `ChessOnlineApp`.

## UI Behavior

In the P4G online board panel:

1. The user requests or receives a snapshot.
2. The 8x8 selected Z-layer grid renders projected top pieces.
3. Clicking an occupied cell automatically treats it as the source.
4. The client calls `RequestLegalPreviewAsync`.
5. The server returns authoritative legal preview options.
6. The board highlights legal targets.
7. The preview option list shows the returned action labels.

The old manual buttons remain:

- `Use Selected as From`
- `Use Selected as To`
- `Submit Normal Move`

They are kept as an advanced fallback until Phase 07 replaces rough From/To flow with exact preview-option dispatch.

## Visual Markers

The current compact board grid uses these colors:

- source/from: green;
- selected cell: blue;
- selected target/to: amber;
- legal move target: blue;
- capture target: orange/red;
- special action target: purple.

The board status line shows the current legal target count.

## Stale / Resync Behavior

If the preview response reports a stale hash:

- the UI shows the stale reason;
- the client requests a fresh authoritative snapshot;
- local preview markers are cleared when the snapshot hash changes.

Preview does not mutate the local board. The board remains authoritative-server driven.

## Boundaries

This phase does not:

- submit actions by clicking a target;
- replace the fallback manual From/To flow;
- implement full special-action UI for Rubik/Hodge/reserve;
- change any Chess3D rule profile.

Those are Phase 07 and later.

## Verification

Verification for this phase:

- `dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64`
- `ChessOnlineContractTests` still pass through the decomposed runner.

Remote Hetzner UI smoke remains manual/operator and is not a CI step.
