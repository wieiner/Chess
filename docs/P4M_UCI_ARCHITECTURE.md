# Chess2D UCI Architecture Audit

## Boundary

`ChessUci` is a console protocol adapter. It has no WPF reference, window, dialog, board renderer, or application settings dependency. Standard input and standard output carry only UCI commands and responses; parse/runtime diagnostics go to standard error.

The executable owns one authoritative position. A `position` command is validated and replayed on a candidate native engine, then commits the resulting FEN only after every coordinate move succeeds. Search runs on another engine initialized from the authority FEN because the existing `Chess_MakeBestMoveEx` operation selects **and commits** its move.

## Native ABI inventory

| Concern | Existing ABI | UCI decision |
| --- | --- | --- |
| Lifecycle | `Chess_Create`, `Chess_Destroy`, `Chess_Reset` | One authority handle plus short-lived candidate/search handles. |
| Position | `Chess_SetFen`, `Chess_GetFen` | Six-field FEN is the transaction interchange. |
| Legal moves | `Chess_GetLegalMoves`, `Chess_GetMoveDescriptor` | Validate all UCI coordinate moves and returned best moves. |
| Apply move | `Chess_TryMakeMove` | Candidate replay only for `position`; search clone commits its own result. |
| Search | `Chess_MakeBestMoveEx` | Background worker, bounded depth/time options. |
| Cancellation | No public ABI before P4M | Add one append-only `Chess_CancelSearch`; no DTO/layout changes. |
| Telemetry | `Chess_GetLastSearchStats`, `Chess_GetLastSearchInfo` | Emit only depth, score, nodes, elapsed time and legal one-move PV actually provided. |
| Transposition table | Boolean search option only | Advertise `Hash` only when a size-setting contract exists; currently it does not. |
| Threads | No configurable native worker count | Do not advertise `Threads`. |
| Time limit | `ChessSearchOptionsDto.timeLimitMs` | Map `movetime` and conservative clock budget; `MoveOverhead` remains adapter-side. |

## Protocol safety

- Input lines are limited to 16 KiB and token counts are bounded.
- Unknown/malformed commands report to stderr and do not terminate the engine.
- Search never blocks the input-reading loop.
- Every search generation emits at most one `bestmove`; canceled stale generations cannot publish later.
- `quit` cancels and performs bounded cleanup.
- `register` and `ponderhit` are accepted as no-op protocol compatibility commands; pondering is not advertised.
- No option is advertised unless it changes real behavior. Initially only adapter-supported `MoveOverhead` and native `OwnBook` behavior, if wired, are candidates.

## Executable layout

The managed project is `src/ChessUci/ChessUci.csproj`, targeting `net8.0-windows` x64 because the current Chess2D native authority is a Windows DLL. It copies `ChessEngine.dll` beside `ChessUci.exe`. Linux UCI is outside P4M and must not be inferred from the Chess3D Linux authority work.
