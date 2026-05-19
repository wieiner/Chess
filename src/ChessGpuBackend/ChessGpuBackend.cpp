#include "ChessGpuBackend.h"

#ifndef NOMINMAX
#define NOMINMAX
#endif

#include <d3d11.h>
#include <d3dcompiler.h>
#include <wrl/client.h>

#include <algorithm>
#include <array>
#include <cstring>
#include <mutex>
#include <string>

#pragma comment(lib, "d3d11.lib")
#pragma comment(lib, "d3dcompiler.lib")

using Microsoft::WRL::ComPtr;

namespace
{
constexpr std::array<int, 7> Material = { 0, 100, 320, 330, 500, 900, 0 };
constexpr int AutoCudaMinBatch = 1024;
constexpr int AutoDirect3DMinBatch = 1024;
constexpr int EvaluatorVersion = 5;

constexpr const char* ComputeShaderSource = R"(
StructuredBuffer<int> Boards : register(t0);
RWStructuredBuffer<int> Scores : register(u0);

cbuffer Params : register(b0)
{
    int BoardCount;
    int SideToMove;
    int Reserved0;
    int Reserved1;
};

int PieceType(int piece)
{
    return piece < 0 ? -piece : piece;
}

int PieceColor(int piece)
{
    return (piece > 0) - (piece < 0);
}

int PieceValue(int piece)
{
    int type = PieceType(piece);
    if (type == 1) return 100;
    if (type == 2) return 320;
    if (type == 3) return 330;
    if (type == 4) return 500;
    if (type == 5) return 900;
    return 0;
}

int Inside(int file, int rank)
{
    return file >= 0 && file < 8 && rank >= 0 && rank < 8;
}

int BoardAt(uint offset, int file, int rank)
{
    return Boards[offset + (uint)(rank * 8 + file)];
}

int CenterBonus(int file, int rank)
{
    return 14 - (abs(file - 3) + abs(rank - 3)) * 3;
}

int PieceSquareBonus(int type, int color, uint square)
{
    int file = (int)(square & 7);
    int rank = (int)(square >> 3);
    int friendlyRank = color > 0 ? rank : 7 - rank;
    int center = CenterBonus(file, rank);

    if (type == 1) return friendlyRank * 7 + (file >= 2 && file <= 5 ? 5 : -3);
    if (type == 2) return center * 3;
    if (type == 3) return center * 2;
    if (type == 4) return friendlyRank >= 6 ? 12 : 0;
    if (type == 5) return center;
    if (type == 6) return friendlyRank <= 1 ? 12 : -center;
    return 0;
}

int RayMobility(uint offset, int file, int rank, int df, int dr, int color)
{
    int result = 0;
    int f = file + df;
    int r = rank + dr;
    while (Inside(f, r) != 0)
    {
        int piece = BoardAt(offset, f, r);
        if (piece == 0)
        {
            result += 3;
        }
        else
        {
            if (PieceColor(piece) == -color)
            {
                result += 5;
            }
            break;
        }
        f += df;
        r += dr;
    }
    return result;
}

int KnightMobility(uint offset, int file, int rank, int color)
{
    int result = 0;
    int target;
    int f;
    int r;

    f = file + 1; r = rank + 2; if (Inside(f, r) != 0) { target = BoardAt(offset, f, r); if (target == 0 || PieceColor(target) == -color) result += 5; }
    f = file + 2; r = rank + 1; if (Inside(f, r) != 0) { target = BoardAt(offset, f, r); if (target == 0 || PieceColor(target) == -color) result += 5; }
    f = file + 2; r = rank - 1; if (Inside(f, r) != 0) { target = BoardAt(offset, f, r); if (target == 0 || PieceColor(target) == -color) result += 5; }
    f = file + 1; r = rank - 2; if (Inside(f, r) != 0) { target = BoardAt(offset, f, r); if (target == 0 || PieceColor(target) == -color) result += 5; }
    f = file - 1; r = rank - 2; if (Inside(f, r) != 0) { target = BoardAt(offset, f, r); if (target == 0 || PieceColor(target) == -color) result += 5; }
    f = file - 2; r = rank - 1; if (Inside(f, r) != 0) { target = BoardAt(offset, f, r); if (target == 0 || PieceColor(target) == -color) result += 5; }
    f = file - 2; r = rank + 1; if (Inside(f, r) != 0) { target = BoardAt(offset, f, r); if (target == 0 || PieceColor(target) == -color) result += 5; }
    f = file - 1; r = rank + 2; if (Inside(f, r) != 0) { target = BoardAt(offset, f, r); if (target == 0 || PieceColor(target) == -color) result += 5; }
    return result;
}

int PieceMobility(uint offset, int type, int file, int rank, int color)
{
    if (type == 2)
    {
        return KnightMobility(offset, file, rank, color);
    }
    if (type == 3)
    {
        return RayMobility(offset, file, rank, 1, 1, color) + RayMobility(offset, file, rank, 1, -1, color) +
            RayMobility(offset, file, rank, -1, 1, color) + RayMobility(offset, file, rank, -1, -1, color);
    }
    if (type == 4)
    {
        return RayMobility(offset, file, rank, 1, 0, color) + RayMobility(offset, file, rank, -1, 0, color) +
            RayMobility(offset, file, rank, 0, 1, color) + RayMobility(offset, file, rank, 0, -1, color);
    }
    if (type == 5)
    {
        return RayMobility(offset, file, rank, 1, 1, color) + RayMobility(offset, file, rank, 1, -1, color) +
            RayMobility(offset, file, rank, -1, 1, color) + RayMobility(offset, file, rank, -1, -1, color) +
            RayMobility(offset, file, rank, 1, 0, color) + RayMobility(offset, file, rank, -1, 0, color) +
            RayMobility(offset, file, rank, 0, 1, color) + RayMobility(offset, file, rank, 0, -1, color);
    }
    return 0;
}

int IsPassedPawn(uint offset, int file, int rank, int color)
{
    int direction = color > 0 ? 1 : -1;
    int enemyPawn = -color;
    for (int df = -1; df <= 1; ++df)
    {
        int f = file + df;
        if (f < 0 || f >= 8)
        {
            continue;
        }
        for (int r = rank + direction; r >= 0 && r < 8; r += direction)
        {
            if (BoardAt(offset, f, r) == enemyPawn)
            {
                return 0;
            }
        }
    }
    return 1;
}

int IsIsolatedPawn(uint offset, int file, int color)
{
    int friendlyPawn = color;
    for (int df = -1; df <= 1; df += 2)
    {
        int f = file + df;
        if (f < 0 || f >= 8)
        {
            continue;
        }
        for (int r = 0; r < 8; ++r)
        {
            if (BoardAt(offset, f, r) == friendlyPawn)
            {
                return 0;
            }
        }
    }
    return 1;
}

int PawnStructureBonus(uint offset, int file, int rank, int color, int endgame)
{
    int friendlyRank = color > 0 ? rank : 7 - rank;
    int result = 0;
    if (IsPassedPawn(offset, file, rank, color) != 0)
    {
        result += 18 + friendlyRank * friendlyRank * 3 + (endgame != 0 ? 18 : 0);
    }
    if (IsIsolatedPawn(offset, file, color) != 0)
    {
        result -= 10;
    }
    return result;
}

int KingShield(uint offset, int kingSquare, int color)
{
    int file = kingSquare & 7;
    int rank = kingSquare >> 3;
    int shieldRank = rank + (color > 0 ? 1 : -1);
    int result = 0;
    for (int df = -1; df <= 1; ++df)
    {
        int f = file + df;
        if (Inside(f, shieldRank) != 0 && BoardAt(offset, f, shieldRank) == color)
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

int KingCentrality(int square)
{
    int file = square & 7;
    int rank = square >> 3;
    return 14 - (abs(file - 3) + abs(rank - 3)) * 2;
}

int KingEdgeBonus(int square)
{
    int file = square & 7;
    int rank = square >> 3;
    int edgeDistance = min(min(file, 7 - file), min(rank, 7 - rank));
    return (3 - edgeDistance) * 18;
}

[numthreads(64, 1, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint boardIndex = dispatchThreadId.x;
    if (boardIndex >= (uint)BoardCount)
    {
        return;
    }

    int score = 0;
    uint offset = boardIndex * 64;
    int whiteNonKing = 0;
    int blackNonKing = 0;
    int whiteKing = 4;
    int blackKing = 60;

    for (uint i = 0; i < 64; ++i)
    {
        int piece = Boards[offset + i];
        int type = PieceType(piece);
        int color = PieceColor(piece);
        int value = PieceValue(piece);
        if (piece == 0)
        {
            continue;
        }
        if (type != 6)
        {
            if (color > 0) whiteNonKing += value; else blackNonKing += value;
        }
        else
        {
            if (color > 0) whiteKing = (int)i; else blackKing = (int)i;
        }
    }

    int endgame = (whiteNonKing + blackNonKing <= 2600) ? 1 : 0;

    for (uint j = 0; j < 64; ++j)
    {
        int piece = Boards[offset + j];
        if (piece == 0)
        {
            continue;
        }
        int type = PieceType(piece);
        int color = PieceColor(piece);
        int file = (int)(j & 7);
        int rank = (int)(j >> 3);
        int term = PieceValue(piece) + PieceSquareBonus(type, color, j) + PieceMobility(offset, type, file, rank, color);
        if (type == 1)
        {
            term += PawnStructureBonus(offset, file, rank, color, endgame);
        }
        score += color * term;
    }

    score += KingShield(offset, whiteKing, 1);
    score -= KingShield(offset, blackKing, -1);
    if (endgame != 0)
    {
        score += KingCentrality(whiteKing) * 5;
        score -= KingCentrality(blackKing) * 5;
        if (whiteNonKing - blackNonKing >= 500)
        {
            score += KingEdgeBonus(blackKing);
        }
        else if (blackNonKing - whiteNonKing >= 500)
        {
            score -= KingEdgeBonus(whiteKing);
        }
    }

    Scores[boardIndex] = SideToMove >= 0 ? score : -score;
}
)";

struct Params
{
    int boardCount = 0;
    int sideToMove = 1;
    int reserved0 = 0;
    int reserved1 = 0;
};

using CudaIsAvailableFn = int(__cdecl*)();
using CudaEvaluateBatchFn = int(__cdecl*)(const int*, int, int, int*);
using CudaGenerateRubikBatchFn = int(__cdecl*)(const int*, const int*, int, int*);
using CudaGetLastErrorFn = int(__cdecl*)(char*, int);

struct GpuContext
{
    bool initialized = false;
    bool available = false;
    bool cudaInitialized = false;
    bool cudaAvailable = false;
    std::string info = "ChessGpuBackend: not initialized.";
    std::string cudaInfo = "CUDA backend not checked.";
    HMODULE cudaModule = nullptr;
    CudaIsAvailableFn cudaIsAvailable = nullptr;
    CudaEvaluateBatchFn cudaEvaluateBatch = nullptr;
    CudaEvaluateBatchFn cudaEvaluate3DBatch = nullptr;
    CudaGenerateRubikBatchFn cudaGenerateRubikBatch = nullptr;
    CudaGetLastErrorFn cudaGetLastError = nullptr;
    ComPtr<ID3D11Device> device;
    ComPtr<ID3D11DeviceContext> context;
    ComPtr<ID3D11ComputeShader> shader;
    int lastBoardCount = 0;
    int totalGpuBatches = 0;
    int totalCpuFallbackBatches = 0;
    int lastBackend = 0;
};

GpuContext g_gpu;
std::mutex g_mutex;

int pieceType(int piece)
{
    return piece < 0 ? -piece : piece;
}

int pieceColor(int piece)
{
    return (piece > 0) - (piece < 0);
}

int pieceSide3D(int piece)
{
    return piece / 10;
}

int pieceType3D(int piece)
{
    return piece == 0 ? 0 : std::abs(piece % 10);
}

bool inside(int file, int rank)
{
    return file >= 0 && file < 8 && rank >= 0 && rank < 8;
}

int boardAt(const int* board64, int file, int rank)
{
    return board64[rank * 8 + file];
}

int index3D(int x, int y, int z)
{
    return z * 64 + y * 8 + x;
}

bool inside3D(int x, int y, int z)
{
    return x >= 0 && x < 8 && y >= 0 && y < 8 && z >= 0 && z < 8;
}

int boardAt3D(const int* board512, int x, int y, int z)
{
    return board512[index3D(x, y, z)];
}

int centerBonus(int file, int rank)
{
    return 14 - (std::abs(file - 3) + std::abs(rank - 3)) * 3;
}

int rayMobilityCpu(const int* board64, int file, int rank, int df, int dr, int color)
{
    int result = 0;
    for (int f = file + df, r = rank + dr; inside(f, r); f += df, r += dr)
    {
        const int piece = boardAt(board64, f, r);
        if (piece == 0)
        {
            result += 3;
            continue;
        }
        if (pieceColor(piece) == -color)
        {
            result += 5;
        }
        break;
    }
    return result;
}

int knightMobilityCpu(const int* board64, int file, int rank, int color)
{
    int result = 0;
    constexpr int Offsets[8][2] = {
        {1, 2}, {2, 1}, {2, -1}, {1, -2}, {-1, -2}, {-2, -1}, {-2, 1}, {-1, 2}
    };
    for (const auto& offset : Offsets)
    {
        const int f = file + offset[0];
        const int r = rank + offset[1];
        if (!inside(f, r))
        {
            continue;
        }
        const int target = boardAt(board64, f, r);
        if (target == 0 || pieceColor(target) == -color)
        {
            result += 5;
        }
    }
    return result;
}

int pieceMobilityCpu(const int* board64, int type, int file, int rank, int color)
{
    if (type == 2)
    {
        return knightMobilityCpu(board64, file, rank, color);
    }
    int result = 0;
    if (type == 3 || type == 5)
    {
        result += rayMobilityCpu(board64, file, rank, 1, 1, color);
        result += rayMobilityCpu(board64, file, rank, 1, -1, color);
        result += rayMobilityCpu(board64, file, rank, -1, 1, color);
        result += rayMobilityCpu(board64, file, rank, -1, -1, color);
    }
    if (type == 4 || type == 5)
    {
        result += rayMobilityCpu(board64, file, rank, 1, 0, color);
        result += rayMobilityCpu(board64, file, rank, -1, 0, color);
        result += rayMobilityCpu(board64, file, rank, 0, 1, color);
        result += rayMobilityCpu(board64, file, rank, 0, -1, color);
    }
    return result;
}

bool isPassedPawnCpu(const int* board64, int file, int rank, int color)
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
            if (boardAt(board64, f, r) == enemyPawn)
            {
                return false;
            }
        }
    }
    return true;
}

bool isIsolatedPawnCpu(const int* board64, int file, int color)
{
    for (int df : { -1, 1 })
    {
        const int f = file + df;
        if (f < 0 || f >= 8)
        {
            continue;
        }
        for (int rank = 0; rank < 8; ++rank)
        {
            if (boardAt(board64, f, rank) == color)
            {
                return false;
            }
        }
    }
    return true;
}

int kingCentralityCpu(int square)
{
    return 14 - (std::abs((square & 7) - 3) + std::abs((square >> 3) - 3)) * 2;
}

int kingEdgeBonusCpu(int square)
{
    const int file = square & 7;
    const int rank = square >> 3;
    const int edgeDistance = std::min(std::min(file, 7 - file), std::min(rank, 7 - rank));
    return (3 - edgeDistance) * 18;
}

int kingShieldCpu(const int* board64, int kingSquare, int color)
{
    const int file = kingSquare & 7;
    const int rank = kingSquare >> 3;
    const int shieldRank = rank + (color > 0 ? 1 : -1);
    int result = 0;
    for (int df = -1; df <= 1; ++df)
    {
        const int f = file + df;
        if (inside(f, shieldRank) && boardAt(board64, f, shieldRank) == color)
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

int evaluateBoardCpu(const int* board64, int sideToMove)
{
    int score = 0;
    int whiteNonKing = 0;
    int blackNonKing = 0;
    int whiteKing = 4;
    int blackKing = 60;
    for (int i = 0; i < 64; ++i)
    {
        const int piece = board64[i];
        if (piece == 0)
        {
            continue;
        }
        const int type = pieceType(piece);
        const int color = pieceColor(piece);
        if (type != 6)
        {
            if (color > 0) whiteNonKing += Material[type]; else blackNonKing += Material[type];
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

    const bool endgame = whiteNonKing + blackNonKing <= 2600;
    for (int i = 0; i < 64; ++i)
    {
        const int piece = board64[i];
        if (piece == 0)
        {
            continue;
        }
        const int type = pieceType(piece);
        const int color = pieceColor(piece);
        const int file = i & 7;
        const int rank = i >> 3;
        const int friendlyRank = color > 0 ? rank : 7 - rank;
        const int center = centerBonus(file, rank);
        int bonus = 0;
        if (type == 1)
        {
            bonus = friendlyRank * 7 + (file >= 2 && file <= 5 ? 5 : -3);
            if (isPassedPawnCpu(board64, file, rank, color))
            {
                bonus += 18 + friendlyRank * friendlyRank * 3 + (endgame ? 18 : 0);
            }
            if (isIsolatedPawnCpu(board64, file, color))
            {
                bonus -= 10;
            }
        }
        else if (type == 2) bonus = center * 3;
        else if (type == 3) bonus = center * 2;
        else if (type == 4) bonus = friendlyRank >= 6 ? 12 : 0;
        else if (type == 5) bonus = center;
        else if (type == 6) bonus = friendlyRank <= 1 ? 12 : -center;
        score += color * (Material[type] + bonus + pieceMobilityCpu(board64, type, file, rank, color));
    }
    score += kingShieldCpu(board64, whiteKing, 1);
    score -= kingShieldCpu(board64, blackKing, -1);
    if (endgame)
    {
        score += kingCentralityCpu(whiteKing) * 5;
        score -= kingCentralityCpu(blackKing) * 5;
        if (whiteNonKing - blackNonKing >= 500)
        {
            score += kingEdgeBonusCpu(blackKing);
        }
        else if (blackNonKing - whiteNonKing >= 500)
        {
            score -= kingEdgeBonusCpu(whiteKing);
        }
    }
    return sideToMove >= 0 ? score : -score;
}

int centerBonus3D(int x, int y, int z)
{
    return 24 - (std::abs(x - 3) + std::abs(y - 3) + std::abs(z - 3)) * 2;
}

std::array<int, 3> forwardForSide3D(int side)
{
    switch (side)
    {
    case 1: return { 0, 0, 1 };
    case 2: return { 0, 0, -1 };
    case 3: return { 0, 1, 0 };
    case 4: return { 0, -1, 0 };
    case 5: return { 1, 0, 0 };
    case 6: return { -1, 0, 0 };
    default: return { 0, 0, 1 };
    }
}

int rayMobility3DCpu(const int* board512, int x, int y, int z, int dx, int dy, int dz, int side)
{
    int result = 0;
    for (int tx = x + dx, ty = y + dy, tz = z + dz; inside3D(tx, ty, tz); tx += dx, ty += dy, tz += dz)
    {
        const int target = boardAt3D(board512, tx, ty, tz);
        if (target == 0)
        {
            result += 2;
            continue;
        }
        if (pieceSide3D(target) != side)
        {
            result += 4;
        }
        break;
    }
    return result;
}

int mobility3DCpu(const int* board512, int type, int x, int y, int z, int side)
{
    if (type == 1)
    {
        const auto f = forwardForSide3D(side);
        int result = 0;
        const int tx = x + f[0];
        const int ty = y + f[1];
        const int tz = z + f[2];
        if (inside3D(tx, ty, tz) && boardAt3D(board512, tx, ty, tz) == 0)
        {
            result += 5;
        }
        return result;
    }
    if (type == 2)
    {
        int result = 0;
        for (int longAxis = 0; longAxis < 3; ++longAxis)
        {
            for (int shortAxis = 0; shortAxis < 3; ++shortAxis)
            {
                if (longAxis == shortAxis)
                {
                    continue;
                }
                for (int longSign : { -1, 1 })
                {
                    for (int shortSign : { -1, 1 })
                    {
                        int d[3] = {};
                        d[longAxis] = 2 * longSign;
                        d[shortAxis] = shortSign;
                        const int tx = x + d[0];
                        const int ty = y + d[1];
                        const int tz = z + d[2];
                        if (inside3D(tx, ty, tz))
                        {
                            const int target = boardAt3D(board512, tx, ty, tz);
                            if (target == 0 || pieceSide3D(target) != side)
                            {
                                result += 5;
                            }
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
                if (dx == 0 && dy == 0 && dz == 0)
                {
                    continue;
                }
                const int axes = (dx != 0) + (dy != 0) + (dz != 0);
                if ((type == 4 && axes != 1) || (type == 3 && axes < 2))
                {
                    continue;
                }
                if (type == 6)
                {
                    const int tx = x + dx;
                    const int ty = y + dy;
                    const int tz = z + dz;
                    if (inside3D(tx, ty, tz))
                    {
                        const int target = boardAt3D(board512, tx, ty, tz);
                        if (target == 0 || pieceSide3D(target) != side)
                        {
                            result += 3;
                        }
                    }
                }
                else
                {
                    result += rayMobility3DCpu(board512, x, y, z, dx, dy, dz, side);
                }
            }
        }
    }
    return result;
}

int evaluateBoard3DCpu(const int* board512, int perspectiveSide)
{
    perspectiveSide = std::clamp(perspectiveSide, 1, 6);
    int score = 0;
    std::array<int, 7> sideMaterial{};
    for (int i = 0; i < 512; ++i)
    {
        const int piece = board512[i];
        if (piece == 0)
        {
            continue;
        }
        const int side = pieceSide3D(piece);
        const int type = pieceType3D(piece);
        if (side >= 1 && side <= 6 && type >= 1 && type <= 6)
        {
            sideMaterial[side] += Material[type];
        }
    }

    for (int z = 0; z < 8; ++z)
    {
        for (int y = 0; y < 8; ++y)
        {
            for (int x = 0; x < 8; ++x)
            {
                const int piece = boardAt3D(board512, x, y, z);
                if (piece == 0)
                {
                    continue;
                }
                const int side = pieceSide3D(piece);
                const int type = pieceType3D(piece);
                if (side < 1 || side > 6 || type < 1 || type > 6)
                {
                    continue;
                }
                const int sign = side == perspectiveSide ? 1 : -1;
                int term = Material[type] + centerBonus3D(x, y, z) + mobility3DCpu(board512, type, x, y, z, side);
                if (type == 6)
                {
                    term += sideMaterial[side] <= 1200 ? centerBonus3D(x, y, z) * 2 : -centerBonus3D(x, y, z);
                }
                score += sign * term;
            }
        }
    }
    return score;
}

std::array<int, 3> rotateLayerSquareCpu(int axis, int layer, int turns, int x, int y, int z)
{
    int u = axis == 2 ? y : x;
    int v = axis == 0 ? y : z;
    for (int i = 0; i < turns; ++i)
    {
        const int nextU = 7 - v;
        const int nextV = u;
        u = nextU;
        v = nextV;
    }
    if (axis == 0) return { u, v, layer };
    if (axis == 1) return { u, layer, v };
    return { layer, u, v };
}

void rotateLayerCpu(const int* board512, int axis, int layer, int turns, int* output512)
{
    std::copy(board512, board512 + 512, output512);
    turns %= 4;
    if (turns < 0)
    {
        turns += 4;
    }
    if (axis < 0 || axis > 2 || layer < 0 || layer >= 8 || turns == 0)
    {
        return;
    }
    for (int z = 0; z < 8; ++z)
    {
        for (int y = 0; y < 8; ++y)
        {
            for (int x = 0; x < 8; ++x)
            {
                const bool inLayer = (axis == 0 && z == layer) || (axis == 1 && y == layer) || (axis == 2 && x == layer);
                if (!inLayer)
                {
                    continue;
                }
                const auto to = rotateLayerSquareCpu(axis, layer, turns, x, y, z);
                output512[index3D(to[0], to[1], to[2])] = board512[index3D(x, y, z)];
            }
        }
    }
}

int copyString(const std::string& value, char* buffer, int capacity)
{
    const int needed = static_cast<int>(value.size()) + 1;
    if (buffer != nullptr && capacity > 0)
    {
        const int count = std::min(capacity - 1, static_cast<int>(value.size()));
        std::memcpy(buffer, value.data(), static_cast<std::size_t>(count));
        buffer[count] = '\0';
    }
    return needed;
}

HMODULE loadCudaBackendModule()
{
    HMODULE currentModule = nullptr;
    wchar_t modulePath[MAX_PATH] = {};
    if (GetModuleHandleExW(
            GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
            reinterpret_cast<LPCWSTR>(&loadCudaBackendModule),
            &currentModule) != 0 &&
        GetModuleFileNameW(currentModule, modulePath, static_cast<DWORD>(std::size(modulePath))) != 0)
    {
        std::wstring adjacentPath(modulePath);
        const size_t slash = adjacentPath.find_last_of(L"\\/");
        if (slash != std::wstring::npos)
        {
            adjacentPath.resize(slash + 1);
            adjacentPath += L"ChessCudaBackend.dll";
            if (HMODULE adjacentModule = LoadLibraryW(adjacentPath.c_str()))
            {
                return adjacentModule;
            }
        }
    }

    return LoadLibraryW(L"ChessCudaBackend.dll");
}

std::string cudaLastErrorLocked()
{
    if (g_gpu.cudaGetLastError == nullptr)
    {
        return "unknown CUDA error";
    }
    char buffer[512] = {};
    g_gpu.cudaGetLastError(buffer, static_cast<int>(std::size(buffer)));
    return buffer;
}

bool initializeCudaLocked()
{
    if (g_gpu.cudaInitialized)
    {
        return g_gpu.cudaAvailable;
    }
    g_gpu.cudaInitialized = true;

    g_gpu.cudaModule = loadCudaBackendModule();
    if (g_gpu.cudaModule == nullptr)
    {
        g_gpu.cudaInfo = "ChessCudaBackend.dll could not be loaded; CUDA disabled. Win32 error " + std::to_string(GetLastError()) + ".";
        return g_gpu.cudaAvailable;
    }

    g_gpu.cudaIsAvailable = reinterpret_cast<CudaIsAvailableFn>(GetProcAddress(g_gpu.cudaModule, "ChessCuda_IsAvailable"));
    g_gpu.cudaEvaluateBatch = reinterpret_cast<CudaEvaluateBatchFn>(GetProcAddress(g_gpu.cudaModule, "ChessCuda_EvaluateBatch"));
    g_gpu.cudaEvaluate3DBatch = reinterpret_cast<CudaEvaluateBatchFn>(GetProcAddress(g_gpu.cudaModule, "ChessCuda_Evaluate3DBatch"));
    g_gpu.cudaGenerateRubikBatch = reinterpret_cast<CudaGenerateRubikBatchFn>(GetProcAddress(g_gpu.cudaModule, "ChessCuda_GenerateRubikBatch"));
    g_gpu.cudaGetLastError = reinterpret_cast<CudaGetLastErrorFn>(GetProcAddress(g_gpu.cudaModule, "ChessCuda_GetLastError"));

    if (g_gpu.cudaIsAvailable == nullptr || g_gpu.cudaEvaluateBatch == nullptr ||
        g_gpu.cudaEvaluate3DBatch == nullptr || g_gpu.cudaGenerateRubikBatch == nullptr)
    {
        g_gpu.cudaInfo = "ChessCudaBackend.dll has incompatible exports; CUDA disabled.";
        return g_gpu.cudaAvailable;
    }

    if (g_gpu.cudaIsAvailable() == 0)
    {
        g_gpu.cudaInfo = "ChessCudaBackend.dll loaded, but CUDA device is unavailable: " + cudaLastErrorLocked();
        return g_gpu.cudaAvailable;
    }

    g_gpu.cudaAvailable = true;
    g_gpu.cudaInfo = "ChessCudaBackend.dll active.";
    return true;
}

bool initializeGpuLocked()
{
    initializeCudaLocked();
    if (g_gpu.initialized)
    {
        return g_gpu.available || g_gpu.cudaAvailable;
    }
    g_gpu.initialized = true;

    D3D_FEATURE_LEVEL requested[] = { D3D_FEATURE_LEVEL_11_1, D3D_FEATURE_LEVEL_11_0 };
    D3D_FEATURE_LEVEL actual{};
    UINT flags = 0;
#if defined(_DEBUG)
    flags |= D3D11_CREATE_DEVICE_DEBUG;
#endif

    HRESULT hr = D3D11CreateDevice(
        nullptr,
        D3D_DRIVER_TYPE_HARDWARE,
        nullptr,
        flags,
        requested,
        static_cast<UINT>(std::size(requested)),
        D3D11_SDK_VERSION,
        &g_gpu.device,
        &actual,
        &g_gpu.context);

#if defined(_DEBUG)
    if (FAILED(hr))
    {
        flags &= ~D3D11_CREATE_DEVICE_DEBUG;
        hr = D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, flags, requested, static_cast<UINT>(std::size(requested)),
            D3D11_SDK_VERSION, &g_gpu.device, &actual, &g_gpu.context);
    }
#endif

    if (FAILED(hr))
    {
        g_gpu.info = "ChessGpuBackend: Direct3D 11 hardware device unavailable. " + g_gpu.cudaInfo;
        return g_gpu.cudaAvailable;
    }

    ComPtr<ID3DBlob> shaderBlob;
    ComPtr<ID3DBlob> errorBlob;
    hr = D3DCompile(
        ComputeShaderSource,
        std::strlen(ComputeShaderSource),
        "ChessGpuEvaluateBatch.hlsl",
        nullptr,
        nullptr,
        "main",
        "cs_5_0",
        D3DCOMPILE_ENABLE_STRICTNESS,
        0,
        &shaderBlob,
        &errorBlob);

    if (FAILED(hr))
    {
        g_gpu.info = "ChessGpuBackend: compute shader compilation failed. " + g_gpu.cudaInfo;
        if (errorBlob)
        {
            g_gpu.info += " ";
            g_gpu.info += static_cast<const char*>(errorBlob->GetBufferPointer());
        }
        return g_gpu.cudaAvailable;
    }

    hr = g_gpu.device->CreateComputeShader(shaderBlob->GetBufferPointer(), shaderBlob->GetBufferSize(), nullptr, &g_gpu.shader);
    if (FAILED(hr))
    {
        g_gpu.info = "ChessGpuBackend: compute shader creation failed. " + g_gpu.cudaInfo;
        return g_gpu.cudaAvailable;
    }

    g_gpu.available = true;
    g_gpu.info = g_gpu.cudaAvailable
        ? "ChessGpuBackend: CUDA backend active with Direct3D 11 fallback (v5 auto-threshold CUDA/Direct3D/CPU)."
        : "ChessGpuBackend: Direct3D 11 compute shader active (v5 2D batch evaluator). " + g_gpu.cudaInfo;
    return true;
}

bool createStructuredBuffer(UINT byteWidth, UINT stride, const void* data, ComPtr<ID3D11Buffer>& buffer)
{
    D3D11_BUFFER_DESC desc{};
    desc.ByteWidth = byteWidth;
    desc.Usage = D3D11_USAGE_DEFAULT;
    desc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
    desc.MiscFlags = D3D11_RESOURCE_MISC_BUFFER_STRUCTURED;
    desc.StructureByteStride = stride;

    D3D11_SUBRESOURCE_DATA init{};
    init.pSysMem = data;
    return SUCCEEDED(g_gpu.device->CreateBuffer(&desc, data != nullptr ? &init : nullptr, &buffer));
}

bool evaluateBatchGpuLocked(const int* boards64, int boardCount, int sideToMove, int* scores)
{
    if (!initializeGpuLocked() || !g_gpu.available || g_gpu.device == nullptr || g_gpu.context == nullptr || g_gpu.shader == nullptr)
    {
        return false;
    }

    const UINT boardBytes = static_cast<UINT>(boardCount * 64 * sizeof(int));
    const UINT scoreBytes = static_cast<UINT>(boardCount * sizeof(int));

    ComPtr<ID3D11Buffer> boardBuffer;
    if (!createStructuredBuffer(boardBytes, sizeof(int), boards64, boardBuffer))
    {
        return false;
    }

    D3D11_SHADER_RESOURCE_VIEW_DESC srvDesc{};
    srvDesc.Format = DXGI_FORMAT_UNKNOWN;
    srvDesc.ViewDimension = D3D11_SRV_DIMENSION_BUFFER;
    srvDesc.Buffer.FirstElement = 0;
    srvDesc.Buffer.NumElements = static_cast<UINT>(boardCount * 64);

    ComPtr<ID3D11ShaderResourceView> boardSrv;
    if (FAILED(g_gpu.device->CreateShaderResourceView(boardBuffer.Get(), &srvDesc, &boardSrv)))
    {
        return false;
    }

    D3D11_BUFFER_DESC outputDesc{};
    outputDesc.ByteWidth = scoreBytes;
    outputDesc.Usage = D3D11_USAGE_DEFAULT;
    outputDesc.BindFlags = D3D11_BIND_UNORDERED_ACCESS;
    outputDesc.MiscFlags = D3D11_RESOURCE_MISC_BUFFER_STRUCTURED;
    outputDesc.StructureByteStride = sizeof(int);

    ComPtr<ID3D11Buffer> outputBuffer;
    if (FAILED(g_gpu.device->CreateBuffer(&outputDesc, nullptr, &outputBuffer)))
    {
        return false;
    }

    D3D11_UNORDERED_ACCESS_VIEW_DESC uavDesc{};
    uavDesc.Format = DXGI_FORMAT_UNKNOWN;
    uavDesc.ViewDimension = D3D11_UAV_DIMENSION_BUFFER;
    uavDesc.Buffer.FirstElement = 0;
    uavDesc.Buffer.NumElements = static_cast<UINT>(boardCount);

    ComPtr<ID3D11UnorderedAccessView> outputUav;
    if (FAILED(g_gpu.device->CreateUnorderedAccessView(outputBuffer.Get(), &uavDesc, &outputUav)))
    {
        return false;
    }

    Params params;
    params.boardCount = boardCount;
    params.sideToMove = sideToMove >= 0 ? 1 : -1;

    D3D11_BUFFER_DESC cbDesc{};
    cbDesc.ByteWidth = sizeof(Params);
    cbDesc.Usage = D3D11_USAGE_DEFAULT;
    cbDesc.BindFlags = D3D11_BIND_CONSTANT_BUFFER;

    D3D11_SUBRESOURCE_DATA cbData{};
    cbData.pSysMem = &params;
    ComPtr<ID3D11Buffer> constantBuffer;
    if (FAILED(g_gpu.device->CreateBuffer(&cbDesc, &cbData, &constantBuffer)))
    {
        return false;
    }

    ID3D11ShaderResourceView* srvs[] = { boardSrv.Get() };
    ID3D11UnorderedAccessView* uavs[] = { outputUav.Get() };
    ID3D11Buffer* cbs[] = { constantBuffer.Get() };
    g_gpu.context->CSSetShader(g_gpu.shader.Get(), nullptr, 0);
    g_gpu.context->CSSetShaderResources(0, 1, srvs);
    g_gpu.context->CSSetUnorderedAccessViews(0, 1, uavs, nullptr);
    g_gpu.context->CSSetConstantBuffers(0, 1, cbs);
    g_gpu.context->Dispatch(static_cast<UINT>((boardCount + 63) / 64), 1, 1);

    ID3D11ShaderResourceView* nullSrv[] = { nullptr };
    ID3D11UnorderedAccessView* nullUav[] = { nullptr };
    ID3D11Buffer* nullCb[] = { nullptr };
    g_gpu.context->CSSetShaderResources(0, 1, nullSrv);
    g_gpu.context->CSSetUnorderedAccessViews(0, 1, nullUav, nullptr);
    g_gpu.context->CSSetConstantBuffers(0, 1, nullCb);
    g_gpu.context->CSSetShader(nullptr, nullptr, 0);

    D3D11_BUFFER_DESC stagingDesc{};
    stagingDesc.ByteWidth = scoreBytes;
    stagingDesc.Usage = D3D11_USAGE_STAGING;
    stagingDesc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
    stagingDesc.MiscFlags = D3D11_RESOURCE_MISC_BUFFER_STRUCTURED;
    stagingDesc.StructureByteStride = sizeof(int);

    ComPtr<ID3D11Buffer> staging;
    if (FAILED(g_gpu.device->CreateBuffer(&stagingDesc, nullptr, &staging)))
    {
        return false;
    }

    g_gpu.context->CopyResource(staging.Get(), outputBuffer.Get());

    D3D11_MAPPED_SUBRESOURCE mapped{};
    if (FAILED(g_gpu.context->Map(staging.Get(), 0, D3D11_MAP_READ, 0, &mapped)))
    {
        return false;
    }

    std::memcpy(scores, mapped.pData, scoreBytes);
    g_gpu.context->Unmap(staging.Get(), 0);
    return true;
}
}

CHESS_GPU_API int ChessGpu_IsAvailable()
{
    std::lock_guard lock(g_mutex);
    return initializeGpuLocked() ? 1 : 0;
}

CHESS_GPU_API int ChessGpu_GetBackendInfo(char* buffer, int capacity)
{
    std::lock_guard lock(g_mutex);
    initializeGpuLocked();
    return copyString(g_gpu.info, buffer, capacity);
}

CHESS_GPU_API int ChessGpu_EvaluateBatch(const int* boards64, int boardCount, int sideToMove, int* scores)
{
    return ChessGpu_EvaluateBatchEx(boards64, boardCount, sideToMove, scores, ChessGpuBackendAuto);
}

CHESS_GPU_API int ChessGpu_EvaluateBatchEx(const int* boards64, int boardCount, int sideToMove, int* scores, int backendMode)
{
    if (boards64 == nullptr || scores == nullptr || boardCount < 0)
    {
        return 0;
    }
    if (boardCount == 0)
    {
        return 0;
    }

    const bool forceCpu = backendMode == ChessGpuBackendCpu;
    const bool forceDirect3D = backendMode == ChessGpuBackendDirect3D;
    const bool forceCuda = backendMode == ChessGpuBackendCuda;
    const bool autoMode = backendMode == ChessGpuBackendAuto;

    if (forceCpu)
    {
        {
            std::lock_guard lock(g_mutex);
            g_gpu.lastBoardCount = boardCount;
            ++g_gpu.totalCpuFallbackBatches;
            g_gpu.lastBackend = 0;
        }
        for (int i = 0; i < boardCount; ++i)
        {
            scores[i] = evaluateBoardCpu(boards64 + i * 64, sideToMove);
        }
        return boardCount;
    }

    {
        std::lock_guard lock(g_mutex);
        g_gpu.lastBoardCount = boardCount;
        if ((forceCuda || (autoMode && boardCount >= AutoCudaMinBatch)) &&
            initializeCudaLocked() && g_gpu.cudaEvaluateBatch != nullptr)
        {
            const int count = g_gpu.cudaEvaluateBatch(boards64, boardCount, sideToMove, scores);
            if (count == boardCount)
            {
                ++g_gpu.totalGpuBatches;
                g_gpu.lastBackend = 2;
                return boardCount;
            }
            g_gpu.cudaInfo = "CUDA 2D evaluation failed: " + cudaLastErrorLocked();
        }
        if (forceCuda)
        {
            return 0;
        }
        if ((forceDirect3D || (autoMode && boardCount >= AutoDirect3DMinBatch)) &&
            evaluateBatchGpuLocked(boards64, boardCount, sideToMove, scores))
        {
            ++g_gpu.totalGpuBatches;
            g_gpu.lastBackend = 1;
            return boardCount;
        }
        if (forceDirect3D)
        {
            return 0;
        }
        ++g_gpu.totalCpuFallbackBatches;
        g_gpu.lastBackend = 0;
    }

    for (int i = 0; i < boardCount; ++i)
    {
        scores[i] = evaluateBoardCpu(boards64 + i * 64, sideToMove);
    }
    return boardCount;
}

CHESS_GPU_API int ChessGpu_Evaluate3DBatch(const int* boards512, int boardCount, int perspectiveSide, int* scores)
{
    if (boards512 == nullptr || scores == nullptr || boardCount <= 0)
    {
        return 0;
    }
    {
        std::lock_guard lock(g_mutex);
        g_gpu.lastBoardCount = boardCount;
        if (initializeCudaLocked() && g_gpu.cudaEvaluate3DBatch != nullptr)
        {
            const int count = g_gpu.cudaEvaluate3DBatch(boards512, boardCount, perspectiveSide, scores);
            if (count == boardCount)
            {
                ++g_gpu.totalGpuBatches;
                g_gpu.lastBackend = 2;
                return boardCount;
            }
            g_gpu.cudaInfo = "CUDA 3D evaluation failed: " + cudaLastErrorLocked();
        }
        ++g_gpu.totalCpuFallbackBatches;
        g_gpu.lastBackend = 0;
    }
    for (int i = 0; i < boardCount; ++i)
    {
        scores[i] = evaluateBoard3DCpu(boards512 + i * 512, perspectiveSide);
    }
    return boardCount;
}

CHESS_GPU_API int ChessGpu_GenerateRubikBatch(const int* board512, const int* actions3, int actionCount, int* outBoards512)
{
    if (board512 == nullptr || actions3 == nullptr || outBoards512 == nullptr || actionCount <= 0)
    {
        return 0;
    }
    {
        std::lock_guard lock(g_mutex);
        g_gpu.lastBoardCount = actionCount;
        if (initializeCudaLocked() && g_gpu.cudaGenerateRubikBatch != nullptr)
        {
            const int count = g_gpu.cudaGenerateRubikBatch(board512, actions3, actionCount, outBoards512);
            if (count == actionCount)
            {
                ++g_gpu.totalGpuBatches;
                g_gpu.lastBackend = 2;
                return actionCount;
            }
            g_gpu.cudaInfo = "CUDA Rubik generation failed: " + cudaLastErrorLocked();
        }
        ++g_gpu.totalCpuFallbackBatches;
        g_gpu.lastBackend = 0;
    }
    for (int i = 0; i < actionCount; ++i)
    {
        const int axis = actions3[i * 3];
        const int layer = actions3[i * 3 + 1];
        const int turns = actions3[i * 3 + 2];
        rotateLayerCpu(board512, axis, layer, turns, outBoards512 + i * 512);
    }
    return actionCount;
}

CHESS_GPU_API int ChessGpu_GetKernelStats(ChessGpuKernelStatsDto* stats)
{
    if (stats == nullptr)
    {
        return 0;
    }

    std::lock_guard lock(g_mutex);
    initializeGpuLocked();
    *stats = ChessGpuKernelStatsDto{};
    stats->backend = g_gpu.lastBackend != 0 ? g_gpu.lastBackend : g_gpu.cudaAvailable ? 2 : g_gpu.available ? 1 : 0;
    stats->lastBoardCount = g_gpu.lastBoardCount;
    stats->totalGpuBatches = g_gpu.totalGpuBatches;
    stats->totalCpuFallbackBatches = g_gpu.totalCpuFallbackBatches;
    stats->evaluatorVersion = EvaluatorVersion;
    return 1;
}
