# CUDA / GPU Architecture

This project keeps GPU work behind `ChessGpuBackend.dll` so the chess engines do not depend on a specific GPU runtime.

## Current Runtime

- `ChessGpu_EvaluateBatch`: auto-routed ordinary 8x8 chess board batches. Small batches stay on CPU; large batches use CUDA first, Direct3D 11 second, CPU fallback last.
- `ChessGpu_EvaluateBatchEx`: strict benchmark/control API with `Auto`, `CPU`, `Direct3D`, and `CUDA` modes.
- `ChessGpu_Evaluate3DBatch`: CUDA first, CPU fallback for 8x8x8 cube board evaluation.
- `ChessGpu_GenerateRubikBatch`: CUDA first, CPU fallback for generating many Rubik-layer successor boards from one 512-cell cube board.

`ChessGpuBackend.dll` dynamically loads `ChessCudaBackend.dll` from the same directory as itself. If that DLL cannot be loaded, if the CUDA device is missing, or if the exported ABI is incompatible, auto mode still returns valid CPU/Direct3D results. Strict CUDA mode returns failure so benchmarks can tell the truth.

## CUDA DLL

`src/ChessGpuBackend/ChessGpuBackendCuda.cu` is compiled by `src/ChessCudaBackend/ChessCudaBackend.vcxproj` and exports:

- `ChessCuda_IsAvailable`
- `ChessCuda_EvaluateBatch`
- `ChessCuda_Evaluate3DBatch`
- `ChessCuda_GenerateRubikBatch`
- `ChessCuda_GetLastError`

The underlying kernels are:

- `ChessCuda_EvaluateBatchKernel` for ordinary 64-cell boards.
- `ChessCuda_Evaluate3DBatchKernel` for 512-cell six-sided cube positions.
- `ChessCuda_GenerateRubikBatchKernel` for batched layer rotations.

The 2D CUDA evaluator now includes the same main heuristic terms as the CPU/Direct3D evaluator: material, piece-square terms, pseudo-mobility, passed/isolated pawn structure, king shield, and endgame king activity. The CUDA DLL keeps a reusable device workspace and stream for eval batches instead of allocating/freeing device buffers on every call.

The intended CUDA DLL split is:

```text
ChessEngine.dll       normal chess rules/search
Chess3DEngine.dll     cube rules, six sides, Rubik layer transforms
ChessGpuBackend.dll   stable exported ABI, CUDA/Direct3D/CPU router
ChessCudaBackend.dll  optional CUDA implementation loaded through the same ABI
RubikEngine.dll       standalone 8x8x8 Rubik state and reverse-history solver
```

CUDA Toolkit is required to build `ChessCudaBackend.dll`. Runtime use is intentionally separable: the apps can run without CUDA, and CUDA mode activates only when the optional DLL and NVIDIA driver/device are available.

## Why Rubik Generation Is Separate

Rubik turns are board transformations, not normal legal moves. The useful GPU shape is therefore:

1. Generate many candidate rotated boards in parallel.
2. Evaluate each resulting 512-cell board for the active side or for all six sides.
3. Feed the scored candidates back to the CPU search/scheduler.

That avoids mixing UI animation, network ordering, and engine rule validation inside GPU kernels.
