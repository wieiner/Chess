# P4K Package Runtime Boundary

Date: 2026-07-11

## Scope

Phase 01 audits how the current repository should build and package an updated `ChessOnlineServer` for the existing Hetzner HTTP 80 diagnostic deployment. It is docs-only and does not deploy, restart services, alter nginx/UFW/443/TLS, or read runtime data contents.

## Current Repository Packaging Paths

### Linux publish script

`scripts/deploy/Publish-ChessOnlineServer-Linux.ps1` is the correct P4K starting point for a Linux server package.

Observed behavior:

- publishes `src/ChessOnlineServer/ChessOnlineServer.csproj`;
- defaults to `Release`;
- defaults to runtime `linux-x64`;
- uses `--self-contained false`;
- accepts `-NativeLibraryPath`;
- when `-NativeLibraryPath` is supplied:
  - verifies the source `.so` exists;
  - passes `-p:Chess3DEngineLinuxPath=...`;
  - copies it as `libChess3DEngine.so`;
  - removes duplicate `libChess3DEngine*.so` names.

Decision: use this script or an archive wrapper around its output for P4K. Do not commit the Linux `.so`, publish output, or deploy archive.

### Legacy scaffold script

`scripts/deploy/Create-ChessOnlineServer-LinuxPackage.ps1` copies from `ProductionOutput/ChessOnlineServer` and still says runtime Linux execution is deferred until the native engine boundary is portable.

Decision: do not use this as the authoritative P4K Hetzner package path. It is useful historical scaffold only. P4K should use the direct `linux-x64` publish path that can include a tested `libChess3DEngine.so`.

### Windows portable packaging

`tools/release/Build-Production.ps1` copies the Windows/server portable output from:

```text
src/ChessOnlineServer/bin/x64/Release/net8.0
```

It is relevant to Windows `ProductionOutput`, not the Linux Hetzner package.

## Runtime Boundaries

### Application package

The application payload belongs under:

```text
/opt/chessonline/server
```

Current service unit shape:

```ini
WorkingDirectory=/opt/chessonline/server
ExecStart=/usr/bin/dotnet /opt/chessonline/server/ChessOnlineServer.dll
```

P4K deploy phases may replace this directory only after backup and only for the ChessOnline payload. They must not change nginx, UFW, TLS, DNS, x-ui/Xray, Outline, Albatronix Docker, or Unreal.

### Mutable runtime state

The service template points mutable state outside the package:

```ini
Environment=CHESS3D_ONLINE_HostedOnline__Persistence__StorePath=/var/lib/chessonline/data/store.json
Environment=CHESS3D_ONLINE_HostedOnline__DataProtection__KeyRingPath=/var/lib/chessonline/keyring
```

Decision:

- do not include `/var/lib/chessonline/data/store.json` in packages;
- do not copy, log, or commit Data Protection keyring contents;
- do not list runtime store/keyring file contents during P4K;
- backup/rollback should preserve runtime data unless a later explicit migration phase says otherwise.

## Server Runtime Configuration

`src/ChessOnlineServer/ChessOnlineServerHost.cs`:

- reads environment variables with the `CHESS3D_ONLINE_` prefix;
- resolves profiles from `Assets/Rules3D/Profiles` next to the app if present;
- creates the persistence store parent directory;
- creates the Data Protection keyring directory;
- persists Data Protection keys to the configured filesystem path;
- maps:
  - `/healthz/live`;
  - `/healthz/ready`;
  - `/chess3d/diagnostics`;
  - `/chess3d/relay`.

`/chess3d/diagnostics` already exposes append-only feature flags and `supportedHubMethods`. The local source includes:

- `resumeMatch`;
- `spectatorMode`;
- `lobbySnapshot`;
- `RequestResumeMatch`;
- `JoinSpectator`;
- `RequestLobbySnapshot`.

The current public Hetzner deployment does not expose those three hub methods, so the deployment gap is package freshness, not missing local implementation.

## Native Library Boundary

The Linux package must include:

```text
libChess3DEngine.so
```

The current public diagnostics show the existing service loads:

```text
/opt/chessonline/server/libChess3DEngine.so
```

Decision: P4K package verification must prove the output contains `libChess3DEngine.so`; it must come from a tested build artifact and remain untracked.

## Assets Required In Package

The server needs at least:

- `Assets/Rules3D/Profiles` with exactly five real Chess3D profiles;
- online/signalr/deployment scenario assets already copied by the server project;
- `ChessOnlineServer.dll`;
- `ChessOnlineProtocol.dll`;
- `ChessOnlinePersistence.dll`;
- runtime/deps config files;
- `libChess3DEngine.so`.

Scenario/playthrough/regression JSON files are not game modes.

## Package Must Exclude

Do not package or commit:

- `/var/lib/chessonline/data/store.json`;
- `/var/lib/chessonline/keyring`;
- tokens, passwords, access/refresh token captures;
- private keys;
- certificates;
- raw SSH logs;
- generated `.tmp` reports;
- GitHub Actions artifacts;
- old package archives.

## Phase 01 Findings

| Area | Finding | Decision |
| --- | --- | --- |
| Local source | P4J resume/spectator/lobby hub code exists locally. | Build/deploy fresh package later. |
| Public server | Current HTTP 80 server lacks resume/spectator/lobby methods. | Treat as deployed package gap. |
| Publish path | `Publish-ChessOnlineServer-Linux.ps1` can publish `linux-x64` and include `.so`. | Use it for P4K package preparation. |
| Runtime data | Store/keyring are configured under `/var/lib/chessonline`. | Never include in app package. |
| Network stack | Nginx 80 and xray 443 are separate boundaries. | Do not touch network config in P4K package audit. |

## Verification Plan

Phase 01 verification:

- inspect publish/runtime scripts;
- inspect server host/options;
- build `ChessOnlineServer`;
- run `tests/run-tests.ps1 -List`;
- run `git diff --check`;
- commit docs only.

Later package phases should additionally:

- publish to a `.tmp`/DeploymentOutput path;
- verify package file list;
- confirm no runtime store/keyring/secrets are included;
- back up remote `/opt/chessonline/server` before replace;
- smoke `/healthz/live`, `/healthz/ready`, `/chess3d/diagnostics`, and resume/spectator/lobby hub calls after deploy.
