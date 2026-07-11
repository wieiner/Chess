# P4K Deploy Rollback Tool

Date: 2026-07-11

## Scope

Phase 09 adds `scripts\deploy\Deploy-ChessOnlineServer-Hetzner.ps1`, a guarded operator script for the later P4K server package replacement. This phase only adds tooling and documentation; it does not deploy anything to Hetzner.

## Script

```text
scripts\deploy\Deploy-ChessOnlineServer-Hetzner.ps1
```

Parameters:

- `-ArchivePath`
- `-ArchiveSha256`
- `-SshTarget`
- `-SshKeyPath`
- `-ExpectedCommit`
- `-DryRun`
- `-SkipUpload`
- `-RollbackOnFailure`
- `-RollbackTo`
- `-RollbackDryRun`
- `-ExpectedCurrentCommit`
- `-ExpectedRollbackCommit`
- `-BackupArchivePath`
- `-HealthTimeoutSeconds`
- `-NoSecretLog`
- `-AllowDirtyTree`
- `-AllowArchiveNameMismatch`

The last two switches are explicit operator overrides. By default, the script refuses a dirty local tree and archive names outside the P4K naming pattern.

## Local Guards

Before any remote mutation, the script checks:

- archive exists;
- archive SHA-256 matches `-ArchiveSha256`;
- archive name matches `ChessOnlineServer-P4K-*.tar.gz` unless overridden;
- local git tree is clean unless overridden;
- `server-build.json` exists and contains `-ExpectedCommit`;
- `server-package-manifest.json` exists;
- `libChess3DEngine.so` exists;
- required managed server files exist;
- exactly five Chess3D RuleProfile JSON files exist, excluding the schema;
- secret-like/runtime archive entries are absent.

## Dry Run

With `-DryRun`, the script performs no SSH mutation and no upload. It validates the local archive and prints:

- planned upload path;
- planned extract directory;
- planned `chessonline.service` stop/start;
- planned loopback and public health URLs;
- expected P4K capabilities;
- rollback plan.

## Real Deploy Boundary

In non-dry-run mode, the script is designed to touch only:

- `/opt/chessonline/incoming`;
- `/opt/chessonline/server.new.<timestamp>`;
- `/opt/chessonline/server`;
- `/opt/chessonline/server.prev.<timestamp>`;
- `chessonline.service`.

It must not touch:

- nginx;
- UFW/firewall;
- TLS/443;
- x-ui/Xray;
- Outline;
- Albatronix Docker;
- Unreal;
- PostgreSQL containers;
- `/var/lib/chessonline`.

## Health and Rollback

After a real swap, the script polls loopback health and diagnostics. It expects:

- live health;
- ready health;
- expected commit in diagnostics;
- `RequestResumeMatch`;
- `JoinSpectator`;
- `RequestLobbySnapshot`.

With `-RollbackOnFailure`, a failed post-swap health/capability check attempts to restore the previous server directory and restart only `chessonline.service`.

## Explicit Rollback Mode

Phase 14 adds an explicit rollback mode for operator use:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Deploy-ChessOnlineServer-Hetzner.ps1 `
  -RollbackTo "/opt/chessonline/server.prev.<timestamp>" `
  -RollbackDryRun `
  -BackupArchivePath "/opt/chessonline/backups/server-before-p4k-<timestamp>.tar.gz" `
  -ExpectedCurrentCommit "<active commit>" `
  -NoSecretLog
```

`-RollbackDryRun` validates the target, backup archive, active payload, and expected current build identity without stopping the service or moving directories. A real rollback requires `-RollbackTo` without `-RollbackDryRun`; it is intentionally not run while the deployed server is healthy.

Rollback targets are restricted to exact `/opt/chessonline/server.prev.<timestamp>` paths. Legacy rollback directories that predate `server-build.json` are accepted only when `-ExpectedRollbackCommit` is omitted and are reported as `legacy-missing`.

## Phase 09 Verification

Phase 09 validates syntax and local dry-run behavior only. Actual upload and server replacement are reserved for later P4K phases.
