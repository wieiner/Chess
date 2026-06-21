# Next Era Micro-Research Log

This log records small source checks before each Next Era phase. It is intentionally concise: each entry ties a decision to a source, a repo action, and a verification plan.

## Phase 00 - Current State / Baseline / Pending Work Check

Date: 2026-06-21

| Topic | Internet/source checked | Key finding | Decision for this repo | Files affected | Test/verify plan |
| --- | --- | --- | --- | --- | --- |
| PowerShell host for baseline scripts | Microsoft Learn, "Install PowerShell on Windows, Linux, and macOS": https://learn.microsoft.com/en-us/powershell/scripting/install/install-powershell | PowerShell 7 is the cross-platform `pwsh` edition; Windows PowerShell 5.1 remains separate. | Prefer `pwsh` for Next Era local gates, while keeping script compatibility with Windows PowerShell where CI or existing scripts require it. | `docs/NEXT_ERA_BASELINE_STATUS.md` | Run `pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List`, Native, Online, and `scripts\verify.ps1`. |
| Controlled MSBuild parallelism | Microsoft Learn, "MSBuild Command-Line Reference": https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference and "Build multiple projects in parallel with MSBuild": https://learn.microsoft.com/en-us/visualstudio/msbuild/building-multiple-projects-in-parallel-with-msbuild | `-maxcpucount`/`/m` controls parallel project builds; using a value makes node count explicit. | Treat bare `/m` as forbidden for project scripts. Baseline records actual controlled `/m:N` behavior and notes contention symptoms if observed. | `docs/NEXT_ERA_BASELINE_STATUS.md` | Use `-MSBuildMaxCpuCount 1` for baseline suite gates; verify may use its configured default and record result. |
| Test timeout discipline | Microsoft Learn, "dotnet test command" and VSTest timeout options: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test and https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-vstest | Official .NET test tooling supports explicit timeout concepts; unbounded test execution is not an acceptable gate. | Keep the repository's C# `TestProcessWatchdog` as the executable timeout authority for custom contract test runners. | `docs/NEXT_ERA_BASELINE_STATUS.md` | Confirm `run-tests.ps1` lists per-test timeouts and that Native/Online tests complete through bounded executable runs. |
| Current CI baseline | GitHub Actions run list via `gh run list --limit 10` | Latest `main` run `27878825241` is green for commit `77d1401`. | Start Next Era from `77d1401` with no local commits ahead/behind. | `docs/NEXT_ERA_BASELINE_STATUS.md` | Record `HEAD`, `origin/main`, local gate results, and last CI status. |
