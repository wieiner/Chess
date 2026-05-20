#pragma once

#ifdef CHESS3DENGINE_EXPORTS
#define CHESS3D_API extern "C" __declspec(dllexport)
#else
#define CHESS3D_API extern "C" __declspec(dllimport)
#endif

// 3D coordinates: x 0..7 means a..h, y 0..7 means ranks 1..8, z 0..7 means levels 1..8.
// Piece code: side * 10 + type. Type: 1 pawn, 2 knight, 3 bishop, 4 rook, 5 queen, 6 king.
// Side ids are 1..6 and are intentionally not tied to +/- colors, because cube-face chess may use up to six sides.

#pragma pack(push, 4)
struct Chess3DMoveDto
{
    int fromX;
    int fromY;
    int fromZ;
    int toX;
    int toY;
    int toZ;
    int piece;
    int captured;
    int promotionType;
    int flags; // capture=1, promotion=8, draftCheck=16 reserved
    int score;
};

struct Chess3DStateDto
{
    int width;
    int height;
    int depth;
    int sideToMove;
    int activeSideCount;
    int legalMoveCount;
    int pieceCount;
    int rulesLoaded;
    int kingSafetyEnabled;
    int lastFromX;
    int lastFromY;
    int lastFromZ;
    int lastToX;
    int lastToY;
    int lastToZ;
};

struct Chess3DRulesInfoDto
{
    int width;
    int height;
    int depth;
    int activeSideCount;
    int movementProfile; // 0 setup-only, 1 draft3d
    int kingSafetyEnabled;
    int maxPiecesPerSide;
};

struct Chess3DCoreStackEntryDto
{
    int side;
    int pieceType;
    int pieceCode;
    int flags;
};
#pragma pack(pop)

CHESS3D_API void* Chess3D_Create();
CHESS3D_API void Chess3D_Destroy(void* handle);
CHESS3D_API void Chess3D_Reset(void* handle);
CHESS3D_API void Chess3D_Clear(void* handle);
CHESS3D_API int Chess3D_LoadRulesJson(void* handle, const char* json);
CHESS3D_API int Chess3D_LoadRuleProfileJson(void* handle, const char* json);
CHESS3D_API int Chess3D_GetRulesJson(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetCurrentRulesetId(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetCurrentRulesetVersion(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetCurrentRulesetDisplayName(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetGoalProfileType(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetCaptureProfileType(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetOccupancyProfileType(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetFusionProfileType(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetCorePhysicsProfileType(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetLayerTurnProfileType(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetVictoryProfileType(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetCoreCube(void* handle, int* xMin, int* xMax, int* yMin, int* yMax, int* zMin, int* zMax);
CHESS3D_API int Chess3D_RecomputeAnchors(void* handle);
CHESS3D_API int Chess3D_GetAnchorCount(void* handle, int side);
CHESS3D_API int Chess3D_GetRequiredAnchorCount(void* handle, int side);
CHESS3D_API int Chess3D_IsTargetSlot(void* handle, int side, int x, int y, int z);
CHESS3D_API int Chess3D_IsAnchoredCell(void* handle, int x, int y, int z);
CHESS3D_API int Chess3D_IsGameOver(void* handle);
CHESS3D_API int Chess3D_GetWinnerSide(void* handle);
CHESS3D_API int Chess3D_GetLastProfileError(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_IsCoreStackEnabled(void* handle);
CHESS3D_API int Chess3D_GetCoreStackCount(void* handle, int x, int y, int z);
CHESS3D_API int Chess3D_GetCoreStackEntry(void* handle, int x, int y, int z, int stackIndex, int* side, int* pieceType, int* pieceCode, int* flags);
CHESS3D_API int Chess3D_PushCoreStackPiece(void* handle, int x, int y, int z, int pieceCode);
CHESS3D_API int Chess3D_ClearCoreStack(void* handle, int x, int y, int z);
CHESS3D_API int Chess3D_RemoveCoreStackEntry(void* handle, int x, int y, int z, int stackIndex);
CHESS3D_API int Chess3D_GetProjectedPiece(void* handle, int x, int y, int z);
CHESS3D_API int Chess3D_GetRulesInfo(void* handle, Chess3DRulesInfoDto* info);
CHESS3D_API int Chess3D_GetState(void* handle, Chess3DStateDto* state);
CHESS3D_API int Chess3D_GetBoard(void* handle, int* pieces512);
CHESS3D_API int Chess3D_SetBoard(void* handle, const int* pieces512, int sideToMove);
CHESS3D_API int Chess3D_SetPiece(void* handle, int x, int y, int z, int side, int type);
CHESS3D_API int Chess3D_GetPiece(void* handle, int x, int y, int z);
CHESS3D_API int Chess3D_GetLegalMoves(void* handle, Chess3DMoveDto* buffer, int capacity);
CHESS3D_API int Chess3D_GetPieceMoves(void* handle, int fromX, int fromY, int fromZ, Chess3DMoveDto* buffer, int capacity);
CHESS3D_API int Chess3D_TryMakeMove(void* handle, int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int promotionType, Chess3DMoveDto* playedMove);
CHESS3D_API int Chess3D_MakeBestMove(void* handle, int depth, Chess3DMoveDto* playedMove);
CHESS3D_API int Chess3D_RotateLayer(void* handle, int axis, int layer, int quarterTurns);
CHESS3D_API int Chess3D_GetPositionText(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetLastInfo(void* handle, char* buffer, int capacity);
