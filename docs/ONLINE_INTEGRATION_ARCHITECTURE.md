# Online Integration Architecture

## Runtime Boundaries

The online layer is intentionally outside the normal chess boards:

```text
ChessOnlineApp
  -> portal clients and account/profile editor
  -> %APPDATA%\ChessAdvisor\integrations.json

ChessApp / Chess2D
  -> ordinary 2D chess UI and engine
  -> may read integration profiles when an advisor/portal workflow is opened

Chess3DApp
  -> 8x8x8 chess UI and engine
  -> uses Chess3DInternetRelayClient for our future web platform
  -> may read integration profiles for saved relay rooms/endpoints

Future web-platform
  -> consumes the same conceptual profile fields server-side
  -> speaks chess3d.relay.v1 over WebSocket and room HTTP APIs
```

`ChessOnlineApp` owns account creation and verification UI. The ordinary 2D and 3D apps do not embed portal login forms in their main windows; they should open/consume the integration layer only when a user explicitly starts an online/advisor workflow.

## Integration Profile Store

`src\ChessOnlineApp\Integrations\IntegrationAccountStore.cs` defines the current shared account-profile contract. Profiles are stored as JSON at:

```text
%APPDATA%\ChessAdvisor\integrations.json
```

The file stores routing and capability metadata, not raw tokens or passwords. A profile can mark `hasSecret: true` so consumers know that the profile was created from an authenticated session, but the actual secret remains session-only until a DPAPI/Windows Credential Manager store is added.

Current JSON shape:

```json
{
  "version": 1,
  "accounts": [
    {
      "id": "stable-profile-id",
      "portalId": "lichess",
      "displayName": "Lichess",
      "username": "player",
      "endpoint": "https://lichess.org/",
      "roomId": "",
      "accessMode": "board",
      "consumers": "Chess2D",
      "hasSecret": true,
      "createdAt": "2026-04-29T00:00:00+00:00",
      "updatedAt": "2026-04-29T00:00:00+00:00",
      "settings": {
        "transport": "HTTPS NDJSON Board API / Bot API",
        "authKind": "BearerToken",
        "capabilities": "PublicProfile, PublicGameArchive, LiveGameStream, OfficialMoveSubmit, BotPlay"
      }
    }
  ]
}
```

Consumer flags:

| Consumer | Meaning |
| --- | --- |
| `Chess2D` | Ordinary chess portals, PGN/FEN/profile/game stream workflows. |
| `Chess3D` | 8x8x8 relay endpoints and local 3D network workflows. |
| `WebPlatform` | Future hosted ChessAdvisor platform room/account metadata. |

After an account/profile is added in `ChessOnlineApp`, the UI writes this store through `JsonIntegrationAccountStore.UpsertAsync`. Future apps should read through the same contract instead of scraping UI controls or duplicating portal-specific assumptions.

## 3D Chess Relay

`Chess3DInternetRelayClient` is the client for a future hosted 3D chess web platform. It is not tied to Lichess or Chess.com because those platforms do not host custom 8x8x8 six-sided rules.

The relay is a room server:

1. A browser/app creates or joins a room.
2. The server assigns up to six player seats and up to six group bridge slots.
3. Clients exchange ordered JSON envelopes over WebSocket.
4. The server stores/rebroadcasts `move3d`, `rotate3d`, `sync3d`, `ready3d`, and `chat3d`.
5. A reconnecting client can request the latest `sync3d` board snapshot and resume.

Expected HTTP endpoints for our platform:

```text
POST /api/chess3d/rooms
GET  /api/chess3d/rooms/{roomId}
GET  /ws/chess3d?room={roomId}&seat={1..6}&groupSlot={0..6}&role=player|group|spectator&node={nodeId}
```

WebSocket envelope:

```json
{
  "Protocol": "chess3d.relay.v1",
  "Type": "move3d",
  "RoomId": "cube-main",
  "NodeId": "client-node",
  "MessageId": "client-node:internet:42",
  "Role": "player",
  "Sequence": 42,
  "Seat": 1,
  "GroupSlot": 0,
  "Payload": {
    "Type": "move3d",
    "FromX": 0,
    "FromY": 0,
    "FromZ": 0,
    "ToX": 0,
    "ToY": 1,
    "ToZ": 0
  },
  "Metadata": {}
}
```

## Ordinary Chess Portal Matrix

`src\ChessOnlineApp\Integrations\OnlineChessPortals.cs` defines one capability matrix and a common client interface.

| Portal | Practical integration |
| --- | --- |
| Lichess | Official HTTPS NDJSON Board API and Bot API. Best live-play target. Keep human Board API and engine Bot API separate. |
| ChessAdvisor 3D Web Platform | Our future WebSocket relay + HTTPS room API for 8x8x8 six-sided chess, group bridges, `sync3d`, `move3d`, `rotate3d`, and `chat3d`. |
| Chess.com | Official Published Data API is read-only public data: profiles, stats, archives, current daily games. It cannot send moves. |
| ChessKid | No public gameplay API found. Treat as Chess.com-family only if a partner API is provided. |
| World Chess / FIDE Online Arena | No public gameplay API found. Use PGN/FEN import or approved partner adapter. |
| ChessBase / Playchess | No public web gameplay API found. Use ChessBase exports or an approved local bridge. |
| ICC | ICS-style/proprietary ecosystem; can use a generic text-server adapter if account/server terms allow. |
| FICS | Historical free ICS/Telnet server. `IcsTextChessClient` provides a line-oriented adapter foundation. |
| GameKnot | Correspondence-focused site; no public move API found. Start with PGN/FEN import/export. |
| Chess24 | Closed as a playing site and folded into Chess.com-family content paths. |
| Chessable | Training/course platform, not a live-play server; future target for course/FEN/PGN import. |

## Safety Model

Do not automate moves on closed platforms by browser scraping. The integration layer only sends moves where an official or permitted protocol exists. Engine-assisted online play must use a bot/engine-allowed mode, not a normal human account.
