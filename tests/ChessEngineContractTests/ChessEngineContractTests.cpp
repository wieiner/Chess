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

bool DescribeMove(void* game, int fromFile, int fromRank, int toFile, int toRank, int promotion, ChessMoveDescriptorDto& descriptor)
{
    return Chess_GetMoveDescriptor(game, fromFile, fromRank, toFile, toRank, promotion, &descriptor) != 0;
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

    ChessMoveDescriptorDto descriptor{};
    const std::string descriptorStartFen = GetFen(game);
    test.Check(DescribeMove(game, 4, 1, 4, 3, 0, descriptor), "Move descriptor accepts legal e2e4");
    test.Check(descriptor.movedPiece == 1 && descriptor.capturedPiece == 0, "Move descriptor exposes moved and captured pieces");
    test.Check(descriptor.move.fromFile == 4 && descriptor.move.toRank == 3, "Move descriptor carries the exact legal move");
    test.Check(descriptor.disambiguation == 0 && descriptor.castleKind == 0, "Pawn move needs no piece disambiguation or castle kind");
    test.Check(GetFen(game) == descriptorStartFen, "Move descriptor does not mutate FEN");
    test.Check(Chess_Undo(game) == 0, "Move descriptor does not append undo history");
    test.Check(!DescribeMove(game, 0, 0, 0, 3, 0, descriptor), "Move descriptor rejects an illegal blocked rook move");
    test.Check(GetFen(game) == descriptorStartFen, "Rejected descriptor does not mutate FEN");

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

    Chess_Reset(game);
    test.Check(TryMove(game, 4, 1, 4, 3), "EP descriptor setup: e2e4");
    test.Check(TryMove(game, 0, 6, 0, 5), "EP descriptor setup: a7a6");
    test.Check(TryMove(game, 4, 3, 4, 4), "EP descriptor setup: e4e5");
    test.Check(TryMove(game, 3, 6, 3, 4), "EP descriptor setup: d7d5");
    test.Check(DescribeMove(game, 4, 4, 3, 5, 0, descriptor), "En-passant descriptor is available");
    test.Check((descriptor.move.flags & 4) != 0 && descriptor.capturedPiece == -1, "En-passant descriptor reports the captured pawn");

    const char* promotionFen = "8/P7/8/8/8/8/8/k6K w - - 0 1";
    test.Check(Chess_SetFen(game, promotionFen) == 1, "Promotion FEN loads");
    ChessMoveDto promotion{};
    test.Check(TryMove(game, 0, 6, 0, 7, 5, &promotion), "Promotion a7a8=Q is accepted");
    test.Check((promotion.flags & 8) != 0 && promotion.promotion == 5, "Promotion move reports promotion flag and queen");

    test.Check(Chess_SetFen(game, promotionFen) == 1, "Promotion descriptor FEN reloads");
    test.Check(DescribeMove(game, 0, 6, 0, 7, 2, descriptor), "Knight promotion descriptor is available");
    test.Check((descriptor.move.flags & 8) != 0 && descriptor.move.promotion == 2, "Descriptor preserves the exact promotion choice");

    test.Check(Chess_SetFen(game, castleFen) == 1, "Castling descriptor FEN reloads");
    test.Check(DescribeMove(game, 4, 0, 6, 0, 0, descriptor), "King-side castling descriptor is available");
    test.Check((descriptor.move.flags & 2) != 0 && descriptor.castleKind == 1, "Castling descriptor reports king-side kind");

    const char* knightAmbiguityFen = "7k/8/8/8/8/8/8/1N3N1K w - - 0 1";
    test.Check(Chess_SetFen(game, knightAmbiguityFen) == 1, "Knight ambiguity FEN loads");
    test.Check(DescribeMove(game, 1, 0, 3, 1, 0, descriptor), "Ambiguous knight descriptor is available");
    test.Check(descriptor.disambiguation == 1, "Knights on different files require file disambiguation");

    const char* rookAmbiguityFen = "7k/8/8/8/8/R7/8/R6K w - - 0 1";
    test.Check(Chess_SetFen(game, rookAmbiguityFen) == 1, "Rook ambiguity FEN loads");
    test.Check(DescribeMove(game, 0, 0, 0, 1, 0, descriptor), "Ambiguous rook descriptor is available");
    test.Check(descriptor.disambiguation == 2, "Rooks on the same file require rank disambiguation");

    const char* bishopAmbiguityFen = "7k/8/8/8/8/8/8/2B3BK w - - 0 1";
    test.Check(Chess_SetFen(game, bishopAmbiguityFen) == 1, "Bishop ambiguity FEN loads");
    test.Check(DescribeMove(game, 2, 0, 4, 2, 0, descriptor), "Ambiguous bishop descriptor is available");
    test.Check(descriptor.disambiguation == 1, "Bishops on different files require file disambiguation");

    const char* bothAmbiguityFen = "7k/8/8/8/8/Q7/8/Q1Q4K w - - 0 1";
    test.Check(Chess_SetFen(game, bothAmbiguityFen) == 1, "Both-coordinate ambiguity FEN loads");
    test.Check(DescribeMove(game, 0, 0, 2, 2, 0, descriptor), "Both-coordinate descriptor is available");
    test.Check(descriptor.disambiguation == 3, "File and rank conflicts require both-coordinate disambiguation");

    const char* captureFen = "7k/8/8/8/8/8/4p3/4R2K w - - 0 1";
    test.Check(Chess_SetFen(game, captureFen) == 1, "Capture descriptor FEN loads");
    test.Check(DescribeMove(game, 4, 0, 4, 1, 0, descriptor), "Capture descriptor is available");
    test.Check((descriptor.move.flags & 1) != 0 && descriptor.capturedPiece == -1, "Capture descriptor exposes capture flag and piece");

    const char* mateMoveFen = "7k/8/5KQ1/8/8/8/8/8 w - - 0 1";
    test.Check(Chess_SetFen(game, mateMoveFen) == 1, "Mate move descriptor FEN loads");
    test.Check(DescribeMove(game, 6, 5, 6, 6, 0, descriptor), "Checkmate move descriptor is available");
    test.Check(descriptor.resultingIsCheck == 1 && descriptor.resultingStatus == 1 && descriptor.resultingLegalMoveCount == 0,
        "Move descriptor distinguishes checkmate from check");

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
