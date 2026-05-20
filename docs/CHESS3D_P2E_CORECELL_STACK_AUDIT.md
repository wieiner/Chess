# Chess3D P2E CoreCell Stack Audit

## 1. Current board storage

`Chess3DEngine.dll` keeps the compatible board in:

```text
Position::board
std::array<int, 512>
```

The index formula is:

```text
index = z * 64 + y * 8 + x
```

The board is still the projection consumed by old ABI calls, WPF rendering, network board snapshots, GPU 3D evaluation smoke tests, and legacy contract tests.

## 2. Piece coding

Pieces use the long-standing integer code:

```text
pieceCode = side * 10 + pieceType
```

Side ids are `1..6`. Piece types are classic chess ids:

- `1` pawn;
- `2` knight;
- `3` bishop/officer;
- `4` rook;
- `5` queen;
- `6` king.

## 3. One-int-per-cell assumptions

These functions and surfaces assume one projected integer per cell:

- `Chess3D_GetBoard` / `Chess3D_SetBoard`;
- `Chess3D_GetPiece` / `Chess3D_SetPiece`;
- move generation using `pos.board`;
- `Chess3D_GetState` piece count;
- position text;
- WPF 3D/2D board rendering;
- network board sync as `board512`;
- GPU 3D evaluator smoke path.

P2E keeps this projection intact.

## 4. GetPiece / SetPiece / TryMakeMove usage

`NativeChess3DEngine.cs` exposes old piece APIs directly to `Chess3DWindow.xaml.cs`. Setup mode, board clicks, network sync, move execution, and rendering still depend on these APIs.

Therefore old APIs must remain deterministic:

- `GetPiece` returns projected/top piece;
- `SetPiece` replaces a core stack with a single entry when core stacks are enabled;
- `SetPiece(..., 0, 0)` clears the stack;
- `TryMakeMove` still consumes and returns `Chess3DMoveDto`.

## 5. P2D anchors

P2D counted anchors by inspecting the projected single piece in each target cell. That was enough for a compatibility projection, but not enough for Forbidden Core co-occupancy.

## 6. Refactor risk

Dangerous areas:

- changing `Position::board` shape would break old ABI, UI, GPU, tests, and network snapshots;
- allowing old `SetPiece` to append would make setup mode unpredictable;
- rotating layers with stacks would corrupt state unless stacks are transformed too;
- GPU and network still see only the projection.

## 7. Minimal safe P2E path

P2E adds `coreStacks` as an overlay owned by the native game state:

```text
std::array<std::vector<CoreStackEntry>, 512>
```

Only cells inside `coreProfile.coreCube` use stacks, and only when the loaded profile enables `coreStack` / `asgardCorePhysics`.

The projected board remains synchronized:

- empty stack -> `board[index] = 0`;
- non-empty stack -> `board[index] = stack.back().pieceCode`.

This preserves old readers while enabling new stack ABI functions.
