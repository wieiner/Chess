# Chess Advisor

Current map: see `docs/NEXT_ERA_PROJECT_MAP.md` for the latest product/deploy status. In short, the repository contains Chess2D, Chess3D, a physical-state-aware NxN Rubik Studio, online integration apps, a portable `net8.0` ChessOnlineServer, and exactly five real Chess3D RuleProfiles. P4L adds multi-color Rubik cubies, portable state files, physical input/validation, verified arbitrary 2x2 solving, and an honest Level A NxN reduction boundary. Linux server smoke has passed through Hetzner systemd + Nginx public HTTP; TLS/domain and production hardening remain separate open work.

## Structure

- `src/ChessEngine` - native C++ DLL with chess rules, legal move generation, FEN, undo and AI search.
- `src/ChessGpuBackend` - stable native GPU ABI with CUDA, Direct3D 11, and CPU fallback routing.
- `src/ChessCudaBackend` - optional native CUDA DLL compiled from `.cu` kernels and loaded dynamically by `ChessGpuBackend.dll`.
- `src/Chess3DEngine` - separate native DLL for experimental cube chess on an 8x8x8 board.
- `src/RubikEngine` - separate native DLL for NxNxN Rubik facelets, cubie orientation, rotations and trusted reverse-history solving.
- `src/ChessApp` - ordinary 8x8 chess C# WPF frontend. It uses P/Invoke and keeps chess logic inside the DLL.
- `src/Chess3DApp` - separate cube-chess WPF frontend. It starts directly into the 8x8x8 game and runs independently from `ChessApp.exe`.
- `src/RubikApp` - separate WPF 3D frontend for NxN rendering, physical state files/input, validation and capability-aware solving.
- `src/ChessOnlineApp` - separate WPF hub for internet integrations, online portal accounts, read-only platform APIs, ICS text servers and the future 3D chess web relay.
- `src/ChessOnlineServer` - portable ASP.NET Core/SignalR authority for Chess3D online play.
- `src/Chess2DBenchmark` - separate native console benchmark executable for ordinary 2D chess engine and CPU/Direct3D/CUDA batch evaluation.
- `rude-resource/` - local ignored read-only archive with historical/source materials. It is not part of Git and is not used as runtime content.
- `src/.../Assets` - runtime assets that are copied to application output during build.
- `ProductionOutput/` - generated portable release output. It is not stored in Git.

## Executables

User-facing executables after Release x64 build:

```text
src\ChessApp\bin\x64\Release\net8.0-windows\ChessApp.exe
src\Chess3DApp\bin\x64\Release\net8.0-windows\Chess3DApp.exe
src\RubikApp\bin\x64\Release\net8.0-windows\RubikApp.exe
src\ChessOnlineApp\bin\x64\Release\net8.0-windows\ChessOnlineApp.exe
src\ChessOnlineServer\bin\x64\Release\net8.0\ChessOnlineServer.exe
bin\x64\Release\Chess2DBenchmark.exe
```

Root launch scripts:

```text
run_chess_2d.bat
run_chess_3d.bat
run_rubik.bat
run_online.bat
run_benchmark_2d.bat
run_3d_six_clients.bat
list_exes.bat
```

## Engine

The C++ engine implements:

- legal move filtering through king-safety checks;
- castling, en passant and pawn promotion;
- check, checkmate, stalemate and fifty-move draw status;
- FEN load/export;
- negamax search with alpha-beta pruning;
- optional quiescence search for capture-heavy leaf positions;
- material and piece-square evaluation;
- passed-pawn, isolated-pawn, king-safety and endgame-king activity evaluation;
- built-in exact draw probes for trivial insufficient-material endings;
- optional move ordering and a small transposition table;
- optional GPU-assisted root move ordering through `ChessGpuBackend.dll`;
- opening variation controls that choose between near-equal early moves while biasing toward center, development and castling;
- search telemetry: requested depth, completed depth, timeout flag, elapsed time, nodes and score;
- extended search options through `Chess_MakeBestMoveEx`.

Depth is no longer artificially limited by the UI. The engine accepts depths up to 64, but practical searches above roughly 8 plies should use the time limit and automatic iterative deepening. The UI no longer caps the time limit at 60 seconds; enter any non-negative millisecond value.

## GPU Scaffold

`ChessGpuBackend.dll` exposes a stable ABI:

- `ChessGpu_IsAvailable`;
- `ChessGpu_GetBackendInfo`;
- `ChessGpu_EvaluateBatch`;
- `ChessGpu_Evaluate3DBatch`;
- `ChessGpu_GenerateRubikBatch`.

The implementation now tries CUDA first by loading `ChessCudaBackend.dll` from the same folder as `ChessGpuBackend.dll`. If the CUDA DLL, CUDA device, or driver path is unavailable, it falls back to Direct3D 11 compute and then to a CPU mirror. The C++ engine can call this DLL to score root child positions before alpha-beta, improving move ordering while keeping GPU work separate from the rule engine.

`ChessCudaBackend.dll` contains real CUDA kernels for ordinary boards, 512-cell cube boards, and batched Rubik rotations. The CUDA Toolkit is needed to build that DLL; running the apps only needs the normal NVIDIA driver stack plus the DLLs copied next to the exe. CPU fallback remains automatic. See `docs\CUDA_ARCHITECTURE.md`.

## Frontend

The WPF app includes:

- classic BMP pieces copied into tracked runtime assets from the local historical archive;
- a transparent PNG set derived from the classic BMP pieces;
- a fallback Unicode piece theme;
- replaceable figure files under `Assets\Figures\ClassicBmp`;
- replaceable transparent figure files under `Assets\Figures\TransparentPng`;
- board coordinates on every side;
- board orientation for white or black;
- setup mode with drag-and-drop side palettes for composing a position, choosing side to move, applying it as FEN, and then searching from it;
- a DirectX-backed WPF `Viewport3D` board mode with mouse orbit and zoom;
- scan-based 3D model sets under `Assets\Models\<SetName>`;
- OBJ overrides for board tiles and pieces;
- source-first 3D loading from `src\ChessApp\Assets\Models`, then output-folder loading after build;
- auto-normalization of OBJ scale/centering so imported Blender-style models sit on the board;
- 3D diagnostics in the status area: selected set, loaded OBJ count and procedural fallback count;
- manual search controls for depth, time limit, automatic iterative deepening, quiescence, transposition table, move ordering, piece-square tables and king safety.
- opening randomness and opening-ply controls;
- Syzygy tablebase path scanning;
- a simple TCP JSON-lines endpoint for host/connect play between two app instances.

## 3D Model Sets

Create a folder such as `Assets\Models\MySet`.

Expected optional OBJ files:

```text
Board\light_tile.obj
Board\dark_tile.obj
Pieces\white_pawn.obj
Pieces\white_knight.obj
Pieces\white_bishop.obj
Pieces\white_rook.obj
Pieces\white_queen.obj
Pieces\white_king.obj
Pieces\black_pawn.obj
Pieces\black_knight.obj
Pieces\black_bishop.obj
Pieces\black_rook.obj
Pieces\black_queen.obj
Pieces\black_king.obj
```

Missing files fall back to procedural WPF 3D geometry.

During development, the app scans `src\ChessApp\Assets\Models` first. After build, it also scans the copied output folder next to `ChessApp.exe`. If a model is loaded successfully, the status line in 3D mode shows a non-zero `loaded` count; missing or failed OBJ files increase `fallback`.

## Draw Rules

The engine tracks repeated positions by position identity, not by repeated move text: pieces, side to move, castling rights and effective en-passant availability are included. It catches non-consecutive cycles too.

Default behavior follows the Lichess/FIDE shape: threefold repetition can be auto-claimed by preference, while fivefold repetition is automatic. The 50-move claim and 75-move automatic draw thresholds are also present in the native rules object.

## Endgame Tables

The engine has built-in exact draw probes for simple insufficient-material endgames, plus a Syzygy-ready path scanner. Put `.rtbw` and `.rtbz` files under `Assets\Tablebases` or enter another semicolon-separated path in the UI. The current scanner reports file counts and max-piece coverage; full WDL/DTZ probing is kept behind the native tablebase ABI for the next integration step.

## Endpoint API

The WPF app can host or connect to a TCP endpoint. Messages are newline-delimited JSON:

```json
{"Type":"fen","Fen":"start-or-current-fen"}
{"Type":"move","FromFile":4,"FromRank":1,"ToFile":4,"ToRank":3,"Promotion":0}
```

This is intentionally small: two local or networked app instances can exchange positions and moves now, while another program can implement the same JSON protocol later.

## Cube Chess 8x8x8

The 3D chess work is isolated from the normal chess engine. Run it as a separate executable:

```text
src\Chess3DApp\bin\x64\Release\net8.0-windows\Chess3DApp.exe
```

- Native module: `Chess3DEngine.dll`.
- Board: 512 cells, addressed as `x/y/z` or `a1.L1` style in status text.
- Piece code: `side * 10 + type`, with six side slots reserved for cube faces.
- Default setup: six cube-face sides with 16 classic pieces per side, placed on each face's central 4x4 patch.
- Rules file: `Assets\Rules3D\cube8x8x8_draft.json`.
- Draft movement profile: king 26 adjacent cells, rook orthogonal 3D axes, bishop 2D/3D diagonals, queen rook+bishop, knight 3D L moves, pawn forward by side vector with captures on the forward-adjacent plane.
- The cube window scans the same OBJ model-set structure as the ordinary 3D chess board: `Assets\Models\<SetName>\Pieces` and `Assets\Models\<SetName>\Board`.
- The `View` selector switches between a single slice and all 512 cells at once. In `All` mode, the `Full` toggle expands the 3D viewport across the work area and hides the side panels.
- `Axis` changes the slice family between horizontal `Z`, rank/depth `Y`, and file `X`; `Alpha` controls the transparency falloff around the selected layer.
- `Grid` controls board visibility: all planes, selected slice only, outer shell, top/bottom planes, middle planes, occupied cells only, or hidden for a pieces-only view.
- Camera controls: left drag orbits, right drag/Shift+drag pans through the cube, mouse wheel flies in/out, Ctrl+wheel or `Cam Up/Down` raises and lowers the camera target.
- In all-layer view, board cells and pieces on distant layers are rendered more transparently so the whole cube can be inspected without hiding the interior. Zooming closer strengthens the distant-layer fade; zooming out makes the full cube more visible.
- `Moves` enables selected-piece move traces. Click a figure in the 2D slice or directly in the 3D view to show destinations produced by the current 3D movement profile.
- `Rubik` rotates any 8x8 layer around `Z`, `Y`, or `X` by `CCW`, `CW`, or `180`. The rotation is done in `Chess3DEngine.dll`, so pieces really move to new cells rather than only being redrawn.
- The 3D network block is separate from the ordinary 2D endpoint. A host accepts six player slots plus six group slots, so one six-player table can also bridge to other six-player tables. Messages are JSON-lines with ids, source node, slot, group id, and action type.
- Current 3D network actions: whole-board sync for new peers, `move3d`, and `rotate3d`. The message sequence is the first step toward a strict turn coroutine where chess moves and Rubik layer turns are both first-class timeline actions.
- To test a full six-seat local 3D table, build Release x64 and run `run_3d_six_clients.bat`. It launches seat 1 as host and seats 2-6 as clients connected to `127.0.0.1:5308`.

The current 3D rules are intentionally marked as draft. The module already supports setup, JSON rule loading, move generation, direct moves, position text, 3D preview, and a small generic AI search. Exact law decisions such as six-player starts, king safety, check/mate semantics, castling, en passant, and final pawn rules can evolve inside this module without touching `ChessEngine.dll`.

## Rubik NxNxN

The Rubik assembly project is a third separate executable:

```text
src\RubikApp\bin\x64\Release\net8.0-windows\RubikApp.exe
```

- Native module: `RubikEngine.dll`.
- State: physical U/R/F/D/L/B facelets plus legacy integer cells, with supported dimensions 2 through 32.
- Operations: rotate any `Z`, `Y`, or `X` layer by `+90`, `180`, or `-90`.
- Scramble: reproducible by seed and length.
- Trusted-history solver: preserves the original inverse-history path for positions produced in the current session.
- Arbitrary solver: bounded owned 2x2 IDDFS with independent replay verification; arbitrary 3x3 is deferred.
- NxN reduction: Level A validation/decomposition/guidance/checkpoint only. It does not solve arbitrary 11x11 states.
- Portable state: atomic `.rubik.json` save/load with strict validation and canonical hash; complete verified solutions use `.rubikmoves`.
- Physical input: six-face draft editor with structured diagnostics and explicit transactional apply.
- The frontend renders multi-color physical stickers in WPF `Viewport3D`, with three-color corners, two-color edges/wings, surface-only/full rendering, orbit, pan and zoom. N=11 rendering and state roundtrip are tested.
- Moves can be animated at selectable speed. Manual layer turns, solution playback, and notation playback all use smooth layer rotation before committing the native state.
- The notation parser accepts compact and spaced formulas such as `RUR'U'`, `R U R' U'`, inner-slice forms from 4x4 tutorials such as `r`, `Uu`, `Rr`, wide forms such as `Rw`, `3Rw`, cube rotations `x/y/z`, parentheses such as `(Uu)2`, and the internal coordinate notation `Z5x2`.
- `History` exports the current trusted move history as coordinate notation; `Notation` applies formulas from the text area; `Play` applies the same formula with animation.

Start with `docs/P4L_RUBIK_USER_GUIDE.md`; format and physical-entry details are in `docs/P4L_RUBIK_STATE_FILE_GUIDE.md` and `docs/P4L_PHYSICAL_CUBE_INPUT_GUIDE.md`.

## Internet Integrations

Internet integration is intentionally split out of the ordinary 2D chess board. Run it separately:

```text
src\ChessOnlineApp\bin\x64\Release\net8.0-windows\ChessOnlineApp.exe
```

The online hub owns the C# `LichessClient` foundation under `src\ChessOnlineApp\Integrations`:

- Board API mode for normal accounts and physical-board style play.
- Bot API mode for bot accounts where engine play is allowed.
- NDJSON stream readers for incoming events and game streams.
- UCI move posting, chat posting, challenge creation, bearer token auth, and basic 429 backoff.

The online hub exposes the token/profile/connection UI separately from `ChessApp.exe`, so the normal chess UI stays a chess board and engine advisor surface.

The current P4K Windows client also provides hands-on Chess3D play against the
single-server Hetzner diagnostic deployment: server health/diagnostics,
temporary users, five-profile matchmaking, authoritative legal targets,
one-click moves, explicit reconnect/resume, a read-only spectator mode, lobby
discovery, and sanitized network reports. Public HTTP 80 is for development
only; do not use real credentials. Start with
`docs\P4K_REMOTE_UX_USER_GUIDE.md`; operators should use
`docs\P4K_HETZNER_OPERATOR_GUIDE.md`.

3D chess has a separate `Chess3DInternetRelayClient` WebSocket client. Lichess cannot host custom 8x8x8 six-sided rules, so the 3D internet path is a relay-room protocol for `Chess3DNetworkMessage` payloads (`move3d`, `rotate3d`, sync messages) rather than a Lichess integration.

See `docs\ONLINE_INTEGRATION_ARCHITECTURE.md` for the full 3D relay protocol and ordinary chess portal capability matrix.

## Build

Open `Chess.sln` in Visual Studio 2022 or newer and build `x64`.

Command line:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' .\Chess.sln /restore /m /p:Configuration=Release /p:Platform=x64
```

The ordinary chess app is produced at:

```text
src\ChessApp\bin\x64\Release\net8.0-windows\ChessApp.exe
```

The cube chess app is produced at:

```text
src\Chess3DApp\bin\x64\Release\net8.0-windows\Chess3DApp.exe
```

The Rubik app is produced at:

```text
src\RubikApp\bin\x64\Release\net8.0-windows\RubikApp.exe
```

Independent package folders can be generated with:

```text
package_2d.bat
package_3d.bat
package_rubik.bat
package_online.bat
package_benchmark_2d.bat
package_all.bat
```

## 2D Benchmark

Run:

```text
run_benchmark_2d.bat --quick
run_benchmark_2d.bat --reps 5 --search-depth 4 --max-batch 65536 --csv bin\x64\Release\benchmark.csv
```

It measures legal move generation, fixed-depth search, search with GPU root ordering, and batch evaluation across forced CPU/Direct3D/CUDA/Auto backends. See `docs\CHESS2D_BENCHMARK.md`.
