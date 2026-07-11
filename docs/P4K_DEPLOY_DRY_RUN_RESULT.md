# P4K Deploy Dry Run Result

Date: 2026-07-11

## Scope

Phase 10 validated the guarded deployment script in `-DryRun` mode. No SSH mutation, SCP upload, directory creation, service stop/start, package extraction, nginx/firewall/TLS/443/x-ui/Xray/Outline/Albatronix/Unreal/PostgreSQL change, or runtime state access occurred.

## Command

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\deploy\Deploy-ChessOnlineServer-Hetzner.ps1 `
  -ArchivePath .tmp\deploy\ChessOnlineServer-P4K-f33240e87cd3.tar.gz `
  -ArchiveSha256 2868635C362DA78BFA2CDD2796AB31EFE7CBEE610D277D7DB2DB192539CE8A1D `
  -SshTarget root@178.105.220.117 `
  -SshKeyPath "$env:USERPROFILE\.ssh\id_ed25519_hetzner" `
  -ExpectedCommit f33240e87cd39ed6d2cfb7b612a8504c28f85586 `
  -DryRun `
  -NoSecretLog
```

## Local Validation

The script validated:

- archive path;
- archive SHA-256;
- clean local git tree;
- P4K archive name pattern;
- `server-build.json`;
- expected commit;
- package id: `chessonline-linux-x64-f33240e87cd3`;
- required server DLLs;
- `libChess3DEngine.so`;
- `server-package-manifest.json`;
- exactly five Chess3D RuleProfile JSON files;
- absence of secret-like/runtime archive entries.

## Planned Operations

The dry-run printed the intended plan:

- planned upload to `/opt/chessonline/incoming/ChessOnlineServer-P4K-f33240e87cd3.tar.gz`;
- planned extract to `/opt/chessonline/server.new.<timestamp>`;
- planned stop/start of `chessonline.service` only;
- planned loopback health checks:
  - `http://127.0.0.1:5077/healthz/live`;
  - `/healthz/ready`;
  - `/chess3d/diagnostics`;
- planned public HTTP health equivalents;
- expected post-deploy capabilities:
  - `resumeMatch`;
  - `spectatorMode`;
  - `lobbySnapshot`;
  - `RequestResumeMatch`;
  - `JoinSpectator`;
  - `RequestLobbySnapshot`;
- rollback path if `-RollbackOnFailure` is used.

## No-Mutation Evidence

The dry-run output included:

```text
DRY RUN ONLY: no SSH mutation, no SCP upload, no service stop/start.
```

After the dry-run, public HTTP still returned:

- `/healthz/live`: `Healthy`
- `/healthz/ready`: ready JSON with `profileCount=5`
- `/chess3d/diagnostics`: unchanged pre-P4K capability surface

Diagnostics still did not include `RequestResumeMatch`, `JoinSpectator`, or `RequestLobbySnapshot`, which confirms that the dry-run did not update the server package.

## Result

Status: PASS

The deploy tool is ready for the upload/stage phase. The actual server replacement remains gated by upload checksum verification, the existing backup, and a later explicit deploy phase.
