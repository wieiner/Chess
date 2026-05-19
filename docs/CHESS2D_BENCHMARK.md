# Chess 2D Benchmark

`Chess2DBenchmark.exe` is a native console executable for measuring ordinary 8x8 chess performance without WPF/UI overhead.

## Run

```text
run_benchmark_2d.bat --quick
run_benchmark_2d.bat --reps 5 --search-depth 4 --max-batch 65536 --csv bin\x64\Release\benchmark.csv
```

Executable path:

```text
bin\x64\Release\Chess2DBenchmark.exe
```

## Metrics

- `legal-move-generation`: repeated `Chess_GetLegalMoves` over a fixed FEN suite.
- `search-depth-cpu`: `Chess_MakeBestMoveEx` at fixed depth with GPU root ordering disabled.
- `search-depth-gpu-root-order`: same search with optional `ChessGpuBackend.dll` root move scoring.
- `batch-eval`: forced `CPU`, `Direct3D`, `CUDA`, and `Auto` `ChessGpu_EvaluateBatchEx` over deterministic generated child-position corpora.

The batch evaluator compares all non-CPU backends against forced CPU scores and reports `diff` mismatches. A non-zero `diff` means evaluator parity has regressed.

## Current Architecture Findings

- Small root batches are CPU-favored. A normal chess root often has 20-40 legal moves, and GPU launch/copy overhead dominates there.
- CUDA becomes useful on larger batch sizes. On the local quick run, CUDA overtook CPU around the 1024-board batch shape.
- Direct3D still recreates buffers per call and is therefore mainly a fallback backend, not the preferred high-throughput route.
- Search is still CPU alpha-beta; GPU is only root move ordering, not full tree search.
- The CUDA 2D evaluator now mirrors the CPU/Direct3D heuristic shape closely enough for benchmark parity checks: material, piece-square, pseudo-mobility, pawn structure, king shield, and endgame king activity.

## CUDA Changes Measured By This Benchmark

- `ChessGpu_EvaluateBatchEx` allows strict backend selection: Auto, CPU, Direct3D, CUDA.
- Auto mode uses CPU for small 2D batches and switches to CUDA/Direct3D only when batch size is large enough to amortize transfer/launch costs.
- `ChessCudaBackend.dll` keeps a growing device workspace and non-blocking stream for 2D/3D eval batches instead of doing `cudaMalloc/cudaFree` every call.
- CUDA 2D scoring was expanded from a simplified evaluator to the same main terms used by CPU/Direct3D.

## Next Performance Steps

- Add a benchmark-only bulk child generation API inside `ChessEngine.dll` if deeper GPU move ordering is needed; generating child boards through exported make/undo is correct but not a zero-overhead harness.
- Consider a persistent Direct3D workspace like CUDA's workspace if Direct3D remains important on non-CUDA systems.
- If moving beyond root ordering, benchmark a CPU-generated frontier batch at depth 2-3 and evaluate that frontier on CUDA. Full GPU alpha-beta is not a good next step without a different engine architecture.
