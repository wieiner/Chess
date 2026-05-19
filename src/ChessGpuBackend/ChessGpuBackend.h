#pragma once

#ifdef CHESSGPUBACKEND_EXPORTS
#define CHESS_GPU_API extern "C" __declspec(dllexport)
#else
#define CHESS_GPU_API extern "C" __declspec(dllimport)
#endif

// Optional GPU acceleration DLL.
// Piece codes match ChessEngine.dll: 0 empty, positive white, negative black.

#pragma pack(push, 4)
struct ChessGpuKernelStatsDto
{
    int backend;          // 0 CPU fallback, 1 Direct3D 11 compute, 2 CUDA-ready ABI
    int lastBoardCount;
    int totalGpuBatches;
    int totalCpuFallbackBatches;
    int evaluatorVersion;
};
#pragma pack(pop)

enum ChessGpuBackendMode
{
    ChessGpuBackendAuto = 0,
    ChessGpuBackendCpu = 1,
    ChessGpuBackendDirect3D = 2,
    ChessGpuBackendCuda = 3
};

CHESS_GPU_API int ChessGpu_IsAvailable();
CHESS_GPU_API int ChessGpu_GetBackendInfo(char* buffer, int capacity);
CHESS_GPU_API int ChessGpu_EvaluateBatch(const int* boards64, int boardCount, int sideToMove, int* scores);
CHESS_GPU_API int ChessGpu_EvaluateBatchEx(const int* boards64, int boardCount, int sideToMove, int* scores, int backendMode);
CHESS_GPU_API int ChessGpu_Evaluate3DBatch(const int* boards512, int boardCount, int perspectiveSide, int* scores);
CHESS_GPU_API int ChessGpu_GenerateRubikBatch(const int* board512, const int* actions3, int actionCount, int* outBoards512);
CHESS_GPU_API int ChessGpu_GetKernelStats(ChessGpuKernelStatsDto* stats);
