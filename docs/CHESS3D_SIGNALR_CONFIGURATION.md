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
- `Auth.EnableAuthentication`
- `Auth.AllowDevAnonymousSessions`
- `Auth.AccessTokenMinutes`
- `Auth.RefreshTokenDays`
- `Persistence.Provider`
- `Persistence.StorePath`
- `DataProtection.ApplicationName`
- `DataProtection.KeyRingPath`

`Normalize()` clamps unsafe or empty values to local defaults.

## Config Files

Development samples:

```text
src/ChessOnlineServer/appsettings.Development.json
src/ChessOnlineServer/appsettings.Local.json
```

`appsettings.Local.json` is a local sample, not a secret store.

P4A defaults keep the P3F local anonymous smoke flow enabled. For production-like local tests set `Auth.EnableAuthentication=true` and `Auth.AllowDevAnonymousSessions=false`. Runtime stores and Data Protection key rings must live outside tracked repository content.

## P4B Production Sample

P4B adds `src/ChessOnlineServer/appsettings.Production.sample.json`. It is a template only and contains no secrets. Copy it to a deployment-local file and keep runtime store/key paths outside git.

Matchmaking uses the same `ProfileRoot` and only accepts the five existing Chess3D RuleProfiles.
