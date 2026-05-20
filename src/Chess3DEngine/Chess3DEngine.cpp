#include "Chess3DEngine.h"

#include <algorithm>
#include <array>
#include <cctype>
#include <cstring>
#include <initializer_list>
#include <limits>
#include <sstream>
#include <string>
#include <vector>

namespace
{
constexpr int BoardSize = 8;
constexpr int CellCount = BoardSize * BoardSize * BoardSize;

constexpr int Empty = 0;
constexpr int Pawn = 1;
constexpr int Knight = 2;
constexpr int Bishop = 3;
constexpr int Rook = 4;
constexpr int Queen = 5;
constexpr int King = 6;

constexpr int MoveCapture = 1;
constexpr int MovePromotion = 8;
constexpr int Infinity = 2000000;

constexpr std::array<int, 7> Material = { 0, 100, 320, 330, 500, 900, 0 };

enum FusionKind
{
    FusionNone = 0,
    FusionSingle = 1,
    FusionFriendlyPair = 2,
    FusionFriendlyStack = 3,
    FusionRoyalPair = 4,
    FusionContested = 5,
    FusionMixedStack = 6,
    FusionImplosionSeed = 7,
    FusionImplosionReady = 8
};

constexpr int FusionFlagContested = 1;
constexpr int FusionFlagRoyalPair = 2;
constexpr int FusionFlagAnchoredFusion = 4;
constexpr int FusionFlagImplosionSeed = 8;
constexpr int FusionFlagImplosionReady = 16;

enum KnockbackDestination
{
    KnockbackNone = 0,
    KnockbackHome = 1,
    KnockbackReserve = 2,
    KnockbackClassicRemoved = 3
};

struct Vec3
{
    int x = 0;
    int y = 0;
    int z = 0;
};

struct SideRule
{
    int id = 1;
    std::string name = "White";
    Vec3 forward{ 0, 0, 1 };
};

struct Rules
{
    int width = BoardSize;
    int height = BoardSize;
    int depth = BoardSize;
    int activeSideCount = 6;
    int movementProfile = 1; // 0 setup-only, 1 draft3d
    bool kingSafetyEnabled = false;
    int maxPiecesPerSide = 16;
    std::array<SideRule, 7> sides{};
    std::string rulesetId = "cube-chess-8x8x8-draft";
    std::string rulesetVersion = "draft";
    std::string rulesetDisplayName = "Cube Chess 8x8x8 Draft";
    std::string goalProfileType = "sandbox";
    std::string captureProfileType = "classicCapture";
    std::string knockbackProfileType = "none";
    std::string reserveProfileType = "none";
    std::string occupancyProfileType = "exclusive";
    std::string fusionProfileType = "none";
    std::string corePhysicsProfileType = "none";
    std::string layerTurnProfileType = "disabled";
    std::string victoryProfileType = "sandbox";
    std::string implosionProfileType = "none";
    std::string implosionProfileMode = "none";
    int coreXMin = 2;
    int coreXMax = 5;
    int coreYMin = 2;
    int coreYMax = 5;
    int coreZMin = 2;
    int coreZMax = 5;
    std::string anchorMode = "none";
    int requiredAnchorCount = 16;
    std::string json;
};

struct Move
{
    int from = -1;
    int to = -1;
    int piece = 0;
    int captured = 0;
    int promotionType = 0;
    int flags = 0;
    int score = 0;
};

struct Position
{
    std::array<int, CellCount> board{};
    int sideToMove = 1;
    Move lastMove{};
};

struct CoreStackEntry
{
    int side = 0;
    int pieceType = 0;
    int pieceCode = 0;
    int flags = 0;
};

struct CoreFusionState
{
    int fusionKind = FusionNone;
    int ownerSide = 0;
    int sideMask = 0;
    int entryCount = 0;
    int friendlyCount = 0;
    int enemyCount = 0;
    int dominantPieceType = 0;
    int flags = 0;
    int implosionStage = 0;
};

struct Game
{
    Rules rules;
    Position pos;
    std::array<std::vector<CoreStackEntry>, CellCount> coreStacks{};
    std::array<CoreFusionState, CellCount> fusionStates{};
    std::array<int, 7> anchorCounts{};
    std::array<int, 7> sideFusionCounts{};
    std::array<int, 7> sideRoyalPairCounts{};
    std::array<int, 7> sideContestedCounts{};
    std::array<int, 7> sideImplosionProgress{};
    std::array<std::array<int, 7>, 7> reserveCounts{};
    bool lastCaptureWasKnockback = false;
    int lastCapturedPieceCode = 0;
    int lastKnockbackDestination = KnockbackNone;
    int lastKnockbackHomeX = -1;
    int lastKnockbackHomeY = -1;
    int lastKnockbackHomeZ = -1;
    bool gameOver = false;
    int winnerSide = 0;
    std::string lastProfileLoadError;
    std::string lastInfo = "3D module ready.";
};

bool inside(int x, int y, int z)
{
    return x >= 0 && x < BoardSize && y >= 0 && y < BoardSize && z >= 0 && z < BoardSize;
}

int indexOf(int x, int y, int z)
{
    return z * 64 + y * 8 + x;
}

int xOf(int index)
{
    return index & 7;
}

int yOf(int index)
{
    return (index >> 3) & 7;
}

int zOf(int index)
{
    return index >> 6;
}

int makePiece(int side, int type)
{
    return side * 10 + type;
}

int pieceSide(int piece)
{
    return piece / 10;
}

int pieceType(int piece)
{
    return piece == 0 ? 0 : std::abs(piece % 10);
}

bool isSameSide(int left, int right)
{
    return left != 0 && right != 0 && pieceSide(left) == pieceSide(right);
}

bool isValidPieceCode(int piece)
{
    if (piece == Empty)
    {
        return true;
    }
    const int side = pieceSide(piece);
    const int type = pieceType(piece);
    return side >= 1 && side <= 6 && type >= Pawn && type <= King;
}

CoreStackEntry makeStackEntry(int pieceCode, int flags = 0)
{
    return CoreStackEntry{ pieceSide(pieceCode), pieceType(pieceCode), pieceCode, flags };
}

bool isInsideCore(const Rules& rules, int x, int y, int z)
{
    return x >= rules.coreXMin && x <= rules.coreXMax &&
        y >= rules.coreYMin && y <= rules.coreYMax &&
        z >= rules.coreZMin && z <= rules.coreZMax;
}

bool isInsideCore(const Rules& rules, int index)
{
    return isInsideCore(rules, xOf(index), yOf(index), zOf(index));
}

bool isCoreStackEnabled(const Rules& rules)
{
    return rules.occupancyProfileType == "coreStack" || rules.corePhysicsProfileType == "asgardCorePhysics";
}

bool isFusionEnabled(const Rules& rules)
{
    return isCoreStackEnabled(rules) && rules.fusionProfileType != "none";
}

bool isReserveEnabled(const Rules& rules)
{
    return rules.reserveProfileType == "sidePieceTypeCounts";
}

bool isKnockbackEnabled(const Rules& rules)
{
    return rules.captureProfileType == "knockbackCapture" &&
        rules.knockbackProfileType == "homeOrReserve" &&
        isReserveEnabled(rules);
}

std::string fusionKindName(int fusionKind)
{
    switch (fusionKind)
    {
    case FusionNone: return "none";
    case FusionSingle: return "single";
    case FusionFriendlyPair: return "friendlyPair";
    case FusionFriendlyStack: return "friendlyStack";
    case FusionRoyalPair: return "royalPair";
    case FusionContested: return "contested";
    case FusionMixedStack: return "mixedStack";
    case FusionImplosionSeed: return "implosionSeed";
    case FusionImplosionReady: return "implosionReady";
    default: return "unknown";
    }
}

char typeChar(int type)
{
    switch (type)
    {
    case Pawn: return 'P';
    case Knight: return 'N';
    case Bishop: return 'B';
    case Rook: return 'R';
    case Queen: return 'Q';
    case King: return 'K';
    default: return '?';
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

std::string defaultRulesJson()
{
    return R"({
  "name": "Cube Chess 8x8x8 Draft",
  "board": { "width": 8, "height": 8, "depth": 8 },
  "activeSideCount": 6,
  "maxPiecesPerSide": 16,
  "movementProfile": "draft3d",
  "kingSafety": false,
  "sides": [
    { "id": 1, "name": "White", "homeFace": "zMin", "forward": [0, 0, 1] },
    { "id": 2, "name": "Black", "homeFace": "zMax", "forward": [0, 0, -1] },
    { "id": 3, "name": "North", "homeFace": "yMin", "forward": [0, 1, 0] },
    { "id": 4, "name": "South", "homeFace": "yMax", "forward": [0, -1, 0] },
    { "id": 5, "name": "West", "homeFace": "xMin", "forward": [1, 0, 0] },
    { "id": 6, "name": "East", "homeFace": "xMax", "forward": [-1, 0, 0] }
  ],
  "draftRules": {
    "setup": "Each cube face uses its central 4x4 square. Pawns occupy the eight side-middle cells of that 4x4 ring; the other eight classic pieces occupy the four corners plus central 2x2.",
    "topology": "Six armies live on the six axis-aligned boundary faces of an 8x8x8 integer lattice. Opposite faces are paired by cube symmetry; adjacent faces meet through the shared volume rather than overlapping starts."
  },
  "notes": [
    "Draft module: placement and configurable rule profile are stable; exact game law is intentionally editable.",
    "Piece code is side * 10 + classic piece type. Up to six cube-face sides are reserved."
  ]
})";
}

std::string lowerCopy(std::string value)
{
    std::transform(value.begin(), value.end(), value.begin(), [](unsigned char ch)
    {
        return static_cast<char>(std::tolower(ch));
    });
    return value;
}

int extractInt(const std::string& json, const std::string& key, int fallback)
{
    const auto keyPos = json.find("\"" + key + "\"");
    if (keyPos == std::string::npos)
    {
        return fallback;
    }
    const auto colon = json.find(':', keyPos);
    if (colon == std::string::npos)
    {
        return fallback;
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
        return fallback;
    }
    int value = 0;
    while (pos < json.size() && std::isdigit(static_cast<unsigned char>(json[pos])))
    {
        value = value * 10 + (json[pos] - '0');
        ++pos;
    }
    return sign * value;
}

bool parseIntAt(const std::string& text, std::size_t& pos, int& value)
{
    while (pos < text.size() && std::isspace(static_cast<unsigned char>(text[pos])))
    {
        ++pos;
    }

    int sign = 1;
    if (pos < text.size() && text[pos] == '-')
    {
        sign = -1;
        ++pos;
    }
    if (pos >= text.size() || !std::isdigit(static_cast<unsigned char>(text[pos])))
    {
        return false;
    }

    int parsed = 0;
    while (pos < text.size() && std::isdigit(static_cast<unsigned char>(text[pos])))
    {
        parsed = parsed * 10 + (text[pos] - '0');
        ++pos;
    }
    value = parsed * sign;
    return true;
}

bool extractBool(const std::string& json, const std::string& key, bool fallback)
{
    const auto keyPos = json.find("\"" + key + "\"");
    if (keyPos == std::string::npos)
    {
        return fallback;
    }
    const auto colon = json.find(':', keyPos);
    if (colon == std::string::npos)
    {
        return fallback;
    }
    const auto rest = lowerCopy(json.substr(colon + 1, 8));
    if (rest.find("true") != std::string::npos)
    {
        return true;
    }
    if (rest.find("false") != std::string::npos)
    {
        return false;
    }
    return fallback;
}

bool hasJsonObjectEnvelope(const std::string& json)
{
    const auto first = json.find_first_not_of(" \t\r\n");
    if (first == std::string::npos)
    {
        return true;
    }
    const auto last = json.find_last_not_of(" \t\r\n");
    return json[first] == '{' && json[last] == '}';
}

std::string extractString(const std::string& json, const std::string& key, const std::string& fallback)
{
    const auto keyPos = json.find("\"" + key + "\"");
    if (keyPos == std::string::npos)
    {
        return fallback;
    }
    const auto colon = json.find(':', keyPos);
    if (colon == std::string::npos)
    {
        return fallback;
    }
    const auto first = json.find('"', colon + 1);
    if (first == std::string::npos)
    {
        return fallback;
    }
    const auto second = json.find('"', first + 1);
    if (second == std::string::npos)
    {
        return fallback;
    }
    return json.substr(first + 1, second - first - 1);
}

std::size_t findMatchingDelimiter(const std::string& text, std::size_t open, char left, char right)
{
    if (open >= text.size() || text[open] != left)
    {
        return std::string::npos;
    }

    int depth = 0;
    bool inString = false;
    bool escaped = false;
    for (std::size_t pos = open; pos < text.size(); ++pos)
    {
        const char ch = text[pos];
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
            continue;
        }
        if (ch == left)
        {
            ++depth;
        }
        else if (ch == right)
        {
            --depth;
            if (depth == 0)
            {
                return pos;
            }
        }
    }
    return std::string::npos;
}

std::string extractObject(const std::string& json, const std::string& key)
{
    const auto keyPos = json.find("\"" + key + "\"");
    if (keyPos == std::string::npos)
    {
        return {};
    }
    const auto colon = json.find(':', keyPos);
    if (colon == std::string::npos)
    {
        return {};
    }
    const auto open = json.find('{', colon + 1);
    if (open == std::string::npos)
    {
        return {};
    }
    const auto close = findMatchingDelimiter(json, open, '{', '}');
    if (close == std::string::npos)
    {
        return {};
    }
    return json.substr(open, close - open + 1);
}

std::string extractArray(const std::string& json, const std::string& key)
{
    const auto keyPos = json.find("\"" + key + "\"");
    if (keyPos == std::string::npos)
    {
        return {};
    }
    const auto colon = json.find(':', keyPos);
    if (colon == std::string::npos)
    {
        return {};
    }
    const auto open = json.find('[', colon + 1);
    if (open == std::string::npos)
    {
        return {};
    }
    const auto close = findMatchingDelimiter(json, open, '[', ']');
    if (close == std::string::npos)
    {
        return {};
    }
    return json.substr(open, close - open + 1);
}

bool stringInSet(const std::string& value, std::initializer_list<const char*> allowed)
{
    return std::find_if(allowed.begin(), allowed.end(), [&](const char* item)
    {
        return value == item;
    }) != allowed.end();
}

std::string profileTypeOrFallback(const std::string& json, const std::string& objectKey, const std::string& fallback)
{
    const std::string object = extractObject(json, objectKey);
    if (object.empty())
    {
        return fallback;
    }
    return extractString(object, "type", fallback);
}

bool extractCoreCube(const std::string& json, Rules& rules)
{
    const std::string coreProfile = extractObject(json, "coreProfile");
    const std::string coreCube = extractObject(coreProfile.empty() ? json : coreProfile, "coreCube");
    if (coreCube.empty())
    {
        return true;
    }

    const int xMin = extractInt(coreCube, "xMin", rules.coreXMin);
    const int xMax = extractInt(coreCube, "xMax", rules.coreXMax);
    const int yMin = extractInt(coreCube, "yMin", rules.coreYMin);
    const int yMax = extractInt(coreCube, "yMax", rules.coreYMax);
    const int zMin = extractInt(coreCube, "zMin", rules.coreZMin);
    const int zMax = extractInt(coreCube, "zMax", rules.coreZMax);
    if (xMin < 0 || xMax >= BoardSize || xMin > xMax ||
        yMin < 0 || yMax >= BoardSize || yMin > yMax ||
        zMin < 0 || zMax >= BoardSize || zMin > zMax)
    {
        return false;
    }

    rules.coreXMin = xMin;
    rules.coreXMax = xMax;
    rules.coreYMin = yMin;
    rules.coreYMax = yMax;
    rules.coreZMin = zMin;
    rules.coreZMax = zMax;
    return true;
}

bool parseRuleProfileMetadata(Rules& rules, std::string& error)
{
    const std::string& json = rules.json;
    const std::string rulesetId = extractString(json, "rulesetId", "");
    if (rulesetId.empty())
    {
        error = "RuleProfile rejected: rulesetId is required.";
        return false;
    }

    rules.rulesetId = rulesetId;
    rules.rulesetVersion = extractString(json, "version", "");
    rules.rulesetDisplayName = extractString(json, "displayName", rules.rulesetId);
    rules.goalProfileType = profileTypeOrFallback(json, "goalProfile", "");
    rules.captureProfileType = profileTypeOrFallback(json, "captureProfile", "");
    rules.knockbackProfileType = profileTypeOrFallback(json, "knockbackProfile", "none");
    rules.reserveProfileType = profileTypeOrFallback(json, "reserveProfile", "none");
    rules.occupancyProfileType = profileTypeOrFallback(json, "occupancyProfile", "");
    rules.fusionProfileType = profileTypeOrFallback(json, "fusionProfile", "");
    rules.corePhysicsProfileType = profileTypeOrFallback(json, "corePhysicsProfile", "none");
    rules.layerTurnProfileType = profileTypeOrFallback(json, "layerTurnProfile", "");
    rules.victoryProfileType = profileTypeOrFallback(json, "victoryProfile", "sandbox");
    rules.implosionProfileType = profileTypeOrFallback(json, "implosionProfile", "none");
    rules.implosionProfileMode = extractString(extractObject(json, "implosionProfile"), "mode", "none");
    rules.anchorMode = extractString(extractObject(json, "coreProfile"), "anchorMode", "none");
    rules.requiredAnchorCount = std::clamp(extractInt(extractObject(json, "victoryProfile"), "requiredPieceCount", 16), 1, 96);

    if (!stringInSet(rules.goalProfileType, { "sandbox", "centerAssembly", "centerAssemblyTraining", "classicCheckmate", "hybrid" }))
    {
        error = "RuleProfile rejected: unsupported goalProfile.type.";
        return false;
    }
    if (!stringInSet(rules.captureProfileType, { "classicCapture", "knockbackCapture" }))
    {
        error = "RuleProfile rejected: unsupported captureProfile.type.";
        return false;
    }
    if (!stringInSet(rules.knockbackProfileType, { "none", "homeOrReserve" }))
    {
        error = "RuleProfile rejected: unsupported knockbackProfile.type.";
        return false;
    }
    if (!stringInSet(rules.reserveProfileType, { "none", "disabled", "sidePieceTypeCounts" }))
    {
        error = "RuleProfile rejected: unsupported reserveProfile.type.";
        return false;
    }
    if (!stringInSet(rules.occupancyProfileType, { "exclusive", "coreStack", "quantumCore" }))
    {
        error = "RuleProfile rejected: unsupported occupancyProfile.type.";
        return false;
    }
    if (!stringInSet(rules.fusionProfileType, { "none", "anchorOnly", "pairFusion", "stackFusion", "colorPermutation", "volumeSurface216" }))
    {
        error = "RuleProfile rejected: unsupported fusionProfile.type.";
        return false;
    }
    if (!stringInSet(rules.corePhysicsProfileType, { "none", "asgardCorePhysics" }))
    {
        error = "RuleProfile rejected: unsupported corePhysicsProfile.type.";
        return false;
    }
    if (!stringInSet(rules.layerTurnProfileType, { "disabled", "ritualTurn", "globalEvent", "sandbox" }))
    {
        error = "RuleProfile rejected: unsupported layerTurnProfile.type.";
        return false;
    }
    if (!stringInSet(rules.victoryProfileType, { "sandbox", "checkmate", "allPiecesAnchored", "requiredPieceCount", "kingOnly", "percentageThreshold", "hybrid" }))
    {
        error = "RuleProfile rejected: unsupported victoryProfile.type.";
        return false;
    }
    if (!stringInSet(rules.implosionProfileType, { "none", "centerCompletion" }))
    {
        error = "RuleProfile rejected: unsupported implosionProfile.type.";
        return false;
    }
    if (!extractCoreCube(json, rules))
    {
        error = "RuleProfile rejected: coreCube bounds are invalid.";
        return false;
    }

    const std::string setup = extractObject(json, "setupProfile");
    const std::string setupType = extractString(setup, "type", "");
    const std::string homeFaces = extractArray(setup, "homeFaces");
    if (setupType.find("sixSide") != std::string::npos || homeFaces.find("X7") != std::string::npos)
    {
        rules.activeSideCount = 6;
    }
    else if (rules.rulesetId.find("single-side") != std::string::npos || setupType.find("single") != std::string::npos)
    {
        rules.activeSideCount = 1;
    }

    return true;
}

bool extractSideForward(const std::string& json, int side, Vec3& forward)
{
    std::size_t pos = 0;
    while ((pos = json.find("\"id\"", pos)) != std::string::npos)
    {
        const auto colon = json.find(':', pos);
        if (colon == std::string::npos)
        {
            return false;
        }

        auto idPos = colon + 1;
        int id = 0;
        if (!parseIntAt(json, idPos, id))
        {
            pos = colon + 1;
            continue;
        }
        const auto nextSide = json.find("\"id\"", idPos);
        if (id != side)
        {
            pos = nextSide == std::string::npos ? idPos : nextSide;
            continue;
        }

        const auto forwardKey = json.find("\"forward\"", idPos);
        if (forwardKey == std::string::npos || (nextSide != std::string::npos && forwardKey > nextSide))
        {
            return false;
        }
        const auto open = json.find('[', forwardKey);
        const auto close = json.find(']', open);
        if (open == std::string::npos || close == std::string::npos)
        {
            return false;
        }

        auto valuePos = open + 1;
        int values[3] = {};
        for (int i = 0; i < 3; ++i)
        {
            if (!parseIntAt(json, valuePos, values[i]))
            {
                return false;
            }
            const auto comma = json.find_first_of(",]", valuePos);
            if (comma == std::string::npos || comma > close)
            {
                return false;
            }
            valuePos = comma + 1;
        }

        const int length = std::abs(values[0]) + std::abs(values[1]) + std::abs(values[2]);
        if (length != 1)
        {
            return false;
        }
        forward = Vec3{ values[0], values[1], values[2] };
        return true;
    }
    return false;
}

void setDefaultSideRules(Rules& rules)
{
    rules.sides[1] = SideRule{ 1, "White", Vec3{ 0, 0, 1 } };
    rules.sides[2] = SideRule{ 2, "Black", Vec3{ 0, 0, -1 } };
    rules.sides[3] = SideRule{ 3, "North", Vec3{ 0, 1, 0 } };
    rules.sides[4] = SideRule{ 4, "South", Vec3{ 0, -1, 0 } };
    rules.sides[5] = SideRule{ 5, "West", Vec3{ 1, 0, 0 } };
    rules.sides[6] = SideRule{ 6, "East", Vec3{ -1, 0, 0 } };
}

void loadRules(Rules& rules, const std::string& json)
{
    rules = Rules{};
    setDefaultSideRules(rules);
    rules.json = json.empty() ? defaultRulesJson() : json;
    rules.width = std::clamp(extractInt(rules.json, "width", BoardSize), 2, BoardSize);
    rules.height = std::clamp(extractInt(rules.json, "height", BoardSize), 2, BoardSize);
    rules.depth = std::clamp(extractInt(rules.json, "depth", BoardSize), 2, BoardSize);
    rules.activeSideCount = std::clamp(extractInt(rules.json, "activeSideCount", 2), 1, 6);
    rules.maxPiecesPerSide = std::clamp(extractInt(rules.json, "maxPiecesPerSide", 16), 1, 64);
    rules.kingSafetyEnabled = extractBool(rules.json, "kingSafety", false);

    const auto profile = lowerCopy(extractString(rules.json, "movementProfile", "draft3d"));
    rules.movementProfile = profile.find("setup") != std::string::npos ? 0 : 1;
    for (int side = 1; side <= 6; ++side)
    {
        Vec3 forward = rules.sides[side].forward;
        if (extractSideForward(rules.json, side, forward))
        {
            rules.sides[side].forward = forward;
        }
    }

    std::string ignoredError;
    if (rules.json.find("\"rulesetId\"") != std::string::npos)
    {
        Rules candidate = rules;
        if (parseRuleProfileMetadata(candidate, ignoredError))
        {
            rules = candidate;
        }
    }
}

void clear(Position& pos)
{
    pos.board.fill(Empty);
    pos.sideToMove = 1;
    pos.lastMove = Move{};
}

int projectedPiece(const Game& game, int index)
{
    const auto& stack = game.coreStacks[static_cast<std::size_t>(index)];
    return !stack.empty() ? stack.back().pieceCode : game.pos.board[static_cast<std::size_t>(index)];
}

void syncProjectedPiece(Game& game, int index)
{
    const auto& stack = game.coreStacks[static_cast<std::size_t>(index)];
    game.pos.board[static_cast<std::size_t>(index)] = stack.empty() ? Empty : stack.back().pieceCode;
}

void clearCoreStacks(Game& game)
{
    for (auto& stack : game.coreStacks)
    {
        stack.clear();
    }
}

void clearFusionStates(Game& game)
{
    for (auto& state : game.fusionStates)
    {
        state = CoreFusionState{};
    }
    game.sideFusionCounts.fill(0);
    game.sideRoyalPairCounts.fill(0);
    game.sideContestedCounts.fill(0);
    game.sideImplosionProgress.fill(0);
}

void clearReserveState(Game& game)
{
    for (auto& sideCounts : game.reserveCounts)
    {
        sideCounts.fill(0);
    }
    game.lastCaptureWasKnockback = false;
    game.lastCapturedPieceCode = 0;
    game.lastKnockbackDestination = KnockbackNone;
    game.lastKnockbackHomeX = -1;
    game.lastKnockbackHomeY = -1;
    game.lastKnockbackHomeZ = -1;
}

void clearLastCaptureState(Game& game)
{
    game.lastCaptureWasKnockback = false;
    game.lastCapturedPieceCode = 0;
    game.lastKnockbackDestination = KnockbackNone;
    game.lastKnockbackHomeX = -1;
    game.lastKnockbackHomeY = -1;
    game.lastKnockbackHomeZ = -1;
}

void clearGamePosition(Game& game)
{
    clear(game.pos);
    clearCoreStacks(game);
    clearFusionStates(game);
    clearReserveState(game);
}

bool setCoreStackSingle(Game& game, int index, int pieceCode)
{
    if (!isCoreStackEnabled(game.rules) || !isInsideCore(game.rules, index) || !isValidPieceCode(pieceCode))
    {
        return false;
    }
    auto& stack = game.coreStacks[static_cast<std::size_t>(index)];
    stack.clear();
    if (pieceCode != Empty)
    {
        stack.push_back(makeStackEntry(pieceCode));
    }
    syncProjectedPiece(game, index);
    return true;
}

bool pushCoreStackPiece(Game& game, int index, int pieceCode)
{
    if (!isCoreStackEnabled(game.rules) || !isInsideCore(game.rules, index) || !isValidPieceCode(pieceCode) || pieceCode == Empty)
    {
        return false;
    }
    auto& stack = game.coreStacks[static_cast<std::size_t>(index)];
    if (stack.empty() && game.pos.board[static_cast<std::size_t>(index)] != Empty)
    {
        stack.push_back(makeStackEntry(game.pos.board[static_cast<std::size_t>(index)]));
    }
    stack.push_back(makeStackEntry(pieceCode));
    syncProjectedPiece(game, index);
    return true;
}

bool removeCoreStackEntry(Game& game, int index, int stackIndex, CoreStackEntry* removed = nullptr)
{
    if (!isCoreStackEnabled(game.rules) || !isInsideCore(game.rules, index))
    {
        return false;
    }
    auto& stack = game.coreStacks[static_cast<std::size_t>(index)];
    if (stackIndex < 0 || stackIndex >= static_cast<int>(stack.size()))
    {
        return false;
    }
    if (removed != nullptr)
    {
        *removed = stack[static_cast<std::size_t>(stackIndex)];
    }
    stack.erase(stack.begin() + stackIndex);
    syncProjectedPiece(game, index);
    return true;
}

Vec3 faceCenterSquare(int side, int localU, int localV)
{
    const int u = 2 + localU;
    const int v = 2 + localV;
    switch (side)
    {
    case 1: return { u, v, 0 };
    case 2: return { 7 - u, 7 - v, 7 };
    case 3: return { u, 0, v };
    case 4: return { 7 - u, 7, 7 - v };
    case 5: return { 0, u, v };
    case 6: return { 7, 7 - u, 7 - v };
    default: return { u, v, 0 };
    }
}

void placeFaceCenter(Position& pos, int side)
{
    constexpr std::array<std::array<int, 2>, 8> pawnCells = { {
        { 1, 0 }, { 2, 0 }, { 0, 1 }, { 0, 2 }, { 3, 1 }, { 3, 2 }, { 1, 3 }, { 2, 3 }
    } };
    constexpr std::array<std::array<int, 3>, 8> pieceCells = { {
        { 0, 0, Rook }, { 3, 0, Knight }, { 0, 3, Knight }, { 3, 3, Rook },
        { 1, 1, Bishop }, { 2, 1, Bishop }, { 1, 2, Queen }, { 2, 2, King }
    } };

    for (const auto& cell : pawnCells)
    {
        const Vec3 p = faceCenterSquare(side, cell[0], cell[1]);
        pos.board[indexOf(p.x, p.y, p.z)] = makePiece(side, Pawn);
    }
    for (const auto& cell : pieceCells)
    {
        const Vec3 p = faceCenterSquare(side, cell[0], cell[1]);
        pos.board[indexOf(p.x, p.y, p.z)] = makePiece(side, cell[2]);
    }
}

int central4x4TargetType(int localU, int localV)
{
    constexpr int pattern[4][4] = {
        { Rook, Pawn, Pawn, Knight },
        { Pawn, Bishop, Bishop, Pawn },
        { Pawn, Queen, King, Pawn },
        { Knight, Pawn, Pawn, Rook }
    };
    if (localU < 0 || localU > 3 || localV < 0 || localV > 3)
    {
        return Empty;
    }
    return pattern[localV][localU];
}

int targetSlotType(const Rules& rules, int side, int x, int y, int z)
{
    if (side < 1 || side > 6)
    {
        return Empty;
    }

    int localU = -1;
    int localV = -1;
    switch (side)
    {
    case 1:
        if (z != rules.coreZMin) return Empty;
        localU = x - rules.coreXMin;
        localV = y - rules.coreYMin;
        break;
    case 2:
        if (z != rules.coreZMax) return Empty;
        localU = x - rules.coreXMin;
        localV = y - rules.coreYMin;
        break;
    case 3:
        if (y != rules.coreYMin) return Empty;
        localU = x - rules.coreXMin;
        localV = z - rules.coreZMin;
        break;
    case 4:
        if (y != rules.coreYMax) return Empty;
        localU = x - rules.coreXMin;
        localV = z - rules.coreZMin;
        break;
    case 5:
        if (x != rules.coreXMin) return Empty;
        localU = y - rules.coreYMin;
        localV = z - rules.coreZMin;
        break;
    case 6:
        if (x != rules.coreXMax) return Empty;
        localU = y - rules.coreYMin;
        localV = z - rules.coreZMin;
        break;
    default:
        return Empty;
    }
    return central4x4TargetType(localU, localV);
}

bool findFreeHomeSlot(const Game& game, int pieceCode, int excludedIndex, int& homeIndex)
{
    const int side = pieceSide(pieceCode);
    const int type = pieceType(pieceCode);
    if (side < 1 || side > 6 || type < Pawn || type > King)
    {
        return false;
    }

    for (int localV = 0; localV < 4; ++localV)
    {
        for (int localU = 0; localU < 4; ++localU)
        {
            if (central4x4TargetType(localU, localV) != type)
            {
                continue;
            }
            const Vec3 home = faceCenterSquare(side, localU, localV);
            const int index = indexOf(home.x, home.y, home.z);
            if (index == excludedIndex)
            {
                continue;
            }
            if (game.pos.board[static_cast<std::size_t>(index)] == Empty)
            {
                homeIndex = index;
                return true;
            }
        }
    }
    return false;
}

void routeCapturedPiece(Game& game, int capturedPiece, int destinationIndex)
{
    clearLastCaptureState(game);
    if (!isValidPieceCode(capturedPiece) || capturedPiece == Empty)
    {
        return;
    }

    game.lastCapturedPieceCode = capturedPiece;
    if (!isKnockbackEnabled(game.rules))
    {
        game.lastKnockbackDestination = KnockbackClassicRemoved;
        return;
    }

    game.lastCaptureWasKnockback = true;
    int homeIndex = -1;
    if (findFreeHomeSlot(game, capturedPiece, destinationIndex, homeIndex))
    {
        game.pos.board[static_cast<std::size_t>(homeIndex)] = capturedPiece;
        game.lastKnockbackDestination = KnockbackHome;
        game.lastKnockbackHomeX = xOf(homeIndex);
        game.lastKnockbackHomeY = yOf(homeIndex);
        game.lastKnockbackHomeZ = zOf(homeIndex);
        return;
    }

    const int side = pieceSide(capturedPiece);
    const int type = pieceType(capturedPiece);
    if (side >= 1 && side <= 6 && type >= Pawn && type <= King)
    {
        ++game.reserveCounts[side][type];
    }
    game.lastKnockbackDestination = KnockbackReserve;
}

bool stackHasTypeForSide(const std::vector<CoreStackEntry>& stack, int side, int type)
{
    return std::any_of(stack.begin(), stack.end(), [&](const CoreStackEntry& entry)
    {
        return entry.side == side && entry.pieceType == type;
    });
}

CoreFusionState computeCoreFusionStateForCell(const Game& game, int index)
{
    CoreFusionState state{};
    if (!isFusionEnabled(game.rules) || !isInsideCore(game.rules, index))
    {
        return state;
    }

    const auto& stack = game.coreStacks[static_cast<std::size_t>(index)];
    state.entryCount = static_cast<int>(stack.size());
    if (stack.empty())
    {
        return state;
    }

    std::array<int, 7> sideCounts{};
    std::array<int, 7> typeCounts{};
    std::array<bool, 7> hasKing{};
    std::array<bool, 7> hasQueen{};
    int sideCount = 0;
    int dominantSide = 0;
    int dominantSideCount = 0;
    int dominantType = 0;
    int dominantTypeCount = 0;

    for (const CoreStackEntry& entry : stack)
    {
        if (entry.side < 1 || entry.side > 6 || entry.pieceType < Pawn || entry.pieceType > King)
        {
            continue;
        }
        if (sideCounts[entry.side] == 0)
        {
            ++sideCount;
            state.sideMask |= 1 << entry.side;
        }
        const int nextSideCount = ++sideCounts[entry.side];
        if (nextSideCount > dominantSideCount)
        {
            dominantSide = entry.side;
            dominantSideCount = nextSideCount;
        }
        const int nextTypeCount = ++typeCounts[entry.pieceType];
        if (nextTypeCount > dominantTypeCount)
        {
            dominantType = entry.pieceType;
            dominantTypeCount = nextTypeCount;
        }
        hasKing[entry.side] = hasKing[entry.side] || entry.pieceType == King;
        hasQueen[entry.side] = hasQueen[entry.side] || entry.pieceType == Queen;
    }

    state.dominantPieceType = dominantType;
    if (state.entryCount == 1)
    {
        state.ownerSide = dominantSide;
        state.friendlyCount = 1;
        state.fusionKind = FusionSingle;
        return state;
    }

    if (sideCount > 1)
    {
        state.fusionKind = FusionContested;
        state.ownerSide = 0;
        state.friendlyCount = dominantSideCount;
        state.enemyCount = state.entryCount - dominantSideCount;
        state.flags |= FusionFlagContested;
        return state;
    }

    state.ownerSide = dominantSide;
    state.friendlyCount = state.entryCount;
    state.enemyCount = 0;
    if (dominantSide >= 1 && dominantSide <= 6 && hasKing[dominantSide] && hasQueen[dominantSide])
    {
        state.fusionKind = FusionRoyalPair;
        state.flags |= FusionFlagRoyalPair;
    }
    else if (state.entryCount == 2)
    {
        state.fusionKind = FusionFriendlyPair;
    }
    else
    {
        state.fusionKind = FusionFriendlyStack;
    }

    const int x = xOf(index);
    const int y = yOf(index);
    const int z = zOf(index);
    const int expectedType = targetSlotType(game.rules, dominantSide, x, y, z);
    if (expectedType != Empty && stackHasTypeForSide(stack, dominantSide, expectedType))
    {
        state.flags |= FusionFlagAnchoredFusion | FusionFlagImplosionSeed;
        state.implosionStage = 1;
    }
    return state;
}

void recomputeFusion(Game& game)
{
    clearFusionStates(game);
    if (!isFusionEnabled(game.rules))
    {
        return;
    }

    for (int z = game.rules.coreZMin; z <= game.rules.coreZMax; ++z)
    {
        for (int y = game.rules.coreYMin; y <= game.rules.coreYMax; ++y)
        {
            for (int x = game.rules.coreXMin; x <= game.rules.coreXMax; ++x)
            {
                const int index = indexOf(x, y, z);
                CoreFusionState state = computeCoreFusionStateForCell(game, index);
                game.fusionStates[static_cast<std::size_t>(index)] = state;
                if ((state.flags & FusionFlagContested) != 0)
                {
                    for (int side = 1; side <= 6; ++side)
                    {
                        if ((state.sideMask & (1 << side)) != 0)
                        {
                            ++game.sideContestedCounts[side];
                        }
                    }
                    continue;
                }
                if (state.ownerSide >= 1 && state.ownerSide <= 6 &&
                    (state.fusionKind == FusionFriendlyPair ||
                     state.fusionKind == FusionFriendlyStack ||
                     state.fusionKind == FusionRoyalPair ||
                     state.fusionKind == FusionImplosionSeed ||
                     state.fusionKind == FusionImplosionReady))
                {
                    ++game.sideFusionCounts[state.ownerSide];
                    if ((state.flags & FusionFlagRoyalPair) != 0)
                    {
                        ++game.sideRoyalPairCounts[state.ownerSide];
                    }
                }
            }
        }
    }
}

void updateImplosionProgress(Game& game)
{
    game.sideImplosionProgress.fill(0);
    if (!isFusionEnabled(game.rules) ||
        game.rules.implosionProfileType == "none" ||
        game.rules.implosionProfileMode != "progressState")
    {
        return;
    }

    for (int side = 1; side <= 6; ++side)
    {
        game.sideImplosionProgress[side] =
            game.anchorCounts[side] +
            game.sideFusionCounts[side] +
            game.sideRoyalPairCounts[side];
    }
}

bool isCenterAssemblyGoal(const Rules& rules)
{
    return rules.goalProfileType == "centerAssembly" || rules.goalProfileType == "centerAssemblyTraining";
}

bool anchorsCanWin(const Rules& rules)
{
    return rules.victoryProfileType == "allPiecesAnchored" ||
        rules.victoryProfileType == "requiredPieceCount" ||
        rules.victoryProfileType == "hybrid";
}

void recomputeAnchors(Game& game)
{
    recomputeFusion(game);
    game.anchorCounts.fill(0);
    game.gameOver = false;
    game.winnerSide = 0;

    if (!isCenterAssemblyGoal(game.rules))
    {
        updateImplosionProgress(game);
        return;
    }

    for (int z = game.rules.coreZMin; z <= game.rules.coreZMax; ++z)
    {
        for (int y = game.rules.coreYMin; y <= game.rules.coreYMax; ++y)
        {
            for (int x = game.rules.coreXMin; x <= game.rules.coreXMax; ++x)
            {
                const int index = indexOf(x, y, z);
                if (isCoreStackEnabled(game.rules))
                {
                    const auto& stack = game.coreStacks[static_cast<std::size_t>(index)];
                    for (int side = 1; side <= 6; ++side)
                    {
                        const int expectedType = targetSlotType(game.rules, side, x, y, z);
                        if (expectedType == Empty)
                        {
                            continue;
                        }
                        const bool anchored = std::any_of(stack.begin(), stack.end(), [&](const CoreStackEntry& entry)
                        {
                            return entry.side == side && entry.pieceType == expectedType;
                        });
                        if (anchored)
                        {
                            ++game.anchorCounts[side];
                        }
                    }
                    continue;
                }

                const int piece = game.pos.board[static_cast<std::size_t>(index)];
                if (piece == Empty)
                {
                    continue;
                }
                const int side = pieceSide(piece);
                if (side >= 1 && side <= 6 && targetSlotType(game.rules, side, x, y, z) == pieceType(piece))
                {
                    ++game.anchorCounts[side];
                }
            }
        }
    }

    if (!anchorsCanWin(game.rules))
    {
        updateImplosionProgress(game);
        return;
    }
    for (int side = 1; side <= 6; ++side)
    {
        if (game.anchorCounts[side] >= game.rules.requiredAnchorCount)
        {
            game.gameOver = true;
            game.winnerSide = side;
            updateImplosionProgress(game);
            return;
        }
    }
    updateImplosionProgress(game);
}

void resetPosition(Game& game)
{
    clearGamePosition(game);
    for (int side = 1; side <= game.rules.activeSideCount; ++side)
    {
        placeFaceCenter(game.pos, side);
    }
    game.pos.sideToMove = 1;
    recomputeAnchors(game);
    game.lastInfo = "3D cube face-centered draft reset.";
}

int nextSide(const Rules& rules, int side)
{
    int candidate = side;
    for (int i = 0; i < 6; ++i)
    {
        candidate = candidate % rules.activeSideCount + 1;
        if (candidate >= 1 && candidate <= rules.activeSideCount)
        {
            return candidate;
        }
    }
    return 1;
}

void addMoveIfValid(const Game& game, const Position& pos, std::vector<Move>& moves, int from, int x, int y, int z)
{
    if (!inside(x, y, z))
    {
        return;
    }
    const int to = indexOf(x, y, z);
    const int piece = pos.board[from];
    const int target = pos.board[to];
    const bool stackTarget = isCoreStackEnabled(game.rules) && isInsideCore(game.rules, to);
    if (target != Empty && isSameSide(piece, target) && !stackTarget)
    {
        return;
    }
    Move move;
    move.from = from;
    move.to = to;
    move.piece = piece;
    move.captured = stackTarget ? Empty : target;
    if (target != Empty && !stackTarget)
    {
        move.flags |= MoveCapture;
    }
    moves.push_back(move);
}

std::vector<Vec3> lineDirectionsFor(int type)
{
    std::vector<Vec3> dirs;
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
                if (type == Rook && axes == 1)
                {
                    dirs.push_back({ dx, dy, dz });
                }
                else if (type == Bishop && axes >= 2)
                {
                    dirs.push_back({ dx, dy, dz });
                }
                else if (type == Queen || type == King)
                {
                    dirs.push_back({ dx, dy, dz });
                }
            }
        }
    }
    return dirs;
}

std::vector<Vec3> knightDirections()
{
    std::vector<Vec3> dirs;
    constexpr int values[3] = { -2, -1, 1 };
    (void)values;
    for (int longAxis = 0; longAxis < 3; ++longAxis)
    {
        for (int shortAxis = 0; shortAxis < 3; ++shortAxis)
        {
            if (shortAxis == longAxis)
            {
                continue;
            }
            for (int longSign : { -1, 1 })
            {
                for (int shortSign : { -1, 1 })
                {
                    Vec3 v;
                    (&v.x)[longAxis] = 2 * longSign;
                    (&v.x)[shortAxis] = shortSign;
                    dirs.push_back(v);
                }
            }
        }
    }
    return dirs;
}

std::vector<Vec3> perpendicularOffsets(Vec3 forward)
{
    std::vector<Vec3> offsets;
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
                if ((forward.x != 0 && dx != 0) || (forward.y != 0 && dy != 0) || (forward.z != 0 && dz != 0))
                {
                    continue;
                }
                offsets.push_back({ dx, dy, dz });
            }
        }
    }
    return offsets;
}

bool isPromotionSquare(const Rules& rules, int side, int square)
{
    const Vec3 f = rules.sides[side].forward;
    return (f.x > 0 && xOf(square) == 7) || (f.x < 0 && xOf(square) == 0) ||
        (f.y > 0 && yOf(square) == 7) || (f.y < 0 && yOf(square) == 0) ||
        (f.z > 0 && zOf(square) == 7) || (f.z < 0 && zOf(square) == 0);
}

bool isPawnStartSquare(const Rules& rules, int side, int square)
{
    const Vec3 f = rules.sides[side].forward;
    return (f.x > 0 && xOf(square) == 0) || (f.x < 0 && xOf(square) == 7) ||
        (f.y > 0 && yOf(square) == 0) || (f.y < 0 && yOf(square) == 7) ||
        (f.z > 0 && zOf(square) == 0) || (f.z < 0 && zOf(square) == 7);
}

void generatePawnMoves(const Game& game, const Position& pos, int from, std::vector<Move>& moves)
{
    const int piece = pos.board[from];
    const int side = pieceSide(piece);
    const Vec3 f = game.rules.sides[side].forward;
    const int x = xOf(from);
    const int y = yOf(from);
    const int z = zOf(from);

    const int oneX = x + f.x;
    const int oneY = y + f.y;
    const int oneZ = z + f.z;
    if (inside(oneX, oneY, oneZ) && pos.board[indexOf(oneX, oneY, oneZ)] == Empty)
    {
        addMoveIfValid(game, pos, moves, from, oneX, oneY, oneZ);

        const int twoX = x + f.x * 2;
        const int twoY = y + f.y * 2;
        const int twoZ = z + f.z * 2;
        if (isPawnStartSquare(game.rules, side, from) &&
            inside(twoX, twoY, twoZ) &&
            pos.board[indexOf(twoX, twoY, twoZ)] == Empty)
        {
            addMoveIfValid(game, pos, moves, from, twoX, twoY, twoZ);
        }
    }

    for (Vec3 offset : perpendicularOffsets(f))
    {
        const int tx = oneX + offset.x;
        const int ty = oneY + offset.y;
        const int tz = oneZ + offset.z;
        if (!inside(tx, ty, tz))
        {
            continue;
        }
        const int target = pos.board[indexOf(tx, ty, tz)];
        if (target != Empty && !isSameSide(piece, target))
        {
            addMoveIfValid(game, pos, moves, from, tx, ty, tz);
        }
    }

    for (Move& move : moves)
    {
        if (move.from == from && isPromotionSquare(game.rules, side, move.to))
        {
            move.promotionType = Queen;
            move.flags |= MovePromotion;
        }
    }
}

void generatePieceMoves(const Game& game, const Position& pos, int from, std::vector<Move>& moves)
{
    const int piece = pos.board[from];
    const int type = pieceType(piece);
    const int x = xOf(from);
    const int y = yOf(from);
    const int z = zOf(from);

    if (type == Pawn)
    {
        generatePawnMoves(game, pos, from, moves);
        return;
    }
    if (type == Knight)
    {
        for (Vec3 d : knightDirections())
        {
            addMoveIfValid(game, pos, moves, from, x + d.x, y + d.y, z + d.z);
        }
        return;
    }

    const bool sliding = type == Rook || type == Bishop || type == Queen;
    for (Vec3 d : lineDirectionsFor(type))
    {
        int tx = x + d.x;
        int ty = y + d.y;
        int tz = z + d.z;
        while (inside(tx, ty, tz))
        {
            const int before = static_cast<int>(moves.size());
            addMoveIfValid(game, pos, moves, from, tx, ty, tz);
            const int target = pos.board[indexOf(tx, ty, tz)];
            if (!sliding || target != Empty || before == static_cast<int>(moves.size()))
            {
                break;
            }
            tx += d.x;
            ty += d.y;
            tz += d.z;
        }
    }
}

std::vector<Move> generateMoves(const Game& game, const Position& pos)
{
    std::vector<Move> moves;
    if (game.rules.movementProfile == 0)
    {
        return moves;
    }
    for (int i = 0; i < CellCount; ++i)
    {
        const int piece = pos.board[i];
        if (piece != Empty && pieceSide(piece) == pos.sideToMove)
        {
            generatePieceMoves(game, pos, i, moves);
        }
    }
    return moves;
}

void applyMove(const Rules& rules, Position& pos, Move move)
{
    int piece = pos.board[move.from];
    if ((move.flags & MovePromotion) != 0 && move.promotionType != 0)
    {
        piece = makePiece(pieceSide(piece), move.promotionType);
    }
    pos.board[move.to] = piece;
    pos.board[move.from] = Empty;
    pos.lastMove = move;
    pos.sideToMove = nextSide(rules, pos.sideToMove);
}

void applyMove(Game& game, Move move)
{
    const bool stackEnabled = isCoreStackEnabled(game.rules);
    const bool fromCore = stackEnabled && isInsideCore(game.rules, move.from);
    const bool toCore = stackEnabled && isInsideCore(game.rules, move.to);

    int piece = fromCore ? projectedPiece(game, move.from) : game.pos.board[static_cast<std::size_t>(move.from)];
    if ((move.flags & MovePromotion) != 0 && move.promotionType != 0)
    {
        piece = makePiece(pieceSide(piece), move.promotionType);
    }

    if (fromCore)
    {
        auto& sourceStack = game.coreStacks[static_cast<std::size_t>(move.from)];
        if (!sourceStack.empty())
        {
            sourceStack.pop_back();
            syncProjectedPiece(game, move.from);
        }
        else
        {
            game.pos.board[static_cast<std::size_t>(move.from)] = Empty;
        }
    }
    else
    {
        game.pos.board[static_cast<std::size_t>(move.from)] = Empty;
    }

    if (!toCore && move.captured != Empty)
    {
        routeCapturedPiece(game, move.captured, move.to);
    }
    else
    {
        clearLastCaptureState(game);
    }

    if (toCore)
    {
        pushCoreStackPiece(game, move.to, piece);
    }
    else
    {
        game.pos.board[static_cast<std::size_t>(move.to)] = piece;
    }

    game.pos.lastMove = move;
    game.pos.sideToMove = nextSide(game.rules, game.pos.sideToMove);
}

Vec3 rotateLayerSquare(int axis, int layer, int turns, int x, int y, int z)
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
        const int nextU = BoardSize - 1 - v;
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

void rotateLayer(Position& pos, int axis, int layer, int turns)
{
    const auto before = pos.board;
    for (int z = 0; z < BoardSize; ++z)
    {
        for (int y = 0; y < BoardSize; ++y)
        {
            for (int x = 0; x < BoardSize; ++x)
            {
                const bool inLayer = (axis == 0 && z == layer) || (axis == 1 && y == layer) || (axis == 2 && x == layer);
                if (!inLayer)
                {
                    continue;
                }
                const Vec3 to = rotateLayerSquare(axis, layer, turns, x, y, z);
                pos.board[indexOf(to.x, to.y, to.z)] = before[indexOf(x, y, z)];
            }
        }
    }
    pos.lastMove = Move{};
}

int evaluateForSide(const Position& pos, int side)
{
    int score = 0;
    for (int i = 0; i < CellCount; ++i)
    {
        const int piece = pos.board[i];
        if (piece == Empty)
        {
            continue;
        }
        const int type = pieceType(piece);
        const int sign = pieceSide(piece) == side ? 1 : -1;
        const int center = 21 - (std::abs(xOf(i) - 3) + std::abs(yOf(i) - 3) + std::abs(zOf(i) - 3)) * 2;
        score += sign * (Material[type] + center);
    }
    return score;
}

int minimax(const Game& game, Position& pos, int depth, int rootSide)
{
    if (depth <= 0)
    {
        return evaluateForSide(pos, rootSide);
    }
    auto moves = generateMoves(game, pos);
    if (moves.empty())
    {
        return evaluateForSide(pos, rootSide);
    }

    const bool maximizing = pos.sideToMove == rootSide;
    int best = maximizing ? -Infinity : Infinity;
    for (Move move : moves)
    {
        Position child = pos;
        applyMove(game.rules, child, move);
        const int score = minimax(game, child, depth - 1, rootSide);
        best = maximizing ? std::max(best, score) : std::min(best, score);
    }
    return best;
}

Chess3DMoveDto toDto(const Move& move)
{
    Chess3DMoveDto dto{};
    dto.fromX = move.from >= 0 ? xOf(move.from) : -1;
    dto.fromY = move.from >= 0 ? yOf(move.from) : -1;
    dto.fromZ = move.from >= 0 ? zOf(move.from) : -1;
    dto.toX = move.to >= 0 ? xOf(move.to) : -1;
    dto.toY = move.to >= 0 ? yOf(move.to) : -1;
    dto.toZ = move.to >= 0 ? zOf(move.to) : -1;
    dto.piece = move.piece;
    dto.captured = move.captured;
    dto.promotionType = move.promotionType;
    dto.flags = move.flags;
    dto.score = move.score;
    return dto;
}

std::string positionText(const Position& pos)
{
    std::ostringstream out;
    for (int z = 0; z < BoardSize; ++z)
    {
        if (z > 0)
        {
            out << " | ";
        }
        out << "L" << (z + 1) << ":";
        for (int y = BoardSize - 1; y >= 0; --y)
        {
            int empty = 0;
            out << '/';
            for (int x = 0; x < BoardSize; ++x)
            {
                const int piece = pos.board[indexOf(x, y, z)];
                if (piece == Empty)
                {
                    ++empty;
                    continue;
                }
                if (empty > 0)
                {
                    out << empty;
                    empty = 0;
                }
                out << pieceSide(piece) << typeChar(pieceType(piece));
            }
            if (empty > 0)
            {
                out << empty;
            }
        }
    }
    out << " side " << pos.sideToMove;
    return out.str();
}

Game* asGame(void* handle)
{
    return static_cast<Game*>(handle);
}
}

CHESS3D_API void* Chess3D_Create()
{
    auto* game = new Game();
    loadRules(game->rules, defaultRulesJson());
    resetPosition(*game);
    return game;
}

CHESS3D_API void Chess3D_Destroy(void* handle)
{
    delete asGame(handle);
}

CHESS3D_API void Chess3D_Reset(void* handle)
{
    if (auto* game = asGame(handle))
    {
        resetPosition(*game);
    }
}

CHESS3D_API void Chess3D_Clear(void* handle)
{
    if (auto* game = asGame(handle))
    {
        clearGamePosition(*game);
        recomputeAnchors(*game);
        game->lastInfo = "3D cube cleared for setup.";
    }
}

CHESS3D_API int Chess3D_LoadRulesJson(void* handle, const char* json)
{
    auto* game = asGame(handle);
    if (game == nullptr)
    {
        return 0;
    }
    const std::string text = json != nullptr ? json : "";
    if (!hasJsonObjectEnvelope(text))
    {
        game->lastInfo = "3D rules JSON rejected: expected a JSON object.";
        return 0;
    }
    loadRules(game->rules, text);
    resetPosition(*game);
    game->lastInfo = "3D rules JSON loaded and face-centered setup applied.";
    return 1;
}

CHESS3D_API int Chess3D_LoadRuleProfileJson(void* handle, const char* json)
{
    auto* game = asGame(handle);
    if (game == nullptr)
    {
        return 0;
    }
    const std::string text = json != nullptr ? json : "";
    if (text.empty() || !hasJsonObjectEnvelope(text))
    {
        game->lastProfileLoadError = "RuleProfile rejected: expected a non-empty JSON object.";
        game->lastInfo = game->lastProfileLoadError;
        return 0;
    }

    Rules candidate;
    loadRules(candidate, text);
    std::string error;
    if (!parseRuleProfileMetadata(candidate, error))
    {
        game->lastProfileLoadError = error;
        game->lastInfo = error;
        return 0;
    }

    game->rules = candidate;
    resetPosition(*game);
    game->lastProfileLoadError.clear();
    game->lastInfo = "3D rule profile loaded: " + game->rules.rulesetId;
    return 1;
}

CHESS3D_API int Chess3D_GetRulesJson(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    return game != nullptr ? copyString(game->rules.json, buffer, capacity) : 0;
}

CHESS3D_API int Chess3D_GetCurrentRulesetId(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    return game != nullptr ? copyString(game->rules.rulesetId, buffer, capacity) : 0;
}

CHESS3D_API int Chess3D_GetCurrentRulesetVersion(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    return game != nullptr ? copyString(game->rules.rulesetVersion, buffer, capacity) : 0;
}

CHESS3D_API int Chess3D_GetCurrentRulesetDisplayName(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    return game != nullptr ? copyString(game->rules.rulesetDisplayName, buffer, capacity) : 0;
}

CHESS3D_API int Chess3D_GetGoalProfileType(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    return game != nullptr ? copyString(game->rules.goalProfileType, buffer, capacity) : 0;
}

CHESS3D_API int Chess3D_GetCaptureProfileType(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    return game != nullptr ? copyString(game->rules.captureProfileType, buffer, capacity) : 0;
}

CHESS3D_API int Chess3D_GetOccupancyProfileType(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    return game != nullptr ? copyString(game->rules.occupancyProfileType, buffer, capacity) : 0;
}

CHESS3D_API int Chess3D_GetFusionProfileType(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    return game != nullptr ? copyString(game->rules.fusionProfileType, buffer, capacity) : 0;
}

CHESS3D_API int Chess3D_GetCorePhysicsProfileType(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    return game != nullptr ? copyString(game->rules.corePhysicsProfileType, buffer, capacity) : 0;
}

CHESS3D_API int Chess3D_GetLayerTurnProfileType(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    return game != nullptr ? copyString(game->rules.layerTurnProfileType, buffer, capacity) : 0;
}

CHESS3D_API int Chess3D_GetVictoryProfileType(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    return game != nullptr ? copyString(game->rules.victoryProfileType, buffer, capacity) : 0;
}

CHESS3D_API int Chess3D_GetCoreCube(void* handle, int* xMin, int* xMax, int* yMin, int* yMax, int* zMin, int* zMax)
{
    auto* game = asGame(handle);
    if (game == nullptr || xMin == nullptr || xMax == nullptr || yMin == nullptr || yMax == nullptr || zMin == nullptr || zMax == nullptr)
    {
        return 0;
    }
    *xMin = game->rules.coreXMin;
    *xMax = game->rules.coreXMax;
    *yMin = game->rules.coreYMin;
    *yMax = game->rules.coreYMax;
    *zMin = game->rules.coreZMin;
    *zMax = game->rules.coreZMax;
    return 1;
}

CHESS3D_API int Chess3D_RecomputeAnchors(void* handle)
{
    auto* game = asGame(handle);
    if (game == nullptr)
    {
        return 0;
    }
    recomputeAnchors(*game);
    return 1;
}

CHESS3D_API int Chess3D_GetAnchorCount(void* handle, int side)
{
    auto* game = asGame(handle);
    if (game == nullptr || side < 1 || side > 6)
    {
        return 0;
    }
    return game->anchorCounts[side];
}

CHESS3D_API int Chess3D_GetRequiredAnchorCount(void* handle, int side)
{
    auto* game = asGame(handle);
    if (game == nullptr || side < 1 || side > 6)
    {
        return 0;
    }
    return game->rules.requiredAnchorCount;
}

CHESS3D_API int Chess3D_IsTargetSlot(void* handle, int side, int x, int y, int z)
{
    auto* game = asGame(handle);
    if (game == nullptr || !inside(x, y, z))
    {
        return 0;
    }
    return targetSlotType(game->rules, side, x, y, z) != Empty ? 1 : 0;
}

CHESS3D_API int Chess3D_IsAnchoredCell(void* handle, int x, int y, int z)
{
    auto* game = asGame(handle);
    if (game == nullptr || !inside(x, y, z) || !isCenterAssemblyGoal(game->rules))
    {
        return 0;
    }
    const int index = indexOf(x, y, z);
    if (isCoreStackEnabled(game->rules) && isInsideCore(game->rules, index))
    {
        const auto& stack = game->coreStacks[static_cast<std::size_t>(index)];
        for (int side = 1; side <= 6; ++side)
        {
            const int expectedType = targetSlotType(game->rules, side, x, y, z);
            if (expectedType == Empty)
            {
                continue;
            }
            if (std::any_of(stack.begin(), stack.end(), [&](const CoreStackEntry& entry)
            {
                return entry.side == side && entry.pieceType == expectedType;
            }))
            {
                return 1;
            }
        }
        return 0;
    }

    const int piece = game->pos.board[static_cast<std::size_t>(index)];
    if (piece == Empty)
    {
        return 0;
    }
    return targetSlotType(game->rules, pieceSide(piece), x, y, z) == pieceType(piece) ? 1 : 0;
}

CHESS3D_API int Chess3D_IsGameOver(void* handle)
{
    auto* game = asGame(handle);
    return game != nullptr && game->gameOver ? 1 : 0;
}

CHESS3D_API int Chess3D_GetWinnerSide(void* handle)
{
    auto* game = asGame(handle);
    return game != nullptr ? game->winnerSide : 0;
}

CHESS3D_API int Chess3D_GetLastProfileError(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    return game != nullptr ? copyString(game->lastProfileLoadError, buffer, capacity) : 0;
}

CHESS3D_API int Chess3D_IsCoreStackEnabled(void* handle)
{
    auto* game = asGame(handle);
    return game != nullptr && isCoreStackEnabled(game->rules) ? 1 : 0;
}

CHESS3D_API int Chess3D_GetCoreStackCount(void* handle, int x, int y, int z)
{
    auto* game = asGame(handle);
    if (game == nullptr || !inside(x, y, z) || !isCoreStackEnabled(game->rules) || !isInsideCore(game->rules, x, y, z))
    {
        return 0;
    }
    return static_cast<int>(game->coreStacks[static_cast<std::size_t>(indexOf(x, y, z))].size());
}

CHESS3D_API int Chess3D_GetCoreStackEntry(void* handle, int x, int y, int z, int stackIndex, int* side, int* pieceTypeOut, int* pieceCode, int* flags)
{
    auto* game = asGame(handle);
    if (game == nullptr || side == nullptr || pieceTypeOut == nullptr || pieceCode == nullptr || flags == nullptr ||
        !inside(x, y, z) || !isCoreStackEnabled(game->rules) || !isInsideCore(game->rules, x, y, z))
    {
        return 0;
    }
    const auto& stack = game->coreStacks[static_cast<std::size_t>(indexOf(x, y, z))];
    if (stackIndex < 0 || stackIndex >= static_cast<int>(stack.size()))
    {
        return 0;
    }
    const CoreStackEntry& entry = stack[static_cast<std::size_t>(stackIndex)];
    *side = entry.side;
    *pieceTypeOut = entry.pieceType;
    *pieceCode = entry.pieceCode;
    *flags = entry.flags;
    return 1;
}

CHESS3D_API int Chess3D_PushCoreStackPiece(void* handle, int x, int y, int z, int pieceCode)
{
    auto* game = asGame(handle);
    if (game == nullptr || !inside(x, y, z))
    {
        return 0;
    }
    const int index = indexOf(x, y, z);
    if (!pushCoreStackPiece(*game, index, pieceCode))
    {
        return 0;
    }
    recomputeAnchors(*game);
    game->lastInfo = "3D core stack piece pushed.";
    return 1;
}

CHESS3D_API int Chess3D_ClearCoreStack(void* handle, int x, int y, int z)
{
    auto* game = asGame(handle);
    if (game == nullptr || !inside(x, y, z) || !isCoreStackEnabled(game->rules) || !isInsideCore(game->rules, x, y, z))
    {
        return 0;
    }
    const int index = indexOf(x, y, z);
    game->coreStacks[static_cast<std::size_t>(index)].clear();
    syncProjectedPiece(*game, index);
    recomputeAnchors(*game);
    game->lastInfo = "3D core stack cleared.";
    return 1;
}

CHESS3D_API int Chess3D_RemoveCoreStackEntry(void* handle, int x, int y, int z, int stackIndex)
{
    auto* game = asGame(handle);
    if (game == nullptr || !inside(x, y, z))
    {
        return 0;
    }
    if (!removeCoreStackEntry(*game, indexOf(x, y, z), stackIndex))
    {
        return 0;
    }
    recomputeAnchors(*game);
    game->lastInfo = "3D core stack entry removed.";
    return 1;
}

CHESS3D_API int Chess3D_GetProjectedPiece(void* handle, int x, int y, int z)
{
    auto* game = asGame(handle);
    if (game == nullptr || !inside(x, y, z))
    {
        return 0;
    }
    return projectedPiece(*game, indexOf(x, y, z));
}

CHESS3D_API int Chess3D_IsFusionEnabled(void* handle)
{
    auto* game = asGame(handle);
    return game != nullptr && isFusionEnabled(game->rules) ? 1 : 0;
}

CHESS3D_API int Chess3D_RecomputeFusion(void* handle)
{
    auto* game = asGame(handle);
    if (game == nullptr)
    {
        return 0;
    }
    recomputeAnchors(*game);
    return 1;
}

CHESS3D_API int Chess3D_GetCoreFusionKind(void* handle, int x, int y, int z)
{
    auto* game = asGame(handle);
    if (game == nullptr || !inside(x, y, z) || !isFusionEnabled(game->rules) || !isInsideCore(game->rules, x, y, z))
    {
        return FusionNone;
    }
    return game->fusionStates[static_cast<std::size_t>(indexOf(x, y, z))].fusionKind;
}

CHESS3D_API int Chess3D_GetCoreFusionState(void* handle, int x, int y, int z, int* fusionKind, int* ownerSide, int* sideMask, int* entryCount, int* friendlyCount, int* enemyCount, int* dominantPieceType, int* flags)
{
    auto* game = asGame(handle);
    if (game == nullptr || fusionKind == nullptr || ownerSide == nullptr || sideMask == nullptr ||
        entryCount == nullptr || friendlyCount == nullptr || enemyCount == nullptr ||
        dominantPieceType == nullptr || flags == nullptr || !inside(x, y, z))
    {
        return 0;
    }
    CoreFusionState state{};
    if (isFusionEnabled(game->rules) && isInsideCore(game->rules, x, y, z))
    {
        state = game->fusionStates[static_cast<std::size_t>(indexOf(x, y, z))];
    }
    *fusionKind = state.fusionKind;
    *ownerSide = state.ownerSide;
    *sideMask = state.sideMask;
    *entryCount = state.entryCount;
    *friendlyCount = state.friendlyCount;
    *enemyCount = state.enemyCount;
    *dominantPieceType = state.dominantPieceType;
    *flags = state.flags;
    return 1;
}

CHESS3D_API int Chess3D_IsCoreCellContested(void* handle, int x, int y, int z)
{
    auto* game = asGame(handle);
    if (game == nullptr || !inside(x, y, z) || !isFusionEnabled(game->rules) || !isInsideCore(game->rules, x, y, z))
    {
        return 0;
    }
    return (game->fusionStates[static_cast<std::size_t>(indexOf(x, y, z))].flags & FusionFlagContested) != 0 ? 1 : 0;
}

CHESS3D_API int Chess3D_HasRoyalPairFusion(void* handle, int x, int y, int z, int side)
{
    auto* game = asGame(handle);
    if (game == nullptr || !inside(x, y, z) || side < 1 || side > 6 ||
        !isFusionEnabled(game->rules) || !isInsideCore(game->rules, x, y, z))
    {
        return 0;
    }
    const CoreFusionState& state = game->fusionStates[static_cast<std::size_t>(indexOf(x, y, z))];
    return state.ownerSide == side && (state.flags & FusionFlagRoyalPair) != 0 ? 1 : 0;
}

CHESS3D_API int Chess3D_GetSideFusionCount(void* handle, int side)
{
    auto* game = asGame(handle);
    if (game == nullptr || side < 1 || side > 6 || !isFusionEnabled(game->rules))
    {
        return 0;
    }
    return game->sideFusionCounts[side];
}

CHESS3D_API int Chess3D_GetSideContestedCount(void* handle, int side)
{
    auto* game = asGame(handle);
    if (game == nullptr || side < 1 || side > 6 || !isFusionEnabled(game->rules))
    {
        return 0;
    }
    return game->sideContestedCounts[side];
}

CHESS3D_API int Chess3D_GetSideImplosionProgress(void* handle, int side)
{
    auto* game = asGame(handle);
    if (game == nullptr || side < 1 || side > 6 || !isFusionEnabled(game->rules))
    {
        return 0;
    }
    return game->sideImplosionProgress[side];
}

CHESS3D_API int Chess3D_GetFusionKindName(int fusionKind, char* buffer, int capacity)
{
    return copyString(fusionKindName(fusionKind), buffer, capacity);
}

CHESS3D_API int Chess3D_IsReserveEnabled(void* handle)
{
    auto* game = asGame(handle);
    return game != nullptr && isReserveEnabled(game->rules) ? 1 : 0;
}

CHESS3D_API int Chess3D_IsKnockbackEnabled(void* handle)
{
    auto* game = asGame(handle);
    return game != nullptr && isKnockbackEnabled(game->rules) ? 1 : 0;
}

CHESS3D_API int Chess3D_GetReserveCount(void* handle, int side, int pieceTypeIn)
{
    auto* game = asGame(handle);
    if (game == nullptr || side < 1 || side > 6 || pieceTypeIn < Pawn || pieceTypeIn > King || !isReserveEnabled(game->rules))
    {
        return 0;
    }
    return game->reserveCounts[side][pieceTypeIn];
}

CHESS3D_API int Chess3D_GetReserveTotal(void* handle, int side)
{
    auto* game = asGame(handle);
    if (game == nullptr || side < 1 || side > 6 || !isReserveEnabled(game->rules))
    {
        return 0;
    }
    int total = 0;
    for (int type = Pawn; type <= King; ++type)
    {
        total += game->reserveCounts[side][type];
    }
    return total;
}

CHESS3D_API int Chess3D_ClearReserve(void* handle, int side)
{
    auto* game = asGame(handle);
    if (game == nullptr || side < 1 || side > 6 || !isReserveEnabled(game->rules))
    {
        return 0;
    }
    game->reserveCounts[side].fill(0);
    game->lastInfo = "3D reserve cleared for side.";
    return 1;
}

CHESS3D_API int Chess3D_GetLastCaptureWasKnockback(void* handle)
{
    auto* game = asGame(handle);
    return game != nullptr && game->lastCaptureWasKnockback ? 1 : 0;
}

CHESS3D_API int Chess3D_GetLastCapturedPieceCode(void* handle)
{
    auto* game = asGame(handle);
    return game != nullptr ? game->lastCapturedPieceCode : 0;
}

CHESS3D_API int Chess3D_GetLastCapturedPieceReserveDestination(void* handle)
{
    auto* game = asGame(handle);
    return game != nullptr ? game->lastKnockbackDestination : KnockbackNone;
}

CHESS3D_API int Chess3D_GetLastKnockbackHomeX(void* handle)
{
    auto* game = asGame(handle);
    return game != nullptr ? game->lastKnockbackHomeX : -1;
}

CHESS3D_API int Chess3D_GetLastKnockbackHomeY(void* handle)
{
    auto* game = asGame(handle);
    return game != nullptr ? game->lastKnockbackHomeY : -1;
}

CHESS3D_API int Chess3D_GetLastKnockbackHomeZ(void* handle)
{
    auto* game = asGame(handle);
    return game != nullptr ? game->lastKnockbackHomeZ : -1;
}

CHESS3D_API int Chess3D_GetLastKnockbackInfo(void* handle, int* capturedPieceCode, int* destinationKind, int* x, int* y, int* z)
{
    auto* game = asGame(handle);
    if (game == nullptr || capturedPieceCode == nullptr || destinationKind == nullptr || x == nullptr || y == nullptr || z == nullptr)
    {
        return 0;
    }
    *capturedPieceCode = game->lastCapturedPieceCode;
    *destinationKind = game->lastKnockbackDestination;
    *x = game->lastKnockbackHomeX;
    *y = game->lastKnockbackHomeY;
    *z = game->lastKnockbackHomeZ;
    return 1;
}

CHESS3D_API int Chess3D_GetRulesInfo(void* handle, Chess3DRulesInfoDto* info)
{
    auto* game = asGame(handle);
    if (game == nullptr || info == nullptr)
    {
        return 0;
    }
    *info = Chess3DRulesInfoDto{};
    info->width = game->rules.width;
    info->height = game->rules.height;
    info->depth = game->rules.depth;
    info->activeSideCount = game->rules.activeSideCount;
    info->movementProfile = game->rules.movementProfile;
    info->kingSafetyEnabled = game->rules.kingSafetyEnabled ? 1 : 0;
    info->maxPiecesPerSide = game->rules.maxPiecesPerSide;
    return 1;
}

CHESS3D_API int Chess3D_GetState(void* handle, Chess3DStateDto* state)
{
    auto* game = asGame(handle);
    if (game == nullptr || state == nullptr)
    {
        return 0;
    }
    const auto moves = generateMoves(*game, game->pos);
    int pieces = 0;
    for (int piece : game->pos.board)
    {
        pieces += piece != Empty ? 1 : 0;
    }
    *state = Chess3DStateDto{};
    state->width = game->rules.width;
    state->height = game->rules.height;
    state->depth = game->rules.depth;
    state->sideToMove = game->pos.sideToMove;
    state->activeSideCount = game->rules.activeSideCount;
    state->legalMoveCount = static_cast<int>(moves.size());
    state->pieceCount = pieces;
    state->rulesLoaded = game->rules.json.empty() ? 0 : 1;
    state->kingSafetyEnabled = game->rules.kingSafetyEnabled ? 1 : 0;
    state->lastFromX = game->pos.lastMove.from >= 0 ? xOf(game->pos.lastMove.from) : -1;
    state->lastFromY = game->pos.lastMove.from >= 0 ? yOf(game->pos.lastMove.from) : -1;
    state->lastFromZ = game->pos.lastMove.from >= 0 ? zOf(game->pos.lastMove.from) : -1;
    state->lastToX = game->pos.lastMove.to >= 0 ? xOf(game->pos.lastMove.to) : -1;
    state->lastToY = game->pos.lastMove.to >= 0 ? yOf(game->pos.lastMove.to) : -1;
    state->lastToZ = game->pos.lastMove.to >= 0 ? zOf(game->pos.lastMove.to) : -1;
    return 1;
}

CHESS3D_API int Chess3D_GetBoard(void* handle, int* pieces512)
{
    auto* game = asGame(handle);
    if (game == nullptr || pieces512 == nullptr)
    {
        return 0;
    }
    std::copy(game->pos.board.begin(), game->pos.board.end(), pieces512);
    return 1;
}

CHESS3D_API int Chess3D_SetBoard(void* handle, const int* pieces512, int sideToMove)
{
    auto* game = asGame(handle);
    if (game == nullptr || pieces512 == nullptr)
    {
        return 0;
    }
    for (int i = 0; i < CellCount; ++i)
    {
        if (!isValidPieceCode(pieces512[i]))
        {
            return 0;
        }
    }
    clearCoreStacks(*game);
    clearReserveState(*game);
    std::copy(pieces512, pieces512 + CellCount, game->pos.board.begin());
    if (isCoreStackEnabled(game->rules))
    {
        for (int i = 0; i < CellCount; ++i)
        {
            if (game->pos.board[static_cast<std::size_t>(i)] != Empty && isInsideCore(game->rules, i))
            {
                setCoreStackSingle(*game, i, game->pos.board[static_cast<std::size_t>(i)]);
            }
        }
    }
    game->pos.sideToMove = std::clamp(sideToMove, 1, game->rules.activeSideCount);
    game->pos.lastMove = Move{};
    recomputeAnchors(*game);
    game->lastInfo = "3D board synchronized.";
    return 1;
}

CHESS3D_API int Chess3D_SetPiece(void* handle, int x, int y, int z, int side, int type)
{
    auto* game = asGame(handle);
    if (game == nullptr || !inside(x, y, z) || side < 0 || side > 6 || type < 0 || type > 6)
    {
        return 0;
    }
    const int index = indexOf(x, y, z);
    const int piece = side == 0 || type == 0 ? Empty : makePiece(side, type);
    if (isCoreStackEnabled(game->rules) && isInsideCore(game->rules, x, y, z))
    {
        setCoreStackSingle(*game, index, piece);
    }
    else
    {
        game->coreStacks[static_cast<std::size_t>(index)].clear();
        game->pos.board[static_cast<std::size_t>(index)] = piece;
    }
    recomputeAnchors(*game);
    game->lastInfo = "3D setup changed.";
    return 1;
}

CHESS3D_API int Chess3D_GetPiece(void* handle, int x, int y, int z)
{
    auto* game = asGame(handle);
    if (game == nullptr || !inside(x, y, z))
    {
        return 0;
    }
    return game->pos.board[indexOf(x, y, z)];
}

CHESS3D_API int Chess3D_GetLegalMoves(void* handle, Chess3DMoveDto* buffer, int capacity)
{
    auto* game = asGame(handle);
    if (game == nullptr)
    {
        return 0;
    }
    const auto moves = generateMoves(*game, game->pos);
    if (buffer != nullptr && capacity > 0)
    {
        const int count = std::min(capacity, static_cast<int>(moves.size()));
        for (int i = 0; i < count; ++i)
        {
            buffer[i] = toDto(moves[static_cast<std::size_t>(i)]);
        }
    }
    return static_cast<int>(moves.size());
}

CHESS3D_API int Chess3D_GetPieceMoves(void* handle, int fromX, int fromY, int fromZ, Chess3DMoveDto* buffer, int capacity)
{
    auto* game = asGame(handle);
    if (game == nullptr || !inside(fromX, fromY, fromZ) || game->rules.movementProfile == 0)
    {
        return 0;
    }

    const int from = indexOf(fromX, fromY, fromZ);
    const int piece = game->pos.board[from];
    if (piece == Empty)
    {
        return 0;
    }

    Position scoped = game->pos;
    scoped.sideToMove = pieceSide(piece);
    std::vector<Move> moves;
    generatePieceMoves(*game, scoped, from, moves);
    if (buffer != nullptr && capacity > 0)
    {
        const int count = std::min(capacity, static_cast<int>(moves.size()));
        for (int i = 0; i < count; ++i)
        {
            buffer[i] = toDto(moves[static_cast<std::size_t>(i)]);
        }
    }
    return static_cast<int>(moves.size());
}

CHESS3D_API int Chess3D_TryMakeMove(void* handle, int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int promotionType, Chess3DMoveDto* playedMove)
{
    auto* game = asGame(handle);
    if (game == nullptr || !inside(fromX, fromY, fromZ) || !inside(toX, toY, toZ))
    {
        return 0;
    }
    const int from = indexOf(fromX, fromY, fromZ);
    const int to = indexOf(toX, toY, toZ);
    auto moves = generateMoves(*game, game->pos);
    for (Move move : moves)
    {
        if (move.from == from && move.to == to)
        {
            if (promotionType >= Knight && promotionType <= Queen)
            {
                move.promotionType = promotionType;
            }
            applyMove(*game, move);
            recomputeAnchors(*game);
            game->lastInfo = "3D move played.";
            if (playedMove != nullptr)
            {
                *playedMove = toDto(move);
            }
            return 1;
        }
    }
    return 0;
}

CHESS3D_API int Chess3D_MakeBestMove(void* handle, int depth, Chess3DMoveDto* playedMove)
{
    auto* game = asGame(handle);
    if (game == nullptr)
    {
        return 0;
    }
    auto moves = generateMoves(*game, game->pos);
    if (moves.empty())
    {
        game->lastInfo = "3D AI has no moves.";
        return 0;
    }
    const int rootSide = game->pos.sideToMove;
    Move best = moves.front();
    int bestScore = -Infinity;
    const int searchDepth = std::clamp(depth, 1, 4);
    for (Move move : moves)
    {
        Position child = game->pos;
        applyMove(game->rules, child, move);
        const int score = minimax(*game, child, searchDepth - 1, rootSide);
        if (score > bestScore)
        {
            bestScore = score;
            best = move;
        }
    }
    best.score = bestScore;
    applyMove(*game, best);
    recomputeAnchors(*game);
    if (playedMove != nullptr)
    {
        *playedMove = toDto(best);
    }
    std::ostringstream info;
    info << "3D AI depth " << searchDepth << ", score " << bestScore
         << ", move " << static_cast<char>('a' + xOf(best.from)) << (yOf(best.from) + 1) << "." << (zOf(best.from) + 1)
         << "-" << static_cast<char>('a' + xOf(best.to)) << (yOf(best.to) + 1) << "." << (zOf(best.to) + 1);
    game->lastInfo = info.str();
    return 1;
}

CHESS3D_API int Chess3D_RotateLayer(void* handle, int axis, int layer, int quarterTurns)
{
    auto* game = asGame(handle);
    if (game == nullptr || axis < 0 || axis > 2 || layer < 0 || layer >= BoardSize)
    {
        return 0;
    }
    if (isCoreStackEnabled(game->rules))
    {
        game->lastInfo = "3D Rubik rotation with CoreCell stacks is deferred.";
        return 0;
    }

    int turns = quarterTurns % 4;
    if (turns < 0)
    {
        turns += 4;
    }
    if (turns == 0)
    {
        game->lastInfo = "3D Rubik rotation skipped.";
        return 1;
    }

    rotateLayer(game->pos, axis, layer, turns);
    recomputeAnchors(*game);
    std::ostringstream info;
    const char axisName = axis == 0 ? 'Z' : axis == 1 ? 'Y' : 'X';
    info << "3D Rubik rotate " << axisName << (layer + 1) << " x" << turns;
    game->lastInfo = info.str();
    return 1;
}

CHESS3D_API int Chess3D_GetPositionText(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    return game != nullptr ? copyString(positionText(game->pos), buffer, capacity) : 0;
}

CHESS3D_API int Chess3D_GetLastInfo(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    return game != nullptr ? copyString(game->lastInfo, buffer, capacity) : 0;
}
