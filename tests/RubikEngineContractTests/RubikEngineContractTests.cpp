#include "../../src/RubikEngine/RubikEngine.h"
#include "../TestSupport/TestSupport.h"

#include <string>
#include <vector>

namespace
{
RubikStateDto StateOf(void* cube)
{
    RubikStateDto state{};
    Rubik_GetState(cube, &state);
    return state;
}
}

int main()
{
    ContractTestRunner test;
    void* cube = Rubik_CreateSized(8);
    test.Check(cube != nullptr, "Rubik_CreateSized(8) returns a handle");
    if (cube == nullptr)
    {
        return test.Finish("RubikEngineContractTests");
    }

    RubikStateDto state = StateOf(cube);
    test.Check(state.size == 8, "Rubik size is 8");
    test.Check(state.cellCount == 512, "Rubik cell count is 512");
    test.Check(state.isSolved == 1, "Rubik reset state is solved");

    test.Check(Rubik_RotateLayer(cube, 0, 0, 1) == 1, "Rubik_RotateLayer accepts one quarter turn");
    state = StateOf(cube);
    test.Check(state.isSolved == 0, "One quarter turn changes solved state");
    test.Check(Rubik_RotateLayer(cube, 0, 0, 1) == 1, "Second quarter turn succeeds");
    test.Check(Rubik_RotateLayer(cube, 0, 0, 1) == 1, "Third quarter turn succeeds");
    test.Check(Rubik_RotateLayer(cube, 0, 0, 1) == 1, "Fourth quarter turn succeeds");
    state = StateOf(cube);
    test.Check(state.isSolved == 1, "Four identical quarter turns return to solved state");

    void* cubeA = Rubik_CreateSized(8);
    void* cubeB = Rubik_CreateSized(8);
    test.Check(cubeA != nullptr && cubeB != nullptr, "Two Rubik handles can be created for reproducibility");
    if (cubeA != nullptr && cubeB != nullptr)
    {
        test.Check(Rubik_Scramble(cubeA, 12345, 12) == 1, "Scramble A succeeds");
        test.Check(Rubik_Scramble(cubeB, 12345, 12) == 1, "Scramble B succeeds");
        std::vector<int> cellsA(512);
        std::vector<int> cellsB(512);
        test.Check(Rubik_GetCells(cubeA, cellsA.data()) == 1, "Read scrambled cells A");
        test.Check(Rubik_GetCells(cubeB, cellsB.data()) == 1, "Read scrambled cells B");
        test.Check(cellsA == cellsB, "Scramble is reproducible for the same seed and length");

        RubikMoveDto solution[64]{};
        const int solutionCount = Rubik_SolveByReverseHistory(cubeA, solution, 64);
        test.Check(solutionCount == 12, "SolveByReverseHistory returns inverse trusted history");
        char commandText[512]{};
        test.Check(Rubik_GetCommandText(cubeA, commandText, static_cast<int>(sizeof(commandText))) != 0, "Rubik_GetCommandText succeeds after solving");
        test.Check(std::string(commandText).size() > 0, "Rubik_GetCommandText returns non-empty text");
        test.Check(Rubik_ApplyMoves(cubeA, solution, solutionCount) == 1, "Applying reverse-history solution succeeds");
        test.Check(StateOf(cubeA).isSolved == 1, "Applying reverse-history solution returns cube to solved state");
    }

    Rubik_Reset(cube);
    test.Check(Rubik_SetCell(cube, 0, 0, 0, 999) == 1, "Rubik_SetCell accepts manual edit");
    state = StateOf(cube);
    test.Check(state.manualState == 1, "Manual SetCell marks cube as manual state");
    RubikMoveDto manualSolution[8]{};
    test.Check(Rubik_SolveByReverseHistory(cube, manualSolution, 8) == -1, "Manual state rejects trusted reverse-history solving");

    if (cubeA != nullptr)
    {
        Rubik_Destroy(cubeA);
    }
    if (cubeB != nullptr)
    {
        Rubik_Destroy(cubeB);
    }
    Rubik_Destroy(cube);
    return test.Finish("RubikEngineContractTests");
}

