# Chess3D SignalR Configuration

`HostedOnlineOptions` controls the local server.

## Main Settings

- `HostUrls`
- `HubPath`
- `AllowedOrigins`
- `MaxReceiveMessageBytes`
- `KeepAliveIntervalSeconds`
- `ClientTimeoutSeconds`
- `EnableDetailedErrors`
- `RateLimitPermitLimit`
- `RateLimitWindowSeconds`
- `MaxRooms`
- `MaxTablesPerRoom`
- `MaxConnections`
- `MaxMessageLogLength`
- `DiagnosticsEnabled`
- `ProfileRoot`

`Normalize()` clamps unsafe or empty values to local defaults.

## Config Files

Development samples:

```text
src/ChessOnlineServer/appsettings.Development.json
src/ChessOnlineServer/appsettings.Local.json
```

`appsettings.Local.json` is a local sample, not a secret store.

