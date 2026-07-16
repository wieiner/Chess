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

std::vector<int> FaceletsOf(void* cube)
{
    const int count = Rubik_GetFaceletCount(cube);
    std::vector<int> result(static_cast<size_t>(count), 0);
    Rubik_GetFacelets(cube, result.data(), count);
    return result;
}

std::vector<int> CellsOf(void* cube, int size)
{
    std::vector<int> result(static_cast<size_t>(size * size * size), 0);
    Rubik_GetCells(cube, result.data());
    return result;
}

int FaceletAt(const std::vector<int>& facelets, int size, int face, int row, int column)
{
    return facelets[static_cast<size_t>(face * size * size + row * size + column)];
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

void CheckRotationInvariants(ContractTestRunner& test, int size)
{
    void* cube = Rubik_CreateSized(size);
    test.Check(cube != nullptr, "Create rotation invariant cube N=" + std::to_string(size));
    if (cube == nullptr)
    {
        return;
    }

    const std::vector<int> solvedFacelets = FaceletsOf(cube);
    const std::vector<int> solvedCells = CellsOf(cube, size);
    const int maximum = size - 1;

    test.Check(Rubik_RotateLayer(cube, 0, maximum, 1) == 1, "Outer quarter turn succeeds N=" + std::to_string(size));
    std::vector<int> turnedFacelets = FaceletsOf(cube);
    test.Check(turnedFacelets != solvedFacelets, "Outer quarter turn changes facelets N=" + std::to_string(size));
    test.Check(Rubik_ValidateFacelets(cube, turnedFacelets.data(), static_cast<int>(turnedFacelets.size())) == 1,
        "Outer turn preserves color counts N=" + std::to_string(size));
    std::vector<int> turnedCells = CellsOf(cube, size);
    std::sort(turnedCells.begin(), turnedCells.end());
    test.Check(turnedCells == solvedCells, "Outer turn preserves cubie ID permutation N=" + std::to_string(size));
    test.Check(Rubik_RotateLayer(cube, 0, maximum, 3) == 1, "Outer inverse turn succeeds N=" + std::to_string(size));
    test.Check(FaceletsOf(cube) == solvedFacelets && CellsOf(cube, size) == solvedCells,
        "Turn plus inverse restores facelets and cubies N=" + std::to_string(size));

    for (int turn = 0; turn < 4; ++turn)
    {
        Rubik_RotateLayer(cube, 2, 0, 1);
    }
    test.Check(FaceletsOf(cube) == solvedFacelets && CellsOf(cube, size) == solvedCells,
        "Four quarter turns restore state N=" + std::to_string(size));

    Rubik_RotateLayer(cube, 1, maximum, 2);
    Rubik_RotateLayer(cube, 1, maximum, 2);
    test.Check(FaceletsOf(cube) == solvedFacelets && CellsOf(cube, size) == solvedCells,
        "Two half turns restore state N=" + std::to_string(size));

    if (size > 2)
    {
        Rubik_RotateLayer(cube, 0, 1, 1);
        Rubik_RotateLayer(cube, 0, 1, 3);
        test.Check(FaceletsOf(cube) == solvedFacelets && CellsOf(cube, size) == solvedCells,
            "Inner slice turn and inverse restore state N=" + std::to_string(size));
    }

    RubikMoveDto wideRoundTrip[] = {
        { 2, maximum, 1 },
        { 2, maximum - 1, 1 },
        { 2, maximum - 1, 3 },
        { 2, maximum, 3 }
    };
    test.Check(Rubik_ApplyMoves(cube, wideRoundTrip, 4) == 1, "Wide-turn sequence applies N=" + std::to_string(size));
    test.Check(FaceletsOf(cube) == solvedFacelets && CellsOf(cube, size) == solvedCells,
        "Wide turn and inverse restore state N=" + std::to_string(size));

    for (int layer = 0; layer < size; ++layer)
    {
        Rubik_RotateLayer(cube, 1, layer, 1);
    }
    for (int layer = size - 1; layer >= 0; --layer)
    {
        Rubik_RotateLayer(cube, 1, layer, 3);
    }
    test.Check(FaceletsOf(cube) == solvedFacelets && CellsOf(cube, size) == solvedCells,
        "Whole-cube rotation and inverse restore state N=" + std::to_string(size));

    Rubik_Destroy(cube);
}

void CheckAxisStripDirections(ContractTestRunner& test)
{
    constexpr int size = 3;
    constexpr int maximum = size - 1;
    void* cube = Rubik_CreateSized(size);
    if (cube == nullptr)
    {
        test.Check(false, "Create axis strip direction cube");
        return;
    }

    Rubik_RotateLayer(cube, 0, maximum, 1);
    std::vector<int> facelets = FaceletsOf(cube);
    test.Check(FaceletAt(facelets, size, 0, maximum, 1) == 2, "Z+ front turn maps R left strip to U bottom");
    test.Check(FaceletAt(facelets, size, 1, 1, 0) == 4, "Z+ front turn maps D top strip to R left");
    test.Check(FaceletAt(facelets, size, 3, 0, 1) == 5, "Z+ front turn maps L right strip to D top");
    test.Check(FaceletAt(facelets, size, 4, 1, maximum) == 1, "Z+ front turn maps U bottom strip to L right");

    Rubik_Reset(cube);
    Rubik_RotateLayer(cube, 2, maximum, 1);
    facelets = FaceletsOf(cube);
    test.Check(FaceletAt(facelets, size, 0, 1, maximum) == 6, "X+ right turn maps B left strip to U right");
    test.Check(FaceletAt(facelets, size, 2, 1, maximum) == 1, "X+ right turn maps U right strip to F right");
    test.Check(FaceletAt(facelets, size, 3, 1, maximum) == 3, "X+ right turn maps F right strip to D right");
    test.Check(FaceletAt(facelets, size, 5, 1, 0) == 4, "X+ right turn maps D right strip to B left");

    Rubik_Reset(cube);
    Rubik_RotateLayer(cube, 1, maximum, 1);
    facelets = FaceletsOf(cube);
    test.Check(FaceletAt(facelets, size, 2, 0, 1) == 2, "Y+ top turn maps R top strip to F top");
    test.Check(FaceletAt(facelets, size, 1, 0, 1) == 6, "Y+ top turn maps B top strip to R top");
    test.Check(FaceletAt(facelets, size, 5, 0, 1) == 5, "Y+ top turn maps L top strip to B top");
    test.Check(FaceletAt(facelets, size, 4, 0, 1) == 3, "Y+ top turn maps F top strip to L top");

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

    for (const int size : { 2, 3, 4, 5, 8, 11 })
    {
        CheckRotationInvariants(test, size);
    }
    CheckAxisStripDirections(test);

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
    std::vector<int> rotatedFacelets(384, 0);
    test.Check(Rubik_GetFacelets(cube, rotatedFacelets.data(), static_cast<int>(rotatedFacelets.size())) == 384,
        "Layer rotation keeps facelets synchronized");
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
        test.Check(FaceletsOf(cubeA) == FaceletsOf(cubeB), "Scramble facelets are reproducible for the same seed and length");

        RubikMoveDto solution[64]{};
        const int solutionCount = Rubik_SolveByReverseHistory(cubeA, solution, 64);
        test.Check(solutionCount == 12, "SolveByReverseHistory returns inverse trusted history");
        char commandText[512]{};
        test.Check(Rubik_GetCommandText(cubeA, commandText, static_cast<int>(sizeof(commandText))) != 0, "Rubik_GetCommandText succeeds after solving");
        test.Check(std::string(commandText).size() > 0, "Rubik_GetCommandText returns non-empty text");
        test.Check(Rubik_ApplyMoves(cubeA, solution, solutionCount) == 1, "Applying reverse-history solution succeeds");
        test.Check(StateOf(cubeA).isSolved == 1, "Applying reverse-history solution returns cube to solved state");
        test.Check(FaceletsOf(cubeA) == SolvedFacelets(8), "Reverse-history solution restores solved facelets");
    }

    Rubik_Reset(cube);
    test.Check(Rubik_SetCell(cube, 0, 0, 0, 999) == 1, "Rubik_SetCell accepts manual edit");
    state = StateOf(cube);
    test.Check(state.manualState == 1, "Manual SetCell marks cube as manual state");
    std::vector<int> unavailableFacelets(384, 0);
    test.Check(Rubik_GetFacelets(cube, unavailableFacelets.data(), static_cast<int>(unavailableFacelets.size())) == -1,
        "Legacy integer edit keeps unknown sticker orientation explicit");
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
