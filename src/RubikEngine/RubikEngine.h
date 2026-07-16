#pragma once

#ifdef RUBIKENGINE_EXPORTS
#define RUBIK_API extern "C" __declspec(dllexport)
#else
#define RUBIK_API extern "C" __declspec(dllimport)
#endif

// N x N x N cube coordinates use the same convention as Chess3DEngine:
// x 0..N-1, y 0..N-1, z 0..N-1, index = z * N * N + y * N + x.

#pragma pack(push, 4)
struct RubikMoveDto
{
    int axis;         // 0 = Z layer, 1 = Y layer, 2 = X layer
    int layer;        // 0..size-1
    int quarterTurns; // normalized 1..3 clockwise turns
};

struct RubikStateDto
{
    int size;
    int cellCount;
    int historyCount;
    int isSolved;
    int manualState;
    int lastAxis;
    int lastLayer;
    int lastQuarterTurns;
};
#pragma pack(pop)

RUBIK_API void* Rubik_Create();
RUBIK_API void* Rubik_CreateSized(int size);
RUBIK_API void Rubik_Destroy(void* handle);
RUBIK_API void Rubik_Reset(void* handle);
RUBIK_API int Rubik_SetSize(void* handle, int size);
RUBIK_API int Rubik_GetState(void* handle, RubikStateDto* state);
RUBIK_API int Rubik_GetCells(void* handle, int* cells);
RUBIK_API int Rubik_SetCells(void* handle, const int* cells);
RUBIK_API int Rubik_SetCell(void* handle, int x, int y, int z, int value);
RUBIK_API int Rubik_RotateLayer(void* handle, int axis, int layer, int quarterTurns);
RUBIK_API int Rubik_Scramble(void* handle, int seed, int length);
RUBIK_API int Rubik_GetHistory(void* handle, RubikMoveDto* buffer, int capacity);
RUBIK_API int Rubik_SolveByReverseHistory(void* handle, RubikMoveDto* buffer, int capacity);
RUBIK_API int Rubik_ApplyMoves(void* handle, const RubikMoveDto* moves, int count);
RUBIK_API int Rubik_GetCommandText(void* handle, char* buffer, int capacity);
RUBIK_API int Rubik_GetLastInfo(void* handle, char* buffer, int capacity);
RUBIK_API int Rubik_GetFaceletSchemaVersion(void* handle);
RUBIK_API int Rubik_GetFaceletCount(void* handle);
RUBIK_API int Rubik_GetFacelets(void* handle, int* facelets, int capacity);
RUBIK_API int Rubik_SetFacelets(void* handle, const int* facelets, int count);
RUBIK_API int Rubik_GetFacelet(void* handle, int face, int row, int column);
RUBIK_API int Rubik_SetFacelet(void* handle, int face, int row, int column, int colorId);
RUBIK_API int Rubik_GetColorScheme(void* handle, char* buffer, int capacity);
RUBIK_API int Rubik_ValidateFacelets(void* handle, const int* facelets, int count);
