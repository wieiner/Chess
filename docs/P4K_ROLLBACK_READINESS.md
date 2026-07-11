# P4K Rollback Readiness

Date: 2026-07-11

## Scope

Phase 14 validates that the Hetzner ChessOnline rollback path is an executable operator command, not only a written plan. The healthy deployed server was not rolled back in this phase.

No changes were made to nginx, UFW/firewall, DNS, TLS/443, x-ui/Xray, Outline, Albatronix Docker, Unreal, PostgreSQL containers, or `/var/lib/chessonline` runtime state.

## Active Deployment

- Active service: `chessonline.service`
- Active public endpoint: `http://178.105.220.117`
- Active server directory: `/opt/chessonline/server`
- Active commit: `f33240e87cd39ed6d2cfb7b612a8504c28f85586`
- Active package id: `chessonline-linux-x64-f33240e87cd3`
- Active service PID after dry-run validation: `3457320`

## Rollback Inputs

- Previous server directory: `/opt/chessonline/server.prev.20260711-191019`
- Backup archive: `/opt/chessonline/backups/server-before-p4k-20260711-184042.tar.gz`
- Backup SHA-256: `65bccdbd74c3da2063c97b45ffb7626c75edbca877fa4ac3b3041d190e8dc043`

The previous server directory predates P4K build identity support and does not contain `server-build.json`. The rollback tool therefore allows legacy rollback payloads when `-ExpectedRollbackCommit` is not provided. The active current payload still requires `server-build.json` and can be checked with `-ExpectedCurrentCommit`.

## Script Changes

`scripts\deploy\Deploy-ChessOnlineServer-Hetzner.ps1` now supports an explicit rollback mode:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Deploy-ChessOnlineServer-Hetzner.ps1 `
  -RollbackTo "/opt/chessonline/server.prev.20260711-191019" `
  -RollbackDryRun `
  -BackupArchivePath "/opt/chessonline/backups/server-before-p4k-20260711-184042.tar.gz" `
  -ExpectedCurrentCommit "f33240e87cd39ed6d2cfb7b612a8504c28f85586" `
  -SshTarget root@178.105.220.117 `
  -SshKeyPath "$env:USERPROFILE\.ssh\id_ed25519_hetzner" `
  -NoSecretLog
```

During local development this command was first proven with `-AllowDirtyTree` because Phase 14 changes were not yet committed. Operators should prefer a clean tree for real use.

## Dry-Run Result

Rollback dry-run completed successfully.

Observed output:

```text
rollback target exists: /opt/chessonline/server.prev.20260711-191019
backup archive exists: /opt/chessonline/backups/server-before-p4k-20260711-184042.tar.gz
current payload: /opt/chessonline/server
rollback payload build identity: legacy-missing
expected current commit verified
ROLLBACK DRY RUN ONLY: no service stop/start and no directory move.
planned stop/start: chessonline.service only
planned archive current payload as: /opt/chessonline/server.rollback-from.<timestamp>
planned restore from: /opt/chessonline/server.prev.20260711-191019
planned health checks: http://127.0.0.1:5077/healthz/live, /healthz/ready, /chess3d/diagnostics
```

## Post Dry-Run Invariants

- `chessonline.service` stayed active.
- Main PID remained `3457320`.
- `/opt/chessonline/server` remained present.
- `/opt/chessonline/server.prev.20260711-191019` remained present.
- Public health stayed `Healthy`.
- Public diagnostics still reported commit `f33240e87cd39ed6d2cfb7b612a8504c28f85586`.
- `RequestResumeMatch`, `JoinSpectator`, and `RequestLobbySnapshot` remained advertised.

## Real Rollback Boundary

A real rollback requires an explicit `-RollbackTo` path without `-RollbackDryRun`. The script validates:

- rollback target is an exact `/opt/chessonline/server.prev.<timestamp>` path;
- current payload exists and has build identity;
- rollback target exists and contains server DLLs;
- backup archive exists;
- expected current commit matches, when provided;
- expected rollback commit matches, when provided.

If executed, the real rollback touches only:

- `/opt/chessonline/server`;
- `/opt/chessonline/server.rollback-from.<timestamp>`;
- the selected `/opt/chessonline/server.prev.<timestamp>`;
- `chessonline.service`.

It then checks loopback live, ready, and diagnostics endpoints.

## Decision

Rollback readiness is PASS for the current P4K deployment. The rollback itself was intentionally not executed because the deployed P4K server is healthy and exposes the expected capabilities.
