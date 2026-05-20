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
    return JsonParser(json).Parse() &&
        !rulesetId.empty() &&
        ValidateBoardProfile(json) &&
        IsAllowed(goal, { "classicCheckmate", "centerAssembly", "hybrid", "sandbox", "centerAssemblyTraining" }) &&
        IsAllowed(capture, { "classicCapture", "knockbackCapture" }) &&
        IsAllowed(occupancy, { "exclusive", "coreStack", "quantumCore" }) &&
        IsAllowed(fusion, { "none", "anchorOnly", "pairFusion", "stackFusion", "colorPermutation", "volumeSurface216" }) &&
        IsAllowed(layerTurn, { "disabled", "ritualTurn", "globalEvent", "sandbox" });
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
    test.Check(!classicProfile.empty(), "classic six-side profile exists");
    test.Check(!singleProfile.empty(), "single-side profile exists");
    test.Check(!asgardProfile.empty(), "asgard convergence profile exists");
    test.Check(!rubikProfile.empty(), "rubik convergence profile exists");

    test.Check(ValidateCommonRuleProfile(classicProfile), "classic six-side profile passes common validation");
    test.Check(ValidateCommonRuleProfile(singleProfile), "single-side profile passes common validation");
    test.Check(ValidateCommonRuleProfile(asgardProfile), "asgard convergence profile passes common validation");
    test.Check(ValidateCommonRuleProfile(rubikProfile), "rubik convergence profile passes common validation");

    test.Check(ExtractStringValue(classicProfile, "rulesetId") == "classic-six-side-3d-8x8x8-v0.1",
        "classic six-side ruleset id matches");
    test.Check(ExtractStringValue(singleProfile, "rulesetId") == "single-side-3d-8x8x8-v0.1",
        "single-side profile ruleset id matches");
    test.Check(ExtractStringValue(asgardProfile, "rulesetId") == "asgard-convergence-3d-8x8x8-v0.1",
        "asgard convergence ruleset id matches");
    test.Check(ExtractStringValue(rubikProfile, "rulesetId") == "rubik-convergence-3d-8x8x8-v0.1",
        "rubik convergence ruleset id matches");

    test.Check(ExtractStringValue(ExtractObject(classicProfile, "goalProfile"), "type") == "classicCheckmate",
        "classic six-side goalProfile is classicCheckmate");
    test.Check(ExtractStringValue(ExtractObject(classicProfile, "captureProfile"), "type") == "classicCapture",
        "classic six-side captureProfile is classicCapture");
    test.Check(ExtractStringValue(ExtractObject(classicProfile, "occupancyProfile"), "type") == "exclusive",
        "classic six-side occupancyProfile is exclusive");
    test.Check(ExtractStringValue(ExtractObject(classicProfile, "fusionProfile"), "type") == "none",
        "classic six-side fusionProfile is none");
    test.Check(ExtractStringValue(ExtractObject(classicProfile, "layerTurnProfile"), "type") == "disabled",
        "classic six-side layerTurnProfile is disabled");

    test.Check(ExtractStringValue(ExtractObject(singleProfile, "setupProfile"), "baseSetup") == "central4x4",
        "single-side profile references central4x4 setup");
    test.Check(ExtractStringValue(ExtractObject(singleProfile, "goalProfile"), "type") == "sandbox",
        "single-side profile is sandbox goal mode");
    test.Check(ExtractStringValue(ExtractObject(singleProfile, "occupancyProfile"), "type") == "exclusive",
        "single-side occupancyProfile is exclusive");
    test.Check(ExtractStringValue(ExtractObject(singleProfile, "fusionProfile"), "type") == "none",
        "single-side fusionProfile is none");

    test.Check(ExtractStringValue(ExtractObject(asgardProfile, "goalProfile"), "type") == "centerAssembly",
        "asgard convergence goalProfile is centerAssembly");
    test.Check(ValidateCoreCube(asgardProfile, 2, 5), "asgard convergence coreCube is x/y/z 2..5");
    test.Check(ExtractStringValue(ExtractObject(asgardProfile, "captureProfile"), "type") == "knockbackCapture",
        "asgard convergence captureProfile is knockbackCapture");
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
    test.Check(asgardProfile.find("Forbidden Core / Asgard / Meru") != std::string::npos,
        "asgard convergence mythProfile names the forbidden core");

    test.Check(ExtractStringValue(rubikProfile, "baseRuleset") == "asgard-convergence-3d-8x8x8-v0.1",
        "rubik convergence points to asgard convergence baseRuleset");
    test.Check(ExtractStringValue(ExtractObject(rubikProfile, "occupancyProfile"), "type") == "coreStack",
        "rubik convergence occupancyProfile is coreStack");
    test.Check(ExtractStringValue(ExtractObject(rubikProfile, "fusionProfile"), "type") == "stackFusion",
        "rubik convergence fusionProfile is stackFusion");
    test.Check(ExtractStringValue(ExtractObject(rubikProfile, "implosionProfile"), "mode") == "progressState",
        "rubik convergence implosion is progress state");
    const std::string rubikLayerTurn = ExtractObject(rubikProfile, "layerTurnProfile");
    test.Check(ExtractStringValue(rubikLayerTurn, "type") == "ritualTurn",
        "rubik convergence layerTurnProfile is ritualTurn");
    test.Check(rubikLayerTurn.find("\"X\"") != std::string::npos &&
        rubikLayerTurn.find("\"Y\"") != std::string::npos &&
        rubikLayerTurn.find("\"Z\"") != std::string::npos,
        "rubik convergence ritualTurn axes include X/Y/Z");
    const std::string layerRange = ExtractObject(rubikLayerTurn, "layerRange");
    int minLayer = -1;
    int maxLayer = -1;
    test.Check(ExtractIntValue(layerRange, "min", minLayer) && ExtractIntValue(layerRange, "max", maxLayer) &&
        minLayer == 0 && maxLayer == 7,
        "rubik convergence ritualTurn layers are 0..7");
    test.Check(rubikLayerTurn.find("-1") != std::string::npos && rubikLayerTurn.find("1") != std::string::npos,
        "rubik convergence ritualTurn quarter turns include -1 and +1");
    const std::string rubikCorePhysics = ExtractObject(rubikProfile, "corePhysicsProfile");
    test.Check(ExtractStringValue(rubikCorePhysics, "layerTurnStackInteraction") == "deferred",
        "rubik convergence defers stack/layer interaction");
    bool rubik216Enabled = true;
    test.Check(ExtractBoolValue(ExtractObject(rubikCorePhysics, "volumeSurface216Principle"), "enabled", rubik216Enabled) && !rubik216Enabled,
        "rubik convergence volumeSurface216Principle is disabled");
    test.Check(rubikProfile.find("Stack/layer interaction is deferred.") != std::string::npos,
        "rubik convergence knownLimitations mention stack/layer interaction deferred");

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

    test.Check(Chess3D_LoadRuleProfileJson(game, singleProfile.c_str()) == 1, "runtime loads single-side profile");
    test.Check(Chess3D_IsFusionEnabled(game) == 0, "single-side profile keeps fusion disabled");
    test.Check(ReadAbiString(game, Chess3D_GetCurrentRulesetId) == "single-side-3d-8x8x8-v0.1",
        "runtime exposes single-side ruleset id");
    test.Check(ReadAbiString(game, Chess3D_GetGoalProfileType) == "sandbox",
        "runtime exposes single-side sandbox goal");

    test.Check(Chess3D_LoadRuleProfileJson(game, asgardProfile.c_str()) == 1, "runtime loads asgard convergence profile");
    test.Check(Chess3D_IsCoreStackEnabled(game) == 1, "asgard profile enables core stacks");
    test.Check(Chess3D_IsFusionEnabled(game) == 1, "asgard profile enables fusion descriptors");
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
    Chess3D_Clear(game);
    test.Check(Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Pawn)) == 1 &&
        Chess3D_PushCoreStackPiece(game, 2, 2, 2, PieceCode(1, Rook)) == 1 &&
        Chess3D_GetCoreFusionKind(game, 2, 2, 2) == FusionFriendlyPair,
        "rubik convergence evaluates stack fusion before layer turns");
    test.Check(Chess3D_RotateLayer(game, 0, 2, 1) == 0 &&
        Chess3D_GetCoreFusionKind(game, 2, 2, 2) == FusionFriendlyPair,
        "rubik convergence keeps fusion stable after deferred stack rotation");

    Chess3D_Destroy(game);
    return test.Finish("Chess3DEngineContractTests");
}
