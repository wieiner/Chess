# P4K Lifecycle Deploy Result

Date: 2026-07-13

## Result

Status: **PASS**

Phase 32 deployed the spectator/disconnect/lifecycle hardening from P4K phases
28-31 to the existing Hetzner diagnostic deployment. Only the
`ChessOnlineServer` payload and its `systemd` process were replaced. Nginx,
UFW/firewall, HTTP port 80, TLS/443, x-ui/Xray, Outline, Albatronix Docker,
Unreal SYServer, PostgreSQL, DNS, and runtime state were not changed.

## Local Gate

The bounded Online test suite completed in 110.4 seconds through the C# process
watchdog:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 `
  -Suite Online -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1 `
  -TestTimeoutSeconds 120 -OnlineTestTimeoutSeconds 180 `
  -GlobalTimeoutSeconds 420
```

Results:

- `ChessOnlineContractTests`: PASS;
- `ChessOnlineSignalRContractTests`: PASS;
- watchdog timeouts: 0;
- cleanup fake-clock, bounded-batch, active/resumable retention, and spectator
  orphan tests: PASS.

## Package Identity

- source commit: `80ef477f0491aa01c17b79405dd04179d38f93f0`;
- package id: `chessonline-linux-x64-80ef477f0491`;
- local ignored archive:
  `.tmp\deploy\ChessOnlineServer-P4K-80ef477f0491.tar.gz`;
- archive SHA-256:
  `9E93AB8B4E8BF918969D9DC7C9E0345AEFF9759A89CE77BB5531C3EB07982F3D`;
- native authority SHA-256:
  `A5B5E0B707D09B199D49FE62CA5B5F00895F28B1A78E4D082584776C9913694D`;
- package manifest file count: 63;
- required runtime profile count: 5;
- forbidden secret/runtime archive entries: 0.

The package contains the framework-dependent `linux-x64` server, the tested
`libChess3DEngine.so`, build identity, package manifest, and the existing five
RuleProfile assets. It contains no private key, token, password, keyring,
runtime store, database, or certificate.

## Backup And Rollback

Before deployment, the active Phase 12 payload and its unit file were archived
on the server:

- backup path:
  `/opt/chessonline/backups/server-before-p4k-lifecycle-20260713-123423.tar.gz`;
- backup SHA-256:
  `120ae70c8cd1b46efab52f5d6fd4a13a4e47ddad1991236af45a3789ed84e5e7`;
- mode: `600`;
- archived entries: 81;
- runtime data under `/var/lib/chessonline`: not copied or modified.

The guarded deployment used archive/commit/profile checks, a 60-second health
deadline, and `RollbackOnFailure`. The retained previous payload is:

`/opt/chessonline/server.prev.20260713-123456`

Rollback was not needed.

## Deployed Health And Identity

Public HTTP checks after the atomic swap returned:

- `/healthz/live`: `Healthy`;
- `/healthz/ready`: `ready`, protocol `chess3d.relay.v1`, profile count 5;
- `/chess3d/diagnostics`: PASS;
- deployed commit: `80ef477f0491aa01c17b79405dd04179d38f93f0`;
- deployed package id: `chessonline-linux-x64-80ef477f0491`;
- authority platform: Linux;
- native library: `libChess3DEngine.so`;
- authority supported: true.

The append-only lifecycle diagnostics are present:

- `activeTableCount`;
- `resumableTableCount`;
- `completedTableCount`;
- `expiredTableCount`;
- `spectatorCount`;
- `cleanupRunCount`;
- `lastCleanupUtc`;
- `lastCleanupRemovedCount`.

Immediately after restart all lifecycle counters were zero, as expected for a
new process before its first cleanup interval. The first scheduled tick is
verified separately below.

At `2026-07-13T12:40:05Z`, after the configured five-minute interval:

- `cleanupRunCount`: 1;
- `resumableTableCount`: 1;
- `expiredTableCount`: 0;
- `lastCleanupRemovedCount`: 0.

The resumable InGame table created by the remote smoke survived the real hosted
cleanup tick. This matches the fake-clock contract and proves the production
loop is active without weakening resume retention.

## Remote Gameplay Regression

The integrated Asgard operator scenario completed through public HTTP 80 in
11.6 seconds:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\deploy\Test-HetznerOnlineUx.ps1 `
  -BaseUrl "http://178.105.220.117" `
  -ProfileId "asgard-convergence-3d-8x8x8-v0.1" `
  -Scenario all -TimeoutSeconds 420 -NoSecretLog `
  -RunId "p4k-phase32-lifecycle-all-20260713"
```

Result: **PASS**

The run proved health/capabilities, temporary-user auth, matchmaking, start,
server-preview action submission, snapshot/action-log agreement, resume, lobby
discovery, spectator read-only rejection, spectator live update, and final
diagnostics. Runtime IDs and credentials were not copied into this document.

## Neighbor And Journal Gate

Listener ownership remained unchanged across deployment:

- `127.0.0.1:5077`: ChessOnline `dotnet`/Kestrel;
- `0.0.0.0:80`: nginx;
- `*:443`: xray;
- `*:22527`: Outline;
- `0.0.0.0:3000`: Docker proxy;
- `:22`: SSH.

The post-deploy `chessonline.service` journal scan found zero matches for crash,
native load failure, persistence failure, permission denial, duplicate
sequence, unhandled exception, fail, or error.

## Security Boundary

This remains a diagnostic/development deployment over HTTP 80. Only generated
temporary users are suitable. Access/refresh tokens and passwords were not
printed, tracked, or included in the package. Production TLS/domain work and
port 443 remain explicitly deferred.
