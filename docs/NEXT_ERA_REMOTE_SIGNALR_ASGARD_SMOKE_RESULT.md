# Next Era Remote SignalR Asgard Smoke Result

Date: 2026-06-21

Public HTTP tooling refresh: 2026-06-27

Host placeholder: `<HETZNER_HOST>`

This phase ran the first functional remote SignalR smoke against the temporary Hetzner Kestrel server. On 2026-06-27, the smoke wrapper was updated to support direct public HTTP smoke against the installed systemd/Nginx deployment. It still does not configure TLS, Redis, a SignalR backplane, or production-ranked matchmaking.

## Scope

The smoke verified the existing five-profile online authority path with the Asgard profile only:

- `GET /healthz/live`;
- `GET /healthz/ready`;
- `GET /chess3d/diagnostics`;
- `POST /api/auth/register` for two ephemeral users;
- authenticated SignalR connection to `/chess3d/relay`;
- `Hello`;
- exact-profile Asgard matchmaking;
- `Ready`;
- `StartGame`;
- `SubmitAction`;
- `RequestSnapshot`;
- `RequestActionLog`.

No new Chess3D RuleProfile was added. The smoke used:

```text
asgard-convergence-3d-8x8x8-v0.1
```

## Tooling Added

The concrete smoke client is:

```text
tools/HetznerSignalRSmoke
```

It is a small `net8.0` console app using the official ASP.NET Core SignalR .NET client package. It prints step names and sanitized ids only. It does not print access tokens, refresh tokens, passwords, store contents, keyring material, or raw server logs.

The operator wrapper is:

```text
scripts/deploy/Test-HetznerSignalRMatchmaking.ps1
```

It accepts:

- `-BaseUrl`;
- `-ServerUrl`;
- `-ProfileId`;
- `-TimeoutSeconds`;
- `-DryRun`;
- `-NoSecretLog`;
- `-SkipActionSubmit`.

It can also still start an SSH local-forward for the older private loopback smoke mode. In all modes it runs the smoke client through:

```text
tools/TestProcessWatchdog
```

The wrapper writes stdout/stderr under:

```text
.tmp/test-logs
```

These logs are ignored runtime diagnostics and must not be committed.

## Command Shape

For the current public HTTP deployment:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 `
  -BaseUrl "http://<HETZNER_HOST>" `
  -ProfileId "asgard-convergence-3d-8x8x8-v0.1" `
  -TimeoutSeconds 180 `
  -NoSecretLog
```

For the earlier private loopback mode, the local command shape remains:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 `
  -SshTarget root@<HETZNER_HOST> `
  -SshKeyPath "$env:USERPROFILE\.ssh\id_ed25519_hetzner" `
  -LocalPort 15077 `
  -TimeoutSeconds 120 `
  -BuildSmokeTool
```

The SSH local-forward maps:

```text
127.0.0.1:15077 -> <HETZNER_HOST>:127.0.0.1:5077
```

The loopback mode keeps token issuance inside the temporary trusted dry-run boundary. The public HTTP mode is useful as an operator smoke only; do not use real accounts over HTTP without TLS.

## Result

The smoke passed through the C# watchdog:

```text
TimedOut: False
ExitCode: 0
Duration: 00:00:04.5733548
```

Sanitized smoke steps:

```text
STEP PASS health
STEP PASS register
STEP PASS SignalR connect
STEP PASS matchmaking room=match-1-asgard table=table-1
STEP PASS Asgard start hash=a0296f7e94a22346
STEP PASS Asgard action notation=#1 S1 MOVE P (2,3,0)->(2,3,1)
STEP PASS snapshot/actionlog finalHash=1116b19374131cc4
SMOKE PASS
```

The 2026-06-27 public HTTP run also passed:

```text
STEP PASS health
STEP PASS register
STEP PASS login
STEP PASS SignalR connect
STEP PASS matchmaking room=match-1-asgard table=table-1
STEP PASS Asgard start hash=a0296f7e94a22346
STEP PASS Asgard action notation=#1 S1 MOVE P (2,3,0)->(2,3,1)
STEP PASS snapshot/actionlog finalHash=1116b19374131cc4
SMOKE PASS
```

Post-smoke diagnostics showed:

- `profileCount`: `5`;
- `authEnabled`: `true`;
- `authorityPlatform`: `Linux`;
- `authorityNativeLibraryName`: `libChess3DEngine.so`;
- `acceptedActionCount`: `1`;
- `rejectedActionCount`: `0`;
- `actionLogLength`: `1`;
- `matchmakingQueueCount`: `0`.

## Cleanup

The temporary Kestrel process was stopped after the smoke.

Post-stop checks:

- no listener on `:5077`;
- no persistent service was installed;
- `/opt/chessonline` and `/var/lib/chessonline` were not touched.

The remote `/tmp/chessonline-smoke` directory may contain temporary store/keyring/log files from the dry-run. These are runtime artifacts and are not tracked.

## Still Deferred

- TLS/domain handling.
- 443 currently belongs to a non-Chess service and must not be touched without a separate TLS/domain phase.
- backup/restore and log rotation.
- Redis/Azure SignalR/backplane.
- public ranked matchmaking.
