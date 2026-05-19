# Build and Release

## Visual Studio

Open `Chess.sln` in Visual Studio 2022 or newer and build:

- Configuration: `Release`
- Platform: `x64`

The default solution configuration builds all required products except `ChessCudaBackend`. CUDA is optional and can be built separately on a machine with CUDA Toolkit MSBuild integration installed.

## Command Line

Use `vswhere` to find MSBuild, or run with a known Visual Studio path:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" .\Chess.sln /restore /m /p:Configuration=Release /p:Platform=x64
```

`scripts\verify.ps1` also resolves MSBuild across Community, Professional, Enterprise, and BuildTools installations.

## Without CUDA

No CUDA Toolkit is required for the default build. `ChessGpuBackend.dll` remains available and can fall back to Direct3D/CPU paths. `ChessCudaBackend.dll` is optional:

1. Build `src\ChessCudaBackend\ChessCudaBackend.vcxproj` separately on a CUDA-enabled build machine.
2. Place `ChessCudaBackend.dll` next to `ChessGpuBackend.dll`.
3. If available, package scripts copy `cudart64*.dll` next to the executable.

## Development Outputs

- `src\ChessApp\bin\x64\Release\net8.0-windows\ChessApp.exe`
- `src\Chess3DApp\bin\x64\Release\net8.0-windows\Chess3DApp.exe`
- `src\RubikApp\bin\x64\Release\net8.0-windows\RubikApp.exe`
- `src\ChessOnlineApp\bin\x64\Release\net8.0-windows\ChessOnlineApp.exe`
- `bin\x64\Release\Chess2DBenchmark.exe`

## Portable Packaging

Run:

```bat
package_all.bat
```

or directly:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\release\Build-Production.ps1 -Product All
```

Products are generated under:

- `ProductionOutput\Chess2D`
- `ProductionOutput\Chess3D`
- `ProductionOutput\Rubik`
- `ProductionOutput\ChessOnlineIntegrations`
- `ProductionOutput\Chess2DBenchmark`

`ProductionOutput/` is generated output and is not committed.

## Root Scripts

- `run_chess_2d.bat`
- `run_chess_3d.bat`
- `run_rubik.bat`
- `run_online.bat`
- `run_benchmark_2d.bat`
- `run_3d_six_clients.bat`
- `list_exes.bat`

For full local verification:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```
