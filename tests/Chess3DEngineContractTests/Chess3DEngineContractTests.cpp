#include "../../src/Chess3DEngine/Chess3DEngine.h"
#include "../TestSupport/TestSupport.h"

#include <algorithm>
#include <array>
#include <cstdlib>
#include <fstream>
#include <iterator>
#include <map>
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

    Chess3D_Destroy(game);
    return test.Finish("Chess3DEngineContractTests");
}
