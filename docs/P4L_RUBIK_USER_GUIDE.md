# Rubik Studio User Guide

## Build and launch

Build `Release|x64` with Visual Studio or Visual Studio MSBuild, then launch:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' .\src\RubikApp\RubikApp.csproj /restore /m:1 /nr:false /p:Configuration=Release /p:Platform=x64
.\src\RubikApp\bin\x64\Release\net8.0-windows\RubikApp.exe
```

Set `Размер` from 2 through 32 and choose `Применить`. The renderer has been exercised at N=2, N=3, N=8, and N=11; native turn/state contracts additionally cover N=4 and N=5.

## Inspect and turn

- Left-drag the background to orbit; Shift/Right-drag pans; the wheel zooms.
- Click/drag a cubie to select and turn its logical layer.
- The top controls can rotate any X/Y/Z layer by +90, 180, or -90 degrees.
- `Surface only` is recommended for large cubes. Corners render three stickers, edges/wings two, and centers one.
- `Scramble` uses a reproducible seed and length.
- The Notation tab accepts face notation, inner/wide moves, whole-cube rotations, and internal forms such as `Z5x2`.

## State and physical input

- `Save State` and `Save As` write portable `.rubik.json` facelets and a canonical hash.
- `Load State` validates and applies through a candidate native handle, so a failed load does not alter the live cube.
- `Physical Editor` opens six U/R/F/D/L/B grids. Complete them, review validation issues, and apply only when accepted.
- `Export Moves`/`Import Moves` are the notation/history compatibility path. Verified solver artifacts use the Solver tab's `Save Solution`/`Load Solution` buttons.

See `P4L_PHYSICAL_CUBE_INPUT_GUIDE.md` and `P4L_RUBIK_STATE_FILE_GUIDE.md` for detailed workflows.

## Solver tab

1. Select the required cube size and load/enter the state.
2. Press `Validate` and review validation, hash, capability, and failure fields.
3. Press `Solve`.
4. For a valid 2x2 state, bounded IDDFS searches off the UI thread and the candidate is replayed on a fresh native engine. Only a verified result enables Save/Play/Step.
5. `Step` applies one move; `Previous Step` applies its inverse; `Play Solution` animates from the current cursor.
6. A state-hash mismatch blocks playback instead of applying a solution to the wrong position.

Current capability boundary:

- arbitrary imported 2x2: implemented with explicit time/depth/node limits;
- arbitrary 3x3: validation works, search backend deferred;
- N>=4: reduction plan/checkpoint Level A only, with no fabricated center/wing moves;
- trusted in-session history: the original reverse-history controls remain available on the Scramble tab.

`Pause` and `Resume` remain disabled because current solver backends do not support them. `Cancel` requests cooperative cancellation and closing the window cancels active solver work.

