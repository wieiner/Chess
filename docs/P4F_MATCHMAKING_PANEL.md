# P4F Matchmaking Panel

Date: 2026-06-27

`ChessOnlineApp` now has a small online match control row under the existing P3F SignalR buttons.

## Controls

- `Create Test Match With Two Local Clients`
- `Ready Both`
- `Start Game`
- `Request Snapshot`
- `Request Action Log`

The existing profile selector remains the source of truth and lists exactly five real Chess3D profiles:

- `classic-six-side-3d-8x8x8-v0.1`
- `single-side-3d-8x8x8-v0.1`
- `asgard-convergence-3d-8x8x8-v0.1`
- `rubik-convergence-3d-8x8x8-v0.1`
- `hodge-projection-duel-3d-8x8x8-v0.1`

No sixth mode is added.

## Two-Client Test Match

`Create Test Match With Two Local Clients`:

1. ensures two temporary authenticated sessions exist;
2. opens two in-memory SignalR clients;
3. sends `Hello` for both;
4. joins matchmaking for the selected ruleset;
5. records `roomId` and `tableId`;
6. shows room/table/player status in the UI;
7. mirrors room/table into the existing P3E/P3F text boxes for compatibility with older buttons.

Tokens and generated passwords are not logged.

## Ready / Start

`Ready Both` sends `Ready` for both local clients.

`Start Game` sends `StartGame` from the primary client and shows the returned ruleset/state hash when a snapshot is present.

## Snapshot / Action Log

`Request Snapshot` and `Request Action Log` call the remote hub through the primary SDK relay client and write compact status into the UI log.

Phase 06 adds the structured snapshot viewer and safe action submit button. Phase 05 keeps the UI orchestration small and focused.

## Limitations

- This is an operator/test flow, not public ranked matchmaking.
- It assumes a single local process controls both temporary players.
- Reconnect/spectator flows are future work.
- Full 3D online board visualization is deferred to P4G.

## Verification

Local verification:

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
```

Remote Hetzner UI smoke is manual/operator and not required by CI.

