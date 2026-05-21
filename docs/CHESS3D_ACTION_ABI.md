# Chess3D Action ABI

P2I adds append-only action/history exports to `Chess3DEngine.dll`. P2J adds Hodge projection query/action exports without changing the existing action-history ABI.

## History

- `Chess3D_GetActionCount`
- `Chess3D_ClearActionHistory`
- `Chess3D_GetActionKind`
- `Chess3D_GetActionSide`
- `Chess3D_GetActionPieceCode`
- `Chess3D_GetActionPieceType`
- `Chess3D_GetActionFromX/Y/Z`
- `Chess3D_GetActionToX/Y/Z`
- `Chess3D_GetActionAxis`
- `Chess3D_GetActionLayer`
- `Chess3D_GetActionQuarterTurns`
- `Chess3D_GetActionCapturedPieceCode`
- `Chess3D_GetActionCaptureDestination`
- `Chess3D_GetActionResultCode`
- `Chess3D_GetActionFlags`
- `Chess3D_GetActionNotation`
- `Chess3D_GetLastActionNotation`
- `Chess3D_GetLastActionInfo`
- `Chess3D_GetActionKindName`
- `Chess3D_GetCaptureDestinationName`

Action indexes are one-based in the C ABI. Invalid indexes return safe default values or an empty string result.

## Reserve Restore

- `Chess3D_CanRestoreReservePiece`
- `Chess3D_RestoreReservePiece`
- `Chess3D_AutoRestoreReservePiece`
- `Chess3D_GetLastReserveRestoreInfo`

## Hodge Projection

- `Chess3D_IsProjectionModeEnabled`
- `Chess3D_GetProjectionMacroPlayerCount`
- `Chess3D_GetProjectionCountForMacroPlayer`
- `Chess3D_GetProjectionSide`
- `Chess3D_GetMacroPlayerForSide`
- `Chess3D_GetProjectionProfileSummary`
- `Chess3D_GetLastProjectionError`
- `Chess3D_TransformMoveBetweenSides`
- `Chess3D_TryMakeProjectedMove`

Successful projected moves are recorded as `ActionProjectionCompositeMove = 5` and set `ActionFlagWasProjection = 512`.

## String Rules

String exports accept `char* buffer` and `capacity`. They do not write past capacity and use the same ABI pattern as the existing profile/fusion/layer-turn string getters.

## Compatibility

No existing export signature changed. C# wrappers in `NativeChess3DEngine.cs` expose the new history, restore, and projection calls for status/UI use.
