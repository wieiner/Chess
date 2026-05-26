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

struct Chess3DCoreFusionStateDto
{
    int fusionKind;
    int ownerSide;
    int sideMask;
    int entryCount;
    int friendlyCount;
    int enemyCount;
    int dominantPieceType;
    int flags;
};

struct Chess3DLegalActionPreviewEntryDto
{
    int kind;
    int fromX;
    int fromY;
    int fromZ;
    int toX;
    int toY;
    int toZ;
    int flags;
    int pieceCode;
    int capturedPieceCode;
    int side;
    int reasonCode;
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
CHESS3D_API int Chess3D_IsFusionEnabled(void* handle);
CHESS3D_API int Chess3D_RecomputeFusion(void* handle);
CHESS3D_API int Chess3D_GetCoreFusionKind(void* handle, int x, int y, int z);
CHESS3D_API int Chess3D_GetCoreFusionState(void* handle, int x, int y, int z, int* fusionKind, int* ownerSide, int* sideMask, int* entryCount, int* friendlyCount, int* enemyCount, int* dominantPieceType, int* flags);
CHESS3D_API int Chess3D_IsCoreCellContested(void* handle, int x, int y, int z);
CHESS3D_API int Chess3D_HasRoyalPairFusion(void* handle, int x, int y, int z, int side);
CHESS3D_API int Chess3D_GetSideFusionCount(void* handle, int side);
CHESS3D_API int Chess3D_GetSideContestedCount(void* handle, int side);
CHESS3D_API int Chess3D_GetSideImplosionProgress(void* handle, int side);
CHESS3D_API int Chess3D_GetFusionKindName(int fusionKind, char* buffer, int capacity);
CHESS3D_API int Chess3D_IsReserveEnabled(void* handle);
CHESS3D_API int Chess3D_IsKnockbackEnabled(void* handle);
CHESS3D_API int Chess3D_GetReserveCount(void* handle, int side, int pieceType);
CHESS3D_API int Chess3D_GetReserveTotal(void* handle, int side);
CHESS3D_API int Chess3D_ClearReserve(void* handle, int side);
CHESS3D_API int Chess3D_GetLastCaptureWasKnockback(void* handle);
CHESS3D_API int Chess3D_GetLastCapturedPieceCode(void* handle);
CHESS3D_API int Chess3D_GetLastCapturedPieceReserveDestination(void* handle);
CHESS3D_API int Chess3D_GetLastKnockbackHomeX(void* handle);
CHESS3D_API int Chess3D_GetLastKnockbackHomeY(void* handle);
CHESS3D_API int Chess3D_GetLastKnockbackHomeZ(void* handle);
CHESS3D_API int Chess3D_GetLastKnockbackInfo(void* handle, int* capturedPieceCode, int* destinationKind, int* x, int* y, int* z);
CHESS3D_API int Chess3D_GetActionCount(void* handle);
CHESS3D_API int Chess3D_ClearActionHistory(void* handle);
CHESS3D_API int Chess3D_GetActionKind(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionSide(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionPieceCode(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionPieceType(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionFromX(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionFromY(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionFromZ(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionToX(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionToY(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionToZ(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionAxis(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionLayer(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionQuarterTurns(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionCapturedPieceCode(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionCaptureDestination(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionResultCode(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionFlags(void* handle, int actionIndex);
CHESS3D_API int Chess3D_GetActionNotation(void* handle, int actionIndex, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetLastActionNotation(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetLastActionInfo(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetActionKindName(int actionKind, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetCaptureDestinationName(int destination, char* buffer, int capacity);
CHESS3D_API int Chess3D_CanRestoreReservePiece(void* handle, int side, int pieceType, int x, int y, int z);
CHESS3D_API int Chess3D_RestoreReservePiece(void* handle, int side, int pieceType, int x, int y, int z);
CHESS3D_API int Chess3D_AutoRestoreReservePiece(void* handle, int side, int pieceType);
CHESS3D_API int Chess3D_GetLastReserveRestoreInfo(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_ClearSelectionPreview(void* handle);
CHESS3D_API int Chess3D_BuildLegalActionPreviewForCell(void* handle, int x, int y, int z, int side);
CHESS3D_API int Chess3D_GetLegalActionPreviewCount(void* handle);
CHESS3D_API int Chess3D_GetLegalActionPreviewEntry(void* handle, int previewIndex, Chess3DLegalActionPreviewEntryDto* entry);
CHESS3D_API int Chess3D_GetPreviewEntryReason(void* handle, int previewIndex, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetLastInvalidActionReason(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetCurrentTurnKind(void* handle);
CHESS3D_API int Chess3D_GetCurrentSide(void* handle);
CHESS3D_API int Chess3D_GetCurrentMacroPlayer(void* handle);
CHESS3D_API int Chess3D_GetAllowedActionMask(void* handle);
CHESS3D_API int Chess3D_GetTurnSummary(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetRulesInfo(void* handle, Chess3DRulesInfoDto* info);
CHESS3D_API int Chess3D_GetState(void* handle, Chess3DStateDto* state);
CHESS3D_API int Chess3D_GetBoard(void* handle, int* pieces512);
CHESS3D_API int Chess3D_SetBoard(void* handle, const int* pieces512, int sideToMove);
CHESS3D_API int Chess3D_SetPiece(void* handle, int x, int y, int z, int side, int type);
CHESS3D_API int Chess3D_GetPiece(void* handle, int x, int y, int z);
CHESS3D_API int Chess3D_GetLegalMoves(void* handle, Chess3DMoveDto* buffer, int capacity);
CHESS3D_API int Chess3D_GetPieceMoves(void* handle, int fromX, int fromY, int fromZ, Chess3DMoveDto* buffer, int capacity);
CHESS3D_API int Chess3D_TryMakeMove(void* handle, int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int promotionType, Chess3DMoveDto* playedMove);
CHESS3D_API int Chess3D_TryMakeProjectedMove(void* handle, int primarySide, int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int promotionType, Chess3DMoveDto* playedMove);
CHESS3D_API int Chess3D_MakeBestMove(void* handle, int depth, Chess3DMoveDto* playedMove);
CHESS3D_API int Chess3D_RotateLayer(void* handle, int axis, int layer, int quarterTurns);
CHESS3D_API int Chess3D_IsLayerTurnEnabled(void* handle);
CHESS3D_API int Chess3D_CanRotateLayer(void* handle, int axis, int layer, int quarterTurns);
CHESS3D_API int Chess3D_GetLastLayerTurnInfo(void* handle, int* axis, int* layer, int* quarterTurns, int* resultCode);
CHESS3D_API int Chess3D_GetLayerTurnProfileSummary(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetLayerTurnResultName(int resultCode, char* buffer, int capacity);
CHESS3D_API int Chess3D_IsProjectionModeEnabled(void* handle);
CHESS3D_API int Chess3D_GetProjectionMacroPlayerCount(void* handle);
CHESS3D_API int Chess3D_GetProjectionCountForMacroPlayer(void* handle, int macroPlayer);
CHESS3D_API int Chess3D_GetProjectionSide(void* handle, int macroPlayer, int projectionIndex);
CHESS3D_API int Chess3D_GetMacroPlayerForSide(void* handle, int side);
CHESS3D_API int Chess3D_GetProjectionProfileSummary(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetLastProjectionError(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_TransformMoveBetweenSides(void* handle, int sourceSide, int targetSide, int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int* outFromX, int* outFromY, int* outFromZ, int* outToX, int* outToY, int* outToZ);
CHESS3D_API int Chess3D_ExportSaveGameJson(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_LoadSaveGameJson(void* handle, const char* json);
CHESS3D_API int Chess3D_ExportReplayJson(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_LoadReplayJson(void* handle, const char* json, int mode);
CHESS3D_API int Chess3D_ReplayAction(void* handle, int actionIndex);
CHESS3D_API int Chess3D_ReplayAll(void* handle);
CHESS3D_API int Chess3D_ResetReplayCursor(void* handle);
CHESS3D_API int Chess3D_GetReplayActionCount(void* handle);
CHESS3D_API int Chess3D_GetReplayCursor(void* handle);
CHESS3D_API int Chess3D_GetLastReplayError(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetStateHash(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetPositionText(void* handle, char* buffer, int capacity);
CHESS3D_API int Chess3D_GetLastInfo(void* handle, char* buffer, int capacity);
