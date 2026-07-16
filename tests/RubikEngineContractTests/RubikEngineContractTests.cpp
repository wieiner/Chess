#include "../../src/RubikEngine/RubikEngine.h"
#include "../TestSupport/TestSupport.h"

#include <algorithm>
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

std::vector<int> SolvedFacelets(int size)
{
    std::vector<int> result(static_cast<size_t>(6 * size * size));
    const int perFace = size * size;
    for (int face = 0; face < 6; ++face)
    {
        std::fill_n(result.begin() + static_cast<size_t>(face * perFace), perFace, face + 1);
    }
    return result;
}

void CheckSolvedFacelets(ContractTestRunner& test, int size)
{
    void* cube = Rubik_CreateSized(size);
    test.Check(cube != nullptr, "Create facelet test cube " + std::to_string(size) + "x" + std::to_string(size));
    if (cube == nullptr)
    {
        return;
    }

    const int expectedCount = 6 * size * size;
    test.Check(Rubik_GetFaceletSchemaVersion(cube) == 1, "Facelet schema version is 1 for N=" + std::to_string(size));
    test.Check(Rubik_GetFaceletCount(cube) == expectedCount, "Facelet count is 6*N*N for N=" + std::to_string(size));
    std::vector<int> facelets(static_cast<size_t>(expectedCount), 0);
    test.Check(Rubik_GetFacelets(cube, facelets.data(), expectedCount) == expectedCount,
        "Read solved facelets for N=" + std::to_string(size));
    test.Check(facelets == SolvedFacelets(size), "Solved face order and colors are canonical for N=" + std::to_string(size));
    test.Check(Rubik_ValidateFacelets(cube, facelets.data(), expectedCount) == 1,
        "Solved facelets validate for N=" + std::to_string(size));
    Rubik_Destroy(cube);
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

    for (const int size : { 2, 3, 8, 11, 32 })
    {
        CheckSolvedFacelets(test, size);
    }

    std::vector<int> importedFacelets = SolvedFacelets(8);
    std::swap(importedFacelets[0], importedFacelets[64]);
    const std::vector<int> beforeInvalidImport = importedFacelets;
    test.Check(Rubik_SetFacelets(cube, importedFacelets.data(), static_cast<int>(importedFacelets.size())) == 1,
        "Rubik_SetFacelets accepts a count-valid manual facelet state");
    std::vector<int> importedRoundTrip(importedFacelets.size(), 0);
    test.Check(Rubik_GetFacelets(cube, importedRoundTrip.data(), static_cast<int>(importedRoundTrip.size())) ==
        static_cast<int>(importedRoundTrip.size()), "Rubik_GetFacelets reads imported state");
    test.Check(importedRoundTrip == importedFacelets, "Facelet import roundtrip is exact");
    std::vector<int> invalidFacelets = importedFacelets;
    invalidFacelets[0] = 99;
    test.Check(Rubik_ValidateFacelets(cube, invalidFacelets.data(), static_cast<int>(invalidFacelets.size())) == 0,
        "Unknown facelet color is rejected");
    test.Check(Rubik_SetFacelets(cube, invalidFacelets.data(), static_cast<int>(invalidFacelets.size())) == 0,
        "Invalid facelet import clean-fails");
    std::vector<int> afterInvalidImport(importedFacelets.size(), 0);
    Rubik_GetFacelets(cube, afterInvalidImport.data(), static_cast<int>(afterInvalidImport.size()));
    test.Check(afterInvalidImport == beforeInvalidImport, "Failed facelet import does not mutate state");
    test.Check(Rubik_GetFacelet(cube, 0, 0, 0) == 2, "Rubik_GetFacelet uses U/R/F/D/L/B row-major coordinates");
    test.Check(Rubik_SetFacelet(cube, 0, 0, 0, 1) == 1, "Rubik_SetFacelet accepts a compact color id");
    char colorScheme[512]{};
    test.Check(Rubik_GetColorScheme(cube, colorScheme, static_cast<int>(sizeof(colorScheme))) > 0,
        "Rubik_GetColorScheme returns versioned color metadata");
    test.Check(std::string(colorScheme).find("\"schemaVersion\":1") != std::string::npos,
        "Color scheme reports facelet schema version");

    Rubik_Reset(cube);

    test.Check(Rubik_RotateLayer(cube, 0, 0, 1) == 1, "Rubik_RotateLayer accepts one quarter turn");
    std::vector<int> staleFacelets(384, 0);
    test.Check(Rubik_GetFacelets(cube, staleFacelets.data(), static_cast<int>(staleFacelets.size())) == -1,
        "Phase 04 rejects stale facelets after legacy cubie-only rotation");
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
