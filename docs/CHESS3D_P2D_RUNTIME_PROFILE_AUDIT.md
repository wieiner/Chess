# Chess3D P2D Runtime Profile Audit

P2D inspected the boundary between profile JSON assets, `Chess3DEngine.dll`, `Chess3DApp.exe`, tests, and portable packaging.

## 1. Profile JSON locations

Canonical configurable rule profiles live in:

```text
assets/rules/profiles/
```

The legacy/runtime draft rules used by the WPF 3D window live in:

```text
src/ChessApp/Assets/Rules3D/
```

`src/Chess3DApp/Chess3DApp.csproj` links shared 3D UI files from `src/ChessApp` and now also links canonical profiles into runtime output as:

```text
Assets/Rules3D/Profiles/
```

## 2. Build output

The 3D app build output now includes profile JSON files under:

```text
src/Chess3DApp/bin/x64/Release/net8.0-windows/Assets/Rules3D/Profiles/
```

`scripts/verify.ps1` checks representative profile files in that output path.

## 3. ProductionOutput

`tools/release/Build-Production.ps1` copies the built `Chess3DApp` output directory into:

```text
ProductionOutput/Chess3D/
```

Because the profiles are now normal `Chess3DApp` content files, they are included in:

```text
ProductionOutput/Chess3D/Assets/Rules3D/Profiles/
```

`scripts/verify.ps1` checks representative Asgard and Rubik convergence profile files in the portable output.

## 4. Existing loader before P2D

Before P2D, `Chess3D_LoadRulesJson` only checked that the input looked like a JSON object. It parsed basic board size, active side count, max pieces, movement profile, king-safety flag, and side forward vectors.

Rule-profile fields such as `goalProfile`, `captureProfile`, `occupancyProfile`, `fusionProfile`, `corePhysicsProfile`, `layerTurnProfile`, `victoryProfile`, `coreCube`, `targetSlots`, and `anchorMode` were data/spec only.

## 5. Runtime state after P2D

P2D adds a lightweight current-profile state to `Chess3DEngine.dll`:

- ruleset id/version/display name;
- goal/capture/occupancy/fusion/core-physics/layer-turn/victory profile type;
- core cube bounds;
- anchor mode;
- required anchor count;
- anchor counts by side;
- game-over flag and winner side;
- last profile load error.

The old `Chess3D_LoadRulesJson` remains available for legacy draft rules. The new `Chess3D_LoadRuleProfileJson` is the stricter profile loader.

## 6. Single-occupancy board storage

The runtime board is still:

```text
std::array<int, 512>
piece = side * 10 + type
```

Public ABI functions such as `Chess3D_GetBoard`, `Chess3D_SetBoard`, `Chess3D_GetPiece`, and `Chess3D_SetPiece` still exchange one integer per cell.

## 7. Why P2D does not add CoreCell stacks

Asgard/Meru profiles describe future core multi-occupancy and fusion. Implementing that literally requires changing board-cell semantics, rendering, move generation, networking, serialization, GPU evaluation, and existing ABI compatibility.

P2D deliberately implements a compatibility projection instead:

- board storage remains single-occupancy;
- typed target slots are computed over the current board;
- anchors are counted from current single-piece cells;
- fusion, contested stacks, reserve, and stack movement remain future stages.

## 8. Safe runtime changes

The safe changes in P2D are append-only:

- add a stricter profile loader;
- add string/int ABI getters;
- compute target slots without changing board storage;
- compute simple anchor progress;
- detect centerAssembly victory only for matching profiles.

No old ABI function changes its signature.
