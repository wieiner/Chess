using System.Runtime.InteropServices;
using System.Text;

namespace ChessApp;

internal sealed class NativeChess3DEngine : IDisposable
{
    public const int Pawn = 1;
    public const int Knight = 2;
    public const int Bishop = 3;
    public const int Rook = 4;
    public const int Queen = 5;
    public const int King = 6;

    private const string DllName = "Chess3DEngine.dll";
    private IntPtr _handle;

    public NativeChess3DEngine()
    {
        _handle = Chess3D_Create();
        if (_handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Chess3DEngine.dll did not create a game instance.");
        }
    }

    public void Reset()
    {
        ThrowIfDisposed();
        Chess3D_Reset(_handle);
    }

    public void Clear()
    {
        ThrowIfDisposed();
        Chess3D_Clear(_handle);
    }

    public bool LoadRulesJson(string json)
    {
        ThrowIfDisposed();
        return Chess3D_LoadRulesJson(_handle, json) != 0;
    }

    public bool LoadRuleProfileJson(string json)
    {
        ThrowIfDisposed();
        return Chess3D_LoadRuleProfileJson(_handle, json) != 0;
    }

    public Chess3DRulesInfoDto GetRulesInfo()
    {
        ThrowIfDisposed();
        return Chess3D_GetRulesInfo(_handle, out var info) != 0 ? info : default;
    }

    public Chess3DStateDto GetState()
    {
        ThrowIfDisposed();
        if (Chess3D_GetState(_handle, out var state) == 0)
        {
            throw new InvalidOperationException("Could not read 3D state.");
        }
        return state;
    }

    public int[] GetBoard()
    {
        ThrowIfDisposed();
        var board = new int[512];
        if (Chess3D_GetBoard(_handle, board) == 0)
        {
            throw new InvalidOperationException("Could not read 3D board.");
        }
        return board;
    }

    public bool SetBoard(int[] board, int sideToMove)
    {
        ThrowIfDisposed();
        if (board.Length != 512)
        {
            throw new ArgumentException("A 3D board snapshot must contain exactly 512 cells.", nameof(board));
        }
        return Chess3D_SetBoard(_handle, board, sideToMove) != 0;
    }

    public bool SetPiece(int x, int y, int z, int side, int type)
    {
        ThrowIfDisposed();
        return Chess3D_SetPiece(_handle, x, y, z, side, type) != 0;
    }

    public int GetPiece(int x, int y, int z)
    {
        ThrowIfDisposed();
        return Chess3D_GetPiece(_handle, x, y, z);
    }

    public Chess3DMoveDto[] GetLegalMoves()
    {
        ThrowIfDisposed();
        var buffer = new Chess3DMoveDto[4096];
        var count = Chess3D_GetLegalMoves(_handle, buffer, buffer.Length);
        if (count <= buffer.Length)
        {
            return buffer.Take(count).ToArray();
        }
        buffer = new Chess3DMoveDto[count];
        Chess3D_GetLegalMoves(_handle, buffer, buffer.Length);
        return buffer;
    }

    public Chess3DMoveDto[] GetPieceMoves(int x, int y, int z)
    {
        ThrowIfDisposed();
        var buffer = new Chess3DMoveDto[4096];
        var count = Chess3D_GetPieceMoves(_handle, x, y, z, buffer, buffer.Length);
        if (count <= buffer.Length)
        {
            return buffer.Take(count).ToArray();
        }
        buffer = new Chess3DMoveDto[count];
        Chess3D_GetPieceMoves(_handle, x, y, z, buffer, buffer.Length);
        return buffer;
    }

    public bool TryMakeMove(int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int promotionType, out Chess3DMoveDto move)
    {
        ThrowIfDisposed();
        return Chess3D_TryMakeMove(_handle, fromX, fromY, fromZ, toX, toY, toZ, promotionType, out move) != 0;
    }

    public bool TryMakeProjectedMove(int primarySide, int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int promotionType, out Chess3DMoveDto move)
    {
        ThrowIfDisposed();
        return Chess3D_TryMakeProjectedMove(_handle, primarySide, fromX, fromY, fromZ, toX, toY, toZ, promotionType, out move) != 0;
    }

    public bool MakeBestMove(int depth, out Chess3DMoveDto move)
    {
        ThrowIfDisposed();
        return Chess3D_MakeBestMove(_handle, depth, out move) != 0;
    }

    public bool RotateLayer(int axis, int layer, int quarterTurns)
    {
        ThrowIfDisposed();
        return Chess3D_RotateLayer(_handle, axis, layer, quarterTurns) != 0;
    }

    public bool IsLayerTurnEnabled()
    {
        ThrowIfDisposed();
        return Chess3D_IsLayerTurnEnabled(_handle) != 0;
    }

    public bool CanRotateLayer(int axis, int layer, int quarterTurns)
    {
        ThrowIfDisposed();
        return Chess3D_CanRotateLayer(_handle, axis, layer, quarterTurns) != 0;
    }

    public (int Axis, int Layer, int QuarterTurns, int ResultCode) GetLastLayerTurnInfo()
    {
        ThrowIfDisposed();
        return Chess3D_GetLastLayerTurnInfo(_handle, out var axis, out var layer, out var quarterTurns, out var resultCode) != 0
            ? (axis, layer, quarterTurns, resultCode)
            : (-1, -1, 0, 0);
    }

    public string GetLayerTurnProfileSummary()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetLayerTurnProfileSummary(_handle, buffer, capacity));
    }

    public string GetLayerTurnResultName(int resultCode)
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetLayerTurnResultName(resultCode, buffer, capacity));
    }

    public bool IsProjectionModeEnabled()
    {
        ThrowIfDisposed();
        return Chess3D_IsProjectionModeEnabled(_handle) != 0;
    }

    public int GetProjectionMacroPlayerCount()
    {
        ThrowIfDisposed();
        return Chess3D_GetProjectionMacroPlayerCount(_handle);
    }

    public int GetProjectionCountForMacroPlayer(int macroPlayer)
    {
        ThrowIfDisposed();
        return Chess3D_GetProjectionCountForMacroPlayer(_handle, macroPlayer);
    }

    public int GetProjectionSide(int macroPlayer, int projectionIndex)
    {
        ThrowIfDisposed();
        return Chess3D_GetProjectionSide(_handle, macroPlayer, projectionIndex);
    }

    public int GetMacroPlayerForSide(int side)
    {
        ThrowIfDisposed();
        return Chess3D_GetMacroPlayerForSide(_handle, side);
    }

    public string GetProjectionProfileSummary()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetProjectionProfileSummary(_handle, buffer, capacity));
    }

    public string GetLastProjectionError()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetLastProjectionError(_handle, buffer, capacity));
    }

    public bool TransformMoveBetweenSides(int sourceSide, int targetSide, int fromX, int fromY, int fromZ, int toX, int toY, int toZ,
        out (int X, int Y, int Z) transformedFrom, out (int X, int Y, int Z) transformedTo)
    {
        ThrowIfDisposed();
        var ok = Chess3D_TransformMoveBetweenSides(_handle, sourceSide, targetSide, fromX, fromY, fromZ, toX, toY, toZ,
            out var outFromX, out var outFromY, out var outFromZ, out var outToX, out var outToY, out var outToZ) != 0;
        transformedFrom = (outFromX, outFromY, outFromZ);
        transformedTo = (outToX, outToY, outToZ);
        return ok;
    }

    public string GetPositionText()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetPositionText(_handle, buffer, capacity));
    }

    public string GetLastInfo()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetLastInfo(_handle, buffer, capacity));
    }

    public string GetCurrentRulesetId()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetCurrentRulesetId(_handle, buffer, capacity));
    }

    public string GetCurrentRulesetDisplayName()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetCurrentRulesetDisplayName(_handle, buffer, capacity));
    }

    public string GetGoalProfileType()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetGoalProfileType(_handle, buffer, capacity));
    }

    public string GetCaptureProfileType()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetCaptureProfileType(_handle, buffer, capacity));
    }

    public string GetOccupancyProfileType()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetOccupancyProfileType(_handle, buffer, capacity));
    }

    public string GetFusionProfileType()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetFusionProfileType(_handle, buffer, capacity));
    }

    public string GetCorePhysicsProfileType()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetCorePhysicsProfileType(_handle, buffer, capacity));
    }

    public string GetLayerTurnProfileType()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetLayerTurnProfileType(_handle, buffer, capacity));
    }

    public string GetVictoryProfileType()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetVictoryProfileType(_handle, buffer, capacity));
    }

    public string GetLastProfileError()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetLastProfileError(_handle, buffer, capacity));
    }

    public int GetAnchorCount(int side)
    {
        ThrowIfDisposed();
        return Chess3D_GetAnchorCount(_handle, side);
    }

    public int GetRequiredAnchorCount(int side)
    {
        ThrowIfDisposed();
        return Chess3D_GetRequiredAnchorCount(_handle, side);
    }

    public bool IsGameOver()
    {
        ThrowIfDisposed();
        return Chess3D_IsGameOver(_handle) != 0;
    }

    public int GetWinnerSide()
    {
        ThrowIfDisposed();
        return Chess3D_GetWinnerSide(_handle);
    }

    public bool IsCoreStackEnabled()
    {
        ThrowIfDisposed();
        return Chess3D_IsCoreStackEnabled(_handle) != 0;
    }

    public int GetCoreStackCount(int x, int y, int z)
    {
        ThrowIfDisposed();
        return Chess3D_GetCoreStackCount(_handle, x, y, z);
    }

    public int GetProjectedPiece(int x, int y, int z)
    {
        ThrowIfDisposed();
        return Chess3D_GetProjectedPiece(_handle, x, y, z);
    }

    public bool IsFusionEnabled()
    {
        ThrowIfDisposed();
        return Chess3D_IsFusionEnabled(_handle) != 0;
    }

    public bool RecomputeFusion()
    {
        ThrowIfDisposed();
        return Chess3D_RecomputeFusion(_handle) != 0;
    }

    public int GetCoreFusionKind(int x, int y, int z)
    {
        ThrowIfDisposed();
        return Chess3D_GetCoreFusionKind(_handle, x, y, z);
    }

    public bool IsCoreCellContested(int x, int y, int z)
    {
        ThrowIfDisposed();
        return Chess3D_IsCoreCellContested(_handle, x, y, z) != 0;
    }

    public int GetSideFusionCount(int side)
    {
        ThrowIfDisposed();
        return Chess3D_GetSideFusionCount(_handle, side);
    }

    public int GetSideImplosionProgress(int side)
    {
        ThrowIfDisposed();
        return Chess3D_GetSideImplosionProgress(_handle, side);
    }

    public string GetFusionKindName(int fusionKind)
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetFusionKindName(fusionKind, buffer, capacity));
    }

    public bool IsReserveEnabled()
    {
        ThrowIfDisposed();
        return Chess3D_IsReserveEnabled(_handle) != 0;
    }

    public bool IsKnockbackEnabled()
    {
        ThrowIfDisposed();
        return Chess3D_IsKnockbackEnabled(_handle) != 0;
    }

    public int GetReserveCount(int side, int pieceType)
    {
        ThrowIfDisposed();
        return Chess3D_GetReserveCount(_handle, side, pieceType);
    }

    public int GetReserveTotal(int side)
    {
        ThrowIfDisposed();
        return Chess3D_GetReserveTotal(_handle, side);
    }

    public int GetLastCapturedPieceCode()
    {
        ThrowIfDisposed();
        return Chess3D_GetLastCapturedPieceCode(_handle);
    }

    public int GetLastCapturedPieceReserveDestination()
    {
        ThrowIfDisposed();
        return Chess3D_GetLastCapturedPieceReserveDestination(_handle);
    }

    public (int CapturedPieceCode, int DestinationKind, int X, int Y, int Z) GetLastKnockbackInfo()
    {
        ThrowIfDisposed();
        return Chess3D_GetLastKnockbackInfo(_handle, out var captured, out var destination, out var x, out var y, out var z) != 0
            ? (captured, destination, x, y, z)
            : (0, 0, -1, -1, -1);
    }

    public int GetActionCount()
    {
        ThrowIfDisposed();
        return Chess3D_GetActionCount(_handle);
    }

    public string GetActionNotation(int actionIndex)
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetActionNotation(_handle, actionIndex, buffer, capacity));
    }

    public string GetLastActionNotation()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetLastActionNotation(_handle, buffer, capacity));
    }

    public bool CanRestoreReservePiece(int side, int pieceType, int x, int y, int z)
    {
        ThrowIfDisposed();
        return Chess3D_CanRestoreReservePiece(_handle, side, pieceType, x, y, z) != 0;
    }

    public bool RestoreReservePiece(int side, int pieceType, int x, int y, int z)
    {
        ThrowIfDisposed();
        return Chess3D_RestoreReservePiece(_handle, side, pieceType, x, y, z) != 0;
    }

    public bool AutoRestoreReservePiece(int side, int pieceType)
    {
        ThrowIfDisposed();
        return Chess3D_AutoRestoreReservePiece(_handle, side, pieceType) != 0;
    }

    public string GetLastReserveRestoreInfo()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetLastReserveRestoreInfo(_handle, buffer, capacity));
    }

    public int BuildLegalActionPreviewForCell(int x, int y, int z, int side)
    {
        ThrowIfDisposed();
        return Chess3D_BuildLegalActionPreviewForCell(_handle, x, y, z, side);
    }

    public Chess3DLegalActionPreviewEntryDto[] GetLegalActionPreview()
    {
        ThrowIfDisposed();
        var count = Chess3D_GetLegalActionPreviewCount(_handle);
        if (count <= 0)
        {
            return Array.Empty<Chess3DLegalActionPreviewEntryDto>();
        }
        var entries = new Chess3DLegalActionPreviewEntryDto[count];
        for (var i = 0; i < count; ++i)
        {
            Chess3D_GetLegalActionPreviewEntry(_handle, i, out entries[i]);
        }
        return entries;
    }

    public string GetPreviewEntryReason(int previewIndex)
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetPreviewEntryReason(_handle, previewIndex, buffer, capacity));
    }

    public string GetLastInvalidActionReason()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetLastInvalidActionReason(_handle, buffer, capacity));
    }

    public int GetCurrentTurnKind()
    {
        ThrowIfDisposed();
        return Chess3D_GetCurrentTurnKind(_handle);
    }

    public int GetCurrentSide()
    {
        ThrowIfDisposed();
        return Chess3D_GetCurrentSide(_handle);
    }

    public int GetCurrentMacroPlayer()
    {
        ThrowIfDisposed();
        return Chess3D_GetCurrentMacroPlayer(_handle);
    }

    public int GetAllowedActionMask()
    {
        ThrowIfDisposed();
        return Chess3D_GetAllowedActionMask(_handle);
    }

    public string GetTurnSummary()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetTurnSummary(_handle, buffer, capacity));
    }

    public int GetGamePhase()
    {
        ThrowIfDisposed();
        return Chess3D_GetGamePhase(_handle);
    }

    public int GetGameOutcome()
    {
        ThrowIfDisposed();
        return Chess3D_GetGameOutcome(_handle);
    }

    public string GetGameOutcomeName(int outcome)
    {
        return ReadString((buffer, capacity) => Chess3D_GetGameOutcomeName(outcome, buffer, capacity));
    }

    public string GetCurrentTurnSummary()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetCurrentTurnSummary(_handle, buffer, capacity));
    }

    public bool IsActionKindAllowed(int actionKind)
    {
        ThrowIfDisposed();
        return Chess3D_IsActionKindAllowed(_handle, actionKind) != 0;
    }

    public string GetModeRuleSummary()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetModeRuleSummary(_handle, buffer, capacity));
    }

    public string GetLastMoveLegalityReason()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetLastMoveLegalityReason(_handle, buffer, capacity));
    }

    public bool IsSideInCheck(int side)
    {
        ThrowIfDisposed();
        return Chess3D_IsSideInCheck(_handle, side) != 0;
    }

    public int GetSideLegalActionCount(int side)
    {
        ThrowIfDisposed();
        return Chess3D_GetSideLegalActionCount(_handle, side);
    }

    public bool HasAnyLegalActionForSide(int side)
    {
        ThrowIfDisposed();
        return Chess3D_HasAnyLegalActionForSide(_handle, side) != 0;
    }

    public string GetCheckStatusSummary(int side)
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetCheckStatusSummary(_handle, side, buffer, capacity));
    }

    public string ExportSaveGameJson()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_ExportSaveGameJson(_handle, buffer, capacity), 65536);
    }

    public bool LoadSaveGameJson(string json)
    {
        ThrowIfDisposed();
        return Chess3D_LoadSaveGameJson(_handle, json) != 0;
    }

    public string ExportReplayJson()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_ExportReplayJson(_handle, buffer, capacity), 65536);
    }

    public bool LoadReplayJson(string json, int mode = 0)
    {
        ThrowIfDisposed();
        return Chess3D_LoadReplayJson(_handle, json, mode) != 0;
    }

    public bool ReplayAction(int actionIndex = 0)
    {
        ThrowIfDisposed();
        return Chess3D_ReplayAction(_handle, actionIndex) != 0;
    }

    public bool ReplayAll()
    {
        ThrowIfDisposed();
        return Chess3D_ReplayAll(_handle) != 0;
    }

    public bool ResetReplayCursor()
    {
        ThrowIfDisposed();
        return Chess3D_ResetReplayCursor(_handle) != 0;
    }

    public int GetReplayActionCount()
    {
        ThrowIfDisposed();
        return Chess3D_GetReplayActionCount(_handle);
    }

    public int GetReplayCursor()
    {
        ThrowIfDisposed();
        return Chess3D_GetReplayCursor(_handle);
    }

    public string GetLastReplayError()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetLastReplayError(_handle, buffer, capacity));
    }

    public string GetStateHash()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetStateHash(_handle, buffer, capacity));
    }

    public long PerftActions(int depth)
    {
        ThrowIfDisposed();
        return Chess3D_PerftActions(_handle, depth);
    }

    public string DivideActionsJson(int depth)
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_DivideActionsJson(_handle, depth, buffer, capacity), 8192);
    }

    public string GetLastPerftError()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetLastPerftError(_handle, buffer, capacity));
    }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            Chess3D_Destroy(_handle);
            _handle = IntPtr.Zero;
        }
    }

    private string ReadString(Func<StringBuilder, int, int> reader, int initialCapacity = 512)
    {
        var buffer = new StringBuilder(initialCapacity);
        var needed = reader(buffer, buffer.Capacity);
        if (needed > buffer.Capacity)
        {
            buffer = new StringBuilder(needed);
            reader(buffer, buffer.Capacity);
        }
        return buffer.ToString();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_handle == IntPtr.Zero, this);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr Chess3D_Create();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void Chess3D_Destroy(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void Chess3D_Reset(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void Chess3D_Clear(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_LoadRulesJson(IntPtr handle, string json);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_LoadRuleProfileJson(IntPtr handle, string json);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetRulesInfo(IntPtr handle, out Chess3DRulesInfoDto info);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetState(IntPtr handle, out Chess3DStateDto state);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetBoard(IntPtr handle, [Out] int[] pieces512);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_SetBoard(IntPtr handle, [In] int[] pieces512, int sideToMove);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_SetPiece(IntPtr handle, int x, int y, int z, int side, int type);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetPiece(IntPtr handle, int x, int y, int z);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetLegalMoves(IntPtr handle, [Out] Chess3DMoveDto[] buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetPieceMoves(IntPtr handle, int fromX, int fromY, int fromZ, [Out] Chess3DMoveDto[] buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_TryMakeMove(IntPtr handle, int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int promotionType, out Chess3DMoveDto playedMove);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_TryMakeProjectedMove(IntPtr handle, int primarySide, int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int promotionType, out Chess3DMoveDto playedMove);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_MakeBestMove(IntPtr handle, int depth, out Chess3DMoveDto playedMove);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_RotateLayer(IntPtr handle, int axis, int layer, int quarterTurns);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_IsLayerTurnEnabled(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_CanRotateLayer(IntPtr handle, int axis, int layer, int quarterTurns);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetLastLayerTurnInfo(IntPtr handle, out int axis, out int layer, out int quarterTurns, out int resultCode);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetLayerTurnProfileSummary(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetLayerTurnResultName(int resultCode, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_IsProjectionModeEnabled(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetProjectionMacroPlayerCount(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetProjectionCountForMacroPlayer(IntPtr handle, int macroPlayer);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetProjectionSide(IntPtr handle, int macroPlayer, int projectionIndex);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetMacroPlayerForSide(IntPtr handle, int side);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetProjectionProfileSummary(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetLastProjectionError(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_TransformMoveBetweenSides(IntPtr handle, int sourceSide, int targetSide, int fromX, int fromY, int fromZ, int toX, int toY, int toZ, out int outFromX, out int outFromY, out int outFromZ, out int outToX, out int outToY, out int outToZ);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetPositionText(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetLastInfo(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetCurrentRulesetId(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetCurrentRulesetDisplayName(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetGoalProfileType(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetCaptureProfileType(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetOccupancyProfileType(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetFusionProfileType(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetCorePhysicsProfileType(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetLayerTurnProfileType(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetVictoryProfileType(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetLastProfileError(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetAnchorCount(IntPtr handle, int side);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetRequiredAnchorCount(IntPtr handle, int side);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_IsGameOver(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetWinnerSide(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_IsCoreStackEnabled(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetCoreStackCount(IntPtr handle, int x, int y, int z);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetProjectedPiece(IntPtr handle, int x, int y, int z);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_IsFusionEnabled(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_RecomputeFusion(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetCoreFusionKind(IntPtr handle, int x, int y, int z);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_IsCoreCellContested(IntPtr handle, int x, int y, int z);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetSideFusionCount(IntPtr handle, int side);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetSideImplosionProgress(IntPtr handle, int side);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetFusionKindName(int fusionKind, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_IsReserveEnabled(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_IsKnockbackEnabled(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetReserveCount(IntPtr handle, int side, int pieceType);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetReserveTotal(IntPtr handle, int side);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetLastCapturedPieceCode(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetLastCapturedPieceReserveDestination(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetLastKnockbackInfo(IntPtr handle, out int capturedPieceCode, out int destinationKind, out int x, out int y, out int z);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetActionCount(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetActionNotation(IntPtr handle, int actionIndex, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetLastActionNotation(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_CanRestoreReservePiece(IntPtr handle, int side, int pieceType, int x, int y, int z);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_RestoreReservePiece(IntPtr handle, int side, int pieceType, int x, int y, int z);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_AutoRestoreReservePiece(IntPtr handle, int side, int pieceType);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetLastReserveRestoreInfo(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_BuildLegalActionPreviewForCell(IntPtr handle, int x, int y, int z, int side);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetLegalActionPreviewCount(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetLegalActionPreviewEntry(IntPtr handle, int previewIndex, out Chess3DLegalActionPreviewEntryDto entry);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetPreviewEntryReason(IntPtr handle, int previewIndex, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetLastInvalidActionReason(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetCurrentTurnKind(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetCurrentSide(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetCurrentMacroPlayer(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetAllowedActionMask(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetTurnSummary(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetGamePhase(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetGameOutcome(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetGameOutcomeName(int outcome, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetCurrentTurnSummary(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_IsActionKindAllowed(IntPtr handle, int actionKind);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetModeRuleSummary(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetLastMoveLegalityReason(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_IsSideInCheck(IntPtr handle, int side);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetSideLegalActionCount(IntPtr handle, int side);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_HasAnyLegalActionForSide(IntPtr handle, int side);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetCheckStatusSummary(IntPtr handle, int side, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_ExportSaveGameJson(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_LoadSaveGameJson(IntPtr handle, string json);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_ExportReplayJson(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_LoadReplayJson(IntPtr handle, string json, int mode);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_ReplayAction(IntPtr handle, int actionIndex);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_ReplayAll(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_ResetReplayCursor(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetReplayActionCount(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_GetReplayCursor(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetLastReplayError(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetStateHash(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern long Chess3D_PerftActions(IntPtr handle, int depth);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_DivideActionsJson(IntPtr handle, int depth, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetLastPerftError(IntPtr handle, StringBuilder buffer, int capacity);
}

[StructLayout(LayoutKind.Sequential)]
internal struct Chess3DMoveDto
{
    public int FromX;
    public int FromY;
    public int FromZ;
    public int ToX;
    public int ToY;
    public int ToZ;
    public int Piece;
    public int Captured;
    public int PromotionType;
    public int Flags;
    public int Score;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Chess3DStateDto
{
    public int Width;
    public int Height;
    public int Depth;
    public int SideToMove;
    public int ActiveSideCount;
    public int LegalMoveCount;
    public int PieceCount;
    public int RulesLoaded;
    public int KingSafetyEnabled;
    public int LastFromX;
    public int LastFromY;
    public int LastFromZ;
    public int LastToX;
    public int LastToY;
    public int LastToZ;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Chess3DRulesInfoDto
{
    public int Width;
    public int Height;
    public int Depth;
    public int ActiveSideCount;
    public int MovementProfile;
    public int KingSafetyEnabled;
    public int MaxPiecesPerSide;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Chess3DLegalActionPreviewEntryDto
{
    public int Kind;
    public int FromX;
    public int FromY;
    public int FromZ;
    public int ToX;
    public int ToY;
    public int ToZ;
    public int Flags;
    public int PieceCode;
    public int CapturedPieceCode;
    public int Side;
    public int ReasonCode;
}
