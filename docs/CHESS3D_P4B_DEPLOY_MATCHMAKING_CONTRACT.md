# Chess3D P4B Contract

P4B adds a deployment and matchmaking MVP without changing any game rules.

Implemented:
- Authenticated SignalR matchmaking commands.
- Exact `rulesetId` queues for the five existing Chess3D profiles.
- Match creation through the existing `OnlineRoomRegistry`.
- Match-found status that includes `roomId`, `tableId`, tickets, and seats.
- Deployment templates for Linux systemd/nginx and Windows service notes.
- Production sample configuration with placeholders only.

Not implemented:
- Cloud deployment automation.
- Real TLS certificate issuance.
- Redis or Azure SignalR backplane.
- Kubernetes.
- Linux-native ChessOnlineServer execution.
- Ranked matchmaking, ratings, party queues, or reconnect-to-queue persistence.
