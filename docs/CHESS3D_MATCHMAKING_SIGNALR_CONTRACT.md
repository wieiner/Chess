# Matchmaking SignalR Contract

New message types:
- `JoinMatchmaking`
- `CancelMatchmaking`
- `GetMatchmakingStatus`
- `ListMatchmakingQueues`

Result/event types:
- `MatchmakingJoined`
- `MatchmakingCancelled`
- `MatchmakingStatus`
- `MatchFound`
- `MatchmakingError`

Payloads use `OnlineMatchmakingCommand` and `OnlineMatchmakingStatus`. `MatchFound` includes tickets plus `roomId` and `tableId`; clients can then use the existing ready/start/action path.

Reject reasons added:
- `alreadyQueued`
- `notQueued`
