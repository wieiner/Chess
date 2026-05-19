# Build and Release Layout

## Main Entry Points

- `package_all.bat` builds Release x64 and creates all portable products.
- `package_2d.bat` builds and publishes only Chess2D.
- `package_3d.bat` builds and publishes only Chess3D.
- `package_rubik.bat` builds and publishes only Rubik.
- `package_online.bat` builds and publishes only Chess Online Integrations.
- `package_benchmark_2d.bat` builds and publishes only the 2D benchmark executable.
- `clean_outputs.bat` removes generated build and portable output folders without touching source files.

All package scripts call `tools\release\Build-Production.ps1`. Keep product-specific release logic there instead of duplicating it in batch files.

## Portable Output

The production output root is `ProductionOutput`.

Generated product folders:

- `ProductionOutput\Chess2D`
- `ProductionOutput\Chess3D`
- `ProductionOutput\Rubik`
- `ProductionOutput\ChessOnlineIntegrations`
- `ProductionOutput\Chess2DBenchmark`

Portable folders include executables, runtime configs, DLL dependencies, and assets needed at runtime. They intentionally exclude debug symbols, import libraries, intermediate files, logs, and old `dist` output.

When `ChessCudaBackend.dll` is present and CUDA Toolkit/runtime is installed on the build machine, `cudart64*.dll` is copied into the same portable folder. If it is not found, the applications still keep their CPU/Direct3D fallback behavior, but CUDA execution on the target machine requires the NVIDIA runtime stack.

## Recommended Flow

1. Run `package_all.bat`.
2. Use `list_exes.bat` to see final launch points.
3. Run products from `ProductionOutput` or via the root `run_*.bat` wrappers.

For a clean slate without rebuilding, run `clean_outputs.bat`.
