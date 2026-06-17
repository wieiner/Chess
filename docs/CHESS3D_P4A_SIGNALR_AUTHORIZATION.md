# Chess3D P4A SignalR Authorization

The P4A hub supports two modes.

## Development Mode

When authentication is disabled, the P3F anonymous dev session flow remains available. This keeps existing local smoke tests and manual experiments working.

## Authenticated Mode

When `HostedOnline.Auth.EnableAuthentication=true`, clients authenticate with an access token. When `AllowDevAnonymousSessions=false`, mutating hub commands require authentication.

The hub rejects authenticated envelopes that try to claim another `playerId`. The server-derived player id is applied before calling:

- `CreateRoom`
- `JoinRoom`
- `CreateTable`
- `JoinTableSeat`
- `Ready`
- `StartGame`
- `SubmitAction`
- snapshot/action-log requests

`Ping` and diagnostics remain safe utility paths. They do not reveal secret material.

P4A does not change the P3E action authority. It only protects who is allowed to speak as a player.
