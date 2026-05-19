#ifndef NOMINMAX
#define NOMINMAX
#endif

#include "ChessEngine.h"

#include <Windows.h>

#include <algorithm>
#include <array>
#include <chrono>
#include <cctype>
#include <cstdint>
#include <cstdlib>
#include <cstring>
#include <filesystem>
#include <limits>
#include <random>
#include <sstream>
#include <string>
#include <unordered_map>
#include <utility>
#include <vector>

namespace
{
constexpr int White = 1;
constexpr int Black = -1;

constexpr int Empty = 0;
constexpr int Pawn = 1;
constexpr int Knight = 2;
constexpr int Bishop = 3;
constexpr int Rook = 4;
constexpr int Queen = 5;
constexpr int King = 6;

constexpr int MoveCapture = 1;
constexpr int MoveCastle = 2;
constexpr int MoveEnPassant = 4;
constexpr int MovePromotion = 8;
constexpr int MoveCheck = 16;

constexpr int StatusPlaying = 0;
constexpr int StatusCheckmate = 1;
constexpr int StatusStalemate = 2;
constexpr int StatusFiftyMoveClaim = 3;
constexpr int StatusRepetitionClaim = 4;
constexpr int StatusRepetitionDraw = 5;
constexpr int StatusSeventyFiveMoveDraw = 6;

constexpr int MateScore = 1000000;
constexpr int Infinity = 2000000;

struct Move
{
    int from = -1;
    int to = -1;
    int promotion = 0;
    int flags = 0;
    int score = 0;
    int piece = 0;
    int captured = 0;
};

struct Snapshot
{
    std::array<int, 64> board{};
    int side = White;
    bool whiteKingCastle = true;
    bool whiteQueenCastle = true;
    bool blackKingCastle = true;
    bool blackQueenCastle = true;
    int enPassant = -1;
    int halfmoveClock = 0;
    int fullmoveNumber = 1;
    Move lastMove{};
};

struct Position
{
    std::array<int, 64> board{};
    int side = White;
    bool whiteKingCastle = true;
    bool whiteQueenCastle = true;
    bool blackKingCastle = true;
    bool blackQueenCastle = true;
    int enPassant = -1;
    int halfmoveClock = 0;
    int fullmoveNumber = 1;
    Move lastMove{};
    std::vector<Snapshot> history;
};

struct TtEntry
{
    int depth = -1;
    int score = 0;
    int flag = 0; // exact=0, lower=1, upper=2
};

struct SearchOptions
{
    int depth = 3;
    int timeLimitMs = 0;
    bool automaticDepth = false;
    bool useQuiescence = true;
    bool useTranspositionTable = true;
    bool useMoveOrdering = true;
    bool usePieceSquareTables = true;
    bool useBishopPairBonus = true;
    bool useKingSafetyBonus = true;
    bool useGpuEvaluation = false;
    bool useEndgameTables = true;
    int openingRandomness = 0;
    int openingMaxPly = 16;
};

struct TablebaseInfo
{
    bool enabled = true;
    int syzygyWdlFiles = 0;
    int syzygyDtzFiles = 0;
    int maxPieces = 0;
    bool builtInEndgameTables = true;
    std::string path;
};

struct DrawRules
{
    int repetitionClaimCount = 3;
    int repetitionAutoDrawCount = 5;
    bool autoClaimThreefold = true;
    int fiftyMoveClaimPlies = 100;
    int seventyFiveMoveAutoPlies = 150;
    bool autoClaimFiftyMove = false;
};

struct SearchContext
{
    std::unordered_map<std::uint64_t, TtEntry> table;
    std::int64_t nodes = 0;
    int requestedDepth = 0;
    int completedDepth = 0;
    int bestScore = 0;
    bool stop = false;
    SearchOptions options{};
    std::vector<Move> rootResults;
    std::chrono::steady_clock::time_point started{};
};

struct ChessGame
{
    Position pos;
    DrawRules drawRules;
    TablebaseInfo tablebaseInfo;
    bool drawClaimed = false;
    ChessSearchInfoDto lastSearchStats{};
    std::string lastSearchInfo = "Search has not been run yet.";
    std::mt19937 rng{ std::random_device{}() };
};

constexpr std::array<int, 7> Material = { 0, 100, 320, 330, 500, 900, 0 };

constexpr std::array<int, 64> PawnTable = {
    0,   0,   0,   0,   0,   0,   0,   0,
    50,  50,  50,  50,  50,  50,  50,  50,
    10,  10,  20,  30,  30,  20,  10,  10,
    5,   5,   10,  25,  25,  10,  5,   5,
    0,   0,   0,   20,  20,  0,   0,   0,
    5,  -5, -10,   0,   0, -10, -5,   5,
    5,   10,  10, -20, -20,  10,  10,  5,
    0,   0,   0,   0,   0,   0,   0,   0
};

constexpr std::array<int, 64> KnightTable = {
    -50, -40, -30, -30, -30, -30, -40, -50,
    -40, -20,   0,   5,   5,   0, -20, -40,
    -30,   5,  10,  15,  15,  10,   5, -30,
    -30,   0,  15,  20,  20,  15,   0, -30,
    -30,   5,  15,  20,  20,  15,   5, -30,
    -30,   0,  10,  15,  15,  10,   0, -30,
    -40, -20,   0,   0,   0,   0, -20, -40,
    -50, -40, -30, -30, -30, -30, -40, -50
};

constexpr std::array<int, 64> BishopTable = {
    -20, -10, -10, -10, -10, -10, -10, -20,
    -10,   5,   0,   0,   0,   0,   5, -10,
    -10,  10,  10,  10,  10,  10,  10, -10,
    -10,   0,  10,  10,  10,  10,   0, -10,
    -10,   5,   5,  10,  10,   5,   5, -10,
    -10,   0,   5,  10,  10,   5,   0, -10,
    -10,   0,   0,   0,   0,   0,   0, -10,
    -20, -10, -10, -10, -10, -10, -10, -20
};

constexpr std::array<int, 64> RookTable = {
    0,   0,   0,   5,   5,   0,   0,   0,
    -5,  0,   0,   0,   0,   0,   0,  -5,
    -5,  0,   0,   0,   0,   0,   0,  -5,
    -5,  0,   0,   0,   0,   0,   0,  -5,
    -5,  0,   0,   0,   0,   0,   0,  -5,
    -5,  0,   0,   0,   0,   0,   0,  -5,
    5,   10,  10,  10,  10,  10,  10,  5,
    0,   0,   0,   0,   0,   0,   0,   0
};

constexpr std::array<int, 64> QueenTable = {
    -20, -10, -10, -5, -5, -10, -10, -20,
    -10,   0,   5,  0,  0,   0,   0, -10,
    -10,   5,   5,  5,  5,   5,   0, -10,
      0,   0,   5,  5,  5,   5,   0,  -5,
     -5,   0,   5,  5,  5,   5,   0,  -5,
    -10,   0,   5,  5,  5,   5,   0, -10,
    -10,   0,   0,  0,  0,   0,   0, -10,
    -20, -10, -10, -5, -5, -10, -10, -20
};

constexpr std::array<int, 64> KingTable = {
     20,  30,  10,   0,   0,  10,  30,  20,
     20,  20,   0,   0,   0,   0,  20,  20,
    -10, -20, -20, -20, -20, -20, -20, -10,
    -20, -30, -30, -40, -40, -30, -30, -20,
    -30, -40, -40, -50, -50, -40, -40, -30,
    -30, -40, -40, -50, -50, -40, -40, -30,
    -30, -40, -40, -50, -50, -40, -40, -30,
    -30, -40, -40, -50, -50, -40, -40, -30
};

bool isInside(int file, int rank)
{
    return file >= 0 && file < 8 && rank >= 0 && rank < 8;
}

int squareOf(int file, int rank)
{
    return rank * 8 + file;
}

int fileOf(int square)
{
    return square & 7;
}

int rankOf(int square)
{
    return square >> 3;
}

int pieceColor(int piece)
{
    return (piece > 0) - (piece < 0);
}

int pieceType(int piece)
{
    return std::abs(piece);
}

int makePiece(int color, int type)
{
    return color * type;
}

int mirrorSquare(int square)
{
    return squareOf(fileOf(square), 7 - rankOf(square));
}

const std::array<int, 64>& tableFor(int type)
{
    switch (type)
    {
    case Pawn: return PawnTable;
    case Knight: return KnightTable;
    case Bishop: return BishopTable;
    case Rook: return RookTable;
    case Queen: return QueenTable;
    case King: return KingTable;
    default: return PawnTable;
    }
}

int tablebasePieceCountFromName(const std::filesystem::path& file)
{
    int count = 0;
    for (char ch : file.stem().string())
    {
        switch (std::toupper(static_cast<unsigned char>(ch)))
        {
        case 'K':
        case 'Q':
        case 'R':
        case 'B':
        case 'N':
        case 'P':
            ++count;
            break;
        default:
            break;
        }
    }
    return count;
}

void scanTablebasePath(TablebaseInfo& info, const std::string& pathList)
{
    info.path = pathList;
    info.syzygyWdlFiles = 0;
    info.syzygyDtzFiles = 0;
    info.maxPieces = 0;
    info.enabled = true;

    std::size_t start = 0;
    while (start <= pathList.size())
    {
        const std::size_t end = pathList.find(';', start);
        const auto token = pathList.substr(start, end == std::string::npos ? std::string::npos : end - start);
        if (!token.empty())
        {
            std::error_code ec;
            const std::filesystem::path root(token);
            if (std::filesystem::exists(root, ec))
            {
                for (std::filesystem::recursive_directory_iterator it(root, std::filesystem::directory_options::skip_permission_denied, ec), last;
                     !ec && it != last;
                     it.increment(ec))
                {
                    if (!it->is_regular_file(ec))
                    {
                        continue;
                    }

                    auto ext = it->path().extension().string();
                    std::transform(ext.begin(), ext.end(), ext.begin(), [](unsigned char ch) { return static_cast<char>(std::tolower(ch)); });
                    if (ext == ".rtbw")
                    {
                        ++info.syzygyWdlFiles;
                        info.maxPieces = std::max(info.maxPieces, tablebasePieceCountFromName(it->path()));
                    }
                    else if (ext == ".rtbz")
                    {
                        ++info.syzygyDtzFiles;
                        info.maxPieces = std::max(info.maxPieces, tablebasePieceCountFromName(it->path()));
                    }
                }
            }
        }

        if (end == std::string::npos)
        {
            break;
        }
        start = end + 1;
    }
}

Snapshot makeSnapshot(const Position& pos)
{
    Snapshot snap;
    snap.board = pos.board;
    snap.side = pos.side;
    snap.whiteKingCastle = pos.whiteKingCastle;
    snap.whiteQueenCastle = pos.whiteQueenCastle;
    snap.blackKingCastle = pos.blackKingCastle;
    snap.blackQueenCastle = pos.blackQueenCastle;
    snap.enPassant = pos.enPassant;
    snap.halfmoveClock = pos.halfmoveClock;
    snap.fullmoveNumber = pos.fullmoveNumber;
    snap.lastMove = pos.lastMove;
    return snap;
}

void restoreSnapshot(Position& pos, const Snapshot& snap)
{
    pos.board = snap.board;
    pos.side = snap.side;
    pos.whiteKingCastle = snap.whiteKingCastle;
    pos.whiteQueenCastle = snap.whiteQueenCastle;
    pos.blackKingCastle = snap.blackKingCastle;
    pos.blackQueenCastle = snap.blackQueenCastle;
    pos.enPassant = snap.enPassant;
    pos.halfmoveClock = snap.halfmoveClock;
    pos.fullmoveNumber = snap.fullmoveNumber;
    pos.lastMove = snap.lastMove;
}

void resetPosition(Position& pos)
{
    pos = Position{};
    pos.board.fill(Empty);

    pos.board[squareOf(0, 0)] = makePiece(White, Rook);
    pos.board[squareOf(1, 0)] = makePiece(White, Knight);
    pos.board[squareOf(2, 0)] = makePiece(White, Bishop);
    pos.board[squareOf(3, 0)] = makePiece(White, Queen);
    pos.board[squareOf(4, 0)] = makePiece(White, King);
    pos.board[squareOf(5, 0)] = makePiece(White, Bishop);
    pos.board[squareOf(6, 0)] = makePiece(White, Knight);
    pos.board[squareOf(7, 0)] = makePiece(White, Rook);

    for (int file = 0; file < 8; ++file)
    {
        pos.board[squareOf(file, 1)] = makePiece(White, Pawn);
        pos.board[squareOf(file, 6)] = makePiece(Black, Pawn);
    }

    pos.board[squareOf(0, 7)] = makePiece(Black, Rook);
    pos.board[squareOf(1, 7)] = makePiece(Black, Knight);
    pos.board[squareOf(2, 7)] = makePiece(Black, Bishop);
    pos.board[squareOf(3, 7)] = makePiece(Black, Queen);
    pos.board[squareOf(4, 7)] = makePiece(Black, King);
    pos.board[squareOf(5, 7)] = makePiece(Black, Bishop);
    pos.board[squareOf(6, 7)] = makePiece(Black, Knight);
    pos.board[squareOf(7, 7)] = makePiece(Black, Rook);
}

int charToPiece(char ch)
{
    switch (ch)
    {
    case 'P': return makePiece(White, Pawn);
    case 'N': return makePiece(White, Knight);
    case 'B': return makePiece(White, Bishop);
    case 'R': return makePiece(White, Rook);
    case 'Q': return makePiece(White, Queen);
    case 'K': return makePiece(White, King);
    case 'p': return makePiece(Black, Pawn);
    case 'n': return makePiece(Black, Knight);
    case 'b': return makePiece(Black, Bishop);
    case 'r': return makePiece(Black, Rook);
    case 'q': return makePiece(Black, Queen);
    case 'k': return makePiece(Black, King);
    default: return Empty;
    }
}

char pieceToChar(int piece)
{
    switch (piece)
    {
    case 1: return 'P';
    case 2: return 'N';
    case 3: return 'B';
    case 4: return 'R';
    case 5: return 'Q';
    case 6: return 'K';
    case -1: return 'p';
    case -2: return 'n';
    case -3: return 'b';
    case -4: return 'r';
    case -5: return 'q';
    case -6: return 'k';
    default: return '1';
    }
}

int findKing(const Position& pos, int color)
{
    const int king = makePiece(color, King);
    for (int square = 0; square < 64; ++square)
    {
        if (pos.board[square] == king)
        {
            return square;
        }
    }
    return -1;
}

bool isSquareAttacked(const Position& pos, int target, int byColor)
{
    const int file = fileOf(target);
    const int rank = rankOf(target);

    const int pawnRank = byColor == White ? rank - 1 : rank + 1;
    if (pawnRank >= 0 && pawnRank < 8)
    {
        for (int df : { -1, 1 })
        {
            const int pf = file + df;
            if (pf >= 0 && pf < 8 && pos.board[squareOf(pf, pawnRank)] == makePiece(byColor, Pawn))
            {
                return true;
            }
        }
    }

    constexpr std::array<std::pair<int, int>, 8> KnightSteps = {
        std::pair<int, int>{1, 2}, {2, 1}, {2, -1}, {1, -2}, {-1, -2}, {-2, -1}, {-2, 1}, {-1, 2}
    };
    for (const auto& [df, dr] : KnightSteps)
    {
        const int f = file + df;
        const int r = rank + dr;
        if (isInside(f, r) && pos.board[squareOf(f, r)] == makePiece(byColor, Knight))
        {
            return true;
        }
    }

    constexpr std::array<std::pair<int, int>, 8> KingSteps = {
        std::pair<int, int>{1, 1}, {1, 0}, {1, -1}, {0, 1}, {0, -1}, {-1, 1}, {-1, 0}, {-1, -1}
    };
    for (const auto& [df, dr] : KingSteps)
    {
        const int f = file + df;
        const int r = rank + dr;
        if (isInside(f, r) && pos.board[squareOf(f, r)] == makePiece(byColor, King))
        {
            return true;
        }
    }

    constexpr std::array<std::pair<int, int>, 4> BishopDirs = {
        std::pair<int, int>{1, 1}, {1, -1}, {-1, 1}, {-1, -1}
    };
    for (const auto& [df, dr] : BishopDirs)
    {
        int f = file + df;
        int r = rank + dr;
        while (isInside(f, r))
        {
            const int piece = pos.board[squareOf(f, r)];
            if (piece != Empty)
            {
                if (pieceColor(piece) == byColor && (pieceType(piece) == Bishop || pieceType(piece) == Queen))
                {
                    return true;
                }
                break;
            }
            f += df;
            r += dr;
        }
    }

    constexpr std::array<std::pair<int, int>, 4> RookDirs = {
        std::pair<int, int>{1, 0}, {-1, 0}, {0, 1}, {0, -1}
    };
    for (const auto& [df, dr] : RookDirs)
    {
        int f = file + df;
        int r = rank + dr;
        while (isInside(f, r))
        {
            const int piece = pos.board[squareOf(f, r)];
            if (piece != Empty)
            {
                if (pieceColor(piece) == byColor && (pieceType(piece) == Rook || pieceType(piece) == Queen))
                {
                    return true;
                }
                break;
            }
            f += df;
            r += dr;
        }
    }

    return false;
}

bool isInCheck(const Position& pos, int color)
{
    const int kingSquare = findKing(pos, color);
    if (kingSquare < 0)
    {
        return true;
    }
    return isSquareAttacked(pos, kingSquare, -color);
}

void addMove(std::vector<Move>& moves, const Position& pos, int from, int to, int promotion, int flags)
{
    Move move;
    move.from = from;
    move.to = to;
    move.promotion = promotion;
    move.flags = flags;
    move.piece = pos.board[from];

    if (flags & MoveEnPassant)
    {
        move.captured = makePiece(-pieceColor(move.piece), Pawn);
        move.flags |= MoveCapture;
    }
    else
    {
        move.captured = pos.board[to];
        if (move.captured != Empty)
        {
            move.flags |= MoveCapture;
        }
    }

    if (promotion != 0)
    {
        move.flags |= MovePromotion;
    }

    moves.push_back(move);
}

void addPromotions(std::vector<Move>& moves, const Position& pos, int from, int to, int flags)
{
    addMove(moves, pos, from, to, Queen, flags);
    addMove(moves, pos, from, to, Rook, flags);
    addMove(moves, pos, from, to, Bishop, flags);
    addMove(moves, pos, from, to, Knight, flags);
}

void generatePawnMoves(const Position& pos, std::vector<Move>& moves, int square, bool tacticalOnly)
{
    const int color = pieceColor(pos.board[square]);
    const int file = fileOf(square);
    const int rank = rankOf(square);
    const int direction = color == White ? 1 : -1;
    const int startRank = color == White ? 1 : 6;
    const int promotionFromRank = color == White ? 6 : 1;

    const int oneRank = rank + direction;
    if (oneRank >= 0 && oneRank < 8)
    {
        const int forward = squareOf(file, oneRank);
        if (pos.board[forward] == Empty)
        {
            if (rank == promotionFromRank)
            {
                addPromotions(moves, pos, square, forward, 0);
            }
            else if (!tacticalOnly)
            {
                addMove(moves, pos, square, forward, 0, 0);
                if (rank == startRank)
                {
                    const int twoRank = rank + direction * 2;
                    const int doubleForward = squareOf(file, twoRank);
                    if (pos.board[doubleForward] == Empty)
                    {
                        addMove(moves, pos, square, doubleForward, 0, 0);
                    }
                }
            }
        }
    }

    for (int df : { -1, 1 })
    {
        const int targetFile = file + df;
        const int targetRank = rank + direction;
        if (!isInside(targetFile, targetRank))
        {
            continue;
        }

        const int target = squareOf(targetFile, targetRank);
        const int occupant = pos.board[target];
        if (occupant != Empty && pieceColor(occupant) == -color && pieceType(occupant) != King)
        {
            if (rank == promotionFromRank)
            {
                addPromotions(moves, pos, square, target, MoveCapture);
            }
            else
            {
                addMove(moves, pos, square, target, 0, MoveCapture);
            }
        }

        if (target == pos.enPassant)
        {
            addMove(moves, pos, square, target, 0, MoveEnPassant);
        }
    }
}

void generateKnightMoves(const Position& pos, std::vector<Move>& moves, int square, bool tacticalOnly)
{
    constexpr std::array<std::pair<int, int>, 8> Steps = {
        std::pair<int, int>{1, 2}, {2, 1}, {2, -1}, {1, -2}, {-1, -2}, {-2, -1}, {-2, 1}, {-1, 2}
    };
    const int color = pieceColor(pos.board[square]);
    const int file = fileOf(square);
    const int rank = rankOf(square);
    for (const auto& [df, dr] : Steps)
    {
        const int f = file + df;
        const int r = rank + dr;
        if (!isInside(f, r))
        {
            continue;
        }
        const int target = squareOf(f, r);
        const int occupant = pos.board[target];
        if (occupant == Empty)
        {
            if (!tacticalOnly)
            {
                addMove(moves, pos, square, target, 0, 0);
            }
        }
        else if (pieceColor(occupant) == -color && pieceType(occupant) != King)
        {
            addMove(moves, pos, square, target, 0, MoveCapture);
        }
    }
}

void generateSliderMoves(const Position& pos, std::vector<Move>& moves, int square, bool tacticalOnly, const std::vector<std::pair<int, int>>& dirs)
{
    const int color = pieceColor(pos.board[square]);
    for (const auto& [df, dr] : dirs)
    {
        int f = fileOf(square) + df;
        int r = rankOf(square) + dr;
        while (isInside(f, r))
        {
            const int target = squareOf(f, r);
            const int occupant = pos.board[target];
            if (occupant == Empty)
            {
                if (!tacticalOnly)
                {
                    addMove(moves, pos, square, target, 0, 0);
                }
            }
            else
            {
                if (pieceColor(occupant) == -color && pieceType(occupant) != King)
                {
                    addMove(moves, pos, square, target, 0, MoveCapture);
                }
                break;
            }
            f += df;
            r += dr;
        }
    }
}

void generateKingMoves(const Position& pos, std::vector<Move>& moves, int square, bool tacticalOnly)
{
    constexpr std::array<std::pair<int, int>, 8> Steps = {
        std::pair<int, int>{1, 1}, {1, 0}, {1, -1}, {0, 1}, {0, -1}, {-1, 1}, {-1, 0}, {-1, -1}
    };
    const int color = pieceColor(pos.board[square]);
    const int file = fileOf(square);
    const int rank = rankOf(square);
    for (const auto& [df, dr] : Steps)
    {
        const int f = file + df;
        const int r = rank + dr;
        if (!isInside(f, r))
        {
            continue;
        }
        const int target = squareOf(f, r);
        const int occupant = pos.board[target];
        if (occupant == Empty)
        {
            if (!tacticalOnly)
            {
                addMove(moves, pos, square, target, 0, 0);
            }
        }
        else if (pieceColor(occupant) == -color && pieceType(occupant) != King)
        {
            addMove(moves, pos, square, target, 0, MoveCapture);
        }
    }

    if (tacticalOnly || isInCheck(pos, color))
    {
        return;
    }

    if (color == White && square == squareOf(4, 0))
    {
        if (pos.whiteKingCastle && pos.board[squareOf(5, 0)] == Empty && pos.board[squareOf(6, 0)] == Empty &&
            pos.board[squareOf(7, 0)] == makePiece(White, Rook) &&
            !isSquareAttacked(pos, squareOf(5, 0), Black) && !isSquareAttacked(pos, squareOf(6, 0), Black))
        {
            addMove(moves, pos, square, squareOf(6, 0), 0, MoveCastle);
        }

        if (pos.whiteQueenCastle && pos.board[squareOf(3, 0)] == Empty && pos.board[squareOf(2, 0)] == Empty &&
            pos.board[squareOf(1, 0)] == Empty && pos.board[squareOf(0, 0)] == makePiece(White, Rook) &&
            !isSquareAttacked(pos, squareOf(3, 0), Black) && !isSquareAttacked(pos, squareOf(2, 0), Black))
        {
            addMove(moves, pos, square, squareOf(2, 0), 0, MoveCastle);
        }
    }
    else if (color == Black && square == squareOf(4, 7))
    {
        if (pos.blackKingCastle && pos.board[squareOf(5, 7)] == Empty && pos.board[squareOf(6, 7)] == Empty &&
            pos.board[squareOf(7, 7)] == makePiece(Black, Rook) &&
            !isSquareAttacked(pos, squareOf(5, 7), White) && !isSquareAttacked(pos, squareOf(6, 7), White))
        {
            addMove(moves, pos, square, squareOf(6, 7), 0, MoveCastle);
        }

        if (pos.blackQueenCastle && pos.board[squareOf(3, 7)] == Empty && pos.board[squareOf(2, 7)] == Empty &&
            pos.board[squareOf(1, 7)] == Empty && pos.board[squareOf(0, 7)] == makePiece(Black, Rook) &&
            !isSquareAttacked(pos, squareOf(3, 7), White) && !isSquareAttacked(pos, squareOf(2, 7), White))
        {
            addMove(moves, pos, square, squareOf(2, 7), 0, MoveCastle);
        }
    }
}

std::vector<Move> generatePseudoMoves(const Position& pos, bool tacticalOnly)
{
    std::vector<Move> moves;
    moves.reserve(96);

    static const std::vector<std::pair<int, int>> BishopDirs = { {1, 1}, {1, -1}, {-1, 1}, {-1, -1} };
    static const std::vector<std::pair<int, int>> RookDirs = { {1, 0}, {-1, 0}, {0, 1}, {0, -1} };
    static const std::vector<std::pair<int, int>> QueenDirs = { {1, 1}, {1, -1}, {-1, 1}, {-1, -1}, {1, 0}, {-1, 0}, {0, 1}, {0, -1} };

    for (int square = 0; square < 64; ++square)
    {
        const int piece = pos.board[square];
        if (piece == Empty || pieceColor(piece) != pos.side)
        {
            continue;
        }

        switch (pieceType(piece))
        {
        case Pawn:
            generatePawnMoves(pos, moves, square, tacticalOnly);
            break;
        case Knight:
            generateKnightMoves(pos, moves, square, tacticalOnly);
            break;
        case Bishop:
            generateSliderMoves(pos, moves, square, tacticalOnly, BishopDirs);
            break;
        case Rook:
            generateSliderMoves(pos, moves, square, tacticalOnly, RookDirs);
            break;
        case Queen:
            generateSliderMoves(pos, moves, square, tacticalOnly, QueenDirs);
            break;
        case King:
            generateKingMoves(pos, moves, square, tacticalOnly);
            break;
        default:
            break;
        }
    }

    return moves;
}

void clearCastlingBySquare(Position& pos, int square, int piece)
{
    if (piece == makePiece(White, Rook))
    {
        if (square == squareOf(0, 0)) pos.whiteQueenCastle = false;
        if (square == squareOf(7, 0)) pos.whiteKingCastle = false;
    }
    else if (piece == makePiece(Black, Rook))
    {
        if (square == squareOf(0, 7)) pos.blackQueenCastle = false;
        if (square == squareOf(7, 7)) pos.blackKingCastle = false;
    }
}

void applyMove(Position& pos, Move& move, bool keepHistory)
{
    if (keepHistory)
    {
        pos.history.push_back(makeSnapshot(pos));
    }

    const int movingPiece = pos.board[move.from];
    const int color = pieceColor(movingPiece);
    const int type = pieceType(movingPiece);
    move.piece = movingPiece;
    move.captured = (move.flags & MoveEnPassant) ? makePiece(-color, Pawn) : pos.board[move.to];

    clearCastlingBySquare(pos, move.from, movingPiece);
    if (move.captured != Empty)
    {
        clearCastlingBySquare(pos, move.to, move.captured);
    }

    if (type == King)
    {
        if (color == White)
        {
            pos.whiteKingCastle = false;
            pos.whiteQueenCastle = false;
        }
        else
        {
            pos.blackKingCastle = false;
            pos.blackQueenCastle = false;
        }
    }

    pos.board[move.from] = Empty;

    if (move.flags & MoveEnPassant)
    {
        const int capturedSquare = move.to - color * 8;
        pos.board[capturedSquare] = Empty;
    }

    int placedPiece = movingPiece;
    if (move.promotion != 0)
    {
        placedPiece = makePiece(color, move.promotion);
    }
    pos.board[move.to] = placedPiece;

    if (move.flags & MoveCastle)
    {
        if (move.to == squareOf(6, 0))
        {
            pos.board[squareOf(5, 0)] = pos.board[squareOf(7, 0)];
            pos.board[squareOf(7, 0)] = Empty;
        }
        else if (move.to == squareOf(2, 0))
        {
            pos.board[squareOf(3, 0)] = pos.board[squareOf(0, 0)];
            pos.board[squareOf(0, 0)] = Empty;
        }
        else if (move.to == squareOf(6, 7))
        {
            pos.board[squareOf(5, 7)] = pos.board[squareOf(7, 7)];
            pos.board[squareOf(7, 7)] = Empty;
        }
        else if (move.to == squareOf(2, 7))
        {
            pos.board[squareOf(3, 7)] = pos.board[squareOf(0, 7)];
            pos.board[squareOf(0, 7)] = Empty;
        }
    }

    pos.enPassant = -1;
    if (type == Pawn && std::abs(move.to - move.from) == 16)
    {
        pos.enPassant = move.from + color * 8;
    }

    if (type == Pawn || move.captured != Empty)
    {
        pos.halfmoveClock = 0;
    }
    else
    {
        ++pos.halfmoveClock;
    }

    if (pos.side == Black)
    {
        ++pos.fullmoveNumber;
    }

    pos.side = -pos.side;
    pos.lastMove = move;
}

std::vector<Move> generateLegalMoves(const Position& pos, bool tacticalOnly)
{
    std::vector<Move> legal;
    const auto pseudo = generatePseudoMoves(pos, tacticalOnly);
    legal.reserve(pseudo.size());

    for (Move move : pseudo)
    {
        Position child = pos;
        applyMove(child, move, false);
        if (!isInCheck(child, -child.side))
        {
            if (isInCheck(child, child.side))
            {
                move.flags |= MoveCheck;
            }
            legal.push_back(move);
        }
    }

    return legal;
}

struct MaterialProfile
{
    int pieceCount = 0;
    int pawns[2]{};
    int knights[2]{};
    int bishops[2]{};
    int rooks[2]{};
    int queens[2]{};
    int nonKingMaterial[2]{};
};

int sideIndex(int color)
{
    return color == White ? 0 : 1;
}

MaterialProfile materialProfile(const Position& pos)
{
    MaterialProfile profile;
    for (int square = 0; square < 64; ++square)
    {
        const int piece = pos.board[square];
        if (piece == Empty)
        {
            continue;
        }

        ++profile.pieceCount;
        const int index = sideIndex(pieceColor(piece));
        const int type = pieceType(piece);
        if (type != King)
        {
            profile.nonKingMaterial[index] += Material[type];
        }
        if (type == Pawn) ++profile.pawns[index];
        else if (type == Knight) ++profile.knights[index];
        else if (type == Bishop) ++profile.bishops[index];
        else if (type == Rook) ++profile.rooks[index];
        else if (type == Queen) ++profile.queens[index];
    }
    return profile;
}

bool hasOnlyMinorPieces(const MaterialProfile& profile)
{
    return profile.pawns[0] == 0 && profile.pawns[1] == 0 &&
        profile.rooks[0] == 0 && profile.rooks[1] == 0 &&
        profile.queens[0] == 0 && profile.queens[1] == 0;
}

bool isBuiltInDrawnEndgame(const Position& pos)
{
    const auto profile = materialProfile(pos);
    if (profile.pieceCount == 2)
    {
        return true;
    }
    if (!hasOnlyMinorPieces(profile))
    {
        return false;
    }

    const int whiteMinors = profile.knights[0] + profile.bishops[0];
    const int blackMinors = profile.knights[1] + profile.bishops[1];
    if ((whiteMinors <= 1 && blackMinors == 0) || (blackMinors <= 1 && whiteMinors == 0))
    {
        return true;
    }
    if (whiteMinors <= 1 && blackMinors <= 1)
    {
        return true;
    }
    if ((profile.knights[0] == 2 && whiteMinors == 2 && blackMinors == 0) ||
        (profile.knights[1] == 2 && blackMinors == 2 && whiteMinors == 0))
    {
        return true;
    }
    return false;
}

bool tryBuiltInEndgameProbe(const Position& pos, int& whiteScore)
{
    if (isBuiltInDrawnEndgame(pos))
    {
        whiteScore = 0;
        return true;
    }
    return false;
}

bool isPassedPawn(const Position& pos, int square, int color)
{
    const int file = fileOf(square);
    const int rank = rankOf(square);
    const int direction = color == White ? 1 : -1;
    const int enemyPawn = makePiece(-color, Pawn);
    for (int df = -1; df <= 1; ++df)
    {
        const int f = file + df;
        if (f < 0 || f >= 8)
        {
            continue;
        }
        for (int r = rank + direction; r >= 0 && r < 8; r += direction)
        {
            if (pos.board[squareOf(f, r)] == enemyPawn)
            {
                return false;
            }
        }
    }
    return true;
}

bool isIsolatedPawn(const Position& pos, int square, int color)
{
    const int file = fileOf(square);
    const int friendlyPawn = makePiece(color, Pawn);
    for (int df : { -1, 1 })
    {
        const int f = file + df;
        if (f < 0 || f >= 8)
        {
            continue;
        }
        for (int rank = 0; rank < 8; ++rank)
        {
            if (pos.board[squareOf(f, rank)] == friendlyPawn)
            {
                return false;
            }
        }
    }
    return true;
}

int kingCentrality(int square)
{
    return 14 - (std::abs(fileOf(square) - 3) + std::abs(rankOf(square) - 3)) * 2;
}

int kingEdgeBonus(int square)
{
    const int file = fileOf(square);
    const int rank = rankOf(square);
    const int edgeDistance = std::min(std::min(file, 7 - file), std::min(rank, 7 - rank));
    return (3 - edgeDistance) * 18;
}

int kingDistance(int left, int right)
{
    return std::max(std::abs(fileOf(left) - fileOf(right)), std::abs(rankOf(left) - rankOf(right)));
}

int pieceSquareValue(int piece, int square, const SearchOptions& options)
{
    const int type = pieceType(piece);
    if (type == 0)
    {
        return 0;
    }
    const int color = pieceColor(piece);
    int value = Material[type];
    if (options.usePieceSquareTables)
    {
        const auto& table = tableFor(type);
        const int lookupSquare = color == White ? square : mirrorSquare(square);
        value += table[lookupSquare];
    }
    return color * value;
}

int evaluateWhite(const Position& pos, const SearchOptions& options)
{
    int exactEndgameScore = 0;
    if (options.useEndgameTables && tryBuiltInEndgameProbe(pos, exactEndgameScore))
    {
        return exactEndgameScore;
    }

    int score = 0;
    int whiteBishops = 0;
    int blackBishops = 0;
    const auto profile = materialProfile(pos);
    const bool endgame = profile.nonKingMaterial[0] + profile.nonKingMaterial[1] <= 2600 ||
        (profile.queens[0] == 0 && profile.queens[1] == 0);

    for (int square = 0; square < 64; ++square)
    {
        const int piece = pos.board[square];
        if (piece == Empty)
        {
            continue;
        }
        score += pieceSquareValue(piece, square, options);
        if (piece == makePiece(White, Bishop)) ++whiteBishops;
        if (piece == makePiece(Black, Bishop)) ++blackBishops;

        const int type = pieceType(piece);
        const int color = pieceColor(piece);
        if (type == Pawn)
        {
            const int friendlyRank = color == White ? rankOf(square) : 7 - rankOf(square);
            if (isPassedPawn(pos, square, color))
            {
                const int passed = 18 + friendlyRank * friendlyRank * 3 + (endgame ? 18 : 0);
                score += color * passed;
            }
            if (isIsolatedPawn(pos, square, color))
            {
                score -= color * 10;
            }
        }
    }

    if (options.useBishopPairBonus)
    {
        if (whiteBishops >= 2) score += 35;
        if (blackBishops >= 2) score -= 35;
    }

    if (options.useKingSafetyBonus && !pos.whiteKingCastle && !pos.whiteQueenCastle && pos.board[squareOf(6, 0)] == makePiece(White, King))
    {
        score += 25;
    }
    if (options.useKingSafetyBonus && !pos.blackKingCastle && !pos.blackQueenCastle && pos.board[squareOf(6, 7)] == makePiece(Black, King))
    {
        score -= 25;
    }

    if (endgame)
    {
        const int whiteKing = findKing(pos, White);
        const int blackKing = findKing(pos, Black);
        score += kingCentrality(whiteKing) * 5;
        score -= kingCentrality(blackKing) * 5;

        const int materialDiff = profile.nonKingMaterial[0] - profile.nonKingMaterial[1];
        if (materialDiff >= 500 && profile.pawns[1] == 0)
        {
            score += kingEdgeBonus(blackKing) + (14 - kingDistance(whiteKing, blackKing)) * 8;
        }
        else if (materialDiff <= -500 && profile.pawns[0] == 0)
        {
            score -= kingEdgeBonus(whiteKing) + (14 - kingDistance(whiteKing, blackKing)) * 8;
        }
    }

    return score;
}

int moveOrderingScore(const Move& move)
{
    int result = 0;
    if (move.flags & MoveCapture)
    {
        result += 10000 + Material[pieceType(move.captured)] * 10 - Material[pieceType(move.piece)];
    }
    if (move.flags & MovePromotion)
    {
        result += 8000 + Material[move.promotion];
    }
    if (move.flags & MoveCheck)
    {
        result += 1000;
    }
    if (move.flags & MoveCastle)
    {
        result += 250;
    }
    return result;
}

void orderMoves(std::vector<Move>& moves)
{
    std::sort(moves.begin(), moves.end(), [](const Move& left, const Move& right)
    {
        return moveOrderingScore(left) > moveOrderingScore(right);
    });
}

int effectiveEnPassantSquare(const std::array<int, 64>& board, int side, int enPassant)
{
    if (enPassant < 0)
    {
        return -1;
    }

    const int targetFile = fileOf(enPassant);
    const int targetRank = rankOf(enPassant);
    const int direction = side == White ? 1 : -1;
    const int pawnRank = targetRank - direction;
    if (pawnRank < 0 || pawnRank >= 8)
    {
        return -1;
    }

    for (int df : { -1, 1 })
    {
        const int pawnFile = targetFile + df;
        if (!isInside(pawnFile, pawnRank))
        {
            continue;
        }
        if (board[squareOf(pawnFile, pawnRank)] == makePiece(side, Pawn))
        {
            return enPassant;
        }
    }

    return -1;
}

std::uint64_t hashCore(const std::array<int, 64>& board, int side, bool wkc, bool wqc, bool bkc, bool bqc, int enPassant)
{
    std::uint64_t hash = 1469598103934665603ull;
    auto mix = [&hash](std::uint64_t value)
    {
        hash ^= value + 0x9e3779b97f4a7c15ull + (hash << 6) + (hash >> 2);
    };

    for (int square = 0; square < 64; ++square)
    {
        mix(static_cast<std::uint64_t>((board[square] + 7) * 67 + square));
    }
    mix(side == White ? 1ull : 2ull);
    mix(wkc ? 3ull : 4ull);
    mix(wqc ? 5ull : 6ull);
    mix(bkc ? 7ull : 8ull);
    mix(bqc ? 9ull : 10ull);
    mix(static_cast<std::uint64_t>(effectiveEnPassantSquare(board, side, enPassant) + 2));
    return hash;
}

std::uint64_t hashPosition(const Position& pos)
{
    return hashCore(pos.board, pos.side, pos.whiteKingCastle, pos.whiteQueenCastle, pos.blackKingCastle, pos.blackQueenCastle, pos.enPassant);
}

std::uint64_t hashSnapshot(const Snapshot& snap)
{
    return hashCore(snap.board, snap.side, snap.whiteKingCastle, snap.whiteQueenCastle, snap.blackKingCastle, snap.blackQueenCastle, snap.enPassant);
}

int repetitionCount(const Position& pos)
{
    const auto current = hashPosition(pos);
    int count = 1;
    for (const auto& snap : pos.history)
    {
        if (hashSnapshot(snap) == current)
        {
            ++count;
        }
    }
    return count;
}

bool isDrawByRules(const Position& pos, const DrawRules& rules)
{
    if (rules.seventyFiveMoveAutoPlies > 0 && pos.halfmoveClock >= rules.seventyFiveMoveAutoPlies)
    {
        return true;
    }
    if (rules.autoClaimFiftyMove && rules.fiftyMoveClaimPlies > 0 && pos.halfmoveClock >= rules.fiftyMoveClaimPlies)
    {
        return true;
    }
    const int reps = repetitionCount(pos);
    if (rules.repetitionAutoDrawCount > 0 && reps >= rules.repetitionAutoDrawCount)
    {
        return true;
    }
    return rules.autoClaimThreefold && rules.repetitionClaimCount > 0 && reps >= rules.repetitionClaimCount;
}

bool shouldStop(SearchContext& context)
{
    if (context.stop || context.options.timeLimitMs <= 0)
    {
        return context.stop;
    }

    if ((context.nodes & 2047) != 0)
    {
        return false;
    }

    const auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::steady_clock::now() - context.started).count();
    context.stop = elapsed >= context.options.timeLimitMs;
    return context.stop;
}

int quiescence(Position& pos, int alpha, int beta, int ply, SearchContext& context);

int negamax(Position& pos, int depth, int alpha, int beta, int ply, SearchContext& context)
{
    ++context.nodes;
    if (shouldStop(context))
    {
        return pos.side * evaluateWhite(pos, context.options);
    }

    const int alphaOriginal = alpha;
    const std::uint64_t hash = hashPosition(pos);

    if (context.options.useTranspositionTable)
    {
        if (const auto found = context.table.find(hash); found != context.table.end() && found->second.depth >= depth)
        {
            const TtEntry& entry = found->second;
            if (entry.flag == 0)
            {
                return entry.score;
            }
            if (entry.flag == 1)
            {
                alpha = std::max(alpha, entry.score);
            }
            else if (entry.flag == 2)
            {
                beta = std::min(beta, entry.score);
            }
            if (alpha >= beta)
            {
                return entry.score;
            }
        }
    }

    if (pos.halfmoveClock >= 100)
    {
        return 0;
    }
    if (repetitionCount(pos) >= 3)
    {
        return 0;
    }
    int exactEndgameScore = 0;
    if (context.options.useEndgameTables && tryBuiltInEndgameProbe(pos, exactEndgameScore))
    {
        return pos.side * exactEndgameScore;
    }

    if (depth <= 0)
    {
        return context.options.useQuiescence
            ? quiescence(pos, alpha, beta, ply, context)
            : pos.side * evaluateWhite(pos, context.options);
    }

    auto moves = generateLegalMoves(pos, false);
    if (moves.empty())
    {
        return isInCheck(pos, pos.side) ? -MateScore + ply : 0;
    }

    if (context.options.useMoveOrdering)
    {
        orderMoves(moves);
    }
    int best = -Infinity;
    for (Move move : moves)
    {
        Position child = pos;
        applyMove(child, move, true);
        const int score = -negamax(child, depth - 1, -beta, -alpha, ply + 1, context);
        best = std::max(best, score);
        alpha = std::max(alpha, score);
        if (alpha >= beta)
        {
            break;
        }
    }

    if (context.options.useTranspositionTable && !context.stop)
    {
        TtEntry entry;
        entry.depth = depth;
        entry.score = best;
        if (best <= alphaOriginal)
        {
            entry.flag = 2;
        }
        else if (best >= beta)
        {
            entry.flag = 1;
        }
        else
        {
            entry.flag = 0;
        }
        context.table[hash] = entry;
    }

    return best;
}

int quiescence(Position& pos, int alpha, int beta, int ply, SearchContext& context)
{
    ++context.nodes;
    if (shouldStop(context))
    {
        return pos.side * evaluateWhite(pos, context.options);
    }

    if (ply > 64 || pos.halfmoveClock >= 100)
    {
        return pos.side * evaluateWhite(pos, context.options);
    }
    if (repetitionCount(pos) >= 3)
    {
        return 0;
    }
    int exactEndgameScore = 0;
    if (context.options.useEndgameTables && tryBuiltInEndgameProbe(pos, exactEndgameScore))
    {
        return pos.side * exactEndgameScore;
    }

    const bool inCheck = isInCheck(pos, pos.side);
    if (!inCheck)
    {
        const int standPat = pos.side * evaluateWhite(pos, context.options);
        if (standPat >= beta)
        {
            return beta;
        }
        alpha = std::max(alpha, standPat);
    }

    auto moves = generateLegalMoves(pos, !inCheck);
    if (moves.empty())
    {
        return inCheck ? -MateScore + ply : alpha;
    }
    if (context.options.useMoveOrdering)
    {
        orderMoves(moves);
    }

    for (Move move : moves)
    {
        Position child = pos;
        applyMove(child, move, true);
        const int score = -quiescence(child, -beta, -alpha, ply + 1, context);
        if (score >= beta)
        {
            return beta;
        }
        alpha = std::max(alpha, score);
    }

    return alpha;
}

bool tryParseFen(Position& pos, const std::string& fen)
{
    std::istringstream input(fen);
    std::string boardPart;
    std::string sidePart;
    std::string castlingPart;
    std::string epPart;
    int halfmove = 0;
    int fullmove = 1;

    if (!(input >> boardPart >> sidePart >> castlingPart >> epPart))
    {
        return false;
    }
    if (!(input >> halfmove))
    {
        halfmove = 0;
    }
    if (!(input >> fullmove))
    {
        fullmove = 1;
    }

    Position parsed;
    parsed.board.fill(Empty);

    int rank = 7;
    int file = 0;
    for (char ch : boardPart)
    {
        if (ch == '/')
        {
            if (file != 8)
            {
                return false;
            }
            --rank;
            file = 0;
            continue;
        }

        if (std::isdigit(static_cast<unsigned char>(ch)))
        {
            file += ch - '0';
        }
        else
        {
            const int piece = charToPiece(ch);
            if (piece == Empty || !isInside(file, rank))
            {
                return false;
            }
            parsed.board[squareOf(file, rank)] = piece;
            ++file;
        }

        if (file > 8)
        {
            return false;
        }
    }

    if (rank != 0 || file != 8)
    {
        return false;
    }

    if (sidePart == "w")
    {
        parsed.side = White;
    }
    else if (sidePart == "b")
    {
        parsed.side = Black;
    }
    else
    {
        return false;
    }

    parsed.whiteKingCastle = castlingPart.find('K') != std::string::npos;
    parsed.whiteQueenCastle = castlingPart.find('Q') != std::string::npos;
    parsed.blackKingCastle = castlingPart.find('k') != std::string::npos;
    parsed.blackQueenCastle = castlingPart.find('q') != std::string::npos;

    parsed.enPassant = -1;
    if (epPart != "-")
    {
        if (epPart.size() != 2 || epPart[0] < 'a' || epPart[0] > 'h' || epPart[1] < '1' || epPart[1] > '8')
        {
            return false;
        }
        parsed.enPassant = squareOf(epPart[0] - 'a', epPart[1] - '1');
    }

    parsed.halfmoveClock = std::max(0, halfmove);
    parsed.fullmoveNumber = std::max(1, fullmove);
    parsed.history.clear();
    parsed.lastMove = Move{};

    int whiteKings = 0;
    int blackKings = 0;
    for (const int piece : parsed.board)
    {
        if (piece == makePiece(White, King)) ++whiteKings;
        if (piece == makePiece(Black, King)) ++blackKings;
    }

    if (whiteKings != 1 || blackKings != 1)
    {
        return false;
    }

    if (isSquareAttacked(parsed, findKing(parsed, White), Black) &&
        isSquareAttacked(parsed, findKing(parsed, Black), White))
    {
        return false;
    }

    pos = parsed;
    return true;
}

std::string makeFen(const Position& pos)
{
    std::ostringstream output;
    for (int rank = 7; rank >= 0; --rank)
    {
        int empty = 0;
        for (int file = 0; file < 8; ++file)
        {
            const int piece = pos.board[squareOf(file, rank)];
            if (piece == Empty)
            {
                ++empty;
            }
            else
            {
                if (empty > 0)
                {
                    output << empty;
                    empty = 0;
                }
                output << pieceToChar(piece);
            }
        }
        if (empty > 0)
        {
            output << empty;
        }
        if (rank > 0)
        {
            output << '/';
        }
    }

    output << (pos.side == White ? " w " : " b ");
    std::string castling;
    if (pos.whiteKingCastle) castling += 'K';
    if (pos.whiteQueenCastle) castling += 'Q';
    if (pos.blackKingCastle) castling += 'k';
    if (pos.blackQueenCastle) castling += 'q';
    output << (castling.empty() ? "-" : castling) << ' ';
    if (pos.enPassant >= 0)
    {
        output << static_cast<char>('a' + fileOf(pos.enPassant)) << static_cast<char>('1' + rankOf(pos.enPassant));
    }
    else
    {
        output << '-';
    }
    output << ' ' << pos.halfmoveClock << ' ' << pos.fullmoveNumber;
    return output.str();
}

ChessMoveDto toDto(const Move& move)
{
    ChessMoveDto dto{};
    dto.fromFile = move.from >= 0 ? fileOf(move.from) : -1;
    dto.fromRank = move.from >= 0 ? rankOf(move.from) : -1;
    dto.toFile = move.to >= 0 ? fileOf(move.to) : -1;
    dto.toRank = move.to >= 0 ? rankOf(move.to) : -1;
    dto.promotion = move.promotion;
    dto.flags = move.flags;
    dto.score = move.score;
    return dto;
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

ChessGame* asGame(void* handle)
{
    return static_cast<ChessGame*>(handle);
}

int statusOf(const Position& pos, int legalCount, const DrawRules& rules, bool drawClaimed)
{
    if (legalCount == 0)
    {
        return isInCheck(pos, pos.side) ? StatusCheckmate : StatusStalemate;
    }

    const int reps = repetitionCount(pos);
    if (rules.seventyFiveMoveAutoPlies > 0 && pos.halfmoveClock >= rules.seventyFiveMoveAutoPlies)
    {
        return StatusSeventyFiveMoveDraw;
    }
    if (rules.repetitionAutoDrawCount > 0 && reps >= rules.repetitionAutoDrawCount)
    {
        return StatusRepetitionDraw;
    }
    if (drawClaimed)
    {
        return StatusRepetitionDraw;
    }
    if (rules.autoClaimThreefold && rules.repetitionClaimCount > 0 && reps >= rules.repetitionClaimCount)
    {
        return StatusRepetitionDraw;
    }
    if (rules.repetitionClaimCount > 0 && reps >= rules.repetitionClaimCount)
    {
        return StatusRepetitionClaim;
    }
    if (rules.autoClaimFiftyMove && rules.fiftyMoveClaimPlies > 0 && pos.halfmoveClock >= rules.fiftyMoveClaimPlies)
    {
        return StatusFiftyMoveClaim;
    }
    if (rules.fiftyMoveClaimPlies > 0 && pos.halfmoveClock >= rules.fiftyMoveClaimPlies)
    {
        return StatusFiftyMoveClaim;
    }
    return StatusPlaying;
}

std::string moveToLongAlgebraic(const Move& move)
{
    if (move.from < 0 || move.to < 0)
    {
        return "(none)";
    }

    std::string result;
    result += static_cast<char>('a' + fileOf(move.from));
    result += static_cast<char>('1' + rankOf(move.from));
    result += (move.flags & MoveCapture) ? 'x' : '-';
    result += static_cast<char>('a' + fileOf(move.to));
    result += static_cast<char>('1' + rankOf(move.to));
    if (move.promotion != 0)
    {
        result += '=';
        result += pieceToChar(makePiece(White, move.promotion));
    }
    if (move.flags & MoveCheck)
    {
        result += '+';
    }
    return result;
}
}

CHESS_API void* Chess_Create()
{
    auto* game = new ChessGame();
    resetPosition(game->pos);
    return game;
}

CHESS_API void Chess_Destroy(void* handle)
{
    delete asGame(handle);
}

CHESS_API void Chess_Reset(void* handle)
{
    if (auto* game = asGame(handle))
    {
        resetPosition(game->pos);
        game->drawClaimed = false;
        game->lastSearchStats = ChessSearchInfoDto{};
        game->lastSearchInfo = "New game.";
    }
}

CHESS_API int Chess_SetFen(void* handle, const char* fen)
{
    auto* game = asGame(handle);
    if (game == nullptr || fen == nullptr)
    {
        return 0;
    }
    const bool ok = tryParseFen(game->pos, fen);
    if (ok)
    {
        game->drawClaimed = false;
        game->lastSearchStats = ChessSearchInfoDto{};
        game->lastSearchInfo = "Position loaded from FEN.";
    }
    return ok ? 1 : 0;
}

CHESS_API int Chess_GetFen(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    if (game == nullptr)
    {
        return 0;
    }
    return copyString(makeFen(game->pos), buffer, capacity);
}

CHESS_API int Chess_GetBoard(void* handle, int* pieces64)
{
    auto* game = asGame(handle);
    if (game == nullptr || pieces64 == nullptr)
    {
        return 0;
    }

    for (int i = 0; i < 64; ++i)
    {
        pieces64[i] = game->pos.board[i];
    }
    return 1;
}

CHESS_API int Chess_GetState(void* handle, ChessStateDto* state)
{
    auto* game = asGame(handle);
    if (game == nullptr || state == nullptr)
    {
        return 0;
    }

    const auto legal = generateLegalMoves(game->pos, false);
    const int reps = repetitionCount(game->pos);
    *state = ChessStateDto{};
    state->sideToMove = game->pos.side;
    state->isCheck = isInCheck(game->pos, game->pos.side) ? 1 : 0;
    state->status = statusOf(game->pos, static_cast<int>(legal.size()), game->drawRules, game->drawClaimed);
    state->halfmoveClock = game->pos.halfmoveClock;
    state->fullmoveNumber = game->pos.fullmoveNumber;
    state->legalMoveCount = static_cast<int>(legal.size());
    state->lastFromFile = game->pos.lastMove.from >= 0 ? fileOf(game->pos.lastMove.from) : -1;
    state->lastFromRank = game->pos.lastMove.from >= 0 ? rankOf(game->pos.lastMove.from) : -1;
    state->lastToFile = game->pos.lastMove.to >= 0 ? fileOf(game->pos.lastMove.to) : -1;
    state->lastToRank = game->pos.lastMove.to >= 0 ? rankOf(game->pos.lastMove.to) : -1;
    state->lastPromotion = game->pos.lastMove.promotion;
    state->lastFlags = game->pos.lastMove.flags;
    state->repetitionCount = reps;
    state->canClaimRepetition = game->drawRules.repetitionClaimCount > 0 && reps >= game->drawRules.repetitionClaimCount ? 1 : 0;
    state->canClaimFiftyMove = game->drawRules.fiftyMoveClaimPlies > 0 && game->pos.halfmoveClock >= game->drawRules.fiftyMoveClaimPlies ? 1 : 0;
    return 1;
}

CHESS_API int Chess_GetDrawRules(void* handle, ChessDrawRulesDto* rules)
{
    auto* game = asGame(handle);
    if (game == nullptr || rules == nullptr)
    {
        return 0;
    }

    *rules = ChessDrawRulesDto{};
    rules->repetitionClaimCount = game->drawRules.repetitionClaimCount;
    rules->repetitionAutoDrawCount = game->drawRules.repetitionAutoDrawCount;
    rules->autoClaimThreefold = game->drawRules.autoClaimThreefold ? 1 : 0;
    rules->fiftyMoveClaimPlies = game->drawRules.fiftyMoveClaimPlies;
    rules->seventyFiveMoveAutoPlies = game->drawRules.seventyFiveMoveAutoPlies;
    rules->autoClaimFiftyMove = game->drawRules.autoClaimFiftyMove ? 1 : 0;
    return 1;
}

CHESS_API int Chess_SetDrawRules(void* handle, const ChessDrawRulesDto* rules)
{
    auto* game = asGame(handle);
    if (game == nullptr || rules == nullptr)
    {
        return 0;
    }

    game->drawRules.repetitionClaimCount = std::clamp(rules->repetitionClaimCount <= 0 ? 3 : rules->repetitionClaimCount, 2, 20);
    game->drawRules.repetitionAutoDrawCount = std::clamp(rules->repetitionAutoDrawCount <= 0 ? 5 : rules->repetitionAutoDrawCount, 2, 20);
    if (game->drawRules.repetitionAutoDrawCount < game->drawRules.repetitionClaimCount)
    {
        game->drawRules.repetitionAutoDrawCount = game->drawRules.repetitionClaimCount;
    }
    game->drawRules.autoClaimThreefold = rules->autoClaimThreefold != 0;
    game->drawRules.fiftyMoveClaimPlies = std::clamp(rules->fiftyMoveClaimPlies <= 0 ? 100 : rules->fiftyMoveClaimPlies, 2, 1000);
    game->drawRules.seventyFiveMoveAutoPlies = std::clamp(rules->seventyFiveMoveAutoPlies <= 0 ? 150 : rules->seventyFiveMoveAutoPlies, 2, 1000);
    if (game->drawRules.seventyFiveMoveAutoPlies < game->drawRules.fiftyMoveClaimPlies)
    {
        game->drawRules.seventyFiveMoveAutoPlies = game->drawRules.fiftyMoveClaimPlies;
    }
    game->drawRules.autoClaimFiftyMove = rules->autoClaimFiftyMove != 0;
    game->lastSearchInfo = "Draw rules updated.";
    return 1;
}

CHESS_API int Chess_SetTablebasePath(void* handle, const char* path)
{
    auto* game = asGame(handle);
    if (game == nullptr)
    {
        return 0;
    }

    scanTablebasePath(game->tablebaseInfo, path != nullptr ? path : "");
    std::ostringstream info;
    info << "Endgame tables: built-in draws on";
    if (!game->tablebaseInfo.path.empty())
    {
        info << ", Syzygy files WDL " << game->tablebaseInfo.syzygyWdlFiles
             << ", DTZ " << game->tablebaseInfo.syzygyDtzFiles
             << ", max pieces " << game->tablebaseInfo.maxPieces;
    }
    else
    {
        info << ", Syzygy path empty";
    }
    game->lastSearchInfo = info.str();
    return 1;
}

CHESS_API int Chess_GetTablebaseInfo(void* handle, ChessTablebaseInfoDto* info)
{
    auto* game = asGame(handle);
    if (game == nullptr || info == nullptr)
    {
        return 0;
    }

    *info = ChessTablebaseInfoDto{};
    info->enabled = game->tablebaseInfo.enabled ? 1 : 0;
    info->syzygyWdlFiles = game->tablebaseInfo.syzygyWdlFiles;
    info->syzygyDtzFiles = game->tablebaseInfo.syzygyDtzFiles;
    info->maxPieces = game->tablebaseInfo.maxPieces;
    info->builtInEndgameTables = game->tablebaseInfo.builtInEndgameTables ? 1 : 0;
    return 1;
}

CHESS_API int Chess_ClaimDraw(void* handle)
{
    auto* game = asGame(handle);
    if (game == nullptr)
    {
        return 0;
    }

    const int reps = repetitionCount(game->pos);
    if ((game->drawRules.repetitionClaimCount > 0 && reps >= game->drawRules.repetitionClaimCount) ||
        (game->drawRules.fiftyMoveClaimPlies > 0 && game->pos.halfmoveClock >= game->drawRules.fiftyMoveClaimPlies))
    {
        game->drawClaimed = true;
        game->lastSearchInfo = "Draw claimed.";
        return 1;
    }
    return 0;
}

CHESS_API int Chess_GetLegalMoves(void* handle, ChessMoveDto* buffer, int capacity)
{
    auto* game = asGame(handle);
    if (game == nullptr)
    {
        return 0;
    }

    const auto legal = generateLegalMoves(game->pos, false);
    if (buffer != nullptr && capacity > 0)
    {
        const int count = std::min(capacity, static_cast<int>(legal.size()));
        for (int i = 0; i < count; ++i)
        {
            buffer[i] = toDto(legal[static_cast<std::size_t>(i)]);
        }
    }
    return static_cast<int>(legal.size());
}

CHESS_API int Chess_TryMakeMove(void* handle, int fromFile, int fromRank, int toFile, int toRank, int promotion, ChessMoveDto* playedMove)
{
    auto* game = asGame(handle);
    if (game == nullptr || !isInside(fromFile, fromRank) || !isInside(toFile, toRank))
    {
        return 0;
    }
    if (game->drawClaimed || isDrawByRules(game->pos, game->drawRules))
    {
        return 0;
    }

    const int from = squareOf(fromFile, fromRank);
    const int to = squareOf(toFile, toRank);
    const int movingPiece = game->pos.board[from];
    if (movingPiece == Empty)
    {
        return 0;
    }

    int wantedPromotion = promotion;
    if (pieceType(movingPiece) == Pawn && (toRank == 0 || toRank == 7) && wantedPromotion == 0)
    {
        wantedPromotion = Queen;
    }

    auto legal = generateLegalMoves(game->pos, false);
    for (Move move : legal)
    {
        if (move.from == from && move.to == to && move.promotion == wantedPromotion)
        {
            applyMove(game->pos, move, true);
            game->drawClaimed = false;
            if (playedMove != nullptr)
            {
                *playedMove = toDto(move);
            }
            return 1;
        }
    }

    return 0;
}

CHESS_API int Chess_MakeBestMove(void* handle, int depth, ChessMoveDto* playedMove)
{
    ChessSearchOptionsDto options{};
    options.depth = depth;
    options.timeLimitMs = 0;
    options.automaticDepth = 0;
    options.useQuiescence = 1;
    options.useTranspositionTable = 1;
    options.useMoveOrdering = 1;
    options.usePieceSquareTables = 1;
    options.useBishopPairBonus = 1;
    options.useKingSafetyBonus = 1;
    options.useGpuEvaluation = 0;
    options.useEndgameTables = 1;
    options.openingRandomness = 0;
    options.openingMaxPly = 16;
    return Chess_MakeBestMoveEx(handle, &options, playedMove);
}

SearchOptions normalizeOptions(const ChessSearchOptionsDto* dto)
{
    SearchOptions options;
    if (dto == nullptr)
    {
        return options;
    }

    options.depth = std::clamp(dto->depth <= 0 ? 3 : dto->depth, 1, 64);
    options.timeLimitMs = std::max(0, dto->timeLimitMs);
    options.automaticDepth = dto->automaticDepth != 0;
    options.useQuiescence = dto->useQuiescence != 0;
    options.useTranspositionTable = dto->useTranspositionTable != 0;
    options.useMoveOrdering = dto->useMoveOrdering != 0;
    options.usePieceSquareTables = dto->usePieceSquareTables != 0;
    options.useBishopPairBonus = dto->useBishopPairBonus != 0;
    options.useKingSafetyBonus = dto->useKingSafetyBonus != 0;
    options.useGpuEvaluation = dto->useGpuEvaluation != 0;
    options.useEndgameTables = dto->useEndgameTables != 0;
    options.openingRandomness = std::clamp(dto->openingRandomness, 0, 100);
    options.openingMaxPly = std::clamp(dto->openingMaxPly <= 0 ? 16 : dto->openingMaxPly, 1, 80);
    return options;
}

bool tryScoreRootMovesWithGpu(const Position& root, std::vector<Move>& moves)
{
    using EvalBatchFn = int(__cdecl*)(const int*, int, int, int*);

    static HMODULE backend = LoadLibraryA("ChessGpuBackend.dll");
    static EvalBatchFn evalBatch = backend != nullptr
        ? reinterpret_cast<EvalBatchFn>(GetProcAddress(backend, "ChessGpu_EvaluateBatch"))
        : nullptr;

    if (evalBatch == nullptr || moves.empty())
    {
        return false;
    }

    std::vector<int> boards(moves.size() * 64);
    for (std::size_t i = 0; i < moves.size(); ++i)
    {
        Position child = root;
        Move move = moves[i];
        applyMove(child, move, false);
        std::copy(child.board.begin(), child.board.end(), boards.begin() + static_cast<std::ptrdiff_t>(i * 64));
    }

    std::vector<int> scores(moves.size());
    const int evaluated = evalBatch(boards.data(), static_cast<int>(moves.size()), root.side, scores.data());
    if (evaluated != static_cast<int>(moves.size()))
    {
        return false;
    }

    for (std::size_t i = 0; i < moves.size(); ++i)
    {
        moves[i].score = scores[i];
    }
    return true;
}

int currentPlyFromStart(const Position& pos);

int openingRootBonus(const Position& root, const Move& move, const SearchOptions& options)
{
    if (options.openingRandomness <= 0 || currentPlyFromStart(root) >= options.openingMaxPly)
    {
        return 0;
    }

    const int color = root.side;
    const int type = pieceType(move.piece);
    const int fromFile = fileOf(move.from);
    const int toFile = fileOf(move.to);
    const int toRank = rankOf(move.to);
    const int friendlyRank = color == White ? toRank : 7 - toRank;
    int bonus = 0;

    if (type == Pawn)
    {
        if ((toFile == 3 || toFile == 4) && friendlyRank >= 3)
        {
            bonus += 120;
        }
        else if ((toFile == 2 || toFile == 5) && friendlyRank >= 3)
        {
            bonus += 55;
        }
        if (fromFile == 0 || fromFile == 7)
        {
            bonus -= 120;
        }
        else if (fromFile == 1 || fromFile == 6)
        {
            bonus -= 45;
        }
    }
    else if (type == Knight)
    {
        if ((toFile == 2 || toFile == 5) && (friendlyRank == 2 || friendlyRank == 3))
        {
            bonus += 80;
        }
        if (toFile == 0 || toFile == 7)
        {
            bonus -= 120;
        }
    }
    else if (type == Bishop)
    {
        if (friendlyRank >= 2)
        {
            bonus += 55;
        }
    }
    else if (type == Queen)
    {
        bonus -= currentPlyFromStart(root) < 10 ? 90 : 25;
    }
    else if (type == King)
    {
        bonus += (move.flags & MoveCastle) ? 120 : -120;
    }

    return bonus * std::max(20, options.openingRandomness) / 100;
}

int searchRoot(const Position& root, int depth, SearchContext& context, Move& bestMove)
{
    auto legal = generateLegalMoves(root, false);
    const bool gpuScored = context.options.useGpuEvaluation && tryScoreRootMovesWithGpu(root, legal);
    if (gpuScored)
    {
        std::sort(legal.begin(), legal.end(), [](const Move& left, const Move& right)
        {
            return left.score > right.score;
        });
    }
    else if (context.options.useMoveOrdering)
    {
        orderMoves(legal);
    }
    bestMove = legal.front();
    Move best = legal.front();
    int bestScore = -Infinity;
    int alpha = -Infinity;
    const int beta = Infinity;
    std::vector<Move> results;
    results.reserve(legal.size());

    for (Move move : legal)
    {
        if (shouldStop(context))
        {
            break;
        }

        Position child = root;
        applyMove(child, move, true);
        const int score = -negamax(child, depth - 1, -beta, -alpha, 1, context) + openingRootBonus(root, move, context.options);
        if (context.stop)
        {
            break;
        }
        Move scored = move;
        scored.score = score;
        results.push_back(scored);
        if (score > bestScore)
        {
            bestScore = score;
            best = move;
        }
        alpha = std::max(alpha, score);
    }

    bestMove = best;
    bestMove.score = bestScore;
    if (!context.stop && !results.empty())
    {
        std::sort(results.begin(), results.end(), [](const Move& left, const Move& right)
        {
            return left.score > right.score;
        });
        context.rootResults = std::move(results);
    }
    return bestScore;
}

int currentPlyFromStart(const Position& pos)
{
    return (pos.fullmoveNumber - 1) * 2 + (pos.side == Black ? 1 : 0);
}

bool chooseOpeningVariation(const Position& root, SearchContext& context, std::mt19937& rng, Move& best, int& candidateCount)
{
    candidateCount = 0;
    if (context.options.openingRandomness <= 0 || currentPlyFromStart(root) >= context.options.openingMaxPly ||
        context.rootResults.size() < 2 || std::abs(best.score) > MateScore / 2)
    {
        return false;
    }

    std::uniform_int_distribution<int> chance(1, 100);
    if (chance(rng) > context.options.openingRandomness)
    {
        return false;
    }

    std::vector<Move> pool;
    for (const Move& move : context.rootResults)
    {
        if (openingRootBonus(root, move, context.options) > 0)
        {
            pool.push_back(move);
        }
    }
    if (pool.size() < 2)
    {
        pool = context.rootResults;
    }
    std::sort(pool.begin(), pool.end(), [](const Move& left, const Move& right)
    {
        return left.score > right.score;
    });

    const int margin = 24 + context.options.openingRandomness * 4;
    const int bestScore = pool.front().score;
    std::vector<Move> candidates;
    for (const Move& move : pool)
    {
        if (move.score < bestScore - margin || candidates.size() >= 8)
        {
            break;
        }
        candidates.push_back(move);
    }

    candidateCount = static_cast<int>(candidates.size());
    if (candidates.size() < 2)
    {
        return false;
    }

    std::vector<int> weights;
    weights.reserve(candidates.size());
    for (const Move& move : candidates)
    {
        weights.push_back(std::max(1, margin - (bestScore - move.score) + 1));
    }

    std::discrete_distribution<int> pick(weights.begin(), weights.end());
    const Move varied = candidates[static_cast<std::size_t>(pick(rng))];
    if (varied.from == best.from && varied.to == best.to && varied.promotion == best.promotion)
    {
        return false;
    }

    best = varied;
    return true;
}

CHESS_API int Chess_MakeBestMoveEx(void* handle, const ChessSearchOptionsDto* rawOptions, ChessMoveDto* playedMove)
{
    auto* game = asGame(handle);
    if (game == nullptr)
    {
        return 0;
    }
    if (game->drawClaimed || isDrawByRules(game->pos, game->drawRules))
    {
        return 0;
    }

    const auto legal = generateLegalMoves(game->pos, false);
    if (legal.empty())
    {
        return 0;
    }

    SearchContext context;
    context.options = normalizeOptions(rawOptions);
    context.requestedDepth = context.options.depth;
    context.started = std::chrono::steady_clock::now();

    Move best = legal.front();
    best.score = -Infinity;
    const int firstDepth = (context.options.automaticDepth || context.options.timeLimitMs > 0) ? 1 : context.options.depth;

    for (int depth = firstDepth; depth <= context.options.depth; ++depth)
    {
        Move iterationBest = best;
        const int score = searchRoot(game->pos, depth, context, iterationBest);
        if (!context.stop || context.completedDepth == 0)
        {
            best = iterationBest;
            best.score = score;
            context.completedDepth = depth;
            context.bestScore = score;
        }
        if (context.stop || (!context.options.automaticDepth && context.options.timeLimitMs <= 0))
        {
            break;
        }
    }

    if (best.from < 0)
    {
        return 0;
    }

    int varietyCandidates = 0;
    const bool usedOpeningVariation = chooseOpeningVariation(game->pos, context, game->rng, best, varietyCandidates);
    applyMove(game->pos, best, true);
    game->drawClaimed = false;
    if (playedMove != nullptr)
    {
        *playedMove = toDto(best);
    }

    const auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::steady_clock::now() - context.started).count();
    game->lastSearchStats = ChessSearchInfoDto{};
    game->lastSearchStats.requestedDepth = context.requestedDepth;
    game->lastSearchStats.completedDepth = context.completedDepth;
    game->lastSearchStats.stoppedByTime = context.stop ? 1 : 0;
    game->lastSearchStats.reachedRequestedDepth = context.completedDepth >= context.requestedDepth && !context.stop ? 1 : 0;
    game->lastSearchStats.timeLimitMs = context.options.timeLimitMs;
    game->lastSearchStats.elapsedMs = static_cast<int>(std::min<std::int64_t>(elapsed, std::numeric_limits<int>::max()));
    game->lastSearchStats.nodes = context.nodes;
    game->lastSearchStats.bestScore = best.score;
    std::ostringstream info;
    info << "depth " << context.completedDepth << "/" << context.requestedDepth
         << ", best " << moveToLongAlgebraic(best)
         << ", score " << best.score
         << " cp, nodes " << context.nodes
         << ", time " << elapsed << " ms"
         << ", qsearch " << (context.options.useQuiescence ? "on" : "off")
         << ", tt " << (context.options.useTranspositionTable ? "on" : "off")
         << ", gpu-root " << (context.options.useGpuEvaluation ? "on" : "off")
         << ", endgame " << (context.options.useEndgameTables ? "on" : "off");
    if (context.options.openingRandomness > 0)
    {
        info << ", opening-variety " << (usedOpeningVariation ? "used" : "ready")
             << " (" << varietyCandidates << " candidates)";
    }
    if (context.stop)
    {
        info << ", stopped by time limit";
    }
    else
    {
        info << ", full depth reached";
    }
    game->lastSearchInfo = info.str();
    return 1;
}

CHESS_API int Chess_Undo(void* handle)
{
    auto* game = asGame(handle);
    if (game == nullptr || game->pos.history.empty())
    {
        return 0;
    }

    const Snapshot snap = game->pos.history.back();
    game->pos.history.pop_back();
    restoreSnapshot(game->pos, snap);
    game->drawClaimed = false;
    game->lastSearchInfo = "Move undone.";
    return 1;
}

CHESS_API int Chess_GetLastSearchStats(void* handle, ChessSearchInfoDto* info)
{
    auto* game = asGame(handle);
    if (game == nullptr || info == nullptr)
    {
        return 0;
    }
    *info = game->lastSearchStats;
    return 1;
}

CHESS_API int Chess_GetLastSearchInfo(void* handle, char* buffer, int capacity)
{
    auto* game = asGame(handle);
    if (game == nullptr)
    {
        return 0;
    }
    return copyString(game->lastSearchInfo, buffer, capacity);
}
