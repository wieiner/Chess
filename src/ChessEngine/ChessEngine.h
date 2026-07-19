#pragma once

#ifdef CHESSENGINE_EXPORTS
#define CHESS_API extern "C" __declspec(dllexport)
#else
#define CHESS_API extern "C" __declspec(dllimport)
#endif

// Coordinates: file 0..7 means a..h, rank 0..7 means ranks 1..8.
// Piece codes returned by Chess_GetBoard:
//  0 empty
//  1 white pawn, 2 white knight, 3 white bishop, 4 white rook, 5 white queen, 6 white king
// -1 black pawn, -2 black knight, -3 black bishop, -4 black rook, -5 black queen, -6 black king

#pragma pack(push, 4)
struct ChessMoveDto
{
    int fromFile;
    int fromRank;
    int toFile;
    int toRank;
    int promotion; // 0 none, 2 knight, 3 bishop, 4 rook, 5 queen
    int flags;     // capture=1, castle=2, enPassant=4, promotion=8, check=16
    int score;     // centipawns from the mover's point of view for searched moves
};

struct ChessStateDto
{
    int sideToMove;       // 1 white, -1 black
    int status;           // 0 playing, 1 checkmate, 2 stalemate, 3 fifty claim, 4 repetition claim, 5 repetition draw, 6 seventy-five draw
    int isCheck;          // 0/1
    int halfmoveClock;
    int fullmoveNumber;
    int legalMoveCount;
    int lastFromFile;
    int lastFromRank;
    int lastToFile;
    int lastToRank;
    int lastPromotion;
    int lastFlags;
    int repetitionCount;
    int canClaimRepetition;
    int canClaimFiftyMove;
};

struct ChessMoveDescriptorDto
{
    ChessMoveDto move;
    int movedPiece;
    int capturedPiece;
    int castleKind;               // 0 none, 1 king-side, 2 queen-side
    int disambiguation;           // file=1, rank=2; both bits may be set
    int resultingStatus;
    int resultingIsCheck;
    int resultingLegalMoveCount;
};

struct ChessSearchOptionsDto
{
    int depth;                  // 1..64, practical UI warning starts around 8
    int timeLimitMs;            // 0 means no time limit
    int automaticDepth;         // 0/1, iterative deepening up to depth
    int useQuiescence;          // 0/1
    int useTranspositionTable;  // 0/1
    int useMoveOrdering;        // 0/1
    int usePieceSquareTables;   // 0/1
    int useBishopPairBonus;     // 0/1
    int useKingSafetyBonus;     // 0/1
    int useGpuEvaluation;       // 0/1, optional ChessGpuBackend root ordering
    int useEndgameTables;       // 0/1, built-in exact endgame draws and Syzygy-ready metadata
    int openingRandomness;      // 0..100, choose between near-equal early moves
    int openingMaxPly;          // randomization window from game start, usually 12..20
};

struct ChessDrawRulesDto
{
    int repetitionClaimCount;       // usually 3
    int repetitionAutoDrawCount;    // usually 5
    int autoClaimThreefold;         // 0/1, Lichess-style user preference
    int fiftyMoveClaimPlies;        // usually 100
    int seventyFiveMoveAutoPlies;   // usually 150
    int autoClaimFiftyMove;         // 0/1
};

struct ChessSearchInfoDto
{
    int requestedDepth;
    int completedDepth;
    int stoppedByTime;
    int reachedRequestedDepth;
    int timeLimitMs;
    int elapsedMs;
    long long nodes;
    int bestScore;
};

struct ChessTablebaseInfoDto
{
    int enabled;
    int syzygyWdlFiles;
    int syzygyDtzFiles;
    int maxPieces;
    int builtInEndgameTables;
};
#pragma pack(pop)

CHESS_API void* Chess_Create();
CHESS_API void Chess_Destroy(void* handle);
CHESS_API void Chess_Reset(void* handle);
CHESS_API int Chess_SetFen(void* handle, const char* fen);
CHESS_API int Chess_GetFen(void* handle, char* buffer, int capacity);
CHESS_API int Chess_GetBoard(void* handle, int* pieces64);
CHESS_API int Chess_GetState(void* handle, ChessStateDto* state);
CHESS_API int Chess_GetDrawRules(void* handle, ChessDrawRulesDto* rules);
CHESS_API int Chess_SetDrawRules(void* handle, const ChessDrawRulesDto* rules);
CHESS_API int Chess_SetTablebasePath(void* handle, const char* path);
CHESS_API int Chess_GetTablebaseInfo(void* handle, ChessTablebaseInfoDto* info);
CHESS_API int Chess_ClaimDraw(void* handle);
CHESS_API int Chess_GetLegalMoves(void* handle, ChessMoveDto* buffer, int capacity);
CHESS_API int Chess_GetMoveDescriptor(void* handle, int fromFile, int fromRank, int toFile, int toRank, int promotion, ChessMoveDescriptorDto* descriptor);
CHESS_API int Chess_TryMakeMove(void* handle, int fromFile, int fromRank, int toFile, int toRank, int promotion, ChessMoveDto* playedMove);
CHESS_API int Chess_MakeBestMove(void* handle, int depth, ChessMoveDto* playedMove);
CHESS_API int Chess_MakeBestMoveEx(void* handle, const ChessSearchOptionsDto* options, ChessMoveDto* playedMove);
CHESS_API int Chess_GetLastSearchStats(void* handle, ChessSearchInfoDto* info);
CHESS_API int Chess_Undo(void* handle);
CHESS_API int Chess_GetLastSearchInfo(void* handle, char* buffer, int capacity);
