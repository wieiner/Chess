# P4L State And Asset Baseline

Date: 2026-07-16  
Branch: `main`

## Repository Baseline

- starting commit: `5a64aa74271436070104f16d81b87757fe564d6a`;
- `HEAD` and `origin/main`: identical;
- initial worktree: clean;
- latest pre-P4L GitHub Actions run: `29263531808`, success;
- P4K remote online UX and operational hardening: complete;
- Hetzner/server/network state: frozen for P4L.

P4L is a local product, persistence, solver, and asset-library program. It does
not deploy a server package and must not modify HTTP 80, 443, nginx, UFW,
x-ui/Xray, Outline, Albatronix, Unreal, PostgreSQL, DNS, or TLS.

## Baseline Build

The requested `dotnet build` command was attempted first for `RubikApp`. It
failed before compilation with `MSB4278` because the .NET CLI invocation did
not resolve the Visual C++ `Microsoft.Cpp.Default.props` imported by
`RubikEngine.vcxproj`. This is a tool-entrypoint limitation for the mixed
C++/WPF project, not an application compile failure.

The installed Visual Studio MSBuild 18.7.8 was then used sequentially with
`/m:1 /nr:false`:

| Product | Result | Release output |
| --- | --- | --- |
| RubikApp | PASS | `src/RubikApp/bin/x64/Release/net8.0-windows/RubikApp.dll` |
| ChessApp | PASS | `src/ChessApp/bin/x64/Release/net8.0-windows/ChessApp.dll` |
| Chess3DApp | PASS | `src/Chess3DApp/bin/x64/Release/net8.0-windows/Chess3DApp.dll` |

The decomposed test runner `-List` command passed and reported the existing
native Chess2D, native Chess3D, Rubik, GPU, managed Online, hosted SignalR, and
optional quick benchmark entries with bounded timeouts.

## Standalone Rubik Baseline

Implemented:

- cube sizes 2 through 32;
- `N*N*N` integer cubie IDs;
- X/Y/Z layer permutation and trusted move history;
- notation parsing, history display, and animated playback;
- surface-only/full rendering, orbit/pan/zoom, and selected-layer highlight;
- text-box integer state export/load;
- `SolveByReverseHistory` for states produced by trusted recorded moves.

Current limitations:

- no per-face sticker/facelet state;
- no explicit cubie orientation state;
- one material/color is selected for an entire rendered cubie;
- corners and edges therefore do not show three/two independent stickers;
- text export is not a versioned portable disk format;
- a manually loaded arbitrary state clears the trusted solution boundary and
  cannot be solved by reverse history;
- there is no physical-cube face editor or arbitrary-state solver.

Standalone `RubikApp` remains distinct from the Chess3D `Rubik Convergence`
RuleProfile. Their formats and rules must not be merged.

## Chess2D Baseline

Implemented:

- complete local board rules and legal move handling;
- FEN load/copy;
- setup/editor controls;
- undo and UI move list;
- AI search and optional GPU backend;
- 2D and WPF `Viewport3D` presentations;
- selectable OBJ model set with procedural fallback;
- local endpoint/network foundations.

Current persistence/protocol limitations:

- FEN stores a position, not a complete played game or application session;
- the UI move list is display-oriented and is not a canonical structured SAN
  game record;
- no complete PGN import/export workflow;
- no versioned `chess-session.json` save/load;
- no autosave/crash-recovery workflow;
- no standalone proven UCI engine executable.

## Model Pipeline Baseline

- canonical runtime root: `assets/models/chess/pieces`;
- catalog: `piece_sets.json`;
- compatibility format: OBJ/MTL with best-effort diffuse textures;
- generated staging: `assets/models/chess/pieces/generated/<set-id>`;
- missing models/materials use readable procedural fallback;
- GLB/glTF is documented as a preferred future runtime format but has no
  selected loader or runtime implementation;
- FBX and `.blend` are not runtime formats and no unified source-model root
  exists yet.

No unknown-license or large binary asset is added in Phase 00.

## Chess3D Profile Invariant

The runtime profile directory contains one schema plus exactly five real JSON
RuleProfiles:

1. `classic-six-side-3d-8x8x8-v0.1`;
2. `single-side-3d-8x8x8-v0.1`;
3. `asgard-convergence-3d-8x8x8-v0.1`;
4. `rubik-convergence-3d-8x8x8-v0.1`;
5. `hodge-projection-duel-3d-8x8x8-v0.1`.

Schemas, scenarios, playthroughs, regressions, standalone Rubik puzzles, and
asset manifests are not additional game profiles.

