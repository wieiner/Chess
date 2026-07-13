# P4K Online Hardening Deploy Result

Date: 2026-07-13

## Result

Status: **PASS**

Phase 41 deployed the Phase 39 request limits and Phase 40 readiness probe to
the existing Hetzner HTTP 80 diagnostic server. Only the ChessOnline server
payload was swapped and only `chessonline.service` was restarted. No nginx,
UFW/firewall, DNS, TLS/443, x-ui/Xray, Outline, Albatronix Docker, Unreal
SYServer, PostgreSQL, or VPS reboot action was performed.

## Local Gate

The bounded Online suite ran sequentially with `/m:1` through the C# process
watchdog and completed in 75.5 seconds:

- `ChessOnlineContractTests`: PASS;
- `ChessOnlineSignalRContractTests`: PASS;
- rate-limit boundary and player isolation: PASS;
- native/profile/store/keyring readiness fixtures: PASS;
- watchdog timeout: none.

Phase 39 CI run `29255364072` and Phase 40 CI run `29256381563` both completed
successfully before packaging.

## Package Identity

- deployed source commit:
  `810f8ff9a917191f420bb6eaa8ae36191ea607ba`;
- package id: `chessonline-linux-x64-810f8ff9a917`;
- ignored local archive:
  `.tmp\deploy\ChessOnlineServer-P4K-810f8ff9a917.tar.gz`;
- archive SHA-256:
  `144DF324F156FE1FE002ADA091A65D66DA999A69F23DE3796FC9A1BE6366BC4C`;
- native library SHA-256:
  `A5B5E0B707D09B199D49FE62CA5B5F00895F28B1A78E4D082584776C9913694D`;
- manifest entries: 63;
- RuleProfiles: exactly 5;
- secret/runtime files: none.

## Backup And Rollback

The previous payload and systemd unit were archived before the swap:

- backup:
  `/opt/chessonline/backups/server-before-p4k-hardening-20260713-141535.tar.gz`;
- backup SHA-256:
  `9166cef17e1819752eae72d87b711e9a1e8e5ebb55ddd64cfc77a72ea1dd07c6`;
- mode/owner: `600`, `root:root`;
- archive entries: 81;
- retained previous payload:
  `/opt/chessonline/server.prev.20260713-141554`.

The guarded deploy used the expected commit, exact archive hash, exact
five-profile check, 60-second health deadline, and automatic rollback on
failure. Its first retry observed the normal sub-second Kestrel startup window;
the bounded loop then passed. Follow-up loopback and public probes were green.
Rollback was not needed.

A post-deploy rollback dry-run verified the active commit, previous commit
`80ef477f0491...`, backup path, planned service boundary, and all health URLs.
It performed no stop/start or directory move.

## Health And Readiness

After deployment:

- `chessonline.service`: active;
- `/healthz/live`: `Healthy` on loopback and public HTTP;
- `/healthz/ready`: HTTP 200, `ready`, profile count 5;
- diagnostics commit/package match the values above;
- authority platform: Linux X64;
- authority native library: `libChess3DEngine.so`;
- authority supported: true;
- resume, spectator, lobby, legal preview, action log, and realtime resync
  capabilities remain enabled.

The ready response now proves native session creation, the exact profile set,
registry initialization, persistence write access, keyring write access, and
configuration validity without returning any path or permission detail.

## Remote Normal-flow Smoke

The Asgard combined operator scenario passed through public HTTP 80:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\deploy\Test-HetznerOnlineUx.ps1 `
  -BaseUrl "http://178.105.220.117" `
  -ProfileId "asgard-convergence-3d-8x8x8-v0.1" `
  -Scenario all -TimeoutSeconds 420 -NoSecretLog `
  -RunId "p4k-phase41-hardening-all-20260713"
```

Result: **PASS in 2.66 seconds**. Auth, matchmaking, start, server legal
preview, two accepted moves, snapshot/action log, resume, lobby, spectator
read-only rejection, and final diagnostics all completed without an HTTP 429.
This proves the conservative limits do not interrupt the normal operator/UI
flow. Local burst tests separately prove bounded HTTP 429 behavior.

## Journal And Neighbor Gate

For the new service process:

- journal lines inspected: 173;
- crash/native/persistence/permission/fatal risk markers: 0;
- token/authorization/password markers: 0;
- normal-flow HTTP 429 markers: 0.

Listener ownership remains unchanged:

- `127.0.0.1:5077`: ChessOnline Kestrel;
- `0.0.0.0:80`: nginx;
- `*:443`: Xray;
- `*:22527`: Outline;
- `0.0.0.0:3000`: Docker proxy;
- `:22`: SSH.

## Security Boundary

HTTP 80 remains diagnostic/development only and only generated temporary users
are suitable. No access/refresh token, password, key, certificate, keyring, or
runtime store was printed, tracked, or included in the package.
