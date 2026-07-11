# P4K Hetzner Upload Result

Date: 2026-07-11

## Scope

Phase 11 staged the P4K server package on Hetzner without changing the active server. The phase created/used `/opt/chessonline/incoming`, uploaded the archive, verified checksums and archive contents, and confirmed public health still worked.

No service was stopped or restarted. No package was extracted into `/opt/chessonline/server`. nginx, UFW/firewall, TLS/443, x-ui/Xray, Outline, Albatronix Docker, Unreal, PostgreSQL containers, DNS, and `/var/lib/chessonline` were not changed.

## Local Archive

- Local path: `.tmp\deploy\ChessOnlineServer-P4K-f33240e87cd3.tar.gz`
- Expected SHA-256: `2868635C362DA78BFA2CDD2796AB31EFE7CBEE610D277D7DB2DB192539CE8A1D`
- Expected commit: `f33240e87cd39ed6d2cfb7b612a8504c28f85586`

## Remote Staging

- Remote directory: `/opt/chessonline/incoming`
- Remote directory owner: `root:root`
- Remote directory mode: `700`
- Remote archive: `/opt/chessonline/incoming/ChessOnlineServer-P4K-f33240e87cd3.tar.gz`
- Remote archive owner: `root:root`
- Remote archive mode: `600`
- Remote archive size: `345K`
- Remote SHA-256: `2868635c362da78bfa2cdd2796ab31efe7cbee610d277d7db2db192539ce8a1d`

The remote SHA-256 matches the local archive hash.

## Archive Content Verification

Required entries were present:

- `./ChessOnlineServer.dll`
- `./ChessOnlinePersistence.dll`
- `./ChessOnlineProtocol.dll`
- `./libChess3DEngine.so`
- `./server-build.json`
- `./server-package-manifest.json`

Profile count:

- real Chess3D RuleProfile JSON files: `5`
- schema excluded from profile count

Build identity:

- `server-build.json` contains `f33240e87cd39ed6d2cfb7b612a8504c28f85586`
- `server-package-manifest.json` contains `f33240e87cd39ed6d2cfb7b612a8504c28f85586`

## Active Server Still Unchanged

After upload, public HTTP still returned:

- `/healthz/live`: `Healthy`
- `/healthz/ready`: ready JSON with `profileCount=5`
- `/chess3d/diagnostics`: old pre-P4K capability surface

Diagnostics still did not include `RequestResumeMatch`, `JoinSpectator`, or `RequestLobbySnapshot`, which confirms that Phase 11 staged the archive only and did not replace the active server package.

## Next Gate

The next phase can perform the atomic server directory swap using the staged archive, after an immediate pre-mutation re-check of:

- service active;
- backup exists;
- staged archive checksum;
- disk space;
- expected commit;
- public health.
