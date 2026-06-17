# Chess3D Matchmaking Model

P4B matchmaking is a single-server in-memory MVP.

Rules:
- A player must be authenticated when the server requires auth.
- One active ticket per player.
- Queues are keyed by exact `rulesetId`.
- Different profiles never cross-match.
- `single-side` currently requires one player; other existing profiles require two.
- A match creates a room, a table, and seat assignments through `OnlineRoomRegistry`.

Not included:
- ranked matchmaking;
- durable queue persistence;
- parties;
- reconnect-to-queue;
- cross-server matchmaking.
