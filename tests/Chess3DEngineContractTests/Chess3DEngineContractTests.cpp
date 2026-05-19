#include "../../src/Chess3DEngine/Chess3DEngine.h"
#include "../TestSupport/TestSupport.h"

#include <algorithm>
#include <fstream>
#include <iterator>
#include <string>
#include <vector>

namespace
{
std::string ReadTextFile(const std::string& path)
{
    std::ifstream file(path, std::ios::binary);
    if (!file)
    {
        return {};
    }
    return {std::istreambuf_iterator<char>(file), std::istreambuf_iterator<char>()};
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
    test.Check(Chess3D_SetPiece(game, 0, 0, 0, 1, 4) == 1, "Chess3D_SetPiece accepts a valid coordinate");
    test.Check(Chess3D_GetPiece(game, 0, 0, 0) == 14, "Chess3D_GetPiece returns the written piece");

    std::vector<int> beforeRotate(512);
    std::vector<int> afterRotate(512);
    test.Check(Chess3D_GetBoard(game, beforeRotate.data()) == 1, "Board read before RotateLayer succeeds");
    test.Check(Chess3D_RotateLayer(game, 0, 0, 1) == 1, "Chess3D_RotateLayer accepts a valid Z layer turn");
    test.Check(Chess3D_GetBoard(game, afterRotate.data()) == 1, "Board read after RotateLayer succeeds");
    test.Check(beforeRotate != afterRotate, "RotateLayer changes the board state");

    char positionText[512]{};
    test.Check(Chess3D_GetPositionText(game, positionText, static_cast<int>(sizeof(positionText))) != 0, "Chess3D_GetPositionText succeeds");
    test.Check(std::string(positionText).size() > 0, "Chess3D_GetPositionText returns non-empty text");

    const std::string rulesJson = ReadTextFile("src\\ChessApp\\Assets\\Rules3D\\cube8x8x8_draft.json");
    if (!rulesJson.empty())
    {
        test.Check(Chess3D_LoadRulesJson(game, rulesJson.c_str()) == 1, "Chess3D_LoadRulesJson accepts cube8x8x8_draft.json");
        Chess3DRulesInfoDto loadedRules{};
        test.Check(Chess3D_GetRulesInfo(game, &loadedRules) == 1 &&
            loadedRules.width == 8 && loadedRules.height == 8 && loadedRules.depth == 8,
            "Loaded draft rules still report 8x8x8");
    }
    else
    {
        std::cout << "SKIP cube8x8x8_draft.json not found in source assets\n";
    }

    Chess3D_Destroy(game);
    return test.Finish("Chess3DEngineContractTests");
}

