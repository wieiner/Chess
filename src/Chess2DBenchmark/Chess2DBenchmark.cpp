#include "../ChessEngine/ChessEngine.h"
#include "../ChessGpuBackend/ChessGpuBackend.h"

#include <algorithm>
#include <chrono>
#include <cstdint>
#include <fstream>
#include <iomanip>
#include <iostream>
#include <numeric>
#include <sstream>
#include <string>
#include <vector>

namespace
{
using Clock = std::chrono::steady_clock;

struct Options
{
    bool quick = false;
    int repetitions = 5;
    int searchDepth = 4;
    int maxBatch = 65536;
    std::string csvPath;
};

struct Metric
{
    std::string name;
    std::string backend;
    int batch = 0;
    int repetitions = 0;
    double milliseconds = 0.0;
    double operationsPerSecond = 0.0;
    long long operations = 0;
    long long nodes = 0;
    int mismatches = 0;
    std::string notes;
};

const std::vector<std::string> BenchmarkFens = {
    "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
    "r3k2r/pppq1ppp/2npbn2/3Np3/2B1P3/2N2Q2/PPP2PPP/R3K2R w KQkq - 0 11",
    "rnbq1rk1/ppp2ppp/3bpn2/3p4/3P4/2PBPN2/PP3PPP/RNBQ1RK1 w - - 2 7",
    "2r2rk1/1bqnbppp/p2ppn2/1p6/3NP3/1BN1BQP1/PPP2P1P/2KR3R w - - 0 14",
    "8/2p5/3p4/3Pp3/2P1P3/8/5K2/6k1 w - e6 0 42",
    "r4rk1/pp1bqppp/2np1n2/2p1p3/2P1P3/2NPBN2/PP1QBPPP/R4RK1 w - - 4 12"
};

std::string backendName(int mode)
{
    switch (mode)
    {
    case ChessGpuBackendCpu:
        return "CPU";
    case ChessGpuBackendDirect3D:
        return "Direct3D";
    case ChessGpuBackendCuda:
        return "CUDA";
    default:
        return "Auto";
    }
}

Options parseArgs(int argc, char** argv)
{
    Options options;
    for (int i = 1; i < argc; ++i)
    {
        const std::string arg = argv[i];
        auto needValue = [&](int& target)
        {
            if (i + 1 < argc)
            {
                target = std::max(1, std::atoi(argv[++i]));
            }
        };
        if (arg == "--quick")
        {
            options.quick = true;
            options.repetitions = 2;
            options.searchDepth = 3;
            options.maxBatch = 8192;
        }
        else if (arg == "--reps")
        {
            needValue(options.repetitions);
        }
        else if (arg == "--search-depth")
        {
            needValue(options.searchDepth);
        }
        else if (arg == "--max-batch")
        {
            needValue(options.maxBatch);
        }
        else if (arg == "--csv" && i + 1 < argc)
        {
            options.csvPath = argv[++i];
        }
    }
    return options;
}

class ChessHandle
{
public:
    ChessHandle()
        : handle_(Chess_Create())
    {
    }

    ~ChessHandle()
    {
        if (handle_ != nullptr)
        {
            Chess_Destroy(handle_);
        }
    }

    ChessHandle(const ChessHandle&) = delete;
    ChessHandle& operator=(const ChessHandle&) = delete;

    void* get() const { return handle_; }

private:
    void* handle_ = nullptr;
};

std::vector<ChessMoveDto> getLegalMoves(void* game)
{
    const int count = Chess_GetLegalMoves(game, nullptr, 0);
    std::vector<ChessMoveDto> moves(static_cast<size_t>(std::max(0, count)));
    if (!moves.empty())
    {
        Chess_GetLegalMoves(game, moves.data(), static_cast<int>(moves.size()));
    }
    return moves;
}

std::vector<int> buildEvaluationCorpus(int minBoards)
{
    std::vector<int> boards;
    boards.reserve(static_cast<size_t>(minBoards) * 64);
    ChessHandle game;
    int rootBoard[64] = {};

    for (const auto& fen : BenchmarkFens)
    {
        if (!Chess_SetFen(game.get(), fen.c_str()))
        {
            continue;
        }

        if (Chess_GetBoard(game.get(), rootBoard) != 0)
        {
            boards.insert(boards.end(), rootBoard, rootBoard + 64);
        }

        const auto moves = getLegalMoves(game.get());
        for (const auto& move : moves)
        {
            ChessMoveDto played{};
            if (Chess_TryMakeMove(game.get(), move.fromFile, move.fromRank, move.toFile, move.toRank, move.promotion, &played))
            {
                if (Chess_GetBoard(game.get(), rootBoard) != 0)
                {
                    boards.insert(boards.end(), rootBoard, rootBoard + 64);
                }
                Chess_Undo(game.get());
            }
        }
    }

    if (boards.empty())
    {
        boards.resize(64);
    }

    const size_t seedBoards = boards.size() / 64;
    while (static_cast<int>(boards.size() / 64) < minBoards)
    {
        const size_t remaining = static_cast<size_t>(minBoards) - boards.size() / 64;
        const size_t copyBoards = std::min(seedBoards, remaining);
        boards.insert(boards.end(), boards.begin(), boards.begin() + static_cast<std::ptrdiff_t>(copyBoards * 64));
    }
    return boards;
}

Metric benchLegalMoveGeneration(const Options& options)
{
    ChessHandle game;
    long long generatedMoves = 0;
    long long calls = 0;

    const auto start = Clock::now();
    for (int rep = 0; rep < options.repetitions * 2000; ++rep)
    {
        for (const auto& fen : BenchmarkFens)
        {
            Chess_SetFen(game.get(), fen.c_str());
            const int count = Chess_GetLegalMoves(game.get(), nullptr, 0);
            generatedMoves += count;
            ++calls;
        }
    }
    const auto elapsed = std::chrono::duration<double, std::milli>(Clock::now() - start).count();

    Metric metric;
    metric.name = "legal-move-generation";
    metric.backend = "ChessEngine";
    metric.repetitions = options.repetitions;
    metric.milliseconds = elapsed;
    metric.operations = calls;
    metric.operationsPerSecond = calls / (elapsed / 1000.0);
    metric.notes = "calls=" + std::to_string(calls) + ", legalMoves=" + std::to_string(generatedMoves);
    return metric;
}

Metric benchSearch(const Options& options, bool gpuRoot)
{
    long long totalNodes = 0;
    long long searches = 0;
    int bestScoreChecksum = 0;

    const auto start = Clock::now();
    for (int rep = 0; rep < options.repetitions; ++rep)
    {
        for (const auto& fen : BenchmarkFens)
        {
            ChessHandle game;
            Chess_SetFen(game.get(), fen.c_str());
            ChessSearchOptionsDto search{};
            search.depth = options.searchDepth;
            search.timeLimitMs = 0;
            search.automaticDepth = 0;
            search.useQuiescence = 1;
            search.useTranspositionTable = 1;
            search.useMoveOrdering = 1;
            search.usePieceSquareTables = 1;
            search.useBishopPairBonus = 1;
            search.useKingSafetyBonus = 1;
            search.useGpuEvaluation = gpuRoot ? 1 : 0;
            search.useEndgameTables = 1;
            ChessMoveDto best{};
            Chess_MakeBestMoveEx(game.get(), &search, &best);
            ChessSearchInfoDto info{};
            Chess_GetLastSearchStats(game.get(), &info);
            totalNodes += info.nodes;
            bestScoreChecksum += info.bestScore;
            ++searches;
        }
    }
    const auto elapsed = std::chrono::duration<double, std::milli>(Clock::now() - start).count();

    Metric metric;
    metric.name = gpuRoot ? "search-depth-gpu-root-order" : "search-depth-cpu";
    metric.backend = gpuRoot ? "ChessEngine+GPU root" : "ChessEngine";
    metric.batch = options.searchDepth;
    metric.repetitions = options.repetitions;
    metric.milliseconds = elapsed;
    metric.operations = searches;
    metric.nodes = totalNodes;
    metric.operationsPerSecond = totalNodes / (elapsed / 1000.0);
    metric.notes = "searches=" + std::to_string(searches) + ", scoreChecksum=" + std::to_string(bestScoreChecksum);
    return metric;
}

Metric benchBatchEval(const std::vector<int>& corpus, int boardCount, int backendMode, int repetitions, const std::vector<int>& reference)
{
    std::vector<int> scores(static_cast<size_t>(boardCount));
    const int* boards = corpus.data();
    const int warmup = ChessGpu_EvaluateBatchEx(boards, boardCount, 1, scores.data(), backendMode);
    if (warmup != boardCount)
    {
        Metric unavailable;
        unavailable.name = "batch-eval";
        unavailable.backend = backendName(backendMode);
        unavailable.batch = boardCount;
        unavailable.repetitions = repetitions;
        unavailable.notes = "unavailable";
        return unavailable;
    }

    const auto start = Clock::now();
    int evaluated = 0;
    for (int rep = 0; rep < repetitions; ++rep)
    {
        evaluated += ChessGpu_EvaluateBatchEx(boards, boardCount, 1, scores.data(), backendMode);
    }
    const auto elapsed = std::chrono::duration<double, std::milli>(Clock::now() - start).count();

    int mismatches = 0;
    if (!reference.empty())
    {
        for (int i = 0; i < boardCount; ++i)
        {
            if (scores[static_cast<size_t>(i)] != reference[static_cast<size_t>(i)])
            {
                ++mismatches;
            }
        }
    }

    Metric metric;
    metric.name = "batch-eval";
    metric.backend = backendName(backendMode);
    metric.batch = boardCount;
    metric.repetitions = repetitions;
    metric.milliseconds = elapsed;
    metric.operations = evaluated;
    metric.operationsPerSecond = evaluated / (elapsed / 1000.0);
    metric.mismatches = mismatches;
    metric.notes = mismatches == 0 ? "scores-ok" : "score-mismatch";
    return metric;
}

void printMetric(const Metric& metric)
{
    std::cout << std::left << std::setw(28) << metric.name
              << std::setw(18) << metric.backend
              << std::right << std::setw(8) << metric.batch
              << std::setw(8) << metric.repetitions
              << std::setw(12) << std::fixed << std::setprecision(2) << metric.milliseconds
              << std::setw(16) << std::fixed << std::setprecision(0) << metric.operationsPerSecond
              << std::setw(14) << metric.nodes
              << std::setw(10) << metric.mismatches
              << "  " << metric.notes << '\n';
}

void writeCsv(const std::string& path, const std::vector<Metric>& metrics)
{
    if (path.empty())
    {
        return;
    }
    std::ofstream csv(path);
    csv << "name,backend,batch,repetitions,milliseconds,ops_per_sec,operations,nodes,mismatches,notes\n";
    for (const auto& m : metrics)
    {
        csv << m.name << ','
            << m.backend << ','
            << m.batch << ','
            << m.repetitions << ','
            << std::fixed << std::setprecision(3) << m.milliseconds << ','
            << std::fixed << std::setprecision(3) << m.operationsPerSecond << ','
            << m.operations << ','
            << m.nodes << ','
            << m.mismatches << ','
            << '"' << m.notes << '"' << '\n';
    }
}

std::vector<int> batchSizes(int maxBatch)
{
    std::vector<int> sizes = { 1, 8, 32, 128, 256, 1024, 4096, 16384, 65536 };
    sizes.erase(std::remove_if(sizes.begin(), sizes.end(), [maxBatch](int size) { return size > maxBatch; }), sizes.end());
    if (sizes.empty())
    {
        sizes.push_back(std::max(1, maxBatch));
    }
    return sizes;
}
}

int main(int argc, char** argv)
{
    const Options options = parseArgs(argc, argv);
    char info[1024] = {};
    ChessGpu_GetBackendInfo(info, static_cast<int>(sizeof(info)));

    std::cout << "Chess2DBenchmark\n";
    std::cout << "Configuration: reps=" << options.repetitions
              << ", searchDepth=" << options.searchDepth
              << ", maxBatch=" << options.maxBatch
              << (options.quick ? ", quick" : "") << "\n";
    std::cout << "GPU: " << info << "\n\n";

    std::vector<Metric> metrics;
    metrics.push_back(benchLegalMoveGeneration(options));
    metrics.push_back(benchSearch(options, false));
    metrics.push_back(benchSearch(options, true));

    const auto sizes = batchSizes(options.maxBatch);
    const int corpusBoards = sizes.back();
    std::vector<int> corpus = buildEvaluationCorpus(corpusBoards);

    for (int size : sizes)
    {
        std::vector<int> reference(static_cast<size_t>(size));
        ChessGpu_EvaluateBatchEx(corpus.data(), size, 1, reference.data(), ChessGpuBackendCpu);
        const int reps = size <= 32 ? options.repetitions * 2000 :
            size <= 256 ? options.repetitions * 500 :
            size <= 4096 ? options.repetitions * 80 :
            options.repetitions * 10;
        metrics.push_back(benchBatchEval(corpus, size, ChessGpuBackendCpu, reps, reference));
        metrics.push_back(benchBatchEval(corpus, size, ChessGpuBackendAuto, reps, reference));
        metrics.push_back(benchBatchEval(corpus, size, ChessGpuBackendDirect3D, reps, reference));
        metrics.push_back(benchBatchEval(corpus, size, ChessGpuBackendCuda, reps, reference));
    }

    std::cout << std::left << std::setw(28) << "metric"
              << std::setw(18) << "backend"
              << std::right << std::setw(8) << "batch"
              << std::setw(8) << "reps"
              << std::setw(12) << "ms"
              << std::setw(16) << "ops/sec"
              << std::setw(14) << "nodes"
              << std::setw(10) << "diff"
              << "  notes\n";
    std::cout << std::string(124, '-') << '\n';
    for (const auto& metric : metrics)
    {
        printMetric(metric);
    }

    ChessGpuKernelStatsDto stats{};
    ChessGpu_GetKernelStats(&stats);
    std::cout << "\nBackend stats: lastBackend=" << stats.backend
              << ", lastBatch=" << stats.lastBoardCount
              << ", gpuBatches=" << stats.totalGpuBatches
              << ", cpuFallbackBatches=" << stats.totalCpuFallbackBatches
              << ", evaluatorVersion=" << stats.evaluatorVersion << "\n";

    writeCsv(options.csvPath, metrics);
    if (!options.csvPath.empty())
    {
        std::cout << "CSV written: " << options.csvPath << "\n";
    }
    return 0;
}
