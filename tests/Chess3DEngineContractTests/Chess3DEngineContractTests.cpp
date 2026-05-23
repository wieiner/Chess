#include "../../src/Chess3DEngine/Chess3DEngine.h"
#include "../TestSupport/TestSupport.h"

#include <algorithm>
#include <array>
#include <cctype>
#include <cstdlib>
#include <fstream>
#include <iterator>
#include <map>
#include <set>
#include <string>
#include <vector>

namespace
{
constexpr int Pawn = 1;
constexpr int Knight = 2;
constexpr int Bishop = 3;
constexpr int Rook = 4;
constexpr int Queen = 5;
constexpr int King = 6;
constexpr int MoveCapture = 1;
constexpr int MovePromotion = 8;
constexpr int FusionNone = 0;
constexpr int FusionSingle = 1;
constexpr int FusionFriendlyPair = 2;
constexpr int FusionFriendlyStack = 3;
constexpr int FusionRoyalPair = 4;
constexpr int FusionContested = 5;
constexpr int FusionFlagContested = 1;
constexpr int FusionFlagRoyalPair = 2;
constexpr int FusionFlagAnchoredFusion = 4;
constexpr int FusionFlagImplosionSeed = 8;
constexpr int KnockbackNone = 0;
constexpr int KnockbackHome = 1;
constexpr int KnockbackReserve = 2;
constexpr int KnockbackClassicRemoved = 3;
constexpr int LayerTurnSuccess = 1;
constexpr int LayerTurnDisabled = 2;
constexpr int LayerTurnInvalidAxis = 3;
constexpr int LayerTurnInvalidLayer = 4;
constexpr int LayerTurnInvalidQuarterTurns = 5;
constexpr int ActionMove = 1;
constexpr int ActionLayerTurn = 2;
constexpr int ActionReserveRestore = 3;
constexpr int ActionProjectionCompositeMove = 5;
constexpr int CaptureDestinationRemoved = 1;
constexpr int CaptureDestinationHome = 2;
constexpr int CaptureDestinationReserve = 3;
constexpr int CaptureDestinationCoreCoOccupancy = 4;
constexpr int ActionFlagWasCapture = 1;
constexpr int ActionFlagWasKnockback = 2;
constexpr int ActionFlagEnteredCore = 4;
constexpr int ActionFlagLeftCore = 8;
constexpr int ActionFlagWasLayerTurn = 16;
constexpr int ActionFlagWasReserveRestore = 32;
constexpr int ActionFlagWasProjection = 512;

std::string ReadTextFile(const std::string& path)
{
    std::ifstream file(path, std::ios::binary);
    if (!file)
    {
        return {};
    }
    return {std::istreambuf_iterator<char>(file), std::istreambuf_iterator<char>()};
}

int IndexOf(int x, int y, int z)
{
    return z * 64 + y * 8 + x;
}

int PieceCode(int side, int type)
{
    return side * 10 + type;
}

int PieceType(int piece)
{
    return piece == 0 ? 0 : piece % 10;
}

bool InBounds(const Chess3DMoveDto& move)
{
    return move.fromX >= 0 && move.fromX < 8 &&
        move.fromY >= 0 && move.fromY < 8 &&
        move.fromZ >= 0 && move.fromZ < 8 &&
        move.toX >= 0 && move.toX < 8 &&
        move.toY >= 0 && move.toY < 8 &&
        move.toZ >= 0 && move.toZ < 8;
}

bool HasMove(const std::vector<Chess3DMoveDto>& moves, int toX, int toY, int toZ)
{
    return std::any_of(moves.begin(), moves.end(), [&](const Chess3DMoveDto& move)
    {
        return move.toX == toX && move.toY == toY && move.toZ == toZ;
    });
}

bool HasCapture(const std::vector<Chess3DMoveDto>& moves, int toX, int toY, int toZ)
{
    return std::any_of(moves.begin(), moves.end(), [&](const Chess3DMoveDto& move)
    {
        return move.toX == toX && move.toY == toY && move.toZ == toZ && (move.flags & MoveCapture) != 0;
    });
}

std::vector<Chess3DMoveDto> PieceMoves(void* game, int x, int y, int z)
{
    std::vector<Chess3DMoveDto> moves(512);
    const int count = Chess3D_GetPieceMoves(game, x, y, z, moves.data(), static_cast<int>(moves.size()));
    if (count <= 0)
    {
        return {};
    }
    moves.resize(static_cast<std::size_t>(std::min<int>(count, static_cast<int>(moves.size()))));
    return moves;
}

void PutSinglePiece(void* game, int type, int x = 3, int y = 3, int z = 3)
{
    Chess3D_Clear(game);
    Chess3D_SetPiece(game, x, y, z, 1, type);
}

bool AllMovesInBounds(const std::vector<Chess3DMoveDto>& moves)
{
    return std::all_of(moves.begin(), moves.end(), InBounds);
}

bool ValidateSingleSideRulesJson(const std::string& json)
{
    return json.find("\"rulesetId\"") != std::string::npos &&
        json.find("single-side-3d-chess-8x8x8-v0.1") != std::string::npos &&
        json.find("\"width\": 8") != std::string::npos &&
        json.find("\"height\": 8") != std::string::npos &&
        json.find("\"depth\": 8") != std::string::npos &&
        json.find("\"setup\"") != std::string::npos;
}

class JsonParser
{
public:
    explicit JsonParser(const std::string& text) : text_(text)
    {
    }

    bool Parse()
    {
        SkipWhitespace();
        if (!ParseValue())
        {
            return false;
        }
        SkipWhitespace();
        return pos_ == text_.size();
    }

private:
    void SkipWhitespace()
    {
        while (pos_ < text_.size() && std::isspace(static_cast<unsigned char>(text_[pos_])))
        {
            ++pos_;
        }
    }

    bool ParseValue()
    {
        SkipWhitespace();
        if (pos_ >= text_.size())
        {
            return false;
        }
        switch (text_[pos_])
        {
        case '{': return ParseObject();
        case '[': return ParseArray();
        case '"': return ParseString();
        case 't': return Match("true");
        case 'f': return Match("false");
        case 'n': return Match("null");
        default: return ParseNumber();
        }
    }

    bool ParseObject()
    {
        if (text_[pos_++] != '{')
        {
            return false;
        }
        SkipWhitespace();
        if (pos_ < text_.size() && text_[pos_] == '}')
        {
            ++pos_;
            return true;
        }
        while (true)
        {
            SkipWhitespace();
            if (pos_ >= text_.size() || text_[pos_] != '"' || !ParseString())
            {
                return false;
            }
            SkipWhitespace();
            if (pos_ >= text_.size() || text_[pos_++] != ':')
            {
                return false;
            }
            if (!ParseValue())
            {
                return false;
            }
            SkipWhitespace();
            if (pos_ >= text_.size())
            {
                return false;
            }
            if (text_[pos_] == '}')
            {
                ++pos_;
                return true;
            }
            if (text_[pos_++] != ',')
            {
                return false;
            }
        }
    }

    bool ParseArray()
    {
        if (text_[pos_++] != '[')
        {
            return false;
        }
        SkipWhitespace();
        if (pos_ < text_.size() && text_[pos_] == ']')
        {
            ++pos_;
            return true;
        }
        while (true)
        {
            if (!ParseValue())
            {
                return false;
            }
            SkipWhitespace();
            if (pos_ >= text_.size())
            {
                return false;
            }
            if (text_[pos_] == ']')
            {
                ++pos_;
                return true;
            }
            if (text_[pos_++] != ',')
            {
                return false;
            }
        }
    }

    bool ParseString()
    {
        if (text_[pos_++] != '"')
        {
            return false;
        }
        while (pos_ < text_.size())
        {
            const char ch = text_[pos_++];
            if (ch == '"')
            {
                return true;
            }
            if (ch == '\\')
            {
                if (pos_ >= text_.size())
                {
                    return false;
                }
                const char escaped = text_[pos_++];
                if (escaped == 'u')
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        if (pos_ >= text_.size() || !std::isxdigit(static_cast<unsigned char>(text_[pos_++])))
                        {
                            return false;
                        }
                    }
                }
            }
            else if (static_cast<unsigned char>(ch) < 0x20)
            {
                return false;
            }
        }
        return false;
    }

    bool ParseNumber()
    {
        const std::size_t start = pos_;
        if (pos_ < text_.size() && text_[pos_] == '-')
        {
            ++pos_;
        }
        if (pos_ >= text_.size() || !std::isdigit(static_cast<unsigned char>(text_[pos_])))
        {
            return false;
        }
        if (text_[pos_] == '0')
        {
            ++pos_;
        }
        else
        {
            while (pos_ < text_.size() && std::isdigit(static_cast<unsigned char>(text_[pos_])))
            {
                ++pos_;
            }
        }
        if (pos_ < text_.size() && text_[pos_] == '.')
        {
            ++pos_;
            if (pos_ >= text_.size() || !std::isdigit(static_cast<unsigned char>(text_[pos_])))
            {
                return false;
            }
            while (pos_ < text_.size() && std::isdigit(static_cast<unsigned char>(text_[pos_])))
            {
                ++pos_;
            }
        }
        if (pos_ < text_.size() && (text_[pos_] == 'e' || text_[pos_] == 'E'))
        {
            ++pos_;
            if (pos_ < text_.size() && (text_[pos_] == '+' || text_[pos_] == '-'))
            {
                ++pos_;
            }
            if (pos_ >= text_.size() || !std::isdigit(static_cast<unsigned char>(text_[pos_])))
            {
                return false;
            }
            while (pos_ < text_.size() && std::isdigit(static_cast<unsigned char>(text_[pos_])))
            {
                ++pos_;
            }
        }
        return pos_ > start;
    }

    bool Match(const char* literal)
    {
        const std::string value(literal);
        if (text_.compare(pos_, value.size(), value) != 0)
        {
            return false;
        }
        pos_ += value.size();
        return true;
    }

    const std::string& text_;
    std::size_t pos_ = 0;
};

std::size_t FindKeyColon(const std::string& json, const std::string& key)
{
    const auto keyPos = json.find("\"" + key + "\"");
    if (keyPos == std::string::npos)
    {
        return std::string::npos;
    }
    return json.find(':', keyPos);
}

std::string ExtractObject(const std::string& json, const std::string& key)
{
    const auto colon = FindKeyColon(json, key);
    if (colon == std::string::npos)
    {
        return {};
    }
    auto pos = json.find('{', colon + 1);
    if (pos == std::string::npos)
    {
        return {};
    }
    const std::size_t start = pos;
    int depth = 0;
    bool inString = false;
    bool escaped = false;
    for (; pos < json.size(); ++pos)
    {
        const char ch = json[pos];
        if (inString)
        {
            if (escaped)
            {
                escaped = false;
            }
            else if (ch == '\\')
            {
                escaped = true;
            }
            else if (ch == '"')
            {
                inString = false;
            }
            continue;
        }
        if (ch == '"')
        {
            inString = true;
        }
        else if (ch == '{')
        {
            ++depth;
        }
        else if (ch == '}')
        {
            --depth;
            if (depth == 0)
            {
                return json.substr(start, pos - start + 1);
            }
        }
    }
    return {};
}

std::string ExtractStringValue(const std::string& json, const std::string& key)
{
    const auto colon = FindKeyColon(json, key);
    if (colon == std::string::npos)
    {
        return {};
    }
    auto first = json.find('"', colon + 1);
    if (first == std::string::npos)
    {
        return {};
    }
    auto second = first + 1;
    bool escaped = false;
    for (; second < json.size(); ++second)
    {
        const char ch = json[second];
        if (escaped)
        {
            escaped = false;
            continue;
        }
        if (ch == '\\')
        {
            escaped = true;
            continue;
        }
        if (ch == '"')
        {
            return json.substr(first + 1, second - first - 1);
        }
    }
    return {};
}

bool ExtractIntValue(const std::string& json, const std::string& key, int& value)
{
    const auto colon = FindKeyColon(json, key);
    if (colon == std::string::npos)
    {
        return false;
    }
    auto pos = colon + 1;
    while (pos < json.size() && std::isspace(static_cast<unsigned char>(json[pos])))
    {
        ++pos;
    }
    int sign = 1;
    if (pos < json.size() && json[pos] == '-')
    {
        sign = -1;
        ++pos;
    }
    if (pos >= json.size() || !std::isdigit(static_cast<unsigned char>(json[pos])))
    {
        return false;
    }
    int parsed = 0;
    while (pos < json.size() && std::isdigit(static_cast<unsigned char>(json[pos])))
    {
        parsed = parsed * 10 + (json[pos] - '0');
        ++pos;
    }
    value = parsed * sign;
    return true;
}

bool ExtractBoolValue(const std::string& json, const std::string& key, bool& value)
{
    const auto colon = FindKeyColon(json, key);
    if (colon == std::string::npos)
    {
        return false;
    }
    auto pos = colon + 1;
    while (pos < json.size() && std::isspace(static_cast<unsigned char>(json[pos])))
    {
        ++pos;
    }
    if (json.compare(pos, 4, "true") == 0)
    {
        value = true;
        return true;
    }
    if (json.compare(pos, 5, "false") == 0)
    {
        value = false;
        return true;
    }
    return false;
}

bool ValidateBoardProfile(const std::string& json)
{
    const std::string board = ExtractObject(json, "boardProfile");
    int width = 0;
    int height = 0;
    int depth = 0;
    return ExtractIntValue(board, "width", width) &&
        ExtractIntValue(board, "height", height) &&
        ExtractIntValue(board, "depth", depth) &&
        width == 8 && height == 8 && depth == 8;
}

bool IsAllowed(const std::string& value, const std::set<std::string>& allowed)
{
    return allowed.find(value) != allowed.end();
}

bool ValidateCommonRuleProfile(const std::string& json)
{
    const std::string rulesetId = ExtractStringValue(json, "rulesetId");
    const std::string goal = ExtractStringValue(ExtractObject(json, "goalProfile"), "type");
    const std::string capture = ExtractStringValue(ExtractObject(json, "captureProfile"), "type");
    const std::string occupancy = ExtractStringValue(ExtractObject(json, "occupancyProfile"), "type");
    const std::string fusion = ExtractStringValue(ExtractObject(json, "fusionProfile"), "type");
    const std::string layerTurn = ExtractStringValue(ExtractObject(json, "layerTurnProfile"), "type");
    const std::string projection = ExtractStringValue(ExtractObject(json, "projectionProfile"), "type");
    return JsonParser(json).Parse() &&
        !rulesetId.empty() &&
        ValidateBoardProfile(json) &&
        IsAllowed(goal, { "classicCheckmate", "centerAssembly", "hybrid", "sandbox", "centerAssemblyTraining" }) &&
        IsAllowed(capture, { "classicCapture", "knockbackCapture" }) &&
        IsAllowed(occupancy, { "exclusive", "coreStack", "quantumCore" }) &&
        IsAllowed(fusion, { "none", "anchorOnly", "pairFusion", "stackFusion", "colorPermutation", "volumeSurface216" }) &&
        IsAllowed(layerTurn, { "disabled", "ritualTurn", "globalEvent", "sandbox" }) &&
        IsAllowed(projection, { "none", "hodgeTriuneProjection" });
}

bool ValidateCoreCube(const std::string& json, int expectedMin, int expectedMax)
{
    const std::string core = ExtractObject(ExtractObject(json, "coreProfile"), "coreCube");
    int xMin = -1;
    int xMax = -1;
    int yMin = -1;
    int yMax = -1;
    int zMin = -1;
    int zMax = -1;
    return ExtractIntValue(core, "xMin", xMin) &&
        ExtractIntValue(core, "xMax", xMax) &&
        ExtractIntValue(core, "yMin", yMin) &&
        ExtractIntValue(core, "yMax", yMax) &&
        ExtractIntValue(core, "zMin", zMin) &&
        ExtractIntValue(core, "zMax", zMax) &&
        xMin == expectedMin && xMax == expectedMax &&
        yMin == expectedMin && yMax == expectedMax &&
        zMin == expectedMin && zMax == expectedMax &&
        xMin >= 0 && xMax < 8 && yMin >= 0 && yMax < 8 && zMin >= 0 && zMax < 8;
}

std::string ReadAbiString(void* game, int (*reader)(void*, char*, int))
{
    char buffer[512] = {};
    const int needed = reader(game, buffer, static_cast<int>(sizeof(buffer)));
    if (needed <= 0)
    {
        return {};
    }
    return std::string(buffer);
}

std::string ReadActionNotation(void* game, int actionIndex)
{
    char buffer[512] = {};
    const int needed = Chess3D_GetActionNotation(game, actionIndex, buffer, static_cast<int>(sizeof(buffer)));
    if (needed <= 0)
    {
        return {};
    }
    return std::string(buffer);
}

std::string ReadFusionKindName(int fusionKind)
{
    char buffer[128] = {};
    const int needed = Chess3D_GetFusionKindName(fusionKind, buffer, static_cast<int>(sizeof(buffer)));
    if (needed <= 0)
    {
        return {};
    }
    return std::string(buffer);
}

struct TargetCell
{
    int x;
    int y;
    int z;
    int type;
};

std::string ReadLayerTurnResultName(int resultCode)
{
    char buffer[128] = {};
    const int needed = Chess3D_GetLayerTurnResultName(resultCode, buffer, static_cast<int>(sizeof(buffer)));
    if (needed <= 0)
    {
        return {};
    }
    return std::string(buffer);
}

std::vector<int> BoardSnapshot(void* game)
{
    std::vector<int> board(512);
    Chess3D_GetBoard(game, board.data());
    return board;
}

struct CoreStackEntrySnapshot
{
    int side = 0;
    int type = 0;
    int piece = 0;
    int flags = 0;
};

std::vector<CoreStackEntrySnapshot> StackSnapshot(void* game, int x, int y, int z)
{
    const int count = Chess3D_GetCoreStackCount(game, x, y, z);
    std::vector<CoreStackEntrySnapshot> entries;
    for (int i = 0; i < count; ++i)
    {
        CoreStackEntrySnapshot entry{};
        if (Chess3D_GetCoreStackEntry(game, x, y, z, i, &entry.side, &entry.type, &entry.piece, &entry.flags) == 1)
        {
            entries.push_back(entry);
        }
    }
    return entries;
}

bool SameStack(const std::vector<CoreStackEntrySnapshot>& left, const std::vector<CoreStackEntrySnapshot>& right)
{
    if (left.size() != right.size())
    {
        return false;
    }
    for (std::size_t i = 0; i < left.size(); ++i)
    {
        if (left[i].side != right[i].side || left[i].type != right[i].type ||
            left[i].piece != right[i].piece || left[i].flags != right[i].flags)
        {
            return false;
        }
    }
    return true;
}

TargetCell RotateCell(int axis, int layer, int quarterTurns, int x, int y, int z, int type = 0)
{
    int turns = quarterTurns % 4;
    if (turns < 0)
    {
        turns += 4;
    }
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
        const int nextU = 7 - v;
        const int nextV = u;
        u = nextU;
        v = nextV;
    }
    switch (axis)
    {
    case 0:
        return { u, v, layer, type };
    case 1:
        return { u, layer, v, type };
    default:
        return { layer, u, v, type };
    }
}

int TargetTypeForLocal(int localU, int localV)
{
    constexpr int pattern[4][4] = {
        { Rook, Pawn, Pawn, Knight },
        { Pawn, Bishop, Bishop, Pawn },
        { Pawn, Queen, King, Pawn },
        { Knight, Pawn, Pawn, Rook }
    };
    return pattern[localV][localU];
}

std::vector<TargetCell> TargetCellsForSide(int side)
{
    std::vector<TargetCell> cells;
    for (int localV = 0; localV < 4; ++localV)
    {
        for (int localU = 0; localU < 4; ++localU)
        {
            const int a = 2 + localU;
            const int b = 2 + localV;
            const int type = TargetTypeForLocal(localU, localV);
            switch (side)
            {
            case 1: cells.push_back({ a, b, 2, type }); break;
            case 2: cells.push_back({ a, b, 5, type }); break;
            case 3: cells.push_back({ a, 2, b, type }); break;
            case 4: cells.push_back({ a, 5, b, type }); break;
            case 5: cells.push_back({ 2, a, b, type }); break;
            case 6: cells.push_back({ 5, a, b, type }); break;
            default: break;
            }
        }
    }
    return cells;
}

std::vector<TargetCell> HomeCellsForSide(int side)
{
    std::vector<TargetCell> cells;
    for (int localV = 0; localV < 4; ++localV)
    {
        for (int localU = 0; localU < 4; ++localU)
        {
            const int u = 2 + localU;
            const int v = 2 + localV;
            const int type = TargetTypeForLocal(localU, localV);
            switch (side)
            {
            case 1: cells.push_back({ u, v, 0, type }); break;
            case 2: cells.push_back({ 7 - u, 7 - v, 7, type }); break;
            case 3: cells.push_back({ u, 0, v, type }); break;
            case 4: cells.push_back({ 7 - u, 7, 7 - v, type }); break;
            case 5: cells.push_back({ 0, u, v, type }); break;
            case 6: cells.push_back({ 7, 7 - u, 7 - v, type }); break;
            default: break;
            }
        }
    }
    return cells;
}
}

int main()
{
    ContractTestRunner test;
    void* game = Chess3D_Create();
    test.Check(game != nullptr, "Chess3D_Create returns a handle");
    if (game == nullptr)
    {
        return test.Finish("Chess3DEngineContractTests");
    }

    Chess3D_Reset(game);
    Chess3DRulesInfoDto rules{};
    test.Check(Chess3D_GetRulesInfo(game, &rules) == 1, "Chess3D_GetRulesInfo succeeds");
    test.Check(rules.width == 8 && rules.height == 8 && rules.depth == 8, "Rules report an 8x8x8 board");
    test.Check(rules.activeSideCount >= 1 && rules.activeSideCount <= 6, "Active side count is within 1..6");

    Chess3DStateDto state{};
    test.Check(Chess3D_GetState(game, &state) == 1, "Chess3D_GetState succeeds");
    test.Check(state.width == 8 && state.height == 8 && state.depth == 8, "State reports an 8x8x8 board");
    test.Check(state.activeSideCount >= 1 && state.activeSideCount <= 6, "State active side count is within 1..6");

    std::vector<int> board(512);
    test.Check(Chess3D_GetBoard(game, board.data()) == 1, "Chess3D_GetBoard succeeds for 512 cells");

    Chess3DMoveDto moves[1024]{};
    const int moveCount = Chess3D_GetLegalMoves(game, moves, 1024);
    test.Check(moveCount >= 0, "Chess3D_GetLegalMoves does not fail");
    if (moveCount > 0)
    {
        Chess3DMoveDto played{};
        const Chess3DMoveDto& first = moves[0];
        test.Check(Chess3D_TryMakeMove(game, first.fromX, first.fromY, first.fromZ, first.toX, first.toY, first.toZ, first.promotionType, &played) == 1,
            "Chess3D_TryMakeMove accepts the first generated legal move");
    }
    else
    {
        Chess3DMoveDto played{};
        test.Check(Chess3D_TryMakeMove(game, 0, 0, 0, 7, 7, 7, 0, &played) == 0,
            "Chess3D_TryMakeMove cleanly rejects a non-generated move");
    }

    Chess3D_Clear(game);
    test.Check(Chess3D_SetPiece(game, 0, 0, 0, 1, Rook) == 1, "Chess3D_SetPiece accepts a valid coordinate");
    test.Check(Chess3D_GetPiece(game, 0, 0, 0) == PieceCode(1, Rook), "Chess3D_GetPiece returns the written piece");

    std::vector<int> beforeRotate(512);
    std::vector<int> afterRotate(512);
    test.Check(Chess3D_GetBoard(game, beforeRotate.data()) == 1, "Board read before RotateLayer succeeds");
    test.Check(Chess3D_RotateLayer(game, 0, 0, 1) == 1, "Chess3D_RotateLayer accepts a valid Z layer turn");
    test.Check(Chess3D_GetBoard(game, afterRotate.data()) == 1, "Board read after RotateLayer succeeds");
    test.Check(beforeRotate != afterRotate, "RotateLayer changes the board state");

    char positionText[512]{};
    test.Check(Chess3D_GetPositionText(game, positionText, static_cast<int>(sizeof(positionText))) != 0, "Chess3D_GetPositionText succeeds");
    test.Check(std::string(positionText).size() > 0, "Chess3D_GetPositionText returns non-empty text");

    const std::string draftRulesJson = ReadTextFile("src\\ChessApp\\Assets\\Rules3D\\cube8x8x8_draft.json");
    if (!draftRulesJson.empty())
    {
        test.Check(Chess3D_LoadRulesJson(game, draftRulesJson.c_str()) == 1, "Chess3D_LoadRulesJson accepts cube8x8x8_draft.json");
        Chess3DRulesInfoDto loadedRules{};
        test.Check(Chess3D_GetRulesInfo(game, &loadedRules) == 1 &&
            loadedRules.width == 8 && loadedRules.height == 8 && loadedRules.depth == 8,
            "Loaded draft rules still report 8x8x8");
    }
    else
    {
        std::cout << "SKIP cube8x8x8_draft.json not found in source assets\n";
    }

    const std::string singleSideJson = ReadTextFile("src\\ChessApp\\Assets\\Rules3D\\single_side_3d_chess_8x8x8_v0_1.json");
    test.Check(!singleSideJson.empty(), "single-side 3D rules JSON exists");
    test.Check(ValidateSingleSideRulesJson(singleSideJson), "single-side rules JSON validates expected metadata");
    test.Check(Chess3D_LoadRulesJson(game, singleSideJson.c_str()) == 1, "Chess3D_LoadRulesJson accepts single-side rules JSON");
    test.Check(Chess3D_LoadRulesJson(game, "not json") == 0, "Chess3D_LoadRulesJson rejects non-JSON text without crashing");
    test.Check(Chess3D_LoadRulesJson(game, singleSideJson.c_str()) == 1, "single-side rules reloads after invalid JSON smoke");

    char rulesJsonBuffer[4096]{};
    test.Check(Chess3D_GetRulesJson(game, rulesJsonBuffer, static_cast<int>(sizeof(rulesJsonBuffer))) > 0, "Chess3D_GetRulesJson returns rules text");
    test.Check(std::string(rulesJsonBuffer).find("single-side-3d-chess-8x8x8-v0.1") != std::string::npos,
        "Rules metadata exposes single-side ruleset id");

    Chess3DRulesInfoDto singleRules{};
    test.Check(Chess3D_GetRulesInfo(game, &singleRules) == 1, "single-side rules info is readable");
    test.Check(singleRules.width == 8 && singleRules.height == 8 && singleRules.depth == 8, "single-side board size is 8x8x8");
    test.Check(singleRules.activeSideCount == 1, "single-side rules use one active side");

    Chess3DStateDto singleState{};
    test.Check(Chess3D_GetState(game, &singleState) == 1, "single-side state is readable");
    test.Check(singleState.pieceCount == 16, "single-side setup has 16 pieces");

    std::vector<int> singleBoard(512);
    test.Check(Chess3D_GetBoard(game, singleBoard.data()) == 1, "single-side board read succeeds");
    std::map<int, int> counts;
    int centralBlockPieces = 0;
    int pawnRingPieces = 0;
    std::array<int, 4> cornerTypes = {
        PieceType(singleBoard[IndexOf(2, 2, 0)]),
        PieceType(singleBoard[IndexOf(5, 2, 0)]),
        PieceType(singleBoard[IndexOf(2, 5, 0)]),
        PieceType(singleBoard[IndexOf(5, 5, 0)])
    };
    std::array<int, 4> centerTypes = {
        PieceType(singleBoard[IndexOf(3, 3, 0)]),
        PieceType(singleBoard[IndexOf(4, 3, 0)]),
        PieceType(singleBoard[IndexOf(3, 4, 0)]),
        PieceType(singleBoard[IndexOf(4, 4, 0)])
    };

    for (int z = 0; z < 8; ++z)
    {
        for (int y = 0; y < 8; ++y)
        {
            for (int x = 0; x < 8; ++x)
            {
                const int piece = singleBoard[IndexOf(x, y, z)];
                if (piece == 0)
                {
                    continue;
                }
                ++counts[PieceType(piece)];
                if (x >= 2 && x <= 5 && y >= 2 && y <= 5 && z == 0)
                {
                    ++centralBlockPieces;
                }
                const bool inRing = x >= 2 && x <= 5 && y >= 2 && y <= 5 &&
                    (x == 2 || x == 5 || y == 2 || y == 5) &&
                    !(x == 2 && y == 2) && !(x == 5 && y == 2) &&
                    !(x == 2 && y == 5) && !(x == 5 && y == 5);
                if (z == 0 && inRing && PieceType(piece) == Pawn)
                {
                    ++pawnRingPieces;
                }
            }
        }
    }
    test.Check(centralBlockPieces == 16, "all single-side setup pieces are in x=2..5, y=2..5, z=0");
    test.Check(counts[Pawn] == 8, "single-side setup has 8 pawns");
    test.Check(counts[Rook] == 2, "single-side setup has 2 rooks");
    test.Check(counts[Knight] == 2, "single-side setup has 2 knights");
    test.Check(counts[Bishop] == 2, "single-side setup has 2 bishops/officers");
    test.Check(counts[Queen] == 1, "single-side setup has 1 queen");
    test.Check(counts[King] == 1, "single-side setup has 1 king");
    test.Check(pawnRingPieces == 8, "single-side pawns occupy non-corner cells of the 4x4 ring");
    test.Check(std::count(cornerTypes.begin(), cornerTypes.end(), Rook) == 2 &&
        std::count(cornerTypes.begin(), cornerTypes.end(), Knight) == 2,
        "single-side corner cells contain 2 rooks and 2 knights");
    test.Check(std::count(centerTypes.begin(), centerTypes.end(), Bishop) == 2 &&
        std::count(centerTypes.begin(), centerTypes.end(), Queen) == 1 &&
        std::count(centerTypes.begin(), centerTypes.end(), King) == 1,
        "single-side center 2x2 contains 2 bishops, queen, and king");

    PutSinglePiece(game, Rook);
    auto rookMoves = PieceMoves(game, 3, 3, 3);
    test.Check(!rookMoves.empty() && AllMovesInBounds(rookMoves), "rook moves are generated in bounds");
    test.Check(HasMove(rookMoves, 3, 3, 7) && HasMove(rookMoves, 7, 3, 3) && HasMove(rookMoves, 3, 7, 3),
        "rook has axis moves");

    PutSinglePiece(game, Bishop);
    auto bishopMoves = PieceMoves(game, 3, 3, 3);
    test.Check(!bishopMoves.empty() && AllMovesInBounds(bishopMoves), "bishop moves are generated in bounds");
    test.Check(HasMove(bishopMoves, 4, 4, 3) && HasMove(bishopMoves, 4, 3, 4) && HasMove(bishopMoves, 4, 4, 4),
        "bishop has 2D and 3D diagonal moves");

    PutSinglePiece(game, Queen);
    auto queenMoves = PieceMoves(game, 3, 3, 3);
    test.Check(!queenMoves.empty() && AllMovesInBounds(queenMoves), "queen moves are generated in bounds");
    test.Check(HasMove(queenMoves, 3, 3, 7) && HasMove(queenMoves, 4, 4, 4),
        "queen has rook-style and bishop-style moves");

    PutSinglePiece(game, King);
    auto kingMoves = PieceMoves(game, 3, 3, 3);
    const bool kingOneStepOnly = std::all_of(kingMoves.begin(), kingMoves.end(), [](const Chess3DMoveDto& move)
    {
        return std::max({std::abs(move.toX - move.fromX), std::abs(move.toY - move.fromY), std::abs(move.toZ - move.fromZ)}) == 1;
    });
    test.Check(kingMoves.size() == 26 && AllMovesInBounds(kingMoves) && kingOneStepOnly, "king has only 26 one-step neighbor moves from center");

    PutSinglePiece(game, Knight);
    auto knightMoves = PieceMoves(game, 3, 3, 3);
    test.Check(!knightMoves.empty() && AllMovesInBounds(knightMoves), "knight moves are generated in bounds");
    test.Check(HasMove(knightMoves, 5, 4, 3) && HasMove(knightMoves, 5, 3, 4) && HasMove(knightMoves, 3, 5, 4),
        "knight has 3D L moves");

    PutSinglePiece(game, Pawn);
    Chess3D_SetPiece(game, 4, 4, 4, 2, Pawn);
    auto pawnMoves = PieceMoves(game, 3, 3, 3);
    test.Check(HasMove(pawnMoves, 3, 3, 4), "pawn has forward move");
    test.Check(HasCapture(pawnMoves, 4, 4, 4), "pawn has forward-layer capture vectors");

    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 3, 3, 0, 1, Pawn);
    auto pawnStartMoves = PieceMoves(game, 3, 3, 0);
    test.Check(HasMove(pawnStartMoves, 3, 3, 1) && HasMove(pawnStartMoves, 3, 3, 2), "pawn has initial double move from z=0");

    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 3, 3, 3, 1, Rook);
    Chess3D_SetPiece(game, 3, 3, 5, 1, Pawn);
    auto blockedRookMoves = PieceMoves(game, 3, 3, 3);
    test.Check(!HasMove(blockedRookMoves, 3, 3, 5) && !HasMove(blockedRookMoves, 3, 3, 6),
        "own piece blocks rook and cannot be captured or jumped");

    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 3, 3, 3, 1, Queen);
    Chess3D_SetPiece(game, 5, 5, 5, 1, Pawn);
    auto blockedQueenMoves = PieceMoves(game, 3, 3, 3);
    test.Check(!HasMove(blockedQueenMoves, 5, 5, 5) && !HasMove(blockedQueenMoves, 6, 6, 6),
        "own piece blocks queen diagonal line");

    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 3, 3, 3, 1, Bishop);
    Chess3D_SetPiece(game, 5, 5, 3, 2, Pawn);
    auto captureBishopMoves = PieceMoves(game, 3, 3, 3);
    test.Check(HasCapture(captureBishopMoves, 5, 5, 3) && !HasMove(captureBishopMoves, 6, 6, 3),
        "bishop can capture enemy but cannot jump beyond blocker");

    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 3, 3, 3, 1, Knight);
    Chess3D_SetPiece(game, 4, 3, 3, 1, Pawn);
    auto jumpingKnightMoves = PieceMoves(game, 3, 3, 3);
    test.Check(HasMove(jumpingKnightMoves, 5, 4, 3), "knight can jump over blockers");

    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 3, 3, 3, 1, Pawn);
    Chess3D_SetPiece(game, 3, 3, 4, 2, Pawn);
    auto blockedPawnMoves = PieceMoves(game, 3, 3, 3);
    test.Check(!HasCapture(blockedPawnMoves, 3, 3, 4) && !HasMove(blockedPawnMoves, 3, 3, 4), "pawn cannot capture straight forward");

    Chess3DMoveDto played{};
    test.Check(Chess3D_TryMakeMove(game, 3, 3, 3, 3, 3, 8, 0, &played) == 0, "out-of-bounds move is rejected");
    test.Check(Chess3D_TryMakeMove(game, 0, 0, 0, 0, 0, 1, 0, &played) == 0, "move from empty cell is rejected");

    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 3, 3, 3, 1, Rook);
    Chess3D_SetPiece(game, 3, 3, 5, 1, Pawn);
    test.Check(Chess3D_TryMakeMove(game, 3, 3, 3, 3, 3, 5, 0, &played) == 0, "move onto own piece is rejected");
    test.Check(Chess3D_TryMakeMove(game, 3, 3, 3, 4, 4, 5, 0, &played) == 0, "illegal vector is rejected");

    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 3, 3, 6, 1, Pawn);
    test.Check(Chess3D_TryMakeMove(game, 3, 3, 6, 3, 3, 7, 0, &played) == 1, "pawn promotion move is accepted");
    test.Check((played.flags & MovePromotion) != 0 && Chess3D_GetPiece(game, 3, 3, 7) == PieceCode(1, Queen),
        "pawn reaching z=7 promotes to queen by default");

    const std::string profileSchema = ReadTextFile("assets\\rules\\profiles\\chess3d_rule_profile.schema.json");
    test.Check(!profileSchema.empty(), "Chess3D rule profile schema exists");
    test.Check(JsonParser(profileSchema).Parse(), "Chess3D rule profile schema parses as JSON");

    const std::string classicProfile = ReadTextFile("assets\\rules\\profiles\\classic_six_side_3d_v0_1.json");
    const std::string singleProfile = ReadTextFile("assets\\rules\\profiles\\single_side_3d_v0_1.json");
    const std::string asgardProfile = ReadTextFile("assets\\rules\\profiles\\asgard_convergence_3d_v0_1.json");
    const std::string rubikProfile = ReadTextFile("assets\\rules\\profiles\\rubik_convergence_3d_v0_1.json");
    const std::string hodgeProfile = ReadTextFile("assets\\rules\\profiles\\hodge_projection_duel_3d_v0_1.json");
    const std::string classicScenario = ReadTextFile("assets\\rules\\scenarios\\chess3d\\classic_six_side_smoke_v0_1.json");
    const std::string asgardScenario = ReadTextFile("assets\\rules\\scenarios\\chess3d\\asgard_core_fusion_smoke_v0_1.json");
    const std::string rubikScenario = ReadTextFile("assets\\rules\\scenarios\\chess3d\\rubik_layer_turn_smoke_v0_1.json");
    const std::string hodgeScenario = ReadTextFile("assets\\rules\\scenarios\\chess3d\\hodge_projection_smoke_v0_1.json");
    test.Check(!classicProfile.empty(), "classic six-side profile exists");
    test.Check(!singleProfile.empty(), "single-side profile exists");
    test.Check(!asgardProfile.empty(), "asgard convergence profile exists");
    test.Check(!rubikProfile.empty(), "rubik convergence profile exists");
    test.Check(!hodgeProfile.empty(), "hodge projection duel profile exists");
    test.Check(!classicScenario.empty() && JsonParser(classicScenario).Parse(), "classic six-side scenario smoke JSON exists and parses");
    test.Check(!asgardScenario.empty() && JsonParser(asgardScenario).Parse(), "asgard scenario smoke JSON exists and parses");
    test.Check(!rubikScenario.empty() && JsonParser(rubikScenario).Parse(), "rubik layer-turn scenario smoke JSON exists and parses");
    test.Check(!hodgeScenario.empty() && JsonParser(hodgeScenario).Parse(), "hodge projection scenario smoke JSON exists and parses");
    test.Check(ExtractStringValue(classicScenario, "rulesetId") == "classic-six-side-3d-8x8x8-v0.1" &&
        classicScenario.find("\"layerTurn\": false") != std::string::npos &&
        classicScenario.find("\"projection\": false") != std::string::npos,
        "classic scenario declares classic capabilities");
    test.Check(ExtractStringValue(asgardScenario, "rulesetId") == "asgard-convergence-3d-8x8x8-v0.1" &&
        asgardScenario.find("\"coreStack\": true") != std::string::npos &&
        asgardScenario.find("\"fusion\": true") != std::string::npos &&
        asgardScenario.find("\"reserve\": true") != std::string::npos,
        "asgard scenario declares core, fusion, and reserve capabilities");
    test.Check(ExtractStringValue(rubikScenario, "rulesetId") == "rubik-convergence-3d-8x8x8-v0.1" &&
        rubikScenario.find("\"layerTurn\": true") != std::string::npos &&
        rubikScenario.find("LAYER") != std::string::npos,
        "rubik scenario declares layer-turn capabilities");
    test.Check(ExtractStringValue(hodgeScenario, "rulesetId") == "hodge-projection-duel-3d-8x8x8-v0.1" &&
        hodgeScenario.find("\"projection\": true") != std::string::npos &&
        hodgeScenario.find("HPD") != std::string::npos,
        "hodge scenario declares projection capabilities");

    test.Check(ValidateCommonRuleProfile(classicProfile), "classic six-side profile passes common validation");
    test.Check(ValidateCommonRuleProfile(singleProfile), "single-side profile passes common validation");
    test.Check(ValidateCommonRuleProfile(asgardProfile), "asgard convergence profile passes common validation");
    test.Check(ValidateCommonRuleProfile(rubikProfile), "rubik convergence profile passes common validation");
    test.Check(ValidateCommonRuleProfile(hodgeProfile), "hodge projection duel profile passes common validation");

    test.Check(ExtractStringValue(classicProfile, "rulesetId") == "classic-six-side-3d-8x8x8-v0.1",
        "classic six-side ruleset id matches");
    test.Check(ExtractStringValue(singleProfile, "rulesetId") == "single-side-3d-8x8x8-v0.1",
        "single-side profile ruleset id matches");
    test.Check(ExtractStringValue(asgardProfile, "rulesetId") == "asgard-convergence-3d-8x8x8-v0.1",
        "asgard convergence ruleset id matches");
    test.Check(ExtractStringValue(rubikProfile, "rulesetId") == "rubik-convergence-3d-8x8x8-v0.1",
        "rubik convergence ruleset id matches");
    test.Check(ExtractStringValue(hodgeProfile, "rulesetId") == "hodge-projection-duel-3d-8x8x8-v0.1",
        "hodge projection duel ruleset id matches");

    test.Check(ExtractStringValue(ExtractObject(classicProfile, "goalProfile"), "type") == "classicCheckmate",
        "classic six-side goalProfile is classicCheckmate");
    test.Check(ExtractStringValue(ExtractObject(classicProfile, "captureProfile"), "type") == "classicCapture",
        "classic six-side captureProfile is classicCapture");
    test.Check(ExtractStringValue(ExtractObject(classicProfile, "knockbackProfile"), "type") == "none",
        "classic six-side knockbackProfile is none");
    test.Check(ExtractStringValue(ExtractObject(classicProfile, "reserveProfile"), "type") == "none",
        "classic six-side reserveProfile is none");
    test.Check(ExtractStringValue(ExtractObject(classicProfile, "occupancyProfile"), "type") == "exclusive",
        "classic six-side occupancyProfile is exclusive");
    test.Check(ExtractStringValue(ExtractObject(classicProfile, "fusionProfile"), "type") == "none",
        "classic six-side fusionProfile is none");
    test.Check(ExtractStringValue(ExtractObject(classicProfile, "layerTurnProfile"), "type") == "disabled",
        "classic six-side layerTurnProfile is disabled");
    test.Check(ExtractStringValue(ExtractObject(classicProfile, "projectionProfile"), "type") == "none",
        "classic six-side projectionProfile is none");

    test.Check(ExtractStringValue(ExtractObject(singleProfile, "setupProfile"), "baseSetup") == "central4x4",
        "single-side profile references central4x4 setup");
    test.Check(ExtractStringValue(ExtractObject(singleProfile, "goalProfile"), "type") == "sandbox",
        "single-side profile is sandbox goal mode");
    test.Check(ExtractStringValue(ExtractObject(singleProfile, "occupancyProfile"), "type") == "exclusive",
        "single-side occupancyProfile is exclusive");
    test.Check(ExtractStringValue(ExtractObject(singleProfile, "fusionProfile"), "type") == "none",
        "single-side fusionProfile is none");
    test.Check(ExtractStringValue(ExtractObject(singleProfile, "knockbackProfile"), "type") == "none",
        "single-side knockbackProfile is none");
    test.Check(ExtractStringValue(ExtractObject(singleProfile, "reserveProfile"), "type") == "none",
        "single-side reserveProfile is none");
    test.Check(ExtractStringValue(ExtractObject(singleProfile, "projectionProfile"), "type") == "none",
        "single-side projectionProfile is none");

    test.Check(ExtractStringValue(ExtractObject(asgardProfile, "goalProfile"), "type") == "centerAssembly",
        "asgard convergence goalProfile is centerAssembly");
    test.Check(ValidateCoreCube(asgardProfile, 2, 5), "asgard convergence coreCube is x/y/z 2..5");
    test.Check(ExtractStringValue(ExtractObject(asgardProfile, "captureProfile"), "type") == "knockbackCapture",
        "asgard convergence captureProfile is knockbackCapture");
    const std::string asgardKnockback = ExtractObject(asgardProfile, "knockbackProfile");
    test.Check(ExtractStringValue(asgardKnockback, "type") == "homeOrReserve",
        "asgard convergence knockbackProfile is homeOrReserve");
    test.Check(ExtractStringValue(asgardKnockback, "homeSlotPolicy") == "firstMatchingFreeHomeSlot" &&
        ExtractStringValue(asgardKnockback, "fallback") == "reserve" &&
        ExtractStringValue(asgardKnockback, "appliesTo") == "outerFieldCaptures",
        "asgard convergence knockbackProfile routes outer-field captures home or reserve");
    bool destructiveCoreCapture = true;
    test.Check(ExtractBoolValue(asgardKnockback, "destructiveCoreCapture", destructiveCoreCapture) && !destructiveCoreCapture,
        "asgard convergence disables destructive core capture");
    const std::string asgardReserve = ExtractObject(asgardProfile, "reserveProfile");
    test.Check(ExtractStringValue(asgardReserve, "type") == "sidePieceTypeCounts" &&
        ExtractStringValue(asgardReserve, "restoreAction") == "deferred" &&
        ExtractStringValue(asgardReserve, "status") == "runtimePartial",
        "asgard convergence reserveProfile is runtimePartial side/type counts");
    const std::string asgardOccupancy = ExtractObject(asgardProfile, "occupancyProfile");
    test.Check(ExtractStringValue(asgardOccupancy, "type") == "coreStack",
        "asgard convergence occupancyProfile is coreStack");
    test.Check(ExtractStringValue(asgardOccupancy, "outerField") == "exclusive",
        "asgard convergence outer field is exclusive");
    test.Check(ExtractStringValue(asgardOccupancy, "core") == "multiOccupancyAllowed",
        "asgard convergence core allows multi-occupancy by profile");
    test.Check(ExtractStringValue(asgardOccupancy, "status") == "specOnly",
        "asgard convergence occupancyProfile is specOnly");
    const std::string asgardFusion = ExtractObject(asgardProfile, "fusionProfile");
    test.Check(ExtractStringValue(asgardFusion, "type") == "stackFusion",
        "asgard convergence fusionProfile is stackFusion");
    test.Check(ExtractStringValue(asgardFusion, "status") == "runtimePartial",
        "asgard convergence fusionProfile is runtimePartial");
    test.Check(ExtractStringValue(asgardFusion, "royalPairFusion") == "enabled",
        "asgard convergence enables royal pair fusion metadata");
    bool destructiveMerge = true;
    test.Check(ExtractBoolValue(asgardFusion, "destructiveMerge", destructiveMerge) && !destructiveMerge,
        "asgard convergence fusion is non-destructive");
    const std::string asgardImplosion = ExtractObject(asgardProfile, "implosionProfile");
    test.Check(ExtractStringValue(asgardImplosion, "type") == "centerCompletion",
        "asgard convergence has centerCompletion implosionProfile");
    test.Check(ExtractStringValue(asgardImplosion, "mode") == "progressState",
        "asgard convergence implosion is progress state");
    bool implosionDestructive = true;
    test.Check(ExtractBoolValue(asgardImplosion, "destructive", implosionDestructive) && !implosionDestructive,
        "asgard convergence implosion is non-destructive");
    const std::string asgardCorePhysics = ExtractObject(asgardProfile, "corePhysicsProfile");
    test.Check(ExtractStringValue(asgardCorePhysics, "type") == "asgardCorePhysics",
        "asgard convergence corePhysicsProfile is asgardCorePhysics");
    test.Check(ExtractStringValue(asgardCorePhysics, "implementationStage") == "specOnly",
        "asgard convergence corePhysicsProfile is specOnly");
    const std::string asgard216 = ExtractObject(asgardCorePhysics, "volumeSurface216Principle");
    bool asgard216Enabled = true;
    test.Check(!asgard216.empty() && ExtractBoolValue(asgard216, "enabled", asgard216Enabled) && !asgard216Enabled,
        "asgard convergence volumeSurface216Principle exists and is disabled");
    test.Check(ExtractStringValue(ExtractObject(asgardProfile, "layerTurnProfile"), "type") == "disabled",
        "asgard convergence layerTurnProfile is disabled");
    test.Check(ExtractStringValue(ExtractObject(asgardProfile, "projectionProfile"), "type") == "none",
        "asgard convergence projectionProfile is none");
    test.Check(asgardProfile.find("Forbidden Core / Asgard / Meru") != std::string::npos,
        "asgard convergence mythProfile names the forbidden core");

    test.Check(ExtractStringValue(rubikProfile, "baseRuleset") == "asgard-convergence-3d-8x8x8-v0.1",
        "rubik convergence points to asgard convergence baseRuleset");
    test.Check(ExtractStringValue(ExtractObject(rubikProfile, "occupancyProfile"), "type") == "coreStack",
        "rubik convergence occupancyProfile is coreStack");
    test.Check(ExtractStringValue(ExtractObject(rubikProfile, "fusionProfile"), "type") == "stackFusion",
        "rubik convergence fusionProfile is stackFusion");
    test.Check(ExtractStringValue(ExtractObject(rubikProfile, "knockbackProfile"), "type") == "homeOrReserve",
        "rubik convergence knockbackProfile is homeOrReserve");
    test.Check(ExtractStringValue(ExtractObject(rubikProfile, "reserveProfile"), "type") == "sidePieceTypeCounts",
        "rubik convergence reserveProfile is sidePieceTypeCounts");
    test.Check(ExtractStringValue(ExtractObject(rubikProfile, "implosionProfile"), "mode") == "progressState",
        "rubik convergence implosion is progress state");
    const std::string rubikLayerTurn = ExtractObject(rubikProfile, "layerTurnProfile");
    test.Check(ExtractStringValue(rubikLayerTurn, "type") == "ritualTurn",
        "rubik convergence layerTurnProfile is ritualTurn");
    test.Check(rubikLayerTurn.find("\"X\"") != std::string::npos &&
        rubikLayerTurn.find("\"Y\"") != std::string::npos &&
        rubikLayerTurn.find("\"Z\"") != std::string::npos,
        "rubik convergence ritualTurn axes include X/Y/Z");
    test.Check(rubikLayerTurn.find("\"layers\"") != std::string::npos &&
        rubikLayerTurn.find("0") != std::string::npos && rubikLayerTurn.find("7") != std::string::npos,
        "rubik convergence ritualTurn layers are 0..7");
    test.Check(rubikLayerTurn.find("-1") != std::string::npos && rubikLayerTurn.find("1") != std::string::npos,
        "rubik convergence ritualTurn quarter turns include -1 and +1");
    bool movesProjected = false;
    bool movesStacks = false;
    bool recomputesFusion = false;
    bool recomputesAnchors = false;
    test.Check(ExtractBoolValue(rubikLayerTurn, "movesProjectedBoard", movesProjected) && movesProjected &&
        ExtractBoolValue(rubikLayerTurn, "movesCoreStacks", movesStacks) && movesStacks &&
        ExtractBoolValue(rubikLayerTurn, "recomputesFusion", recomputesFusion) && recomputesFusion &&
        ExtractBoolValue(rubikLayerTurn, "recomputesAnchors", recomputesAnchors) && recomputesAnchors,
        "rubik convergence ritualTurn declares runtime board/stack/fusion/anchor recompute");
    test.Check(ExtractStringValue(rubikLayerTurn, "reserveInteraction") == "unaffected" &&
        ExtractStringValue(rubikLayerTurn, "status") == "runtimePartial",
        "rubik convergence ritualTurn declares reserve unaffected runtimePartial behavior");
    const std::string rubikCorePhysics = ExtractObject(rubikProfile, "corePhysicsProfile");
    test.Check(ExtractStringValue(rubikCorePhysics, "layerTurnStackInteraction") == "movesWholeCoreStacks",
        "rubik convergence moves whole CoreCell stacks during layer turns");
    bool rubik216Enabled = true;
    test.Check(ExtractBoolValue(ExtractObject(rubikCorePhysics, "volumeSurface216Principle"), "enabled", rubik216Enabled) && !rubik216Enabled,
        "rubik convergence volumeSurface216Principle is disabled");
    test.Check(rubikProfile.find("P2H layer turns move the projected board and whole CoreCell stacks") != std::string::npos,
        "rubik convergence knownLimitations mention P2H runtime stack layer turns");

    const std::string hodgeProjection = ExtractObject(hodgeProfile, "projectionProfile");
    test.Check(ExtractStringValue(hodgeProjection, "type") == "hodgeTriuneProjection",
        "hodge projection duel projectionProfile is hodgeTriuneProjection");
    bool hodgeEnabled = false;
    test.Check(ExtractBoolValue(hodgeProjection, "enabled", hodgeEnabled) && hodgeEnabled,
        "hodge projection duel enables projectionProfile");
    test.Check(ExtractStringValue(hodgeProjection, "mirrorPolicy") == "allOrNothing" &&
        ExtractStringValue(hodgeProjection, "actionHistoryMode") == "compositeTurnWithChildren",
        "hodge projection duel declares all-or-nothing composite action history");
    test.Check(hodgeProjection.find("\"sideIds\": [1, 3, 5]") != std::string::npos &&
        hodgeProjection.find("\"sideIds\": [2, 4, 6]") != std::string::npos,
        "hodge projection duel groups positive and negative triads");
    test.Check(ExtractStringValue(ExtractObject(hodgeProfile, "goalProfile"), "type") == "sandbox" &&
        ExtractStringValue(ExtractObject(hodgeProfile, "captureProfile"), "type") == "classicCapture" &&
        ExtractStringValue(ExtractObject(hodgeProfile, "occupancyProfile"), "type") == "exclusive" &&
        ExtractStringValue(ExtractObject(hodgeProfile, "fusionProfile"), "type") == "none" &&
        ExtractStringValue(ExtractObject(hodgeProfile, "layerTurnProfile"), "type") == "disabled",
        "hodge projection duel stays classic/exclusive/no-fusion/no-layer-turn by default");

    test.Check(Chess3D_LoadRuleProfileJson(game, classicProfile.c_str()) == 1, "runtime loads classic six-side profile");
    test.Check(Chess3D_IsCoreStackEnabled(game) == 0, "classic profile keeps core stacks disabled");
    test.Check(Chess3D_IsFusionEnabled(game) == 0, "classic profile keeps fusion disabled");
    test.Check(Chess3D_GetCoreFusionKind(game, 2, 2, 2) == FusionNone, "classic profile reports no core fusion");
    test.Check(Chess3D_GetCoreFusionKind(game, 0, 0, 0) == FusionNone, "outside core fusion kind is none");
    test.Check(Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn)) == 0,
        "classic profile rejects explicit core stack push");
    test.Check(ReadAbiString(game, Chess3D_GetCurrentRulesetId) == "classic-six-side-3d-8x8x8-v0.1",
        "runtime exposes classic ruleset id");
    test.Check(ReadAbiString(game, Chess3D_GetGoalProfileType) == "classicCheckmate",
        "runtime exposes classic goal profile");
    test.Check(ReadAbiString(game, Chess3D_GetCaptureProfileType) == "classicCapture",
        "runtime exposes classic capture profile");
    test.Check(ReadAbiString(game, Chess3D_GetOccupancyProfileType) == "exclusive",
        "runtime exposes classic occupancy profile");
    test.Check(ReadAbiString(game, Chess3D_GetFusionProfileType) == "none",
        "runtime exposes classic fusion profile");
    test.Check(ReadAbiString(game, Chess3D_GetLayerTurnProfileType) == "disabled",
        "runtime exposes classic layer profile");
    test.Check(Chess3D_IsReserveEnabled(game) == 0 && Chess3D_IsKnockbackEnabled(game) == 0,
        "classic profile keeps reserve and knockback disabled");
    Chess3D_Clear(game);
    test.Check(Chess3D_SetPiece(game, 0, 0, 0, 1, Rook) == 1 &&
        Chess3D_SetPiece(game, 0, 0, 3, 2, Pawn) == 1 &&
        Chess3D_TryMakeMove(game, 0, 0, 0, 0, 0, 3, 0, &played) == 1,
        "classic profile preserves old outer capture behavior");
    int lastCaptured = 0;
    int lastDestination = -1;
    int lastX = -1;
    int lastY = -1;
    int lastZ = -1;
    test.Check(Chess3D_GetPiece(game, 0, 0, 3) == PieceCode(1, Rook) &&
        Chess3D_GetLastKnockbackInfo(game, &lastCaptured, &lastDestination, &lastX, &lastY, &lastZ) == 1 &&
        lastCaptured == PieceCode(2, Pawn) && lastDestination == KnockbackClassicRemoved &&
        Chess3D_GetReserveTotal(game, 2) == 0,
        "classic capture removes captured piece without reserve");

    test.Check(Chess3D_LoadRuleProfileJson(game, singleProfile.c_str()) == 1, "runtime loads single-side profile");
    test.Check(Chess3D_IsFusionEnabled(game) == 0, "single-side profile keeps fusion disabled");
    test.Check(Chess3D_IsReserveEnabled(game) == 0 && Chess3D_IsKnockbackEnabled(game) == 0,
        "single-side profile keeps reserve and knockback disabled");
    test.Check(ReadAbiString(game, Chess3D_GetCurrentRulesetId) == "single-side-3d-8x8x8-v0.1",
        "runtime exposes single-side ruleset id");
    test.Check(ReadAbiString(game, Chess3D_GetGoalProfileType) == "sandbox",
        "runtime exposes single-side sandbox goal");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime loads asgard convergence profile");
    test.Check(Chess3D_IsCoreStackEnabled(game) == 1, "asgard profile enables core stacks");
    test.Check(Chess3D_IsFusionEnabled(game) == 1, "asgard profile enables fusion descriptors");
    test.Check(Chess3D_IsReserveEnabled(game) == 1 && Chess3D_IsKnockbackEnabled(game) == 1,
        "asgard profile enables reserve and knockback");
    Chess3D_Clear(game);
    test.Check(Chess3D_GetCoreStackCount(game, 2, 2, 2) == 0, "asgard clear leaves target stack empty");
    test.Check(Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn)) == 1, "asgard push first core stack piece succeeds");
    test.Check(Chess3D_GetCoreStackCount(game, 2, 2, 2) == 1, "asgard stack count is one after first push");
    int stackSide = 0;
    int stackType = 0;
    int stackPiece = 0;
    int stackFlags = -1;
    test.Check(Chess3D_GetCoreStackEntry(game, 2, 2, 2, 0, &stackSide, &stackType, &stackPiece, &stackFlags) == 1 &&
        stackSide == 1 && stackType == Pawn && stackPiece == PieceCode(1, Pawn) && stackFlags == 0,
        "asgard stack entry exposes side/type/pieceCode/flags");
    test.Check(Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(2, Knight)) == 1, "asgard push second core stack piece succeeds");
    test.Check(Chess3D_GetCoreStackCount(game, 2, 2, 2) == 2, "asgard stack count is two after second push");
    test.Check(Chess3D_GetPiece(game, 2, 2, 2) == PieceCode(2, Knight) &&
        Chess3D_GetProjectedPiece(game, 2, 2, 2) == PieceCode(2, Knight),
        "asgard projected piece is top stack entry");
    test.Check(Chess3D_PushCoreStackPiece(game, 0, 0, 0, PieceCode(1, Pawn)) == 0, "core stack push outside core fails");
    test.Check(Chess3D_PushCoreStackPiece(game, -1, 2, 2, PieceCode(1, Pawn)) == 0, "core stack push with invalid coords fails");
    test.Check(Chess3D_GetCoreStackEntry(game, 2, 2, 2, 99, &stackSide, &stackType, &stackPiece, &stackFlags) == 0,
        "invalid stack index fails cleanly");
    test.Check(Chess3D_GetCoreStackEntry(game, 2, 2, 2, 0, nullptr, &stackType, &stackPiece, &stackFlags) == 0,
        "null stack entry pointer fails cleanly");
    test.Check(Chess3D_SetPiece(game, 2, 2, 2, 1, Rook) == 1 &&
        Chess3D_GetCoreStackCount(game, 2, 2, 2) == 1 &&
        Chess3D_GetProjectedPiece(game, 2, 2, 2) == PieceCode(1, Rook),
        "SetPiece inside core replaces stack with one entry");
    test.Check(Chess3D_SetPiece(game, 2, 2, 2, 0, 0) == 1 &&
        Chess3D_GetCoreStackCount(game, 2, 2, 2) == 0 &&
        Chess3D_GetPiece(game, 2, 2, 2) == 0,
        "SetPiece empty inside core clears stack");
    test.Check(Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn)) == 1 &&
        Chess3D_ClearCoreStack(game, 2, 2, 2) == 1 &&
        Chess3D_GetCoreStackCount(game, 2, 2, 2) == 0,
        "ClearCoreStack clears a core stack");
    test.Check(Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn)) == 1 &&
        Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Rook)) == 1 &&
        Chess3D_RemoveCoreStackEntry(game, 2, 2, 2, 1) == 1 &&
        Chess3D_GetCoreStackCount(game, 2, 2, 2) == 1 &&
        Chess3D_GetProjectedPiece(game, 2, 2, 2) == PieceCode(1, Pawn),
        "RemoveCoreStackEntry removes selected entry and updates projection");
    Chess3D_Reset(game);
    test.Check(Chess3D_GetCoreStackCount(game, 2, 2, 2) == 0, "Reset clears explicit core stacks");
    test.Check(Chess3D_GetSideImplosionProgress(game, 1) == 0, "Reset clears implosion progress");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime reloads asgard profile for fusion tests");
    Chess3D_Clear(game);
    int fusionKind = -1;
    int fusionOwner = -1;
    int fusionMask = 0;
    int fusionEntries = -1;
    int fusionFriendly = -1;
    int fusionEnemy = -1;
    int fusionDominantType = -1;
    int fusionFlags = -1;

    test.Check(Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn)) == 1, "fusion test pushes single piece");
    test.Check(Chess3D_RecomputeFusion(game) == 1, "fusion recompute succeeds");
    test.Check(Chess3D_GetCoreFusionState(game, 2, 2, 2, &fusionKind, &fusionOwner, &fusionMask, &fusionEntries, &fusionFriendly, &fusionEnemy, &fusionDominantType, &fusionFlags) == 1 &&
        fusionKind == FusionSingle && fusionOwner == 1 && fusionEntries == 1 && fusionFriendly == 1 && fusionEnemy == 0 &&
        (fusionMask & (1 << 1)) != 0 && fusionDominantType == Pawn,
        "single core entry reports single fusion state");
    test.Check(ReadFusionKindName(FusionSingle) == "single", "fusion kind name exposes single");

    Chess3D_Clear(game);
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn));
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Rook));
    test.Check(Chess3D_RecomputeFusion(game) == 1 &&
        Chess3D_GetCoreFusionKind(game, 2, 2, 2) == FusionFriendlyPair &&
        Chess3D_IsCoreCellContested(game, 2, 2, 2) == 0 &&
        Chess3D_GetSideFusionCount(game, 1) == 1,
        "friendly pair fusion is detected");

    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Bishop));
    test.Check(Chess3D_RecomputeFusion(game) == 1 &&
        Chess3D_GetCoreFusionKind(game, 2, 2, 2) == FusionFriendlyStack &&
        Chess3D_GetCoreFusionState(game, 2, 2, 2, &fusionKind, &fusionOwner, &fusionMask, &fusionEntries, &fusionFriendly, &fusionEnemy, &fusionDominantType, &fusionFlags) == 1 &&
        fusionEntries == 3 && fusionOwner == 1,
        "friendly stack fusion is detected");

    Chess3D_Clear(game);
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, King));
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Queen));
    test.Check(Chess3D_RecomputeFusion(game) == 1 &&
        Chess3D_GetCoreFusionKind(game, 2, 2, 2) == FusionRoyalPair &&
        Chess3D_HasRoyalPairFusion(game, 2, 2, 2, 1) == 1 &&
        Chess3D_GetCoreFusionState(game, 2, 2, 2, &fusionKind, &fusionOwner, &fusionMask, &fusionEntries, &fusionFriendly, &fusionEnemy, &fusionDominantType, &fusionFlags) == 1 &&
        (fusionFlags & FusionFlagRoyalPair) != 0,
        "king and queen form royalPair fusion descriptor");

    Chess3D_Clear(game);
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn));
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(2, Knight));
    test.Check(Chess3D_RecomputeFusion(game) == 1 &&
        Chess3D_GetCoreFusionKind(game, 2, 2, 2) == FusionContested &&
        Chess3D_IsCoreCellContested(game, 2, 2, 2) == 1 &&
        Chess3D_GetCoreFusionState(game, 2, 2, 2, &fusionKind, &fusionOwner, &fusionMask, &fusionEntries, &fusionFriendly, &fusionEnemy, &fusionDominantType, &fusionFlags) == 1 &&
        fusionOwner == 0 && fusionEntries == 2 && (fusionMask & (1 << 1)) != 0 && (fusionMask & (1 << 2)) != 0 &&
        (fusionFlags & FusionFlagContested) != 0 && Chess3D_GetCoreStackCount(game, 2, 2, 2) == 2 &&
        Chess3D_GetSideContestedCount(game, 1) == 1 && Chess3D_GetSideContestedCount(game, 2) == 1,
        "enemy co-occupancy reports contested state without removing entries");

    test.Check(Chess3D_RemoveCoreStackEntry(game, 2, 2, 2, 1) == 1 &&
        Chess3D_RecomputeFusion(game) == 1 &&
        Chess3D_GetCoreFusionKind(game, 2, 2, 2) == FusionSingle &&
        Chess3D_IsCoreCellContested(game, 2, 2, 2) == 0,
        "removing enemy entry clears contested fusion state");
    test.Check(Chess3D_ClearCoreStack(game, 2, 2, 2) == 1 &&
        Chess3D_GetCoreFusionKind(game, 2, 2, 2) == FusionNone,
        "clearing stack resets fusion kind");

    test.Check(ReadAbiString(game, Chess3D_GetCurrentRulesetId) == "asgard-convergence-3d-8x8x8-v0.1",
        "runtime exposes asgard ruleset id");
    test.Check(ReadAbiString(game, Chess3D_GetGoalProfileType) == "centerAssembly",
        "runtime exposes asgard centerAssembly goal");
    test.Check(ReadAbiString(game, Chess3D_GetCaptureProfileType) == "knockbackCapture",
        "runtime exposes asgard knockback capture profile");
    test.Check(ReadAbiString(game, Chess3D_GetOccupancyProfileType) == "coreStack",
        "runtime exposes asgard coreStack occupancy profile");
    test.Check(ReadAbiString(game, Chess3D_GetFusionProfileType) == "stackFusion",
        "runtime exposes asgard stackFusion profile");
    test.Check(ReadAbiString(game, Chess3D_GetCorePhysicsProfileType) == "asgardCorePhysics",
        "runtime exposes asgard core physics profile");
    test.Check(ReadAbiString(game, Chess3D_GetLayerTurnProfileType) == "disabled",
        "runtime exposes asgard disabled layer turns");
    test.Check(ReadAbiString(game, Chess3D_GetVictoryProfileType) == "allPiecesAnchored",
        "runtime exposes asgard allPiecesAnchored victory");

    int xMin = -1;
    int xMax = -1;
    int yMin = -1;
    int yMax = -1;
    int zMin = -1;
    int zMax = -1;
    test.Check(Chess3D_GetCoreCube(game, &xMin, &xMax, &yMin, &yMax, &zMin, &zMax) == 1,
        "runtime exposes coreCube bounds");
    test.Check(xMin == 2 && xMax == 5 && yMin == 2 && yMax == 5 && zMin == 2 && zMax == 5,
        "runtime coreCube is x/y/z 2..5");

    for (int side = 1; side <= 6; ++side)
    {
        int targetCount = 0;
        for (int z = 0; z < 8; ++z)
        {
            for (int y = 0; y < 8; ++y)
            {
                for (int x = 0; x < 8; ++x)
                {
                    targetCount += Chess3D_IsTargetSlot(game, side, x, y, z) != 0 ? 1 : 0;
                }
            }
        }
        test.Check(targetCount == 16, "runtime target slot count is 16 for side " + std::to_string(side));
        for (const TargetCell& cell : TargetCellsForSide(side))
        {
            test.Check(Chess3D_IsTargetSlot(game, side, cell.x, cell.y, cell.z) == 1,
                "runtime target slot coordinate is recognized for side " + std::to_string(side));
            test.Check(cell.x >= 2 && cell.x <= 5 && cell.y >= 2 && cell.y <= 5 && cell.z >= 2 && cell.z <= 5,
                "runtime target slot coordinate is inside coreCube for side " + std::to_string(side));
        }
    }
    test.Check(Chess3D_IsTargetSlot(game, 1, 0, 0, 0) == 0, "runtime rejects non-core target slot coordinate");

    test.Check(Chess3D_LoadRuleProfileJson(game, "{\"rulesetId\":") == 0,
        "runtime rejects invalid profile JSON cleanly");
    test.Check(ReadAbiString(game, Chess3D_GetCurrentRulesetId) == "asgard-convergence-3d-8x8x8-v0.1",
        "runtime keeps previous valid profile after invalid profile load");
    test.Check(!ReadAbiString(game, Chess3D_GetLastProfileError).empty(),
        "runtime exposes profile load error after invalid profile load");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime reloads asgard profile after invalid load");
    Chess3D_Clear(game);
    test.Check(Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn)) == 1, "move test seeds occupied core stack");
    test.Check(Chess3D_SetPiece(game, 2, 2, 1, 1, Rook) == 1, "move test places rook outside core");
    test.Check(Chess3D_TryMakeMove(game, 2, 2, 1, 2, 2, 2, 0, &played) == 1,
        "TryMakeMove entering occupied core stack succeeds");
    test.Check(Chess3D_GetCoreStackCount(game, 2, 2, 2) == 2, "entering core appends to existing stack");
    test.Check(Chess3D_GetPiece(game, 2, 2, 1) == 0, "entering core clears source square");
    test.Check(Chess3D_GetProjectedPiece(game, 2, 2, 2) == PieceCode(1, Rook), "entering core projects moved top piece");
    test.Check(played.captured == 0 && (played.flags & MoveCapture) == 0, "entering core does not ordinary-capture previous occupant");
    test.Check(Chess3D_GetCoreFusionKind(game, 2, 2, 2) == FusionFriendlyPair,
        "entering occupied core updates fusion state automatically");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime reloads asgard profile for core-to-core move test");
    Chess3D_Clear(game);
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn));
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Rook));
    test.Check(Chess3D_TryMakeMove(game, 2, 2, 2, 3, 2, 2, 0, &played) == 1,
        "TryMakeMove core-to-core moves projected stack entry");
    test.Check(Chess3D_GetCoreStackCount(game, 2, 2, 2) == 1 &&
        Chess3D_GetCoreStackCount(game, 3, 2, 2) == 1 &&
        Chess3D_GetProjectedPiece(game, 3, 2, 2) == PieceCode(1, Rook),
        "core-to-core move updates source and target stacks");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime reloads asgard profile for leaving-core move test");
    Chess3D_Clear(game);
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn));
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Rook));
    test.Check(Chess3D_TryMakeMove(game, 2, 2, 2, 2, 2, 1, 0, &played) == 1,
        "TryMakeMove leaving core moves projected stack entry outside");
    test.Check(Chess3D_GetCoreStackCount(game, 2, 2, 2) == 1 &&
        Chess3D_GetProjectedPiece(game, 2, 2, 2) == PieceCode(1, Pawn) &&
        Chess3D_GetPiece(game, 2, 2, 1) == PieceCode(1, Rook),
        "leaving core removes top entry and places it outside");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime reloads asgard profile for knockback home test");
    Chess3D_Clear(game);
    test.Check(Chess3D_SetPiece(game, 0, 0, 0, 1, Rook) == 1 &&
        Chess3D_SetPiece(game, 0, 0, 3, 2, Pawn) == 1 &&
        Chess3D_TryMakeMove(game, 0, 0, 0, 0, 0, 3, 0, &played) == 1,
        "asgard outside-to-outside enemy capture succeeds");
    test.Check(Chess3D_GetLastKnockbackInfo(game, &lastCaptured, &lastDestination, &lastX, &lastY, &lastZ) == 1 &&
        lastCaptured == PieceCode(2, Pawn) &&
        lastDestination == KnockbackHome &&
        Chess3D_GetPiece(game, 0, 0, 3) == PieceCode(1, Rook) &&
        Chess3D_GetPiece(game, lastX, lastY, lastZ) == PieceCode(2, Pawn) &&
        Chess3D_GetReserveCount(game, 2, Pawn) == 0,
        "asgard capture returns captured piece to first free matching home slot");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime reloads asgard profile for reserve fallback test");
    Chess3D_Clear(game);
    for (const TargetCell& home : HomeCellsForSide(2))
    {
        if (home.type == Pawn)
        {
            Chess3D_SetPiece(game, home.x, home.y, home.z, 2, Pawn);
        }
    }
    test.Check(Chess3D_SetPiece(game, 0, 0, 0, 1, Rook) == 1 &&
        Chess3D_SetPiece(game, 0, 0, 3, 2, Pawn) == 1 &&
        Chess3D_TryMakeMove(game, 0, 0, 0, 0, 0, 3, 0, &played) == 1,
        "asgard capture succeeds when captured home slots are blocked");
    test.Check(Chess3D_GetLastKnockbackInfo(game, &lastCaptured, &lastDestination, &lastX, &lastY, &lastZ) == 1 &&
        lastCaptured == PieceCode(2, Pawn) &&
        lastDestination == KnockbackReserve &&
        Chess3D_GetReserveCount(game, 2, Pawn) == 1 &&
        Chess3D_GetReserveTotal(game, 2) == 1,
        "asgard capture falls back to reserve when matching home slots are blocked");
    test.Check(Chess3D_ClearReserve(game, 2) == 1 && Chess3D_GetReserveTotal(game, 2) == 0,
        "reserve can be cleared for a side through ABI");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime reloads asgard profile for own-destination capture test");
    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 0, 0, 0, 1, Rook);
    Chess3D_SetPiece(game, 0, 0, 3, 1, Pawn);
    test.Check(Chess3D_TryMakeMove(game, 0, 0, 0, 0, 0, 3, 0, &played) == 0 &&
        Chess3D_GetReserveTotal(game, 1) == 0,
        "own-piece outside destination is rejected and reserve remains unchanged");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime reloads asgard profile for outside-to-core reserve isolation test");
    Chess3D_Clear(game);
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(2, Pawn));
    Chess3D_SetPiece(game, 2, 2, 1, 1, Rook);
    test.Check(Chess3D_TryMakeMove(game, 2, 2, 1, 2, 2, 2, 0, &played) == 1 &&
        Chess3D_GetCoreStackCount(game, 2, 2, 2) == 2 &&
        Chess3D_IsCoreCellContested(game, 2, 2, 2) == 1 &&
        Chess3D_GetReserveTotal(game, 2) == 0 &&
        Chess3D_GetLastCapturedPieceReserveDestination(game) == KnockbackNone,
        "outside-to-core entry preserves occupants and does not knock back");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime reloads asgard profile for core-to-outside capture test");
    Chess3D_Clear(game);
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn));
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Rook));
    Chess3D_SetPiece(game, 2, 2, 1, 2, Pawn);
    test.Check(Chess3D_TryMakeMove(game, 2, 2, 2, 2, 2, 1, 0, &played) == 1 &&
        Chess3D_GetPiece(game, 2, 2, 1) == PieceCode(1, Rook) &&
        Chess3D_GetCoreStackCount(game, 2, 2, 2) == 1 &&
        Chess3D_GetLastKnockbackInfo(game, &lastCaptured, &lastDestination, &lastX, &lastY, &lastZ) == 1 &&
        lastCaptured == PieceCode(2, Pawn) &&
        (lastDestination == KnockbackHome || lastDestination == KnockbackReserve),
        "core-to-outside enemy capture routes captured piece through knockback");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime reloads asgard profile for reserve reset test");
    Chess3D_Clear(game);
    for (const TargetCell& home : HomeCellsForSide(2))
    {
        if (home.type == Pawn)
        {
            Chess3D_SetPiece(game, home.x, home.y, home.z, 2, Pawn);
        }
    }
    Chess3D_SetPiece(game, 0, 0, 0, 1, Rook);
    Chess3D_SetPiece(game, 0, 0, 3, 2, Pawn);
    Chess3D_TryMakeMove(game, 0, 0, 0, 0, 0, 3, 0, &played);
    test.Check(Chess3D_GetReserveCount(game, 2, Pawn) == 1, "reserve increments before reset");
    Chess3D_Reset(game);
    test.Check(Chess3D_GetReserveTotal(game, 2) == 0 &&
        Chess3D_GetLastCapturedPieceReserveDestination(game) == KnockbackNone,
        "reset clears reserve and last knockback state");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime reloads asgard profile for anchor stack test");
    Chess3D_Clear(game);
    for (const TargetCell& cell : TargetCellsForSide(1))
    {
        test.Check(Chess3D_PushCoreStackPiece(game, cell.x, cell.y, cell.z, PieceCode(2, Queen)) == 1,
            "runtime places non-matching stack entry before matching anchor");
        test.Check(Chess3D_PushCoreStackPiece(game, cell.x, cell.y, cell.z, PieceCode(1, cell.type)) == 1,
            "runtime pushes matching side-1 target stack piece");
    }
    test.Check(Chess3D_RecomputeAnchors(game) == 1, "runtime recomputes anchors");
    test.Check(Chess3D_GetAnchorCount(game, 1) == 16, "runtime anchors all 16 matching side-1 target slots over stacks");
    test.Check(Chess3D_GetRequiredAnchorCount(game, 1) == 16, "runtime required anchor count defaults to profile value 16");
    test.Check(Chess3D_IsAnchoredCell(game, 2, 2, 2) == 1, "runtime reports matching target cell as anchored");
    test.Check(Chess3D_IsGameOver(game) == 1, "runtime centerAssembly victory triggers after all anchors");
    test.Check(Chess3D_GetWinnerSide(game) == 1, "runtime centerAssembly winner is side 1");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime reloads asgard profile for fusion anchor test");
    Chess3D_Clear(game);
    const TargetCell fusionTarget = TargetCellsForSide(1).front();
    test.Check(Chess3D_PushCoreStackPiece(game, fusionTarget.x, fusionTarget.y, fusionTarget.z, PieceCode(1, fusionTarget.type)) == 1 &&
        Chess3D_PushCoreStackPiece(game, fusionTarget.x, fusionTarget.y, fusionTarget.z, PieceCode(1, Rook)) == 1 &&
        Chess3D_RecomputeFusion(game) == 1,
        "runtime creates matching target slot with friendly fusion");
    test.Check(Chess3D_GetAnchorCount(game, 1) == 1 &&
        Chess3D_GetSideFusionCount(game, 1) == 1 &&
        Chess3D_GetSideImplosionProgress(game, 1) > 0,
        "anchor and friendly fusion contribute to implosion progress");
    test.Check(Chess3D_GetCoreFusionState(game, fusionTarget.x, fusionTarget.y, fusionTarget.z, &fusionKind, &fusionOwner, &fusionMask, &fusionEntries, &fusionFriendly, &fusionEnemy, &fusionDominantType, &fusionFlags) == 1 &&
        (fusionFlags & FusionFlagAnchoredFusion) != 0 &&
        (fusionFlags & FusionFlagImplosionSeed) != 0,
        "anchored friendly fusion exposes implosion seed flags");
    Chess3D_Reset(game);
    test.Check(Chess3D_GetSideImplosionProgress(game, 1) == 0, "implosion progress resets after Reset");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime reloads asgard profile for type matching tests");
    Chess3D_Clear(game);
    const TargetCell firstTarget = TargetCellsForSide(1).front();
    test.Check(Chess3D_SetPiece(game, firstTarget.x, firstTarget.y, firstTarget.z, 2, firstTarget.type) == 1,
        "runtime places wrong-side piece on side-1 target");
    test.Check(Chess3D_RecomputeAnchors(game) == 1, "runtime recomputes wrong-side anchor test");
    test.Check(Chess3D_GetAnchorCount(game, 1) == 0, "wrong side does not satisfy side-1 anchor");
    test.Check(Chess3D_SetPiece(game, firstTarget.x, firstTarget.y, firstTarget.z, 1, Queen) == 1,
        "runtime places wrong-type piece on side-1 target");
    test.Check(Chess3D_RecomputeAnchors(game) == 1, "runtime recomputes wrong-type anchor test");
    test.Check(Chess3D_GetAnchorCount(game, 1) == 0, "wrong type does not satisfy side-1 anchor");
    test.Check(Chess3D_SetPiece(game, firstTarget.x, firstTarget.y, firstTarget.z, 1, firstTarget.type) == 1,
        "runtime places correct side/type on side-1 target");
    test.Check(Chess3D_RecomputeAnchors(game) == 1, "runtime recomputes correct side/type anchor test");
    test.Check(Chess3D_GetAnchorCount(game, 1) == 1, "correct side/type satisfies one anchor");

    test.Check(Chess3D_LoadRuleProfileJson(game, classicProfile.c_str()) == 1, "runtime reloads classic profile for isolation test");
    Chess3D_Clear(game);
    for (const TargetCell& cell : TargetCellsForSide(1))
    {
        Chess3D_SetPiece(game, cell.x, cell.y, cell.z, 1, cell.type);
    }
    test.Check(Chess3D_RecomputeAnchors(game) == 1, "runtime recomputes classic isolation anchors");
    test.Check(Chess3D_GetAnchorCount(game, 1) == 0, "classic profile does not count centerAssembly anchors");
    test.Check(Chess3D_IsGameOver(game) == 0 && Chess3D_GetWinnerSide(game) == 0,
        "classic profile does not trigger centerAssembly winner");

    test.Check(Chess3D_LoadRuleProfileJson(game, singleProfile.c_str()) == 1, "runtime reloads single-side sandbox profile for isolation test");
    Chess3D_Clear(game);
    for (const TargetCell& cell : TargetCellsForSide(1))
    {
        Chess3D_SetPiece(game, cell.x, cell.y, cell.z, 1, cell.type);
    }
    test.Check(Chess3D_RecomputeAnchors(game) == 1, "runtime recomputes sandbox isolation anchors");
    test.Check(Chess3D_IsGameOver(game) == 0 && Chess3D_GetWinnerSide(game) == 0,
        "sandbox profile does not trigger accidental winner");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime reloads asgard profile for disabled layer-turn test");
    Chess3D_Clear(game);
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn));
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(2, Knight));
    Chess3D_RecomputeFusion(game);
    for (const TargetCell& home : HomeCellsForSide(2))
    {
        if (home.type == Pawn)
        {
            Chess3D_SetPiece(game, home.x, home.y, home.z, 2, Pawn);
        }
    }
    Chess3D_SetPiece(game, 0, 0, 0, 1, Rook);
    Chess3D_SetPiece(game, 0, 0, 3, 2, Pawn);
    Chess3D_TryMakeMove(game, 0, 0, 0, 0, 0, 3, 0, &played);
    const std::vector<int> asgardBoardBeforeLayerTurn = BoardSnapshot(game);
    const auto asgardStackBeforeLayerTurn = StackSnapshot(game, 2, 2, 2);
    const int asgardFusionBeforeLayerTurn = Chess3D_GetCoreFusionKind(game, 2, 2, 2);
    const int asgardReserveBeforeLayerTurn = Chess3D_GetReserveCount(game, 2, Pawn);
    test.Check(Chess3D_IsLayerTurnEnabled(game) == 0 &&
        Chess3D_CanRotateLayer(game, 0, 2, 1) == 0,
        "asgard convergence keeps ritual layer turns disabled");
    test.Check(Chess3D_RotateLayer(game, 0, 2, 1) == 0,
        "asgard convergence rejects layer turn without mutating state");
    int lastAxis = -2;
    int lastLayer = -2;
    int lastQuarterTurns = 0;
    int lastLayerResult = 0;
    test.Check(Chess3D_GetLastLayerTurnInfo(game, &lastAxis, &lastLayer, &lastQuarterTurns, &lastLayerResult) == 1 &&
        lastLayerResult == LayerTurnDisabled && ReadLayerTurnResultName(lastLayerResult) == "disabled",
        "disabled layer turn reports disabled result code");
    test.Check(BoardSnapshot(game) == asgardBoardBeforeLayerTurn &&
        SameStack(StackSnapshot(game, 2, 2, 2), asgardStackBeforeLayerTurn) &&
        Chess3D_GetCoreFusionKind(game, 2, 2, 2) == asgardFusionBeforeLayerTurn &&
        Chess3D_GetReserveCount(game, 2, Pawn) == asgardReserveBeforeLayerTurn,
        "disabled layer turn preserves projected board, stack, fusion, and reserve");

    test.Check(Chess3D_LoadRuleProfileJson(game, classicProfile.c_str()) == 1 &&
        Chess3D_IsLayerTurnEnabled(game) == 0 &&
        Chess3D_CanRotateLayer(game, 0, 0, 1) == 0,
        "classic profile does not enable ritual layer turns");
    test.Check(Chess3D_LoadRuleProfileJson(game, singleProfile.c_str()) == 1 &&
        Chess3D_IsLayerTurnEnabled(game) == 0 &&
        Chess3D_CanRotateLayer(game, 0, 0, 1) == 0,
        "single-side profile does not enable ritual layer turns");

    test.Check(Chess3D_LoadRuleProfileJson(game, rubikProfile.c_str()) == 1, "runtime loads rubik convergence profile");
    test.Check(Chess3D_IsCoreStackEnabled(game) == 1, "rubik convergence enables core stacks");
    test.Check(Chess3D_IsFusionEnabled(game) == 1, "rubik convergence enables fusion descriptors");
    test.Check(ReadAbiString(game, Chess3D_GetCurrentRulesetId) == "rubik-convergence-3d-8x8x8-v0.1",
        "runtime exposes rubik ruleset id");
    test.Check(ReadAbiString(game, Chess3D_GetGoalProfileType) == "centerAssembly",
        "runtime exposes rubik centerAssembly goal");
    test.Check(ReadAbiString(game, Chess3D_GetLayerTurnProfileType) == "ritualTurn",
        "runtime exposes rubik ritualTurn layer profile");
    test.Check(ReadAbiString(game, Chess3D_GetOccupancyProfileType) == "coreStack",
        "runtime exposes rubik coreStack occupancy profile");
    test.Check(ReadAbiString(game, Chess3D_GetFusionProfileType) == "stackFusion",
        "runtime exposes rubik stackFusion profile");
    test.Check(Chess3D_IsReserveEnabled(game) == 1 && Chess3D_IsKnockbackEnabled(game) == 1,
        "rubik convergence loads reserve and knockback runtime profiles");
    test.Check(Chess3D_IsLayerTurnEnabled(game) == 1 &&
        Chess3D_CanRotateLayer(game, 0, 2, 1) == 1 &&
        Chess3D_CanRotateLayer(game, 1, 3, -1) == 1 &&
        Chess3D_CanRotateLayer(game, 2, 4, 1) == 1 &&
        Chess3D_CanRotateLayer(game, 3, 2, 1) == 0 &&
        Chess3D_CanRotateLayer(game, 0, 8, 1) == 0 &&
        Chess3D_CanRotateLayer(game, 0, 2, 2) == 0,
        "rubik convergence validates axes, layers, and quarter turns");
    test.Check(ReadAbiString(game, Chess3D_GetLayerTurnProfileSummary).find("coreStacks=true") != std::string::npos,
        "rubik convergence layer-turn summary reports core stack movement");

    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 0, 0, 0, 1, Pawn);
    Chess3D_SetPiece(game, 1, 2, 0, 2, Knight);
    test.Check(Chess3D_RotateLayer(game, 0, 0, 1) == 1, "rubik profile rotates projected Z layer +1");
    test.Check(Chess3D_GetPiece(game, 7, 0, 0) == PieceCode(1, Pawn) &&
        Chess3D_GetPiece(game, 5, 1, 0) == PieceCode(2, Knight),
        "projected Z +1 coordinates follow documented transform");
    test.Check(Chess3D_RotateLayer(game, 0, 0, -1) == 1 &&
        Chess3D_GetPiece(game, 0, 0, 0) == PieceCode(1, Pawn) &&
        Chess3D_GetPiece(game, 1, 2, 0) == PieceCode(2, Knight),
        "projected Z -1 returns pieces to original cells");

    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 2, 3, 4, 1, Bishop);
    TargetCell rotated = RotateCell(1, 3, 1, 2, 3, 4, Bishop);
    test.Check(Chess3D_RotateLayer(game, 1, 3, 1) == 1 &&
        Chess3D_GetPiece(game, rotated.x, rotated.y, rotated.z) == PieceCode(1, Bishop),
        "projected Y layer rotation follows engine convention");
    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 4, 1, 2, 1, Queen);
    rotated = RotateCell(2, 4, 1, 4, 1, 2, Queen);
    test.Check(Chess3D_RotateLayer(game, 2, 4, 1) == 1 &&
        Chess3D_GetPiece(game, rotated.x, rotated.y, rotated.z) == PieceCode(1, Queen),
        "projected X layer rotation follows engine convention");

    Chess3D_Clear(game);
    test.Check(Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn)) == 1 &&
        Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Rook)) == 1 &&
        Chess3D_GetCoreFusionKind(game, 2, 2, 2) == FusionFriendlyPair,
        "rubik convergence evaluates stack fusion before layer turns");
    const auto originalStack = StackSnapshot(game, 2, 2, 2);
    const TargetCell movedStackCell = RotateCell(0, 2, 1, 2, 2, 2);
    test.Check(Chess3D_RotateLayer(game, 0, 2, 1) == 1 &&
        Chess3D_GetCoreStackCount(game, 2, 2, 2) == 0 &&
        SameStack(StackSnapshot(game, movedStackCell.x, movedStackCell.y, movedStackCell.z), originalStack) &&
        Chess3D_GetProjectedPiece(game, movedStackCell.x, movedStackCell.y, movedStackCell.z) == PieceCode(1, Rook),
        "rubik layer turn moves whole CoreCell stack and projected top piece");
    test.Check(Chess3D_GetCoreFusionKind(game, 2, 2, 2) == FusionNone &&
        Chess3D_GetCoreFusionKind(game, movedStackCell.x, movedStackCell.y, movedStackCell.z) == FusionFriendlyPair,
        "fusion descriptor follows moved core stack");

    Chess3D_Clear(game);
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn));
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Rook));
    Chess3D_PushCoreStackPiece(game, 3, 5, 2, PieceCode(2, King));
    Chess3D_PushCoreStackPiece(game, 3, 5, 2, PieceCode(2, Queen));
    const auto boardBeforeFourTurns = BoardSnapshot(game);
    const auto stackA = StackSnapshot(game, 2, 2, 2);
    const auto stackB = StackSnapshot(game, 3, 5, 2);
    for (int i = 0; i < 4; ++i)
    {
        test.Check(Chess3D_RotateLayer(game, 0, 2, 1) == 1, "four-turn identity step succeeds");
    }
    test.Check(BoardSnapshot(game) == boardBeforeFourTurns &&
        SameStack(StackSnapshot(game, 2, 2, 2), stackA) &&
        SameStack(StackSnapshot(game, 3, 5, 2), stackB),
        "four identical layer turns restore projected board and stacks");

    Chess3D_Clear(game);
    const TargetCell anchorBefore = TargetCellsForSide(1).front();
    Chess3D_PushCoreStackPiece(game, anchorBefore.x, anchorBefore.y, anchorBefore.z, PieceCode(1, anchorBefore.type));
    Chess3D_RecomputeAnchors(game);
    const int anchorCountBeforeTurn = Chess3D_GetAnchorCount(game, 1);
    test.Check(anchorCountBeforeTurn == 1, "rubik anchor is counted before rotating fixed world target slot away");
    test.Check(Chess3D_RotateLayer(game, 0, anchorBefore.z, 1) == 1 &&
        Chess3D_GetAnchorCount(game, 1) == 0,
        "anchor/victory recomputes after layer turn against fixed world target slots");

    Chess3D_Clear(game);
    for (const TargetCell& home : HomeCellsForSide(2))
    {
        if (home.type == Pawn)
        {
            Chess3D_SetPiece(game, home.x, home.y, home.z, 2, Pawn);
        }
    }
    Chess3D_SetPiece(game, 0, 0, 0, 1, Rook);
    Chess3D_SetPiece(game, 0, 0, 3, 2, Pawn);
    Chess3D_TryMakeMove(game, 0, 0, 0, 0, 0, 3, 0, &played);
    const int reserveBeforeTurn = Chess3D_GetReserveCount(game, 2, Pawn);
    test.Check(reserveBeforeTurn == 1, "rubik reserve setup creates reserve count before layer turn");
    test.Check(Chess3D_RotateLayer(game, 0, 0, 1) == 1 &&
        Chess3D_GetReserveCount(game, 2, Pawn) == reserveBeforeTurn,
        "rubik layer turn leaves reserve counts unaffected");
    test.Check(Chess3D_GetLastLayerTurnInfo(game, &lastAxis, &lastLayer, &lastQuarterTurns, &lastLayerResult) == 1 &&
        lastAxis == 0 && lastLayer == 0 && lastQuarterTurns == 1 && lastLayerResult == LayerTurnSuccess,
        "successful layer turn reports last axis/layer/quarter/result");
    test.Check(Chess3D_RotateLayer(game, 0, 0, 2) == 0 &&
        Chess3D_GetLastLayerTurnInfo(game, &lastAxis, &lastLayer, &lastQuarterTurns, &lastLayerResult) == 1 &&
        lastLayerResult == LayerTurnInvalidQuarterTurns,
        "invalid quarter turn reports invalidQuarterTurns result code");

    test.Check(Chess3D_LoadRuleProfileJson(game, classicProfile.c_str()) == 1 &&
        Chess3D_IsProjectionModeEnabled(game) == 0,
        "classic profile keeps Hodge projection mode disabled");
    const auto classicProjectionBefore = BoardSnapshot(game);
    const int classicProjectionActionBefore = Chess3D_GetActionCount(game);
    test.Check(Chess3D_TryMakeProjectedMove(game, 1, 3, 3, 0, 3, 3, 1, 0, &played) == 0 &&
        BoardSnapshot(game) == classicProjectionBefore &&
        Chess3D_GetActionCount(game) == classicProjectionActionBefore,
        "non-Hodge profile rejects projected move without mutation");
    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1 &&
        Chess3D_IsProjectionModeEnabled(game) == 0,
        "asgard profile keeps Hodge projection mode disabled");
    test.Check(Chess3D_LoadRuleProfileJson(game, rubikProfile.c_str()) == 1 &&
        Chess3D_IsProjectionModeEnabled(game) == 0,
        "rubik profile keeps Hodge projection mode disabled");

    test.Check(Chess3D_LoadRuleProfileJson(game, hodgeProfile.c_str()) == 1, "runtime loads hodge projection duel profile");
    test.Check(Chess3D_IsProjectionModeEnabled(game) == 1 &&
        Chess3D_GetProjectionMacroPlayerCount(game) == 2 &&
        Chess3D_GetProjectionCountForMacroPlayer(game, 1) == 3 &&
        Chess3D_GetProjectionCountForMacroPlayer(game, 2) == 3,
        "hodge runtime exposes two triune macro players");
    std::set<int> hodgeSides;
    for (int macro = 1; macro <= 2; ++macro)
    {
        for (int i = 0; i < 3; ++i)
        {
            hodgeSides.insert(Chess3D_GetProjectionSide(game, macro, i));
        }
    }
    test.Check(hodgeSides == std::set<int>({ 1, 2, 3, 4, 5, 6 }) &&
        Chess3D_GetMacroPlayerForSide(game, 1) == 1 &&
        Chess3D_GetMacroPlayerForSide(game, 3) == 1 &&
        Chess3D_GetMacroPlayerForSide(game, 5) == 1 &&
        Chess3D_GetMacroPlayerForSide(game, 2) == 2 &&
        Chess3D_GetMacroPlayerForSide(game, 4) == 2 &&
        Chess3D_GetMacroPlayerForSide(game, 6) == 2,
        "hodge macro-player groups cover all six sides exactly once");
    test.Check(ReadAbiString(game, Chess3D_GetProjectionProfileSummary).find("hodgeTriuneProjection") != std::string::npos,
        "hodge projection summary names hodgeTriuneProjection");

    int tfX = -1;
    int tfY = -1;
    int tfZ = -1;
    int ttX = -1;
    int ttY = -1;
    int ttZ = -1;
    test.Check(Chess3D_TransformMoveBetweenSides(game, 1, 3, 3, 3, 0, 3, 3, 1, &tfX, &tfY, &tfZ, &ttX, &ttY, &ttZ) == 1 &&
        tfX == 3 && tfY == 0 && tfZ == 3 &&
        ttX == 3 && ttY == 1 && ttZ == 3,
        "hodge transform maps side 1 forward move into side 3 local frame");
    int roundFromX = -1;
    int roundFromY = -1;
    int roundFromZ = -1;
    int roundToX = -1;
    int roundToY = -1;
    int roundToZ = -1;
    test.Check(Chess3D_TransformMoveBetweenSides(game, 3, 1, tfX, tfY, tfZ, ttX, ttY, ttZ, &roundFromX, &roundFromY, &roundFromZ, &roundToX, &roundToY, &roundToZ) == 1 &&
        roundFromX == 3 && roundFromY == 3 && roundFromZ == 0 &&
        roundToX == 3 && roundToY == 3 && roundToZ == 1,
        "hodge transform round-trips between side 1 and side 3");

    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 3, 3, 0, 1, Pawn);
    Chess3D_SetPiece(game, 3, 0, 3, 3, Pawn);
    Chess3D_SetPiece(game, 0, 3, 3, 5, Pawn);
    test.Check(Chess3D_TryMakeProjectedMove(game, 1, 3, 3, 0, 3, 3, 1, 0, &played) == 1 &&
        Chess3D_GetPiece(game, 3, 3, 1) == PieceCode(1, Pawn) &&
        Chess3D_GetPiece(game, 3, 1, 3) == PieceCode(3, Pawn) &&
        Chess3D_GetPiece(game, 1, 3, 3) == PieceCode(5, Pawn) &&
        Chess3D_GetActionCount(game) == 1 &&
        Chess3D_GetActionKind(game, 1) == ActionProjectionCompositeMove &&
        (Chess3D_GetActionFlags(game, 1) & ActionFlagWasProjection) != 0 &&
        ReadActionNotation(game, 1).find("HPD") != std::string::npos,
        "hodge projected move applies primary and two mirror moves as one composite action");

    Chess3D_LoadRuleProfileJson(game, hodgeProfile.c_str());
    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 3, 3, 0, 1, Pawn);
    Chess3D_SetPiece(game, 3, 0, 3, 3, Pawn);
    Chess3D_SetPiece(game, 3, 1, 3, 3, Knight);
    Chess3D_SetPiece(game, 0, 3, 3, 5, Pawn);
    const auto hodgeBlockedBefore = BoardSnapshot(game);
    const int hodgeActionBefore = Chess3D_GetActionCount(game);
    test.Check(Chess3D_TryMakeProjectedMove(game, 1, 3, 3, 0, 3, 3, 1, 0, &played) == 0 &&
        BoardSnapshot(game) == hodgeBlockedBefore &&
        Chess3D_GetActionCount(game) == hodgeActionBefore &&
        !ReadAbiString(game, Chess3D_GetLastProjectionError).empty(),
        "hodge all-or-nothing projected move rejects blocked mirror without mutation");

    Chess3D_LoadRuleProfileJson(game, hodgeProfile.c_str());
    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 2, 2, 0, 1, Rook);
    Chess3D_SetPiece(game, 2, 2, 1, 2, Pawn);
    Chess3D_SetPiece(game, 2, 0, 2, 3, Rook);
    Chess3D_SetPiece(game, 0, 2, 2, 5, Rook);
    test.Check(Chess3D_TryMakeProjectedMove(game, 1, 2, 2, 0, 2, 2, 1, 0, &played) == 1 &&
        Chess3D_GetPiece(game, 2, 2, 1) == PieceCode(1, Rook) &&
        Chess3D_GetActionKind(game, 1) == ActionProjectionCompositeMove &&
        Chess3D_GetActionCapturedPieceCode(game, 1) == PieceCode(2, Pawn) &&
        Chess3D_GetActionCaptureDestination(game, 1) == CaptureDestinationRemoved &&
        (Chess3D_GetActionFlags(game, 1) & ActionFlagWasCapture) != 0,
        "hodge projected composite action records classic capture without knockback");

    test.Check(Chess3D_LoadRuleProfileJson(game, singleProfile.c_str()) == 1, "action history test loads single-side profile");
    Chess3D_Reset(game);
    test.Check(Chess3D_GetActionCount(game) == 0, "Reset clears action history");
    test.Check(Chess3D_TryMakeMove(game, 0, 0, 0, 7, 7, 7, 0, &played) == 0 &&
        Chess3D_GetActionCount(game) == 0,
        "invalid move does not append action history");
    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 3, 3, 0, 1, Pawn);
    test.Check(Chess3D_TryMakeMove(game, 3, 3, 0, 3, 3, 1, 0, &played) == 1 &&
        Chess3D_GetActionCount(game) == 1 &&
        Chess3D_GetActionKind(game, 1) == ActionMove &&
        Chess3D_GetActionPieceCode(game, 1) == PieceCode(1, Pawn) &&
        Chess3D_GetActionFromX(game, 1) == 3 &&
        Chess3D_GetActionToZ(game, 1) == 1 &&
        ReadActionNotation(game, 1).find("#1 S1 MOVE P (3,3,0)->(3,3,1)") != std::string::npos &&
        ReadAbiString(game, Chess3D_GetLastActionNotation).find("MOVE") != std::string::npos,
        "successful move records deterministic action history and notation");
    char tinyNotation[4] = {};
    test.Check(Chess3D_GetActionNotation(game, 1, tinyNotation, static_cast<int>(sizeof(tinyNotation))) > 0,
        "action notation ABI is safe for small buffers");
    tinyNotation[0] = 'x';
    test.Check(Chess3D_GetActionNotation(game, 99, tinyNotation, static_cast<int>(sizeof(tinyNotation))) > 0 &&
        tinyNotation[0] == '\0',
        "invalid action notation index returns a safe empty string");

    test.Check(Chess3D_LoadRuleProfileJson(game, classicProfile.c_str()) == 1, "action history classic capture test loads classic profile");
    Chess3D_Clear(game);
    Chess3D_SetPiece(game, 0, 0, 0, 1, Rook);
    Chess3D_SetPiece(game, 0, 0, 3, 2, Pawn);
    test.Check(Chess3D_TryMakeMove(game, 0, 0, 0, 0, 0, 3, 0, &played) == 1 &&
        Chess3D_GetActionCount(game) == 1 &&
        Chess3D_GetActionKind(game, 1) == ActionMove &&
        Chess3D_GetActionCapturedPieceCode(game, 1) == PieceCode(2, Pawn) &&
        Chess3D_GetActionCaptureDestination(game, 1) == CaptureDestinationRemoved &&
        (Chess3D_GetActionFlags(game, 1) & ActionFlagWasCapture) != 0,
        "classic capture records removed capture destination in action history");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "action history asgard capture test loads asgard profile");
    Chess3D_Clear(game);
    for (const TargetCell& home : HomeCellsForSide(2))
    {
        if (home.type == Pawn)
        {
            Chess3D_SetPiece(game, home.x, home.y, home.z, 2, Pawn);
        }
    }
    Chess3D_SetPiece(game, 0, 0, 0, 1, Rook);
    Chess3D_SetPiece(game, 0, 0, 3, 2, Pawn);
    test.Check(Chess3D_TryMakeMove(game, 0, 0, 0, 0, 0, 3, 0, &played) == 1 &&
        Chess3D_GetActionCount(game) == 1 &&
        Chess3D_GetActionCapturedPieceCode(game, 1) == PieceCode(2, Pawn) &&
        Chess3D_GetActionCaptureDestination(game, 1) == CaptureDestinationReserve &&
        (Chess3D_GetActionFlags(game, 1) & ActionFlagWasKnockback) != 0,
        "asgard knockback reserve capture is recorded in action history");
    const int countBeforeInvalidRestore = Chess3D_GetActionCount(game);
    test.Check(Chess3D_RestoreReservePiece(game, 2, Pawn, 0, 0, 0) == 0 &&
        Chess3D_GetReserveCount(game, 2, Pawn) == 1 &&
        Chess3D_GetActionCount(game) == countBeforeInvalidRestore,
        "invalid reserve restore does not mutate reserve or action history");
    TargetCell freePawnHome{};
    bool foundPawnHome = false;
    for (const TargetCell& home : HomeCellsForSide(2))
    {
        if (home.type == Pawn)
        {
            freePawnHome = home;
            foundPawnHome = true;
            break;
        }
    }
    test.Check(foundPawnHome, "side 2 has a pawn home slot for reserve restore tests");
    Chess3D_SetPiece(game, freePawnHome.x, freePawnHome.y, freePawnHome.z, 0, 0);
    test.Check(Chess3D_CanRestoreReservePiece(game, 2, Pawn, freePawnHome.x, freePawnHome.y, freePawnHome.z) == 1 &&
        Chess3D_RestoreReservePiece(game, 2, Pawn, freePawnHome.x, freePawnHome.y, freePawnHome.z) == 1 &&
        Chess3D_GetReserveCount(game, 2, Pawn) == 0 &&
        Chess3D_GetPiece(game, freePawnHome.x, freePawnHome.y, freePawnHome.z) == PieceCode(2, Pawn) &&
        Chess3D_GetActionCount(game) == countBeforeInvalidRestore + 1 &&
        Chess3D_GetActionKind(game, countBeforeInvalidRestore + 1) == ActionReserveRestore &&
        (Chess3D_GetActionFlags(game, countBeforeInvalidRestore + 1) & ActionFlagWasReserveRestore) != 0 &&
        ReadActionNotation(game, countBeforeInvalidRestore + 1).find("RESTORE") != std::string::npos &&
        !ReadAbiString(game, Chess3D_GetLastReserveRestoreInfo).empty(),
        "reserve restore action decrements reserve, restores piece, and records notation");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "auto restore test reloads asgard profile");
    Chess3D_Clear(game);
    for (const TargetCell& home : HomeCellsForSide(2))
    {
        if (home.type == Pawn)
        {
            Chess3D_SetPiece(game, home.x, home.y, home.z, 2, Pawn);
        }
    }
    Chess3D_SetPiece(game, 0, 0, 0, 1, Rook);
    Chess3D_SetPiece(game, 0, 0, 3, 2, Pawn);
    Chess3D_TryMakeMove(game, 0, 0, 0, 0, 0, 3, 0, &played);
    Chess3D_SetPiece(game, freePawnHome.x, freePawnHome.y, freePawnHome.z, 0, 0);
    test.Check(Chess3D_AutoRestoreReservePiece(game, 2, Pawn) == 1 &&
        Chess3D_GetReserveCount(game, 2, Pawn) == 0 &&
        Chess3D_GetPiece(game, freePawnHome.x, freePawnHome.y, freePawnHome.z) == PieceCode(2, Pawn),
        "auto reserve restore uses first free matching home slot");
    test.Check(Chess3D_AutoRestoreReservePiece(game, 2, Pawn) == 0,
        "auto reserve restore clean-fails when reserve is empty");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "action history core move tests reload asgard profile");
    Chess3D_Clear(game);
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(2, Pawn));
    Chess3D_SetPiece(game, 2, 2, 1, 1, Rook);
    test.Check(Chess3D_TryMakeMove(game, 2, 2, 1, 2, 2, 2, 0, &played) == 1 &&
        (Chess3D_GetActionFlags(game, 1) & ActionFlagEnteredCore) != 0 &&
        Chess3D_GetActionCaptureDestination(game, 1) == CaptureDestinationCoreCoOccupancy,
        "outside-to-core move records enteredCore and core co-occupancy destination");
    Chess3D_Clear(game);
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn));
    Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Rook));
    test.Check(Chess3D_TryMakeMove(game, 2, 2, 2, 3, 2, 2, 0, &played) == 1 &&
        Chess3D_GetActionCount(game) == 1 &&
        Chess3D_GetActionKind(game, 1) == ActionMove,
        "core-to-core move records a move action");
    Chess3D_Clear(game);
    Chess3D_PushCoreStackPiece(game, 3, 2, 2, PieceCode(1, Pawn));
    Chess3D_PushCoreStackPiece(game, 3, 2, 2, PieceCode(1, Rook));
    test.Check(Chess3D_TryMakeMove(game, 3, 2, 2, 3, 2, 1, 0, &played) == 1 &&
        Chess3D_GetActionCount(game) == 1 &&
        (Chess3D_GetActionFlags(game, 1) & ActionFlagLeftCore) != 0,
        "core-to-outside move records leftCore flag");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "disabled layer turn action history test reloads asgard profile");
    Chess3D_Clear(game);
    test.Check(Chess3D_RotateLayer(game, 0, 2, 1) == 0 &&
        Chess3D_GetActionCount(game) == 0,
        "disabled layer turn does not append action history");
    test.Check(Chess3D_LoadRuleProfileJson(game, rubikProfile.c_str()) == 1, "layer turn action history test loads rubik profile");
    Chess3D_Clear(game);
    test.Check(Chess3D_RotateLayer(game, 0, 2, 1) == 1 &&
        Chess3D_GetActionCount(game) == 1 &&
        Chess3D_GetActionKind(game, 1) == ActionLayerTurn &&
        Chess3D_GetActionAxis(game, 1) == 0 &&
        Chess3D_GetActionLayer(game, 1) == 2 &&
        Chess3D_GetActionQuarterTurns(game, 1) == 1 &&
        (Chess3D_GetActionFlags(game, 1) & ActionFlagWasLayerTurn) != 0 &&
        ReadActionNotation(game, 1).find("#1 LAYER Z[2]+") != std::string::npos,
        "successful rubik layer turn records layer action notation");
    char nameBuffer[64] = {};
    test.Check(Chess3D_GetActionKindName(ActionReserveRestore, nameBuffer, static_cast<int>(sizeof(nameBuffer))) > 0 &&
        std::string(nameBuffer) == "reserveRestore" &&
        Chess3D_GetCaptureDestinationName(CaptureDestinationReserve, nameBuffer, static_cast<int>(sizeof(nameBuffer))) > 0 &&
        std::string(nameBuffer) == "reserve",
        "action and capture destination name helpers are exposed");

    Chess3D_Destroy(game);
    return test.Finish("Chess3DEngineContractTests");
}
