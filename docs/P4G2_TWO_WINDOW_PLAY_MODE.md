# P4G2 Two-Window Play Mode

Date: 2026-06-27

Scope: P4G2 Phase 10. This phase adds a manual two-window play path to `ChessOnlineApp` while preserving the existing one-app test-pair flow. It does not change server deployment, HTTP/TLS boundaries, Chess3D rules, native authority, or online protocol DTO layout.

## UI Modes

The online tab now exposes a `Play Mode` selector:

- `Single-App Test Pair` keeps the previous flow where one app instance creates two temporary users and drives both local clients.
- `Two-Window Manual Player` is the operator/player flow where each app window owns one temporary user, one SignalR relay connection, one matchmaking ticket, and one seat.

The selector is informational for now; the buttons determine the actual flow. It is included in saved session reports so reports identify whether they came from a one-app or two-window run.

## Two-Window Flow

Run two separate `ChessOnlineApp` instances.

In both windows:

1. Click `Use Hetzner HTTP`.
2. Click `Check Health`.
3. Click `Check Diagnostics`.
4. Select the same RuleProfile.
5. Set `Play Mode` to `Two-Window Manual Player`.
6. Click `Manual Join Matchmaking`.

When both windows have joined the same profile queue, the server returns or broadcasts `MatchFound`. Each window shows:

- room id;
- table id;
- primary short player id;
- assigned seat;
- current side/macro once a snapshot exists;
- whether this window can act now.

Then in both windows:

1. Click `Ready This Window`.
2. After both seats are ready, click `Start This Window` in either window.
3. Click `Snapshot This Window`.
4. Select an occupied source cell when it is your turn.
5. Click a highlighted legal target.
6. Watch action status, snapshot hash, action log, and seat/turn status update.

## One-App Flow Is Preserved

The existing one-app test-pair buttons remain:

- `Create Test Match With Two Local Clients`;
- `Ready Both`;
- `Start Game`;
- `Request Snapshot`;
- `Submit Safe Asgard Test Action`.

Internally, board actions now require only the primary relay plus a room/table, so the same board and legal-preview UI works for both one-app and two-window flows.

## Authority Boundary

The client still only hints and submits. The server remains authoritative for:

- authenticated player identity;
- room/table membership;
- seat ownership;
- actor side/macro-player;
- state hash freshness;
- legal action validation.

If the UI predicts that the primary window cannot act, it blocks submit with a readable reason. If the UI is stale or wrong, the server still rejects the action and the app shows the server reason.

## Safety

The two-window path uses temporary users by default. Tokens and passwords are not printed. HTTP 80 remains diagnostic/development-only. TLS/domain/443 are intentionally deferred.

## Verification

Local verification for this phase:

- `dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64`;
- `tests\run-tests.ps1 -Only ChessOnlineContractTests ...`;
- GitHub Actions Windows Build after commit.

Manual remote verification remains operator-run and is not required in CI.
