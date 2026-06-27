# P4G2 Phase 15 - Hetzner Legal Preview Server Package

Date: 2026-06-27

## Purpose

Prepare an updated Linux `ChessOnlineServer` package that contains the P4G2 legal-preview hub method and diagnostics capabilities, without touching Hetzner Nginx, systemd, UFW/firewall, TLS/443, x-ui/Xray, Outline, Albatronix, or Unreal services.

## Build Command

The package was built with the existing publish script and the previously tested Linux native authority library:

```powershell
$commit = git rev-parse --short HEAD
$publish = ".tmp\publish\chessonline-server"
$deploy = ".tmp\deploy"
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Publish-ChessOnlineServer-Linux.ps1 `
  -OutputPath $publish `
  -NativeLibraryPath .\DeploymentOutput\linux-x64\ChessOnlineServer\libChess3DEngine.so
tar -czf ".tmp\deploy\ChessOnlineServer-P4G2-$commit.tar.gz" -C $publish .
```

Archive produced:

```text
.tmp\deploy\ChessOnlineServer-P4G2-e12b3b56b.tar.gz
```

The archive is intentionally under `.tmp` and is ignored by Git.

## Contents

Key files present:

- `ChessOnlineServer.dll`
- `ChessOnlineProtocol.dll`
- `ChessOnlinePersistence.dll`
- `ChessOnlineServer.runtimeconfig.json`
- `libChess3DEngine.so`
- `Assets/Rules3D/Profiles/*`
- `Assets/Rules3D/OnlineScenarios/*`
- `Assets/Rules3D/SignalRScenarios/*`
- `Deploy/linux/*` templates

The package includes exactly the five real Chess3D RuleProfiles:

1. `classic_six_side_3d_v0_1.json`
2. `single_side_3d_v0_1.json`
3. `asgard_convergence_3d_v0_1.json`
4. `rubik_convergence_3d_v0_1.json`
5. `hodge_projection_duel_3d_v0_1.json`

## Exclusions

The package does not include generated runtime stores, keyrings, certificates, private keys, tokens, passwords, or raw smoke logs.

Reviewed paths:

- `.tmp\publish\chessonline-server`
- `.tmp\deploy\ChessOnlineServer-P4G2-e12b3b56b.tar.gz`

`appsettings.Development.json`, `appsettings.Local.json`, and `appsettings.Production.sample.json` are tracked/sample configuration files and do not contain real credentials.

## Verification

Local publish completed successfully. Package inspection verified:

- `ChessOnlineServer.dll` exists;
- `libChess3DEngine.so` exists;
- server assets are copied;
- no runtime store/keyring/cert/private-key artifacts are included.

Next phase should back up the current `/opt/chessonline/server` deployment before copying this archive to Hetzner.
