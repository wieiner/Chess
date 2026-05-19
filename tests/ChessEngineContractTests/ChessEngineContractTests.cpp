#include "../../src/ChessEngine/ChessEngine.h"
#include "../TestSupport/TestSupport.h"

#include <cstring>
#include <string>

namespace
{
std::string GetFen(void* game)
{
    char buffer[256]{};
    if (Chess_GetFen(game, buffer, static_cast<int>(sizeof(buffer))) == 0)
    {
        return {};
    }
    return buffer;
}

bool HasMove(const ChessMoveDto* moves, int count, int fromFile, int fromRank, int toFile, int toRank, int promotion = 0)
{
    for (int i = 0; i < count; ++i)
    {
        if (moves[i].fromFile == fromFile &&
            moves[i].fromRank == fromRank &&
            moves[i].toFile == toFile &&
            moves[i].toRank == toRank &&
            moves[i].promotion == promotion)
        {
            return true;
        }
    }
    return false;
}

bool TryMove(void* game, int fromFile, int fromRank, int toFile, int toRank, int promotion = 0, ChessMoveDto* played = nullptr)
{
    ChessMoveDto local{};
    return Chess_TryMakeMove(game, fromFile, fromRank, toFile, toRank, promotion, played != nullptr ? played : &local) != 0;
}
}

int main()
{
    ContractTestRunner test;
    void* game = Chess_Create();
    test.Check(game != nullptr, "Chess_Create returns a handle");
    if (game == nullptr)
    {
        return test.Finish("ChessEngineContractTests");
    }

    Chess_Reset(game);
    ChessStateDto state{};
    test.Check(Chess_GetState(game, &state) == 1, "Chess_GetState after reset succeeds");
    test.Check(state.sideToMove == 1, "Reset side to move is white");
    test.Check(state.legalMoveCount == 20, "Start position has 20 legal moves");

    ChessMoveDto moves[256]{};
    const int moveCount = Chess_GetLegalMoves(game, moves, 256);
    test.Check(moveCount == 20, "Chess_GetLegalMoves returns 20 start moves");
    test.Check(HasMove(moves, moveCount, 4, 1, 4, 3), "e2e4 is legal in the start position");

    ChessMoveDto played{};
    test.Check(TryMove(game, 4, 1, 4, 3, 0, &played), "Chess_TryMakeMove accepts e2e4");
    test.Check(played.fromFile == 4 && played.fromRank == 1 && played.toFile == 4 && played.toRank == 3, "Played move DTO matches e2e4");

    Chess_Reset(game);
    test.Check(!TryMove(game, 0, 0, 0, 3), "Blocked rook move a1a4 is rejected");

    const char* startFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    test.Check(Chess_SetFen(game, startFen) == 1, "Start FEN loads");
    test.Check(GetFen(game) == startFen, "Start FEN roundtrips exactly");

    const char* simpleFen = "8/8/8/8/8/8/4K3/7k w - - 0 1";
    test.Check(Chess_SetFen(game, simpleFen) == 1, "Simple FEN loads");
    test.Check(GetFen(game) == simpleFen, "Simple FEN roundtrips exactly");

    const char* castleFen = "r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1";
    test.Check(Chess_SetFen(game, castleFen) == 1, "Castling-rights FEN loads");
    test.Check(GetFen(game) == castleFen, "Castling rights survive FEN roundtrip");

    Chess_Reset(game);
    test.Check(TryMove(game, 4, 1, 4, 3), "EP setup: e2e4");
    test.Check(TryMove(game, 0, 6, 0, 5), "EP setup: a7a6");
    test.Check(TryMove(game, 4, 3, 4, 4), "EP setup: e4e5");
    test.Check(TryMove(game, 3, 6, 3, 4), "EP setup: d7d5");
    ChessMoveDto epMove{};
    test.Check(TryMove(game, 4, 4, 3, 5, 0, &epMove), "En-passant capture e5xd6 is accepted");
    test.Check((epMove.flags & 4) != 0, "En-passant move reports en-passant flag");

    const char* promotionFen = "8/P7/8/8/8/8/8/k6K w - - 0 1";
    test.Check(Chess_SetFen(game, promotionFen) == 1, "Promotion FEN loads");
    ChessMoveDto promotion{};
    test.Check(TryMove(game, 0, 6, 0, 7, 5, &promotion), "Promotion a7a8=Q is accepted");
    test.Check((promotion.flags & 8) != 0 && promotion.promotion == 5, "Promotion move reports promotion flag and queen");

    const char* mateFen = "rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3";
    test.Check(Chess_SetFen(game, mateFen) == 1, "Checkmate FEN loads");
    test.Check(Chess_GetState(game, &state) == 1 && state.status == 1, "Checkmate status is reported");

    const char* stalemateFen = "7k/5Q2/6K1/8/8/8/8/8 b - - 0 1";
    test.Check(Chess_SetFen(game, stalemateFen) == 1, "Stalemate FEN loads");
    test.Check(Chess_GetState(game, &state) == 1 && state.status == 2, "Stalemate status is reported");

    test.Check(Chess_SetFen(game, startFen) == 1, "Search setup FEN loads");
    ChessSearchOptionsDto options{};
    options.depth = 1;
    options.useQuiescence = 1;
    options.useTranspositionTable = 1;
    options.useMoveOrdering = 1;
    options.usePieceSquareTables = 1;
    options.useBishopPairBonus = 1;
    options.useKingSafetyBonus = 1;
    options.useEndgameTables = 1;
    test.Check(Chess_MakeBestMoveEx(game, &options, &played) == 1, "Chess_MakeBestMoveEx depth 1 returns a move");
    ChessSearchInfoDto searchInfo{};
    test.Check(Chess_GetLastSearchStats(game, &searchInfo) == 1, "Chess_GetLastSearchStats succeeds");
    test.Check(searchInfo.requestedDepth == 1, "Search stats requestedDepth is populated");
    test.Check(searchInfo.completedDepth >= 1, "Search stats completedDepth is populated");
    test.Check(searchInfo.nodes > 0, "Search stats nodes is populated");
    test.Check(searchInfo.elapsedMs >= 0, "Search stats elapsedMs is populated");

    ChessDrawRulesDto rules{};
    test.Check(Chess_GetDrawRules(game, &rules) == 1, "Chess_GetDrawRules succeeds");
    rules.repetitionClaimCount = 3;
    rules.repetitionAutoDrawCount = 5;
    rules.autoClaimThreefold = 1;
    rules.fiftyMoveClaimPlies = 100;
    rules.seventyFiveMoveAutoPlies = 150;
    rules.autoClaimFiftyMove = 1;
    test.Check(Chess_SetDrawRules(game, &rules) == 1, "Chess_SetDrawRules succeeds");
    ChessDrawRulesDto updatedRules{};
    test.Check(Chess_GetDrawRules(game, &updatedRules) == 1, "Chess_GetDrawRules succeeds after write");
    test.Check(updatedRules.repetitionClaimCount == 3 && updatedRules.repetitionAutoDrawCount == 5, "Repetition draw rule fields roundtrip");
    test.Check(updatedRules.fiftyMoveClaimPlies == 100 && updatedRules.seventyFiveMoveAutoPlies == 150, "50/75-move draw rule fields roundtrip");

    const char* drawFen = "8/8/8/8/8/8/8/k6K w - - 150 1";
    test.Check(Chess_SetFen(game, drawFen) == 1, "High halfmove-clock FEN loads");
    test.Check(Chess_GetState(game, &state) == 1, "Draw-state smoke read succeeds");
    test.Check(state.halfmoveClock == 150, "50/75-move halfmove field is exposed");
    test.Check(state.canClaimFiftyMove == 1 || state.status == 6, "50/75-move draw status fields are exposed");
    test.Check(state.repetitionCount >= 1, "Repetition count field is exposed");

    Chess_Destroy(game);
    return test.Finish("ChessEngineContractTests");
}

