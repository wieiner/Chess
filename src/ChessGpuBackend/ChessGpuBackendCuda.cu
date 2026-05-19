// CUDA-ready evaluator source. This file is intentionally not compiled by the
// default Visual Studio project until the CUDA Toolkit targets are installed.
// It mirrors the Direct3D compute ABI: one thread evaluates one 64-square board.
// The lower section stages the 8x8x8 cube evaluator and Rubik-layer board generator
// for a future CUDA-enabled ChessCudaBackend.dll.

#include <cuda_runtime.h>

#include <mutex>

#ifdef _WIN32
#define CHESS_CUDA_API extern "C" __declspec(dllexport)
#else
#define CHESS_CUDA_API extern "C"
#endif

extern "C"
{
struct CudaEvalParams
{
    int boardCount;
    int sideToMove;
};
}

__constant__ int cuda_material[7] = { 0, 100, 320, 330, 500, 900, 0 };

__device__ int cuda_piece_type(int piece)
{
    return piece < 0 ? -piece : piece;
}

__device__ int cuda_piece_color(int piece)
{
    return (piece > 0) - (piece < 0);
}

__device__ int cuda_piece_value(int piece)
{
    const int type = cuda_piece_type(piece);
    return type >= 0 && type <= 6 ? cuda_material[type] : 0;
}

__device__ int cuda_inside(int file, int rank)
{
    return file >= 0 && file < 8 && rank >= 0 && rank < 8;
}

__device__ int cuda_board_at(const int* boards, int offset, int file, int rank)
{
    return boards[offset + rank * 8 + file];
}

__device__ int cuda_center_bonus(int file, int rank)
{
    return 14 - (abs(file - 3) + abs(rank - 3)) * 3;
}

__device__ int cuda_piece_square_bonus(int type, int color, int square)
{
    const int file = square & 7;
    const int rank = square >> 3;
    const int friendlyRank = color > 0 ? rank : 7 - rank;
    const int center = cuda_center_bonus(file, rank);
    if (type == 1) return friendlyRank * 7 + (file >= 2 && file <= 5 ? 5 : -3);
    if (type == 2) return center * 3;
    if (type == 3) return center * 2;
    if (type == 4) return friendlyRank >= 6 ? 12 : 0;
    if (type == 5) return center;
    if (type == 6) return friendlyRank <= 1 ? 12 : -center;
    return 0;
}

__device__ int cuda_ray_mobility(const int* boards, int offset, int file, int rank, int df, int dr, int color)
{
    int result = 0;
    for (int f = file + df, r = rank + dr; cuda_inside(f, r); f += df, r += dr)
    {
        const int piece = cuda_board_at(boards, offset, f, r);
        if (piece == 0)
        {
            result += 3;
            continue;
        }
        if (cuda_piece_color(piece) == -color)
        {
            result += 5;
        }
        break;
    }
    return result;
}

__device__ int cuda_piece_mobility(const int* boards, int offset, int type, int file, int rank, int color)
{
    if (type == 2)
    {
        const int offsets[8][2] = {
            {1, 2}, {2, 1}, {2, -1}, {1, -2}, {-1, -2}, {-2, -1}, {-2, 1}, {-1, 2}
        };
        int result = 0;
        for (int i = 0; i < 8; ++i)
        {
            const int f = file + offsets[i][0];
            const int r = rank + offsets[i][1];
            if (cuda_inside(f, r))
            {
                const int target = cuda_board_at(boards, offset, f, r);
                if (target == 0 || cuda_piece_color(target) == -color)
                {
                    result += 5;
                }
            }
        }
        return result;
    }

    int result = 0;
    if (type == 3 || type == 5)
    {
        result += cuda_ray_mobility(boards, offset, file, rank, 1, 1, color);
        result += cuda_ray_mobility(boards, offset, file, rank, 1, -1, color);
        result += cuda_ray_mobility(boards, offset, file, rank, -1, 1, color);
        result += cuda_ray_mobility(boards, offset, file, rank, -1, -1, color);
    }
    if (type == 4 || type == 5)
    {
        result += cuda_ray_mobility(boards, offset, file, rank, 1, 0, color);
        result += cuda_ray_mobility(boards, offset, file, rank, -1, 0, color);
        result += cuda_ray_mobility(boards, offset, file, rank, 0, 1, color);
        result += cuda_ray_mobility(boards, offset, file, rank, 0, -1, color);
    }
    return result;
}

__device__ int cuda_is_passed_pawn(const int* boards, int offset, int file, int rank, int color)
{
    const int direction = color > 0 ? 1 : -1;
    const int enemyPawn = -color;
    for (int df = -1; df <= 1; ++df)
    {
        const int f = file + df;
        if (f < 0 || f >= 8)
        {
            continue;
        }
        for (int r = rank + direction; r >= 0 && r < 8; r += direction)
        {
            if (cuda_board_at(boards, offset, f, r) == enemyPawn)
            {
                return 0;
            }
        }
    }
    return 1;
}

__device__ int cuda_is_isolated_pawn(const int* boards, int offset, int file, int color)
{
    for (int df = -1; df <= 1; df += 2)
    {
        const int f = file + df;
        if (f < 0 || f >= 8)
        {
            continue;
        }
        for (int rank = 0; rank < 8; ++rank)
        {
            if (cuda_board_at(boards, offset, f, rank) == color)
            {
                return 0;
            }
        }
    }
    return 1;
}

__device__ int cuda_pawn_structure_bonus(const int* boards, int offset, int file, int rank, int color, int endgame)
{
    const int friendlyRank = color > 0 ? rank : 7 - rank;
    int result = 0;
    if (cuda_is_passed_pawn(boards, offset, file, rank, color))
    {
        result += 18 + friendlyRank * friendlyRank * 3 + (endgame ? 18 : 0);
    }
    if (cuda_is_isolated_pawn(boards, offset, file, color))
    {
        result -= 10;
    }
    return result;
}

__device__ int cuda_king_shield(const int* boards, int offset, int kingSquare, int color)
{
    const int file = kingSquare & 7;
    const int rank = kingSquare >> 3;
    const int shieldRank = rank + (color > 0 ? 1 : -1);
    int result = 0;
    for (int df = -1; df <= 1; ++df)
    {
        const int f = file + df;
        if (cuda_inside(f, shieldRank) && cuda_board_at(boards, offset, f, shieldRank) == color)
        {
            result += 8;
        }
        else
        {
            result -= 5;
        }
    }
    return result;
}

__device__ int cuda_king_centrality(int square)
{
    const int file = square & 7;
    const int rank = square >> 3;
    return 14 - (abs(file - 3) + abs(rank - 3)) * 2;
}

__device__ int cuda_king_edge_bonus(int square)
{
    const int file = square & 7;
    const int rank = square >> 3;
    const int edgeDistance = min(min(file, 7 - file), min(rank, 7 - rank));
    return (3 - edgeDistance) * 18;
}

extern "C" __global__
void ChessCuda_EvaluateBatchKernel(const int* boards64, int boardCount, int sideToMove, int* scores)
{
    const int boardIndex = blockIdx.x * blockDim.x + threadIdx.x;
    if (boardIndex >= boardCount)
    {
        return;
    }

    const int offset = boardIndex * 64;
    int score = 0;
    int whiteNonKing = 0;
    int blackNonKing = 0;
    int whiteKing = 4;
    int blackKing = 60;

    for (int i = 0; i < 64; ++i)
    {
        const int piece = boards64[offset + i];
        if (piece == 0)
        {
            continue;
        }

        const int type = cuda_piece_type(piece);
        const int color = cuda_piece_color(piece);
        const int value = cuda_piece_value(piece);
        if (type != 6)
        {
            if (color > 0) whiteNonKing += value; else blackNonKing += value;
        }
        else if (color > 0)
        {
            whiteKing = i;
        }
        else
        {
            blackKing = i;
        }
    }

    const int endgame = whiteNonKing + blackNonKing <= 2600;

    for (int i = 0; i < 64; ++i)
    {
        const int piece = boards64[offset + i];
        if (piece == 0)
        {
            continue;
        }

        const int type = cuda_piece_type(piece);
        const int color = cuda_piece_color(piece);
        const int file = i & 7;
        const int rank = i >> 3;
        int bonus = cuda_piece_square_bonus(type, color, i);
        if (type == 1)
        {
            bonus += cuda_pawn_structure_bonus(boards64, offset, file, rank, color, endgame);
        }
        score += color * (cuda_piece_value(piece) + bonus + cuda_piece_mobility(boards64, offset, type, file, rank, color));
    }

    score += cuda_king_shield(boards64, offset, whiteKing, 1);
    score -= cuda_king_shield(boards64, offset, blackKing, -1);
    if (endgame)
    {
        score += cuda_king_centrality(whiteKing) * 5;
        score -= cuda_king_centrality(blackKing) * 5;
        if (whiteNonKing - blackNonKing >= 500)
        {
            score += cuda_king_edge_bonus(blackKing);
        }
        else if (blackNonKing - whiteNonKing >= 500)
        {
            score -= cuda_king_edge_bonus(whiteKing);
        }
    }

    scores[boardIndex] = sideToMove >= 0 ? score : -score;
}

__device__ int cuda_index3d(int x, int y, int z)
{
    return z * 64 + y * 8 + x;
}

__device__ int cuda_inside3d(int x, int y, int z)
{
    return x >= 0 && x < 8 && y >= 0 && y < 8 && z >= 0 && z < 8;
}

__device__ int cuda_piece_side3d(int piece)
{
    return piece / 10;
}

__device__ int cuda_piece_type3d(int piece)
{
    return piece == 0 ? 0 : abs(piece % 10);
}

__device__ int cuda_piece_value3d(int type)
{
    if (type == 1) return 100;
    if (type == 2) return 320;
    if (type == 3) return 330;
    if (type == 4) return 500;
    if (type == 5) return 900;
    return 0;
}

__device__ int cuda_center3d(int x, int y, int z)
{
    return 24 - (abs(x - 3) + abs(y - 3) + abs(z - 3)) * 2;
}

__device__ void cuda_forward3d(int side, int* dx, int* dy, int* dz)
{
    *dx = 0; *dy = 0; *dz = 1;
    if (side == 2) { *dz = -1; }
    else if (side == 3) { *dy = 1; *dz = 0; }
    else if (side == 4) { *dy = -1; *dz = 0; }
    else if (side == 5) { *dx = 1; *dz = 0; }
    else if (side == 6) { *dx = -1; *dz = 0; }
}

__device__ int cuda_board_at3d(const int* boards512, int offset, int x, int y, int z)
{
    return boards512[offset + cuda_index3d(x, y, z)];
}

__device__ int cuda_ray_mobility3d(const int* boards512, int offset, int x, int y, int z, int dx, int dy, int dz, int side)
{
    int result = 0;
    for (int tx = x + dx, ty = y + dy, tz = z + dz; cuda_inside3d(tx, ty, tz); tx += dx, ty += dy, tz += dz)
    {
        int target = cuda_board_at3d(boards512, offset, tx, ty, tz);
        if (target == 0)
        {
            result += 2;
            continue;
        }
        if (cuda_piece_side3d(target) != side)
        {
            result += 4;
        }
        break;
    }
    return result;
}

__device__ int cuda_mobility3d(const int* boards512, int offset, int type, int x, int y, int z, int side)
{
    if (type == 1)
    {
        int dx, dy, dz;
        cuda_forward3d(side, &dx, &dy, &dz);
        int tx = x + dx;
        int ty = y + dy;
        int tz = z + dz;
        return cuda_inside3d(tx, ty, tz) && cuda_board_at3d(boards512, offset, tx, ty, tz) == 0 ? 5 : 0;
    }

    if (type == 2)
    {
        int result = 0;
        for (int longAxis = 0; longAxis < 3; ++longAxis)
        {
            for (int shortAxis = 0; shortAxis < 3; ++shortAxis)
            {
                if (longAxis == shortAxis) continue;
                for (int ls = -1; ls <= 1; ls += 2)
                {
                    for (int ss = -1; ss <= 1; ss += 2)
                    {
                        int d[3] = {0, 0, 0};
                        d[longAxis] = 2 * ls;
                        d[shortAxis] = ss;
                        int tx = x + d[0];
                        int ty = y + d[1];
                        int tz = z + d[2];
                        if (cuda_inside3d(tx, ty, tz))
                        {
                            int target = cuda_board_at3d(boards512, offset, tx, ty, tz);
                            if (target == 0 || cuda_piece_side3d(target) != side) result += 5;
                        }
                    }
                }
            }
        }
        return result;
    }

    int result = 0;
    for (int dx = -1; dx <= 1; ++dx)
    {
        for (int dy = -1; dy <= 1; ++dy)
        {
            for (int dz = -1; dz <= 1; ++dz)
            {
                if (dx == 0 && dy == 0 && dz == 0) continue;
                int axes = (dx != 0) + (dy != 0) + (dz != 0);
                if ((type == 4 && axes != 1) || (type == 3 && axes < 2)) continue;
                if (type == 6)
                {
                    int tx = x + dx;
                    int ty = y + dy;
                    int tz = z + dz;
                    if (cuda_inside3d(tx, ty, tz))
                    {
                        int target = cuda_board_at3d(boards512, offset, tx, ty, tz);
                        if (target == 0 || cuda_piece_side3d(target) != side) result += 3;
                    }
                }
                else
                {
                    result += cuda_ray_mobility3d(boards512, offset, x, y, z, dx, dy, dz, side);
                }
            }
        }
    }
    return result;
}

extern "C" __global__
void ChessCuda_Evaluate3DBatchKernel(const int* boards512, int boardCount, int perspectiveSide, int* scores)
{
    const int boardIndex = blockIdx.x * blockDim.x + threadIdx.x;
    if (boardIndex >= boardCount)
    {
        return;
    }

    const int offset = boardIndex * 512;
    int score = 0;
    for (int i = 0; i < 512; ++i)
    {
        const int piece = boards512[offset + i];
        if (piece == 0) continue;
        const int side = cuda_piece_side3d(piece);
        const int type = cuda_piece_type3d(piece);
        if (side < 1 || side > 6 || type < 1 || type > 6) continue;
        const int x = i & 7;
        const int y = (i >> 3) & 7;
        const int z = i >> 6;
        const int sign = side == perspectiveSide ? 1 : -1;
        int term = cuda_piece_value3d(type) + cuda_center3d(x, y, z) + cuda_mobility3d(boards512, offset, type, x, y, z, side);
        score += sign * term;
    }
    scores[boardIndex] = score;
}

__device__ int3 cuda_rotate_square3d(int axis, int layer, int turns, int x, int y, int z)
{
    int u = axis == 2 ? y : x;
    int v = axis == 0 ? y : z;
    for (int i = 0; i < turns; ++i)
    {
        int nextU = 7 - v;
        int nextV = u;
        u = nextU;
        v = nextV;
    }
    if (axis == 0) return make_int3(u, v, layer);
    if (axis == 1) return make_int3(u, layer, v);
    return make_int3(layer, u, v);
}

extern "C" __global__
void ChessCuda_GenerateRubikBatchKernel(const int* board512, const int* actions3, int actionCount, int* outBoards512)
{
    const int globalIndex = blockIdx.x * blockDim.x + threadIdx.x;
    const int total = actionCount * 512;
    if (globalIndex >= total)
    {
        return;
    }

    const int action = globalIndex / 512;
    const int square = globalIndex % 512;
    const int axis = actions3[action * 3];
    const int layer = actions3[action * 3 + 1];
    int turns = actions3[action * 3 + 2] % 4;
    if (turns < 0) turns += 4;

    int x = square & 7;
    int y = (square >> 3) & 7;
    int z = square >> 6;
    bool inLayer = (axis == 0 && z == layer) || (axis == 1 && y == layer) || (axis == 2 && x == layer);
    if (!inLayer || axis < 0 || axis > 2 || layer < 0 || layer >= 8 || turns == 0)
    {
        outBoards512[action * 512 + square] = board512[square];
        return;
    }

    int3 to = cuda_rotate_square3d(axis, layer, turns, x, y, z);
    outBoards512[action * 512 + cuda_index3d(to.x, to.y, to.z)] = board512[square];
}

namespace
{
cudaError_t g_lastError = cudaSuccess;
std::mutex g_cudaMutex;

struct CudaWorkspace
{
    int* deviceInput = nullptr;
    int* deviceScores = nullptr;
    size_t inputCapacityInts = 0;
    size_t scoreCapacityInts = 0;
    cudaStream_t stream = nullptr;
};

CudaWorkspace g_workspace;

cudaError_t ensureWorkspace(size_t inputInts, size_t scoreInts)
{
    if (g_workspace.stream == nullptr)
    {
        cudaError_t error = cudaStreamCreateWithFlags(&g_workspace.stream, cudaStreamNonBlocking);
        if (error != cudaSuccess)
        {
            return error;
        }
    }

    if (g_workspace.inputCapacityInts < inputInts)
    {
        if (g_workspace.deviceInput != nullptr)
        {
            cudaFree(g_workspace.deviceInput);
            g_workspace.deviceInput = nullptr;
        }
        cudaError_t error = cudaMalloc(&g_workspace.deviceInput, inputInts * sizeof(int));
        if (error != cudaSuccess)
        {
            g_workspace.inputCapacityInts = 0;
            return error;
        }
        g_workspace.inputCapacityInts = inputInts;
    }

    if (g_workspace.scoreCapacityInts < scoreInts)
    {
        if (g_workspace.deviceScores != nullptr)
        {
            cudaFree(g_workspace.deviceScores);
            g_workspace.deviceScores = nullptr;
        }
        cudaError_t error = cudaMalloc(&g_workspace.deviceScores, scoreInts * sizeof(int));
        if (error != cudaSuccess)
        {
            g_workspace.scoreCapacityInts = 0;
            return error;
        }
        g_workspace.scoreCapacityInts = scoreInts;
    }

    return cudaSuccess;
}

int launchIntBatchKernel(const int* hostInput, int inputCount, int outputCount, int side, int* hostScores, bool cube3d)
{
    if (hostInput == nullptr || hostScores == nullptr || inputCount <= 0 || outputCount <= 0)
    {
        return 0;
    }

    const size_t inputBytes = static_cast<size_t>(inputCount) * sizeof(int);
    const size_t scoreBytes = static_cast<size_t>(outputCount) * sizeof(int);

    g_lastError = ensureWorkspace(static_cast<size_t>(inputCount), static_cast<size_t>(outputCount));
    if (g_lastError != cudaSuccess)
    {
        return 0;
    }
    g_lastError = cudaMemcpyAsync(g_workspace.deviceInput, hostInput, inputBytes, cudaMemcpyHostToDevice, g_workspace.stream);
    if (g_lastError == cudaSuccess)
    {
        const int threads = 128;
        const int blocks = (outputCount + threads - 1) / threads;
        if (cube3d)
        {
            ChessCuda_Evaluate3DBatchKernel<<<blocks, threads, 0, g_workspace.stream>>>(g_workspace.deviceInput, outputCount, side, g_workspace.deviceScores);
        }
        else
        {
            ChessCuda_EvaluateBatchKernel<<<blocks, threads, 0, g_workspace.stream>>>(g_workspace.deviceInput, outputCount, side, g_workspace.deviceScores);
        }
        g_lastError = cudaGetLastError();
        if (g_lastError == cudaSuccess)
        {
            g_lastError = cudaMemcpyAsync(hostScores, g_workspace.deviceScores, scoreBytes, cudaMemcpyDeviceToHost, g_workspace.stream);
        }
        if (g_lastError == cudaSuccess)
        {
            g_lastError = cudaStreamSynchronize(g_workspace.stream);
        }
    }

    return g_lastError == cudaSuccess ? outputCount : 0;
}
}

CHESS_CUDA_API int ChessCuda_IsAvailable()
{
    std::lock_guard<std::mutex> lock(g_cudaMutex);
    int count = 0;
    g_lastError = cudaGetDeviceCount(&count);
    return g_lastError == cudaSuccess && count > 0 ? 1 : 0;
}

CHESS_CUDA_API int ChessCuda_EvaluateBatch(const int* boards64, int boardCount, int sideToMove, int* scores)
{
    std::lock_guard<std::mutex> lock(g_cudaMutex);
    return launchIntBatchKernel(boards64, boardCount * 64, boardCount, sideToMove, scores, false);
}

CHESS_CUDA_API int ChessCuda_Evaluate3DBatch(const int* boards512, int boardCount, int perspectiveSide, int* scores)
{
    std::lock_guard<std::mutex> lock(g_cudaMutex);
    return launchIntBatchKernel(boards512, boardCount * 512, boardCount, perspectiveSide, scores, true);
}

CHESS_CUDA_API int ChessCuda_GenerateRubikBatch(const int* board512, const int* actions3, int actionCount, int* outBoards512)
{
    std::lock_guard<std::mutex> lock(g_cudaMutex);
    if (board512 == nullptr || actions3 == nullptr || outBoards512 == nullptr || actionCount <= 0)
    {
        return 0;
    }

    int* deviceBoard = nullptr;
    int* deviceActions = nullptr;
    int* deviceOutput = nullptr;
    const size_t boardBytes = 512 * sizeof(int);
    const size_t actionBytes = static_cast<size_t>(actionCount) * 3 * sizeof(int);
    const size_t outputBytes = static_cast<size_t>(actionCount) * 512 * sizeof(int);

    g_lastError = cudaMalloc(&deviceBoard, boardBytes);
    if (g_lastError != cudaSuccess)
    {
        return 0;
    }
    g_lastError = cudaMalloc(&deviceActions, actionBytes);
    if (g_lastError != cudaSuccess)
    {
        cudaFree(deviceBoard);
        return 0;
    }
    g_lastError = cudaMalloc(&deviceOutput, outputBytes);
    if (g_lastError != cudaSuccess)
    {
        cudaFree(deviceActions);
        cudaFree(deviceBoard);
        return 0;
    }

    g_lastError = cudaMemcpy(deviceBoard, board512, boardBytes, cudaMemcpyHostToDevice);
    if (g_lastError == cudaSuccess)
    {
        g_lastError = cudaMemcpy(deviceActions, actions3, actionBytes, cudaMemcpyHostToDevice);
    }
    if (g_lastError == cudaSuccess)
    {
        const int total = actionCount * 512;
        const int threads = 256;
        const int blocks = (total + threads - 1) / threads;
        ChessCuda_GenerateRubikBatchKernel<<<blocks, threads>>>(deviceBoard, deviceActions, actionCount, deviceOutput);
        g_lastError = cudaGetLastError();
        if (g_lastError == cudaSuccess)
        {
            g_lastError = cudaDeviceSynchronize();
        }
        if (g_lastError == cudaSuccess)
        {
            g_lastError = cudaMemcpy(outBoards512, deviceOutput, outputBytes, cudaMemcpyDeviceToHost);
        }
    }

    cudaFree(deviceOutput);
    cudaFree(deviceActions);
    cudaFree(deviceBoard);
    return g_lastError == cudaSuccess ? actionCount : 0;
}

CHESS_CUDA_API int ChessCuda_GetLastError(char* buffer, int capacity)
{
    std::lock_guard<std::mutex> lock(g_cudaMutex);
    const char* text = cudaGetErrorString(g_lastError);
    int needed = 0;
    while (text[needed] != '\0')
    {
        ++needed;
    }
    ++needed;
    if (buffer != nullptr && capacity > 0)
    {
        int count = needed - 1;
        if (count > capacity - 1)
        {
            count = capacity - 1;
        }
        for (int i = 0; i < count; ++i)
        {
            buffer[i] = text[i];
        }
        buffer[count] = '\0';
    }
    return needed;
}
