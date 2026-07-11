# P4K Hetzner Backup Result

Date: 2026-07-11

## Scope

Phase 08 created a rollback backup of the currently deployed ChessOnline application payload on Hetzner before any server package replacement. It did not deploy new code and did not change nginx, UFW/firewall, TLS/443, x-ui/Xray, Outline, Albatronix Docker, Unreal, PostgreSQL containers, DNS, or runtime data.

## Backup Created

- Backup path: `/opt/chessonline/backups/server-before-p4k-20260711-184042.tar.gz`
- Size: `413K`
- Owner: `root:root`
- Mode: `600`
- SHA-256: `65bccdbd74c3da2063c97b45ffb7626c75edbca877fa4ac3b3041d190e8dc043`
- Archive entry count: `84`

Included paths:

- `/opt/chessonline/server`
- `/etc/systemd/system/chessonline.service`

Required entries verified:

- `opt/chessonline/server/ChessOnlineServer.dll`
- `opt/chessonline/server/ChessOnlineProtocol.dll`
- `opt/chessonline/server/libChess3DEngine.so`
- `etc/systemd/system/chessonline.service`

## Existing Backup Directory

Metadata-only listing showed:

- `server-before-p4g2-20260627-203832.tar.gz`
- `server-before-p4k-20260711-184042.tar.gz`

No runtime store or keyring archive was created in this phase.

## Runtime State Boundary

Runtime state under `/var/lib/chessonline` was intentionally not archived by this phase because it may contain account/session/persistence/keyring material. The P4K package deploy is designed to replace only `/opt/chessonline/server`; runtime state must remain server-side and must not be copied locally or committed.

## Command Shape

The successful remote command used a quoted script block to avoid local PowerShell interpolation of bash syntax:

```bash
set -euo pipefail
timestamp=$(date -u +%Y%m%d-%H%M%S)
backup_dir=/opt/chessonline/backups
backup_file=$backup_dir/server-before-p4k-$timestamp.tar.gz
install -d -m 700 "$backup_dir"
tar -czf "$backup_file" /opt/chessonline/server /etc/systemd/system/chessonline.service
chmod 600 "$backup_file"
sha256sum "$backup_file"
ls -lh "$backup_file"
tar -tzf "$backup_file" | wc -l
tar -tzf "$backup_file" | grep -E 'ChessOnlineServer.dll|ChessOnlineProtocol.dll|libChess3DEngine.so|etc/systemd/system/chessonline.service'
```

An earlier attempt used unsafe local quoting and failed before producing a valid P4K backup. The successful archive above is the rollback artifact for the next deployment phase.

## Rollback Use

If a later package replacement fails and no newer verified rollback point exists, the operator can use this archive to restore the previous application payload and service unit. Runtime state should not be overwritten by this archive.
