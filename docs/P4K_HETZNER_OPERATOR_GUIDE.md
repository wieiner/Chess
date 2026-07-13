# P4K Hetzner Operator Guide

Date: 2026-07-13

This runbook covers the current single-server diagnostic deployment. Replace
`<HETZNER_HOST>` with the approved host locally. Never paste private-key
contents, tokens, generated passwords, keyring files, or runtime stores into
logs or tracked documents.

## Safety Boundary

Routine P4K operation may inspect health, run temporary-user smoke, package the
server, deploy only the ChessOnline payload, and restart only
`chessonline.service` through the guarded script.

Do not modify nginx, UFW/firewall, DNS, TLS/443, x-ui/Xray, Outline, Albatronix
Docker, Unreal SYServer, PostgreSQL, or reboot the VPS. HTTP 80 is
diagnostic/development only.

## SSH And Health

```powershell
ssh -i "$env:USERPROFILE\.ssh\id_ed25519_hetzner" root@<HETZNER_HOST>

curl.exe http://<HETZNER_HOST>/healthz/live
curl.exe http://<HETZNER_HOST>/healthz/ready
curl.exe http://<HETZNER_HOST>/chess3d/diagnostics
```

Expected: `Healthy`, ready HTTP 200 with `profileCount=5`, deployed build
identity, Linux X64 authority, `libChess3DEngine.so`, and supported resume,
spectator, lobby, legal-preview, action-log, and realtime-resync capabilities.

Read the service journal without exporting raw logs:

```powershell
ssh -i "$env:USERPROFILE\.ssh\id_ed25519_hetzner" root@<HETZNER_HOST> `
  "systemctl status chessonline.service --no-pager; journalctl -u chessonline.service -n 200 --no-pager"
```

Do not paste lines that contain authentication material into issues or docs.

## Operator Smoke

Use one bounded combined scenario per registration window:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\deploy\Test-HetznerOnlineUx.ps1 `
  -BaseUrl "http://<HETZNER_HOST>" `
  -ProfileId "asgard-convergence-3d-8x8x8-v0.1" `
  -Scenario all -TimeoutSeconds 420 -NoSecretLog `
  -RunId "operator-all-$(Get-Date -Format yyyyMMdd-HHmmss)"
```

Raw logs belong only under ignored `.tmp/remote-ux-smoke`. A `429 rateLimited`
means the fixed registration window is working. Wait for genuine renewal; do
not restart the service or weaken the policy.

## Package Layout

A publish/package must include:

- `ChessOnlineServer.dll` and managed dependencies;
- canonical `libChess3DEngine.so`;
- `server-build.json` and `server-package-manifest.json`;
- exactly five RuleProfile JSON files and required schemas/assets;
- no database/store, keyring, token, password, certificate, private key, or log.

Build an ignored Linux package with the tested external native library:

```powershell
$so = "<PATH_TO_TESTED_LIBCHESS3DENGINE_SO>"
pwsh -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\deploy\Publish-ChessOnlineServer-Linux.ps1 `
  -NativeLibraryPath $so `
  -OutputPath .\.tmp\publish\ChessOnlineServer
```

Use the package manifest and archive SHA-256 as immutable deploy inputs.

## Guarded Deploy Template

Always run a local dry-run first, from a clean tree:

```powershell
$archive = ".\.tmp\deploy\ChessOnlineServer-P4K-<commit>.tar.gz"
$sha = "<ARCHIVE_SHA256>"
$commit = "<FULL_SOURCE_COMMIT>"

pwsh -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\deploy\Deploy-ChessOnlineServer-Hetzner.ps1 `
  -ArchivePath $archive -ArchiveSha256 $sha `
  -ExpectedCommit $commit -SshTarget root@<HETZNER_HOST> `
  -SshKeyPath "$env:USERPROFILE\.ssh\id_ed25519_hetzner" `
  -HealthTimeoutSeconds 60 -RollbackOnFailure -NoSecretLog -DryRun
```

After inspecting the plan, repeat without `-DryRun`. The script may touch only
`/opt/chessonline/incoming`, a temporary server directory,
`/opt/chessonline/server`, a timestamped `server.prev`, and
`chessonline.service`.

## Current Rollback Inputs

- active source commit:
  `810f8ff9a917191f420bb6eaa8ae36191ea607ba`;
- previous payload: `/opt/chessonline/server.prev.20260713-141554`;
- backup: `/opt/chessonline/backups/server-before-p4k-hardening-20260713-141535.tar.gz`;
- previous payload source: `80ef477f0491...`.

Validate rollback without changing the service:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\deploy\Deploy-ChessOnlineServer-Hetzner.ps1 `
  -RollbackTo "/opt/chessonline/server.prev.20260713-141554" `
  -RollbackDryRun `
  -BackupArchivePath "/opt/chessonline/backups/server-before-p4k-hardening-20260713-141535.tar.gz" `
  -ExpectedCurrentCommit "810f8ff9a917191f420bb6eaa8ae36191ea607ba" `
  -ExpectedRollbackCommit "80ef477f0491aa01c17b79405dd04179d38f93f0" `
  -SshTarget root@<HETZNER_HOST> `
  -SshKeyPath "$env:USERPROFILE\.ssh\id_ed25519_hetzner" `
  -NoSecretLog
```

An actual rollback is the same command without `-RollbackDryRun`. Execute it
only for a confirmed ChessOnline payload failure, after recording health and
service state. It archives the current payload and restarts only
`chessonline.service`.

## Backup Locations

- immutable operator backups: `/opt/chessonline/backups`;
- current payload: `/opt/chessonline/server`;
- retained previous payloads: `/opt/chessonline/server.prev.<timestamp>`;
- mutable runtime state: `/var/lib/chessonline` (never package or overwrite);
- service logs: systemd journal / configured runtime log location (never track raw output).
