#include "../../src/ChessGpuBackend/ChessGpuBackend.h"
#include "../TestSupport/TestSupport.h"

#include <algorithm>
#include <array>
#include <string>
#include <vector>

namespace
{
std::array<int, 64> StartBoard()
{
    return {
        4, 2, 3, 5, 6, 3, 2, 4,
        1, 1, 1, 1, 1, 1, 1, 1,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        -1, -1, -1, -1, -1, -1, -1, -1,
        -4, -2, -3, -5, -6, -3, -2, -4
    };
}
}

int main()
{
    ContractTestRunner test;
    test.Check(true, "GpuBackendContractTests process started");

    const int available = ChessGpu_IsAvailable();
    test.Check(available == 0 || available == 1, "ChessGpu_IsAvailable is callable");

    char info[1024]{};
    test.Check(ChessGpu_GetBackendInfo(info, static_cast<int>(sizeof(info))) != 0, "ChessGpu_GetBackendInfo succeeds");
    test.Check(std::string(info).size() > 0, "ChessGpu_GetBackendInfo returns text");

    std::array<int, 64> start = StartBoard();
    std::vector<int> boards64(4 * 64);
    for (int i = 0; i < 4; ++i)
    {
        std::copy(start.begin(), start.end(), boards64.begin() + i * 64);
    }

    int scores[4]{};
    test.Check(ChessGpu_EvaluateBatchEx(boards64.data(), 4, 1, scores, ChessGpuBackendCpu) == 4, "CPU EvaluateBatchEx scores four boards");
    test.Check(ChessGpu_EvaluateBatchEx(boards64.data(), 4, 1, scores, ChessGpuBackendAuto) == 4, "Auto EvaluateBatchEx scores four boards");

    const int d3dCount = ChessGpu_EvaluateBatchEx(boards64.data(), 4, 1, scores, ChessGpuBackendDirect3D);
    test.Check(d3dCount == 0 || d3dCount == 4, "Direct3D EvaluateBatchEx either scores or cleanly reports unavailable");

    std::vector<int> boards512(512);
    boards512[0] = 14;
    boards512[511] = 26;
    int score3d = 0;
    test.Check(ChessGpu_Evaluate3DBatch(boards512.data(), 1, 1, &score3d) == 1, "Evaluate3DBatch scores one 512-cell board without requiring CUDA");

    std::vector<int> rubikBoard(512);
    for (int i = 0; i < 512; ++i)
    {
        rubikBoard[i] = i;
    }
    const int actions[6] = {0, 0, 1, 1, 3, 2};
    std::vector<int> outBoards(2 * 512);
    test.Check(ChessGpu_GenerateRubikBatch(rubikBoard.data(), actions, 2, outBoards.data()) == 2, "GenerateRubikBatch produces two rotated boards");
    test.Check(!std::equal(rubikBoard.begin(), rubikBoard.end(), outBoards.begin()), "Generated Rubik board differs after rotation");

    ChessGpuKernelStatsDto stats{};
    test.Check(ChessGpu_GetKernelStats(&stats) == 1, "ChessGpu_GetKernelStats succeeds");
    test.Check(stats.evaluatorVersion > 0, "Kernel stats include evaluator version");

    return test.Finish("GpuBackendContractTests");
}
