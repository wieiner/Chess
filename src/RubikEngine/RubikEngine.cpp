#include "RubikEngine.h"

#include <algorithm>
#include <cstring>
#include <random>
#include <sstream>
#include <string>
#include <vector>

namespace
{
constexpr int DefaultCubeSize = 8;
constexpr int MinCubeSize = 2;
constexpr int MaxCubeSize = 32;

struct Vec3
{
    int x;
    int y;
    int z;
};

struct Move
{
    int axis;
    int layer;
    int turns;
};

struct Engine
{
    int size = DefaultCubeSize;
    std::vector<int> cells;
    std::vector<Move> history;
    std::string lastInfo;
    std::string lastCommandText;
    Move lastMove{ -1, -1, 0 };
    bool manualState = false;
};

int safeSize(int size)
{
    return std::clamp(size, MinCubeSize, MaxCubeSize);
}

int cellCount(int size)
{
    return size * size * size;
}

int indexOf(const Engine& engine, int x, int y, int z)
{
    return z * engine.size * engine.size + y * engine.size + x;
}

bool inside(const Engine& engine, int x, int y, int z)
{
    return x >= 0 && x < engine.size && y >= 0 && y < engine.size && z >= 0 && z < engine.size;
}

int normalizeTurns(int quarterTurns)
{
    int turns = quarterTurns % 4;
    if (turns < 0)
    {
        turns += 4;
    }
    return turns;
}

const char* axisName(int axis)
{
    switch (axis)
    {
    case 0:
        return "Z";
    case 1:
        return "Y";
    default:
        return "X";
    }
}

std::string moveText(const Move& move)
{
    std::ostringstream text;
    text << axisName(move.axis) << (move.layer + 1);
    if (move.turns == 2)
    {
        text << "x2";
    }
    else if (move.turns == 3)
    {
        text << "'";
    }
    return text.str();
}

std::string movesText(const std::vector<Move>& moves)
{
    std::ostringstream text;
    for (size_t i = 0; i < moves.size(); ++i)
    {
        if (i != 0)
        {
            text << ' ';
        }
        text << moveText(moves[i]);
    }
    return text.str();
}

int copyText(const std::string& value, char* buffer, int capacity)
{
    const int required = static_cast<int>(value.size()) + 1;
    if (buffer == nullptr || capacity <= 0)
    {
        return required;
    }

    const int bytesToCopy = std::min(capacity - 1, static_cast<int>(value.size()));
    if (bytesToCopy > 0)
    {
        std::memcpy(buffer, value.data(), static_cast<size_t>(bytesToCopy));
    }
    buffer[bytesToCopy] = '\0';
    return required;
}

void reset(Engine& engine)
{
    engine.cells.resize(cellCount(engine.size));
    for (int i = 0; i < static_cast<int>(engine.cells.size()); ++i)
    {
        engine.cells[i] = i;
    }
    engine.history.clear();
    engine.lastMove = { -1, -1, 0 };
    engine.manualState = false;
    engine.lastCommandText.clear();

    std::ostringstream info;
    info << "Rubik cube reset to solved " << engine.size << "x" << engine.size << "x" << engine.size << " state.";
    engine.lastInfo = info.str();
}

bool setSize(Engine& engine, int requestedSize)
{
    const int nextSize = safeSize(requestedSize);
    engine.size = nextSize;
    reset(engine);
    std::ostringstream info;
    info << "Cube dimension set to " << engine.size << "x" << engine.size << "x" << engine.size << ".";
    engine.lastInfo = info.str();
    return true;
}

bool isSolved(const Engine& engine)
{
    if (static_cast<int>(engine.cells.size()) != cellCount(engine.size))
    {
        return false;
    }
    for (int i = 0; i < static_cast<int>(engine.cells.size()); ++i)
    {
        if (engine.cells[i] != i)
        {
            return false;
        }
    }
    return true;
}

Vec3 rotateLayerSquare(const Engine& engine, int axis, int layer, int turns, int x, int y, int z)
{
    int u = 0;
    int v = 0;
    switch (axis)
    {
    case 0:
        u = x;
        v = y;
        break;
    case 1:
        u = x;
        v = z;
        break;
    default:
        u = y;
        v = z;
        break;
    }

    for (int i = 0; i < turns; ++i)
    {
        const int nextU = engine.size - 1 - v;
        const int nextV = u;
        u = nextU;
        v = nextV;
    }

    switch (axis)
    {
    case 0:
        return { u, v, layer };
    case 1:
        return { u, layer, v };
    default:
        return { layer, u, v };
    }
}

void rememberMove(Engine& engine, Move move)
{
    if (!engine.history.empty())
    {
        Move& last = engine.history.back();
        if (last.axis == move.axis && last.layer == move.layer)
        {
            last.turns = normalizeTurns(last.turns + move.turns);
            if (last.turns == 0)
            {
                engine.history.pop_back();
            }
            return;
        }
    }
    engine.history.push_back(move);
}

bool rotateLayer(Engine& engine, int axis, int layer, int quarterTurns, bool recordHistory)
{
    if (axis < 0 || axis > 2 || layer < 0 || layer >= engine.size)
    {
        engine.lastInfo = "Rotation rejected: axis or layer is out of range.";
        return false;
    }

    const int turns = normalizeTurns(quarterTurns);
    if (turns == 0)
    {
        engine.lastInfo = "Rotation skipped: turn count is a full cycle.";
        return true;
    }

    const std::vector<int> before = engine.cells;
    for (int z = 0; z < engine.size; ++z)
    {
        for (int y = 0; y < engine.size; ++y)
        {
            for (int x = 0; x < engine.size; ++x)
            {
                const bool inLayer = (axis == 0 && z == layer) || (axis == 1 && y == layer) || (axis == 2 && x == layer);
                if (!inLayer)
                {
                    continue;
                }
                const Vec3 to = rotateLayerSquare(engine, axis, layer, turns, x, y, z);
                engine.cells[indexOf(engine, to.x, to.y, to.z)] = before[indexOf(engine, x, y, z)];
            }
        }
    }

    const Move move{ axis, layer, turns };
    engine.lastMove = move;
    if (recordHistory)
    {
        engine.manualState = false;
        rememberMove(engine, move);
    }

    std::ostringstream info;
    info << "Rotated " << moveText(move) << " on " << engine.size << "x" << engine.size << "x" << engine.size << ".";
    engine.lastInfo = info.str();
    return true;
}

Engine* asEngine(void* handle)
{
    return static_cast<Engine*>(handle);
}

std::vector<Move> reverseHistory(const Engine& engine)
{
    std::vector<Move> result;
    result.reserve(engine.history.size());
    for (auto it = engine.history.rbegin(); it != engine.history.rend(); ++it)
    {
        const int inverseTurns = normalizeTurns(4 - it->turns);
        if (inverseTurns != 0)
        {
            result.push_back({ it->axis, it->layer, inverseTurns });
        }
    }
    return result;
}
}

RUBIK_API void* Rubik_Create()
{
    return Rubik_CreateSized(DefaultCubeSize);
}

RUBIK_API void* Rubik_CreateSized(int size)
{
    auto* engine = new Engine();
    engine->size = safeSize(size);
    reset(*engine);
    return engine;
}

RUBIK_API void Rubik_Destroy(void* handle)
{
    delete asEngine(handle);
}

RUBIK_API void Rubik_Reset(void* handle)
{
    if (auto* engine = asEngine(handle))
    {
        reset(*engine);
    }
}

RUBIK_API int Rubik_SetSize(void* handle, int size)
{
    auto* engine = asEngine(handle);
    return engine != nullptr && setSize(*engine, size) ? 1 : 0;
}

RUBIK_API int Rubik_GetState(void* handle, RubikStateDto* state)
{
    auto* engine = asEngine(handle);
    if (engine == nullptr || state == nullptr)
    {
        return 0;
    }

    state->size = engine->size;
    state->cellCount = static_cast<int>(engine->cells.size());
    state->historyCount = static_cast<int>(engine->history.size());
    state->isSolved = isSolved(*engine) ? 1 : 0;
    state->manualState = engine->manualState ? 1 : 0;
    state->lastAxis = engine->lastMove.axis;
    state->lastLayer = engine->lastMove.layer;
    state->lastQuarterTurns = engine->lastMove.turns;
    return 1;
}

RUBIK_API int Rubik_GetCells(void* handle, int* cells)
{
    auto* engine = asEngine(handle);
    if (engine == nullptr || cells == nullptr)
    {
        return 0;
    }
    std::copy(engine->cells.begin(), engine->cells.end(), cells);
    return 1;
}

RUBIK_API int Rubik_SetCells(void* handle, const int* cells)
{
    auto* engine = asEngine(handle);
    if (engine == nullptr || cells == nullptr)
    {
        return 0;
    }
    std::copy(cells, cells + engine->cells.size(), engine->cells.begin());
    engine->history.clear();
    engine->manualState = true;
    engine->lastCommandText.clear();
    engine->lastInfo = "Manual cube state loaded. Reverse-history solving is unavailable for this state.";
    return 1;
}

RUBIK_API int Rubik_SetCell(void* handle, int x, int y, int z, int value)
{
    auto* engine = asEngine(handle);
    if (engine == nullptr || !inside(*engine, x, y, z))
    {
        return 0;
    }
    engine->cells[indexOf(*engine, x, y, z)] = value;
    engine->history.clear();
    engine->manualState = true;
    engine->lastCommandText.clear();
    engine->lastInfo = "Manual cell edit applied. Reverse-history solving is unavailable for this state.";
    return 1;
}

RUBIK_API int Rubik_RotateLayer(void* handle, int axis, int layer, int quarterTurns)
{
    auto* engine = asEngine(handle);
    return engine != nullptr && rotateLayer(*engine, axis, layer, quarterTurns, true) ? 1 : 0;
}

RUBIK_API int Rubik_Scramble(void* handle, int seed, int length)
{
    auto* engine = asEngine(handle);
    if (engine == nullptr)
    {
        return 0;
    }

    reset(*engine);
    const int safeLength = std::clamp(length, 0, 10000);
    std::mt19937 rng(static_cast<unsigned int>(seed));
    std::uniform_int_distribution<int> axisDist(0, 2);
    std::uniform_int_distribution<int> layerDist(0, engine->size - 1);
    std::uniform_int_distribution<int> turnDist(1, 3);

    for (int i = 0; i < safeLength; ++i)
    {
        rotateLayer(*engine, axisDist(rng), layerDist(rng), turnDist(rng), true);
    }

    engine->lastCommandText = movesText(engine->history);
    std::ostringstream info;
    info << "Generated scramble with " << safeLength << " rotations for " << engine->size << "x" << engine->size << "x" << engine->size << ".";
    engine->lastInfo = info.str();
    return 1;
}

RUBIK_API int Rubik_GetHistory(void* handle, RubikMoveDto* buffer, int capacity)
{
    auto* engine = asEngine(handle);
    if (engine == nullptr)
    {
        return 0;
    }

    const int count = static_cast<int>(engine->history.size());
    if (buffer != nullptr && capacity > 0)
    {
        const int limit = std::min(count, capacity);
        for (int i = 0; i < limit; ++i)
        {
            buffer[i] = { engine->history[i].axis, engine->history[i].layer, engine->history[i].turns };
        }
    }
    return count;
}

RUBIK_API int Rubik_SolveByReverseHistory(void* handle, RubikMoveDto* buffer, int capacity)
{
    auto* engine = asEngine(handle);
    if (engine == nullptr)
    {
        return 0;
    }

    if (engine->manualState)
    {
        engine->lastCommandText.clear();
        engine->lastInfo = "Cannot solve manual state yet: no trusted rotation history is attached.";
        return -1;
    }

    const std::vector<Move> solution = reverseHistory(*engine);
    if (buffer != nullptr && capacity > 0)
    {
        const int limit = std::min(static_cast<int>(solution.size()), capacity);
        for (int i = 0; i < limit; ++i)
        {
            buffer[i] = { solution[i].axis, solution[i].layer, solution[i].turns };
        }
    }

    engine->lastCommandText = movesText(solution);
    std::ostringstream info;
    info << "Reverse-history solution contains " << solution.size() << " rotations.";
    engine->lastInfo = info.str();
    return static_cast<int>(solution.size());
}

RUBIK_API int Rubik_ApplyMoves(void* handle, const RubikMoveDto* moves, int count)
{
    auto* engine = asEngine(handle);
    if (engine == nullptr || moves == nullptr || count < 0)
    {
        return 0;
    }

    for (int i = 0; i < count; ++i)
    {
        if (!rotateLayer(*engine, moves[i].axis, moves[i].layer, moves[i].quarterTurns, true))
        {
            return 0;
        }
    }
    return 1;
}

RUBIK_API int Rubik_GetCommandText(void* handle, char* buffer, int capacity)
{
    auto* engine = asEngine(handle);
    return engine == nullptr ? 0 : copyText(engine->lastCommandText, buffer, capacity);
}

RUBIK_API int Rubik_GetLastInfo(void* handle, char* buffer, int capacity)
{
    auto* engine = asEngine(handle);
    return engine == nullptr ? 0 : copyText(engine->lastInfo, buffer, capacity);
}
