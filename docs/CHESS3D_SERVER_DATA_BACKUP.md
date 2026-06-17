# Server Data Backup

Runtime data lives outside git:

- JSON store;
- DataProtection key ring;
- logs;
- any future database files.

`scripts/deploy/Backup-ChessOnlineServerData.ps1.template` is a local operator template. Backups may contain private player/session material and must not be committed.
