# P4M Baseline

## Scope

P4M starts the Chess2D professional game workflow and unified 3D asset library work. This baseline records the repository state before SAN, PGN, session persistence, UCI, or model-catalog changes. Hetzner, ChessOnline deployment, Chess3D rules, and the completed P4L Rubik workflow are outside this stage.

## Repository baseline

- Branch: `main`.
- Starting commit: `97a5cdd7da84e0342be47e0ece8ad9c3c2f74c41` (`P4L phase 30: finalize physical Rubik workflow`).
- `HEAD` and `origin/main` matched at phase start.
- Working tree was clean.
- Latest known GitHub Actions run: `29526186835`, `success`.
- Visual Studio MSBuild used for the baseline: `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe`.

## Baseline verification

The following builds were run sequentially with `/m:1 /nr:false` in `Release|x64`:

1. `src\ChessEngine\ChessEngine.vcxproj`: PASS.
2. `src\ChessApp\ChessApp.csproj`: PASS.

The build produced `ChessEngine.dll` and the `net8.0-windows` ChessApp output. `tests\run-tests.ps1 -List` completed successfully and listed the native, managed, online, Rubik, GPU, Chess3D, and optional Chess2D benchmark test entries. No full test or verify gate was required for this documentation-only phase.

## ChessEngine ABI and position state

The public C ABI currently exports:

- lifecycle: `Chess_Create`, `Chess_Destroy`, `Chess_Reset`;
- position: `Chess_SetFen`, `Chess_GetFen`, `Chess_GetBoard`, `Chess_GetState`;
- draw and tablebase settings: `Chess_GetDrawRules`, `Chess_SetDrawRules`, `Chess_ClaimDraw`, `Chess_SetTablebasePath`, `Chess_GetTablebaseInfo`;
- play: `Chess_GetLegalMoves`, `Chess_TryMakeMove`, `Chess_Undo`;
- search: `Chess_MakeBestMove`, `Chess_MakeBestMoveEx`, `Chess_GetLastSearchStats`, `Chess_GetLastSearchInfo`.

`ChessMoveDto` carries coordinates, promotion, flags, and search score. Its flags distinguish capture, castling, en passant, promotion, and check. It is a move transport DTO, not a complete immutable game-record object.

FEN import/export is already authoritative for one position. The native `Position` stores the 64 squares, side to move, four castling rights, en-passant square, halfmove clock, fullmove number, and last move. Legal move generation filters pseudo-legal moves for king safety before exposing or applying them.

## History and undo

Native undo uses a vector of pre-move `Snapshot` values. Each snapshot preserves board, side, castling rights, en-passant square, halfmove/fullmove counters, and last move. Search probes call the move application path without retaining history; committed player or engine moves retain a snapshot.

The native history is intentionally private and exposes only one-step `Chess_Undo`. It does not expose a durable move list, SAN, PGN headers, comments, clock snapshots, or a replay cursor.

ChessApp maintains a separate display-only `MoveList`. After a successful move, it appends a long-coordinate string such as `1. e2-e4`; captures, promotion, and check are decorated from DTO flags. Undo removes the last list item. Reset and FEN load clear the list. These strings are not sufficient as persistence authority and can diverge from native history if future workflows are built directly on them.

## Current Chess2D persistence and dialogs

- FEN can be loaded from a text box and copied to the clipboard.
- Chess2D currently has no PGN import/export, application-session JSON, autosave/recovery, or standalone UCI process.
- Chess2D `MainWindow` currently has no `OpenFileDialog` or `SaveFileDialog` workflow.
- Search settings, model selection, board orientation, clocks, and TCP endpoint state are UI/application concerns and are not represented by FEN.
- The local TCP JSON-lines endpoint exchanges FEN and moves; it is not a game-file format and must not become PGN authority.

Chess3D already has separate save/replay/action-log dialogs. Those formats and native ABI are not reused implicitly for Chess2D and must remain compatible.

## Current 3D asset path

The canonical tracked root is `assets\models\chess\pieces` and currently contains:

- one `piece_sets.json` v1 catalog;
- one `default-obj` set for Chess2D and Chess3D;
- 14 OBJ files and 14 MTL files for six white pieces, six black pieces, and two board tiles;
- no colocated raster texture files in the default set;
- a generated manifest example and asset notes.

The tracked OBJ payload is approximately 25.6 MB. The catalog maps pawn, rook, knight, bishop/officer, queen, king, and board tiles. It declares readable ivory and medium-charcoal fallback colors.

`ObjModelLibrary` discovers model-set directories in runtime and repository roots, parses OBJ vertices/UVs/faces, reads `mtllib`, `Ks`, `Ns`, and `map_Kd`, and uses WPF `MaterialGroup` fallbacks when an MTL or texture is absent. Normal, roughness, and general PBR maps are not a WPF Media3D runtime path. Procedural geometry remains the last-resort fallback and must not be removed.

The current loader is shared as source inside ChessApp for its Chess2D and Chess3D windows, but the catalog is directory-oriented rather than semantic-role-oriented. GLB is not a production runtime format at this baseline.

## Packaging paths

`src\ChessApp\ChessApp.csproj` copies the canonical model tree to `Assets\Models` in its build output. The existing verify gate checks the catalog and representative OBJ/MTL files in:

- source assets;
- `src\ChessApp\bin\x64\Release\net8.0-windows\Assets\Models`;
- `src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Models`;
- `ProductionOutput\Chess2D\Assets\Models`;
- `ProductionOutput\Chess3D\Assets\Models`.

`tools\release\Build-Production.ps1` copies the ChessApp release output to `ProductionOutput\Chess2D`. Generated outputs remain untracked.

## P4M boundary

P4M will add structured Chess2D records above the existing legal-move engine, then SAN, PGN, session recovery, and a UI-independent UCI executable. Asset work will evolve the catalog and add audited conversion/loading paths while retaining OBJ/MTL and procedural fallbacks. Existing FEN and legal-move ABI functions remain unchanged; any native additions must be append-only.
