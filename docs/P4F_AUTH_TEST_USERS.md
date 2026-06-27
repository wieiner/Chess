# P4F Auth / Test Users Panel

Date: 2026-06-27

`ChessOnlineApp` now has an `Auth / Test Users` panel in the hosted SignalR area.

## Controls

- `Register Temp`
- `Login`
- `Logout`
- `Create Two Test Players`
- `Clear Session`
- username field;
- password field;
- redacted auth status.

## Temporary Users

Temporary users are generated through the existing server auth endpoints:

- `POST /api/auth/register`;
- `POST /api/auth/login` for manual login;
- `POST /api/auth/logout`.

Generated users use P4F-oriented prefixes such as:

```text
p4f_test_
p4f_a_
p4f_b_
```

Generated passwords are not displayed and not logged. The register endpoint returns an authenticated token response, so the UI can immediately use the temporary user without showing the generated password.

## SignalR Token Handoff

When the primary P4F session is authenticated, the existing P3F `Connect` button passes the in-memory access token to the SignalR .NET client access-token provider.

The token is not written to:

- UI logs;
- docs;
- repo files;
- local session reports.

## Two Test Players

`Create Two Test Players` registers two runtime-only players and stores their auth token responses in memory:

- primary player A;
- secondary player B.

Phase 05 uses this state for the two-client matchmaking flow. In Phase 04 the panel only creates and tracks the users.

## Manual Login Warning

The server connection panel already shows:

```text
HTTP 80 is diagnostic/dev only. Do not enter real passwords until TLS is enabled.
```

Manual login exists for development testing only. Use temporary users for public HTTP smoke.

## Verification

Local verification for this phase:

```powershell
dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64
```

Remote auth/matchmaking remains manual/operator smoke and is not required in CI.

