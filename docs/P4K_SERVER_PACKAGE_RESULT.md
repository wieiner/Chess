# P4K Server Package Result

Date: 2026-07-11

## Scope

Phase 05 built a local, untracked Linux `ChessOnlineServer` deployment package for the currently committed P4K source. No files were copied to Hetzner, and no server, nginx, firewall, TLS/443, x-ui/Xray, Outline, Albatronix Docker, Unreal, PostgreSQL container, or runtime data path was changed.

## Source Identity

- Source commit: `f33240e87cd39ed6d2cfb7b612a8504c28f85586`
- Source short commit: `f33240e87cd3`
- Package id: `chessonline-linux-x64-f33240e87cd3`
- Runtime: `linux-x64`
- Deployment mode: framework-dependent .NET publish

## Native Library

The package includes the previously built and tested Linux native authority library:

- Local source path: `DeploymentOutput\linux-x64\ChessOnlineServer\libChess3DEngine.so`
- SHA-256: `A5B5E0B707D09B199D49FE62CA5B5F00895F28B1A78E4D082584776C9913694D`
- ELF check: `ELF64`, x86-64 shared object
- ABI check basis: exported `Chess3D_*` symbols were visible via `llvm-nm -D`

## Local Package Output

Untracked local output:

- Publish directory: `.tmp\publish\chessonline-f33240e87cd3`
- Manifest: `.tmp\publish\chessonline-f33240e87cd3\server-package-manifest.json`
- Archive: `.tmp\deploy\ChessOnlineServer-P4K-f33240e87cd3.tar.gz`
- Archive SHA-256: `2868635C362DA78BFA2CDD2796AB31EFE7CBEE610D277D7DB2DB192539CE8A1D`

These `.tmp` files are local operator artifacts and must not be committed.

## Manifest Summary

The generated package manifest reports:

- format: `chessonline-server-package-manifest`
- package id: `chessonline-linux-x64-f33240e87cd3`
- commit: `f33240e87cd39ed6d2cfb7b612a8504c28f85586`
- file count: `63`

Required files present:

- `ChessOnlineServer.dll`
- `ChessOnlineProtocol.dll`
- `ChessOnlinePersistence.dll`
- `ChessOnlineServer.runtimeconfig.json`
- `ChessOnlineServer.deps.json`
- `appsettings.Production.sample.json`
- `server-build.json`
- `server-package-manifest.json`
- `libChess3DEngine.so`
- `Assets/Rules3D/Profiles/classic_six_side_3d_v0_1.json`
- `Assets/Rules3D/Profiles/single_side_3d_v0_1.json`
- `Assets/Rules3D/Profiles/asgard_convergence_3d_v0_1.json`
- `Assets/Rules3D/Profiles/rubik_convergence_3d_v0_1.json`
- `Assets/Rules3D/Profiles/hodge_projection_duel_3d_v0_1.json`

The schema file is also present, but it is not counted as a sixth runtime rule profile.

## Excluded Files

The publish script now removes local/development-only output before manifest generation and archive creation:

- `*.pdb`
- `appsettings.Development.json`
- `appsettings.Local.json`

The script also rejects Windows native `Chess3DEngine.dll` in Linux packages and can fail on secret-like/runtime filenames with `-FailOnSecretLikeFiles`.

## Commands Used

```powershell
dotnet build src\ChessOnlineServer\ChessOnlineServer.csproj -c Release

$Commit = (git rev-parse HEAD).Trim()
$Short = (git rev-parse --short=12 HEAD).Trim()
$Publish = ".tmp\publish\chessonline-$Short"

pwsh -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\deploy\Publish-ChessOnlineServer-Linux.ps1 `
  -OutputPath $Publish `
  -NativeLibraryPath "DeploymentOutput\linux-x64\ChessOnlineServer\libChess3DEngine.so" `
  -CommitSha $Commit `
  -PackageId "chessonline-linux-x64-$Short" `
  -Clean `
  -FailOnSecretLikeFiles

$Archive = ".tmp\deploy\ChessOnlineServer-P4K-$Short.tar.gz"
tar -czf $Archive -C $Publish .
Get-FileHash $Archive -Algorithm SHA256
```

## Verification

- Server project build: PASS
- Publish script with native `.so`, build identity, manifest and secret-like file guard: PASS
- Archive content check: PASS
- Forbidden deploy content check: PASS
- Server deployment: not performed in this phase

## Next Step

The next P4K phase can use this package shape for a backup-first server replacement of `/opt/chessonline/server`, without changing nginx, UFW, 443/TLS, x-ui/Xray, Outline, Albatronix, Unreal, or runtime store/keyring paths.
