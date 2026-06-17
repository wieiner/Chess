# Chess3D Windows Server Runbook

Status: P4C runtime package hardening. This runbook covers the current Windows-native `ChessOnlineServer` package. It does not make the server Linux-portable; the rules authority still uses the Windows `Chess3DEngine.dll` boundary.

## Package Location

Use:

```powershell
ProductionOutput\ChessOnlineServer
```

The package must contain:

- `ChessOnlineServer.exe`
- `Chess3DEngine.dll`
- `appsettings.Production.sample.json`
- `Deploy\windows\README.md`
- `Deploy\windows\Start-ChessOnlineServer-Windows.ps1`
- `Deploy\windows\Stop-ChessOnlineServer-Windows.ps1`
- `Deploy\windows\Test-ChessOnlineServer-Windows.ps1`

Runtime state is intentionally not tracked. Keep it under `ProductionOutput\ChessOnlineServer\Data` or another operator-owned path.

## Console Run

For a foreground run:

```powershell
cd ProductionOutput\ChessOnlineServer
.\run_chess_online_server.bat --urls http://127.0.0.1:5077
```

This is useful for local diagnostics because logs stay in the console.

## PowerShell Start

For a background local run:

```powershell
.\Deploy\windows\Start-ChessOnlineServer-Windows.ps1 `
  -ServerRoot . `
  -HostUrls http://127.0.0.1:5077 `
  -DataRoot .\Data
```

The script sets production-safe local environment variables:

- `ASPNETCORE_ENVIRONMENT=Production`
- `CHESS3D_ONLINE_HostedOnline__HostUrls`
- `CHESS3D_ONLINE_HostedOnline__Persistence__StorePath`
- `CHESS3D_ONLINE_HostedOnline__DataProtection__KeyRingPath`

## Smoke Test

From the package folder:

```powershell
.\Deploy\windows\Test-ChessOnlineServer-Windows.ps1 -ServerRoot . -Port 5077
```

The smoke test starts the server, checks:

- `/healthz/live`
- `/healthz/ready`

Then it stops the process using the PID file.

## Firewall and Port

The default local URL is:

```text
http://127.0.0.1:5077
```

For LAN or public exposure, bind explicitly, for example:

```powershell
-HostUrls http://0.0.0.0:5077
```

Then configure the Windows firewall or reverse proxy intentionally. Do not expose a development machine by accident.

## Configuration

Copy:

```text
appsettings.Production.sample.json
```

to a local untracked:

```text
appsettings.Production.json
```

Do not commit secrets, passwords, tokens, certificates, stores, or generated key-ring files.

## Data, Keys, and Logs

Default runtime paths:

- Store: `Data\chess3d-online-store.json`
- ASP.NET Core Data Protection key ring: `Data\keys`
- PID file: `Data\chessonline-server.pid`
- Stdout log: `Data\chessonline-server.out.log`
- Stderr log: `Data\chessonline-server.err.log`

These files are runtime artifacts. They should not appear in source control or portable release assets.

## Stop and Restart

Stop:

```powershell
.\Deploy\windows\Stop-ChessOnlineServer-Windows.ps1 -ServerRoot . -DataRoot .\Data
```

Restart:

```powershell
.\Deploy\windows\Stop-ChessOnlineServer-Windows.ps1 -ServerRoot . -DataRoot .\Data
.\Deploy\windows\Start-ChessOnlineServer-Windows.ps1 -ServerRoot . -HostUrls http://127.0.0.1:5077 -DataRoot .\Data
```

## Backup

Back up the `Data` directory before replacing binaries:

- `Data\chess3d-online-store.json`
- `Data\keys`

The repository also keeps `scripts\deploy\Backup-ChessOnlineServerData.ps1.template` as a deploy-time template for operator-specific backup policy.

## Update and Rollback

Update:

1. Stop the server.
2. Back up `Data`.
3. Replace binaries and assets from the new `ProductionOutput\ChessOnlineServer` package.
4. Keep the existing `Data` directory.
5. Start the server.
6. Run health checks.

Rollback:

1. Stop the server.
2. Restore the previous package folder.
3. Restore the matching `Data` backup if the store format changed.
4. Start the server and run the smoke test.

## Linux Note

P4C keeps Windows hosting real and honest. Linux deployment remains a portability target, not a complete product path, until a Linux-compatible rules authority is available and verified by state-hash parity tests.
