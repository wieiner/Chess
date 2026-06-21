# Next Era Linux Server Package Result

Date: 2026-06-21

## Scope

Phase 03 completed a local `linux-x64` framework-dependent publish package for `ChessOnlineServer` using the tested Linux-native `libChess3DEngine.so` copied from the temporary Hetzner build workspace.

No Linux native binary was committed. The `.so` was copied to a local temp file and used only as an input to the publish script.

## Inputs

- Linux native artifact source: `<HETZNER_HOST>:/tmp/chess3d-build/build-linux-clang/libChess3DEngine.so`
- Local temporary copy: `%TEMP%\libChess3DEngine-next-era.so`
- Publish script: `scripts\deploy\Publish-ChessOnlineServer-Linux.ps1`
- Output root: `DeploymentOutput\linux-x64\ChessOnlineServer`

## ABI Check

Command:

```powershell
$so = Join-Path $env:TEMP 'libChess3DEngine-next-era.so'
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tools\abi\Compare-Chess3DEngineExports.ps1 -LinuxLibraryPath $so
```

Result:

```text
Expected Chess3D exports: 154
Windows Chess3DEngine.dll: OK (154 Chess3D exports found; 154 required exports present).
Linux libChess3DEngine.so: OK (154 Chess3D exports found; 154 required exports present).
```

## Publish Command

```powershell
$so = Join-Path $env:TEMP 'libChess3DEngine-next-era.so'
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Publish-ChessOnlineServer-Linux.ps1 -NativeLibraryPath $so
```

Result: publish completed successfully.

## Package Contents Verified

The package contains:

- `ChessOnlineServer.dll`
- `libChess3DEngine.so`
- `appsettings.Production.sample.json`
- `Assets\Rules3D\Profiles\classic_six_side_3d_v0_1.json`
- `Assets\Rules3D\Profiles\hodge_projection_duel_3d_v0_1.json`
- `Assets\Rules3D\SignalRScenarios\signalr_hello_connect_v0_1.json`
- `Deploy\linux\chessonline-server.service.template`
- `Deploy\linux\nginx-chessonline.conf.template`

The publish script now copies the native library under the canonical Linux loader name `libChess3DEngine.so` and removes temporary-name variants such as `libChess3DEngine-next-era.so`.

## Secret / Runtime Artifact Check

The package check found no runtime database, SQLite file, private key, `.pfx`, `.pem`, token file, password file, Data Protection keyring, or `chess3d-online-store.json`.

Scenario names that contain `no_secret` are descriptors and are allowed.

## Optional Verify Hook

Normal CI does not require remote Linux native artifacts.

Operators can verify the Linux package locally with:

```powershell
$env:CHESS_VERIFY_LINUX_PACKAGE = "1"
$env:CHESS3D_LINUX_NATIVE_LIBRARY = "$env:TEMP\libChess3DEngine-next-era.so"
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1 -SkipBenchmark
```

When enabled, `verify.ps1` asserts:

- the native input exists;
- linux-x64 publish succeeds;
- canonical `libChess3DEngine.so` exists in the package;
- representative profiles, scenarios, deploy templates, and sample config are present;
- Windows `Chess3DEngine.dll` is not included in the Linux package.

## Remaining Work

The package has not yet been executed on Hetzner in this phase. Next phase: copy this package to a temporary Hetzner path and run a Kestrel-only smoke on `127.0.0.1:5077`.
