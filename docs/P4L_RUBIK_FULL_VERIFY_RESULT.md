# P4L Rubik Full Verify Result

Date: 2026-07-16

## Result

The Phase 28 Rubik product gate passed on Windows x64. The decomposed runner selected three Rubik contract executables; all completed under their 120-second watchdog without timeout.

| Executable | Result | PASS assertions |
| --- | --- | ---: |
| `RubikEngineContractTests` | PASS | 170 |
| `RubikVisualContractTests` | PASS | 68 |
| `RubikStateContractTests` | PASS | 335 |

## Commands

```powershell
git diff --check
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -Suite Rubik -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1 -TestTimeoutSeconds 120 -GlobalTimeoutSeconds 600

& $msbuild .\src\RubikEngine\RubikEngine.vcxproj /restore /m:1 /nr:false /p:Configuration=Release /p:Platform=x64
& $msbuild .\src\RubikApp\RubikApp.csproj /restore /m:1 /nr:false /p:Configuration=Release /p:Platform=x64
```

Both product builds passed. `dotnet build` is not the mixed C++/WPF authority because it does not provide Visual C++ targets; Visual Studio MSBuild is used intentionally.

## Coverage

- Legacy integer-cell ABI, facelet ABI, exact U/R/F/D/L/B coordinates, orientation basis, and sticker masks passed.
- X/Y/Z outer turns, inner slices, wide turns, whole-cube rotations, notation, deterministic scramble, and trusted reverse-history playback passed.
- Visual descriptors preserve exact sticker orientation. Corners expose three colors and edges/wings expose two colors; facelet-only imports use the explicit shell fallback.
- Portable state serialization, canonical hash, bounded parser, atomic save/replace, backup, invalid-load no-mutation, and N=11 roundtrip passed.
- Physical editor draft, structured diagnostics, cubie decomposition, small-cube solvability invariants, and honest NxN partial validation passed.
- Managed move simulation matched native execution for all tested N=2 and N=3 outer moves.
- Six arbitrary imported/constructed 2x2 fixtures were solved by the bounded owned backend and independently replay-verified on fresh native handles.
- Solution replay rejects malformed, illegal, truncated, incorrect, and cancelled candidates without mutating the input.
- Reduction planning/checkpoint contracts passed for N=4, N=5, N=7, and N=11.
- N=11 achieved declared Level A: load, validation, decomposition, guided phase plan, atomic checkpoint, and explicitly incomplete/unverified move artifact. Center solving, wing pairing, and arbitrary N=11 solution generation are not implemented.

## Product boundary

Arbitrary 3x3 solving remains deferred. Pause/resume remains disabled for solver implementations that advertise no pause support. The existing native ABI and trusted reverse-history flow remain available.
