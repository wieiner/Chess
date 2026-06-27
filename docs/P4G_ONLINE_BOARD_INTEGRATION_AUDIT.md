# P4G Online Board Integration Audit

Date: 2026-06-27

Scope: P4G audits how to turn the P4F online snapshot/action viewer into a realtime, clickable Chess3D online board over the existing public HTTP 80 diagnostic deployment. This phase does not change server deployment, TLS, rules, native ABI, or the five existing Chess3D RuleProfiles.

## Existing Local Chess3D Board

The richest local board implementation is `src\ChessApp\Chess3DWindow.xaml.cs`.

- `BuildLayerGrid()` creates the 8x8 selected-layer button grid in `LayerGrid`.
- `RefreshLayer()` reads the native engine board state, selected cell, legal preview entries, stack/fusion/core metadata, and writes the cell button content/background.
- `RefreshPreview3D()` rebuilds the 3D `Viewport3D` view and visual overlays from the same local engine state.
- `Cell_Click()` implements the 2D layer click flow: first click selects a source cell, second click can dispatch a legal preview target.
- `Preview3D_MouseDown/Move/Up` and `TryPickSquare()` implement 3D hit-test to logical board coordinates.
- `SelectedPreviewEntries()` calls `BuildLegalActionPreviewForCell(...)` against the local native engine.
- `TryApplySelectedAction(...)` matches the target against preview entries before dispatching normal moves or Hodge projected moves.

This is already strong local playability work, but it is not a standalone board component. It owns a local `NativeChess3DEngine`, local profile loading, local save/replay, local AI helpers, local mode panels, and local visual state. Directly embedding `Chess3DWindow` into `ChessOnlineApp` would risk duplicate local-vs-authoritative state and a large WPF rewrite.

## Current Online UI

`src\ChessOnlineApp\MainWindow.xaml` and `MainWindow.xaml.cs` currently provide the P4F hands-on online flow:

- server connection panel;
- temporary auth/test users;
- two-client matchmaking helper;
- five-profile selector;
- ready/start/snapshot/action-log buttons;
- compact snapshot status;
- action log list;
- `Submit Safe Asgard Test Action`, currently a known safe normal move for the Asgard smoke profile.

The UI can prove remote health, auth, SignalR, matchmaking, game start, snapshot retrieval, action submit, and action log retrieval. It does not yet render `projectedBoard`, select pieces, show legal targets, or dispatch arbitrary clicked actions from an online board.

## Online Snapshot Boundary

`src\ChessOnlineProtocol\OnlineProtocolDtos.cs` defines `OnlineSnapshot` with:

- `RoomId`;
- `TableId`;
- `RulesetId`;
- `ProfileSummary`;
- `ServerSeq`;
- `StateHash`;
- `GamePhase`;
- `GameOutcome`;
- `TurnSummary`;
- `SaveGameJson`;
- `ActionCount`;
- `LastActionNotation`.

There is no first-class online cell/piece DTO yet. The authoritative board is embedded in `SaveGameJson`.

The savegame v0.1 format, documented in `docs\CHESS3D_SAVEGAME_FORMAT.md`, includes:

- `board`: dimensions, currently `8x8x8`;
- `currentSide`;
- `currentMacroPlayer`;
- `currentTurnKind`;
- `projectedBoard`: 512 piece codes;
- `coreStacks`;
- `reserveCounts`;
- `gameOver`;
- `winnerSide`;
- `actionHistory`;
- recompute flags for fusion/anchors.

For P4G, the safest first adapter is therefore:

1. parse `OnlineSnapshot.SaveGameJson`;
2. validate `format == chess3d-savegame` and `projectedBoard.Count == 512`;
3. map index `i` to `(x,y,z)` using the existing engine convention `index = z * 64 + y * 8 + x`;
4. expose a client-side `OnlineChess3DBoardCell` list for WPF rendering;
5. keep `StateHash` and `ServerSeq` attached to the view model for stale-action protection.

## Online Action Boundary

`OnlineActionCommand` already carries the action fields needed for current server dispatch:

- `NormalMove`: `ActorSide`, `FromX/Y/Z`, `ToX/Y/Z`, `PromotionType`;
- `HodgeProjectedMove`: primary/actor side plus from/to;
- `RubikLayerTurn`: `Axis`, `Layer`, `QuarterTurns`;
- `ReserveRestore`: `Side`, `PieceType`, `X/Y/Z`;
- `ExpectedStateHashBefore` for stale-state protection.

`OnlineGameSession.TryApply(...)` applies those commands through the server-side native authority and returns clear rejection text from the engine. This means the online client should not mutate board state optimistically as the source of truth. It should submit an action, then refresh from the authoritative server snapshot/action event.

Current gap: the server does not expose a dedicated online legal-preview DTO. P4G should first render board state and support a constrained safe action path. A later phase can add an append-only hub method for authoritative legal action preview, for example `RequestLegalActionsForCell`, returning normalized target/action records.

## SignalR Event Boundary

`ChessOnlineRelayClient` registers all current hub events:

- `ReceiveGameStarted`;
- `ReceiveActionAccepted`;
- `ReceiveActionRejected`;
- `ReceiveAuthoritativeSnapshot`;
- `ReceiveActionLogChunk`;
- `ReceiveResyncRequired`;
- matchmaking, diagnostics, room/table, and error events.

It stores `LastSnapshot`, `LastActionLog`, and `LastMatchmakingStatus`, and logs event summaries without printing tokens. This is enough for a first realtime board sync loop:

1. start game or request snapshot;
2. render board from `LastSnapshot`;
3. submit an action with `ExpectedStateHashBefore`;
4. on `ReceiveActionAccepted`, request/consume a fresh authoritative snapshot;
5. on `ReceiveActionRejected` or `ReceiveResyncRequired`, keep the old board, show the reason, and request snapshot if needed.

Current gap: there is no ordered event queue, gap detector, or automatic snapshot refresh after every accepted action. P4G should add this incrementally in the client layer, not by changing the server protocol first.

## RuleProfile Boundary

The real server-side profile catalog is still exactly five:

1. `classic-six-side-3d-8x8x8-v0.1`
2. `single-side-3d-8x8x8-v0.1`
3. `asgard-convergence-3d-8x8x8-v0.1`
4. `rubik-convergence-3d-8x8x8-v0.1`
5. `hodge-projection-duel-3d-8x8x8-v0.1`

P4G must not add a sixth profile. Scenario/playthrough/regression files are not game modes.

Mode-specific implications:

- Classic and Single-Side can eventually use normal move click-to-move once legal preview is available online.
- Asgard needs stack/fusion/core metadata beyond raw projected board for rich overlays; the first online board can still show projected top pieces.
- Rubik layer turns remain separate action controls, not board-cell target clicks.
- Hodge requires composite projected action preview and mirror target display before arbitrary click-to-move is honest.

## Recommended P4G Integration Path

The lowest-risk path is a staged client-side board integration:

1. Add an online board snapshot model in `src\ChessOnlineClient`, parsing `SaveGameJson` into 512 cells.
2. Render a compact 8x8x8/slice board inside `ChessOnlineApp`, with profile, turn, state hash, selected layer, selected cell, and piece code.
3. Wire SignalR snapshot/action events to refresh the board model.
4. Add target/action feedback from server rejections before adding general arbitrary action dispatch.
5. Add a narrow safe action builder for the current Asgard smoke position, then broaden to server-backed legal preview.
6. Add append-only hub/API support for legal preview only when the UI needs authoritative target lists for all profiles.

## Risks

- Directly reusing `Chess3DWindow` could create competing local native engine state and stale visuals.
- Parsing `SaveGameJson` in the client is intentionally read-only; any client-side board mutation would be unsafe unless immediately reconciled with an authoritative snapshot.
- Asgard/Rubik/Hodge need extra metadata for full visual parity; raw `projectedBoard` is enough for first playable online visibility but not complete mode visualization.
- Without an online legal-preview endpoint, arbitrary click-to-move can only be server-rejected after submit. A good UI should label this honestly until preview exists.
- Public HTTP 80 is diagnostic/dev only. Temporary users and in-memory tokens are acceptable for this stage; real accounts over HTTP are not.

## Safe Changes For P4G

- Add client-only snapshot parsing DTOs/view models.
- Add WPF board controls to `ChessOnlineApp` without replacing existing panels.
- Add event-to-board refresh wiring in `ChessOnlineRelayClient`/app code.
- Add sanitized session reports that summarize board state without credentials.
- Add docs and tests for parsing and profile count.

Out of scope:

- TLS/domain/443.
- nginx/systemd/firewall changes.
- Redis/Azure SignalR/Kubernetes.
- new Chess3D modes.
- rule changes for Classic/Single/Asgard/Rubik/Hodge.
- server-side protocol breakage.
