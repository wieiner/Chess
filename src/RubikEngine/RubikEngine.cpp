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
constexpr int FaceletSchemaVersion = 1;
constexpr int FaceCount = 6;
constexpr int FirstColorId = 1;
constexpr int LastColorId = 6;
constexpr const char* ColorSchemeJson =
    "{\"schemaVersion\":1,\"U\":{\"id\":1,\"name\":\"white\"},"
    "\"R\":{\"id\":2,\"name\":\"red\"},\"F\":{\"id\":3,\"name\":\"green\"},"
    "\"D\":{\"id\":4,\"name\":\"yellow\"},\"L\":{\"id\":5,\"name\":\"orange\"},"
    "\"B\":{\"id\":6,\"name\":\"blue\"}}";

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
    std::vector<int> facelets;
    bool faceletsSynchronized = true;
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

int faceletCount(int size)
{
    return FaceCount * size * size;
}

int faceletIndex(const Engine& engine, int face, int row, int column)
{
    return face * engine.size * engine.size + row * engine.size + column;
}

bool validFaceletCoordinate(const Engine& engine, int face, int row, int column)
{
    return face >= 0 && face < FaceCount && row >= 0 && row < engine.size &&
        column >= 0 && column < engine.size;
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

bool validateFacelets(Engine& engine, const int* facelets, int count, std::string& error)
{
    const int expected = faceletCount(engine.size);
    if (facelets == nullptr)
    {
        error = "Facelet validation failed: input buffer is null.";
        return false;
    }
    if (count != expected)
    {
        std::ostringstream message;
        message << "Facelet validation failed: expected " << expected << " values, received " << count << ".";
        error = message.str();
        return false;
    }

    std::vector<int> colorCounts(static_cast<size_t>(LastColorId + 1), 0);
    for (int i = 0; i < count; ++i)
    {
        const int color = facelets[i];
        if (color < FirstColorId || color > LastColorId)
        {
            std::ostringstream message;
            message << "Facelet validation failed: unsupported color id " << color << " at index " << i << ".";
            error = message.str();
            return false;
        }
        ++colorCounts[static_cast<size_t>(color)];
    }

    const int expectedPerColor = engine.size * engine.size;
    for (int color = FirstColorId; color <= LastColorId; ++color)
    {
        if (colorCounts[static_cast<size_t>(color)] != expectedPerColor)
        {
            std::ostringstream message;
            message << "Facelet validation failed: color id " << color << " has "
                << colorCounts[static_cast<size_t>(color)] << " values; expected " << expectedPerColor << ".";
            error = message.str();
            return false;
        }
    }

    error.clear();
    return true;
}

bool faceletsSolved(const Engine& engine)
{
    if (!engine.faceletsSynchronized || static_cast<int>(engine.facelets.size()) != faceletCount(engine.size))
    {
        return false;
    }

    const int perFace = engine.size * engine.size;
    for (int face = 0; face < FaceCount; ++face)
    {
        const int solvedColor = face + FirstColorId;
        for (int offset = 0; offset < perFace; ++offset)
        {
            if (engine.facelets[static_cast<size_t>(face * perFace + offset)] != solvedColor)
            {
                return false;
            }
        }
    }
    return true;
}

void reset(Engine& engine)
{
    engine.cells.resize(cellCount(engine.size));
    for (int i = 0; i < static_cast<int>(engine.cells.size()); ++i)
    {
        engine.cells[i] = i;
    }
    engine.facelets.resize(static_cast<size_t>(faceletCount(engine.size)));
    const int perFace = engine.size * engine.size;
    for (int face = 0; face < FaceCount; ++face)
    {
        std::fill_n(
            engine.facelets.begin() + static_cast<size_t>(face * perFace),
            perFace,
            face + FirstColorId);
    }
    engine.faceletsSynchronized = true;
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
    return !engine.faceletsSynchronized || faceletsSolved(engine);
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

struct Sticker
{
    Vec3 position;
    Vec3 normal;
};

Sticker faceletToSticker(const Engine& engine, int face, int row, int column)
{
    const int maximum = engine.size - 1;
    switch (face)
    {
    case 0: // U
        return { { column, maximum, row }, { 0, 1, 0 } };
    case 1: // R
        return { { maximum, maximum - row, maximum - column }, { 1, 0, 0 } };
    case 2: // F
        return { { column, maximum - row, maximum }, { 0, 0, 1 } };
    case 3: // D
        return { { column, 0, maximum - row }, { 0, -1, 0 } };
    case 4: // L
        return { { 0, maximum - row, column }, { -1, 0, 0 } };
    default: // B
        return { { maximum - column, maximum - row, 0 }, { 0, 0, -1 } };
    }
}

Vec3 rotateNormal(int axis, int turns, Vec3 normal)
{
    for (int turn = 0; turn < turns; ++turn)
    {
        switch (axis)
        {
        case 0:
            normal = { -normal.y, normal.x, normal.z };
            break;
        case 1:
            normal = { -normal.z, normal.y, normal.x };
            break;
        default:
            normal = { normal.x, -normal.z, normal.y };
            break;
        }
    }
    return normal;
}

bool stickerToFacelet(const Engine& engine, const Sticker& sticker, int& face, int& row, int& column)
{
    const int maximum = engine.size - 1;
    if (sticker.normal.y == 1)
    {
        face = 0;
        row = sticker.position.z;
        column = sticker.position.x;
    }
    else if (sticker.normal.x == 1)
    {
        face = 1;
        row = maximum - sticker.position.y;
        column = maximum - sticker.position.z;
    }
    else if (sticker.normal.z == 1)
    {
        face = 2;
        row = maximum - sticker.position.y;
        column = sticker.position.x;
    }
    else if (sticker.normal.y == -1)
    {
        face = 3;
        row = maximum - sticker.position.z;
        column = sticker.position.x;
    }
    else if (sticker.normal.x == -1)
    {
        face = 4;
        row = maximum - sticker.position.y;
        column = sticker.position.z;
    }
    else if (sticker.normal.z == -1)
    {
        face = 5;
        row = maximum - sticker.position.y;
        column = maximum - sticker.position.x;
    }
    else
    {
        return false;
    }
    return validFaceletCoordinate(engine, face, row, column);
}

bool buildRotatedFacelets(const Engine& engine, int axis, int layer, int turns, std::vector<int>& result)
{
    if (!engine.faceletsSynchronized)
    {
        result.clear();
        return true;
    }
    if (static_cast<int>(engine.facelets.size()) != faceletCount(engine.size))
    {
        return false;
    }

    result = engine.facelets;
    for (int face = 0; face < FaceCount; ++face)
    {
        for (int row = 0; row < engine.size; ++row)
        {
            for (int column = 0; column < engine.size; ++column)
            {
                Sticker sticker = faceletToSticker(engine, face, row, column);
                const int coordinate = axis == 0
                    ? sticker.position.z
                    : axis == 1 ? sticker.position.y : sticker.position.x;
                if (coordinate != layer)
                {
                    continue;
                }

                sticker.position = rotateLayerSquare(
                    engine,
                    axis,
                    layer,
                    turns,
                    sticker.position.x,
                    sticker.position.y,
                    sticker.position.z);
                sticker.normal = rotateNormal(axis, turns, sticker.normal);

                int toFace = -1;
                int toRow = -1;
                int toColumn = -1;
                if (!stickerToFacelet(engine, sticker, toFace, toRow, toColumn))
                {
                    return false;
                }
                result[static_cast<size_t>(faceletIndex(engine, toFace, toRow, toColumn))] =
                    engine.facelets[static_cast<size_t>(faceletIndex(engine, face, row, column))];
            }
        }
    }
    return true;
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

    std::vector<int> rotatedFacelets;
    if (!buildRotatedFacelets(engine, axis, layer, turns, rotatedFacelets))
    {
        engine.lastInfo = "Rotation rejected: synchronized facelet state is invalid.";
        return false;
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
    if (engine.faceletsSynchronized)
    {
        engine.facelets.swap(rotatedFacelets);
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
    engine->faceletsSynchronized = false;
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
    engine->faceletsSynchronized = false;
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

RUBIK_API int Rubik_GetFaceletSchemaVersion(void* handle)
{
    return asEngine(handle) != nullptr ? FaceletSchemaVersion : 0;
}

RUBIK_API int Rubik_GetFaceletCount(void* handle)
{
    const auto* engine = asEngine(handle);
    return engine != nullptr ? faceletCount(engine->size) : 0;
}

RUBIK_API int Rubik_GetFacelets(void* handle, int* facelets, int capacity)
{
    auto* engine = asEngine(handle);
    if (engine == nullptr)
    {
        return -1;
    }

    const int required = faceletCount(engine->size);
    if (!engine->faceletsSynchronized)
    {
        engine->lastInfo = "Facelet read rejected: legacy cubie state has no synchronized sticker orientation.";
        return -1;
    }
    if (facelets == nullptr || capacity <= 0)
    {
        return required;
    }
    if (capacity < required)
    {
        engine->lastInfo = "Facelet read rejected: output buffer is too small.";
        return -1;
    }

    std::copy(engine->facelets.begin(), engine->facelets.end(), facelets);
    return required;
}

RUBIK_API int Rubik_SetFacelets(void* handle, const int* facelets, int count)
{
    auto* engine = asEngine(handle);
    if (engine == nullptr)
    {
        return 0;
    }

    std::string error;
    if (!validateFacelets(*engine, facelets, count, error))
    {
        engine->lastInfo = error;
        return 0;
    }

    std::vector<int> next(facelets, facelets + count);
    engine->facelets.swap(next);
    engine->faceletsSynchronized = true;
    engine->history.clear();
    engine->manualState = true;
    engine->lastMove = { -1, -1, 0 };
    engine->lastCommandText.clear();
    engine->lastInfo = "Manual facelet state loaded and basic color counts validated.";
    return 1;
}

RUBIK_API int Rubik_GetFacelet(void* handle, int face, int row, int column)
{
    auto* engine = asEngine(handle);
    if (engine == nullptr || !engine->faceletsSynchronized ||
        !validFaceletCoordinate(*engine, face, row, column))
    {
        return -1;
    }
    return engine->facelets[static_cast<size_t>(faceletIndex(*engine, face, row, column))];
}

RUBIK_API int Rubik_SetFacelet(void* handle, int face, int row, int column, int colorId)
{
    auto* engine = asEngine(handle);
    if (engine == nullptr || !engine->faceletsSynchronized ||
        !validFaceletCoordinate(*engine, face, row, column) ||
        colorId < FirstColorId || colorId > LastColorId)
    {
        return 0;
    }

    engine->facelets[static_cast<size_t>(faceletIndex(*engine, face, row, column))] = colorId;
    engine->history.clear();
    engine->manualState = true;
    engine->lastMove = { -1, -1, 0 };
    engine->lastCommandText.clear();
    engine->lastInfo = "Manual facelet edit applied. Validate the full state before solving.";
    return 1;
}

RUBIK_API int Rubik_GetColorScheme(void* handle, char* buffer, int capacity)
{
    return asEngine(handle) != nullptr ? copyText(ColorSchemeJson, buffer, capacity) : 0;
}

RUBIK_API int Rubik_ValidateFacelets(void* handle, const int* facelets, int count)
{
    auto* engine = asEngine(handle);
    if (engine == nullptr)
    {
        return 0;
    }

    std::string error;
    const bool valid = validateFacelets(*engine, facelets, count, error);
    engine->lastInfo = valid
        ? "Facelet validation passed: size, color ids, and color counts are valid."
        : error;
    return valid ? 1 : 0;
}
