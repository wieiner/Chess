# P4L Rubik Visual Contracts

## Authority boundary

`RubikVisuals` is a pure `net8.0` library. It has no WPF dependency and does not
own Rubik state. Its input is a snapshot of authoritative native data:

- current coordinate and cubie ID;
- physical sticker mask;
- exact integer orientation basis, when available;
- canonical `U/R/F/D/L/B` facelets.

`RubikVisualDescriptorBuilder` turns that snapshot into immutable cubie,
sticker, and scene descriptors. `RubikApp` consumes the same descriptors that
the headless contract executable checks.

## Descriptor invariants

The contracts enforce:

- exactly `6*N*N` rendered stickers;
- exactly `N*N` stickers of each color ID;
- 8 corners, `12*(N-2)` edges/wings, `6*(N-2)^2` centers, and `(N-2)^3` internals;
- three distinct sticker colors on every corner;
- two distinct sticker colors on every edge/wing;
- one axis-aligned unit normal per sticker;
- no duplicate world normal on one cubie;
- no physical sticker on an internal cubie;
- unchanged counts after outer, inner, wide, and whole-cube turns;
- explicit shell fallback for facelet-only imports with unavailable orientation.

Surface-only mode changes plastic body count, not physical sticker count.

## Headless coverage

`RubikVisualContractTests` creates real native cubes and checks:

- solved 2x2, 3x3, 8x8, and 11x11;
- N=11 outer turn, inner slice, wide turn, and whole-cube rotation;
- deterministic scramble;
- reverse-history playback;
- facelet-only import fallback;
- the compact `solved-3x3.visual.json` representative fixture.

The fixture records a three-color corner, a two-color edge, and a one-color
center. It is deliberately small and deterministic; desktop screenshots are
not the sole regression authority.

Run the complete Rubik contract surface with:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 `
  -Suite Rubik -SkipSolutionBuild -SkipBenchmark -MSBuildMaxCpuCount 1
```
