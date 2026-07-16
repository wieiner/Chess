# P4L Rubik Render Performance

## Optimization

Rubik cubies no longer allocate one body mesh and one mesh per sticker during
every scene refresh. The WPF renderer now shares seven frozen meshes:

- one unit plastic body;
- six unit sticker planes, one per world normal.

Each logical cubie retains its own `Model3DGroup` and one frozen scale/translate
transform. The group remains the layer-animation and hit-test owner. Frozen
materials are cached by ARGB and specular power, so the normal palette remains
bounded while selected and internal opacity variants stay correct.

Surface-only remains the default. For N=11 it renders 602 bodies and 726
stickers; full-cube mode renders all 1331 bodies while still rendering only 726
physical stickers.

## Reproducible probe

Build Release x64, then run:

```powershell
.\src\RubikApp\bin\x64\Release\net8.0-windows\RubikApp.exe --measure-render-performance
```

The opt-in probe writes ignored
`.tmp/rubik-render-performance/result.json`. It records:

- first surface rebuild and render wall time;
- eight repeated surface rebuild times and UI-thread allocations;
- managed/private memory before and after repeated refresh;
- full-cube rebuild/render time;
- rendering callbacks and wall time for one N=11 animated layer turn;
- shared mesh and cached material counts.

The probe operates on a deterministic N=11 scramble, commits exactly one test
turn, then reconstructs the user's trusted state from native history and checks
exact facelet equality. Manual states clean-fail because their physical cubie
orientation cannot be restored.

## Recorded result

The Release x64 probe ran under `TestProcessWatchdog` and completed in 7.2
seconds on a 16 logical-processor Windows host with .NET 8.0.28.

| Measurement | Result |
| --- | ---: |
| Shared meshes | 7 |
| Cached materials | 15 |
| N=11 surface bodies / stickers | 602 / 726 |
| First surface rebuild | 25.3 ms |
| First surface rebuild + render wall | 47.3 ms |
| Eight-refresh average rebuild | 41.8 ms |
| Average UI-thread allocation per rebuild | 2.26 MB |
| Managed live bytes after eight refreshes | +96 bytes after full GC |
| N=11 full bodies / stickers | 1331 / 726 |
| Full-cube rebuild | 100.5 ms |
| Full-cube rebuild + render wall | 105.9 ms |
| 260 ms requested layer animation | 332.3 ms wall, 10 render callbacks |

Private process memory rose from about 97 MB to 210 MB while WPF realized and
replaced repeated 3D scenes. Managed live memory stayed flat after collection,
so this is renderer/native resource retention rather than an accumulating
managed object graph. It remains a manual long-session observation point; the
measured surface rebuild and animation are interactive, and no DirectX rewrite
is justified in this phase.

The in-process PNG evidence was regenerated after mesh sharing. The 3x3 R-turn
and 11x11 inner-slice images retained correct winding, sticker colors, plastic
gaps, and zero invalid descriptors.
