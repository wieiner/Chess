# P4K WSL Package Preflight

Date: 2026-07-11

## Scope

Phase 06 attempted an optional local Linux preflight for the P4K `ChessOnlineServer` package. This phase did not deploy anything to Hetzner and did not change nginx, UFW/firewall, TLS/443, x-ui/Xray, Outline, Albatronix Docker, Unreal, runtime stores, keyrings, or production paths.

## Result

Status: `SKIPPED: prerequisite unavailable`

The local machine has WSL available, but the named distro from the prompt was not present and the available Ubuntu distro does not have the .NET SDK/runtime installed.

## Checks

```powershell
wsl.exe -l -v
```

Observed:

- available distro: `Ubuntu`
- state: `Running`
- WSL version: `2`

```powershell
wsl.exe -d Ubuntu-24.04 -- dotnet --info
```

Observed:

- failed with `WSL_E_DISTRO_NOT_FOUND`
- reason: distro `Ubuntu-24.04` is not registered locally

```powershell
wsl.exe -d Ubuntu -- dotnet --info
```

Observed:

- `/bin/bash: line 1: dotnet: command not found`
- WSL also reported that it could not translate the current Windows path `E:\repos\mygames\Chess`

```powershell
wsl.exe -d Ubuntu -- uname -a
wsl.exe -d Ubuntu -- sh -lc "command -v dotnet || true; command -v curl || true"
```

Observed:

- kernel: WSL2 Linux x86_64
- `curl`: `/usr/bin/curl`
- `dotnet`: not found

## Decision

No package smoke was attempted inside WSL because `.NET` is unavailable in the local WSL distro. The phase intentionally did not install .NET or modify WSL because this preflight is optional and the P4K deploy path already requires a backup-first Hetzner package replacement gate.

## Next Gate

Before any server update, continue with:

1. read-only Hetzner pre-deploy inventory;
2. backup of the current `/opt/chessonline/server` and service unit;
3. copy and deploy the already built package only after backup and rollback paths are documented.
