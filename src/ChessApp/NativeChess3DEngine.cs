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

    public string GetLayerTurnProfileType()
    {
        ThrowIfDisposed();
        return ReadString((buffer, capacity) => Chess3D_GetLayerTurnProfileType(_handle, buffer, capacity));
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

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            Chess3D_Destroy(_handle);
            _handle = IntPtr.Zero;
        }
    }

    private string ReadString(Func<StringBuilder, int, int> reader)
    {
        var buffer = new StringBuilder(512);
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
    private static extern int Chess3D_MakeBestMove(IntPtr handle, int depth, out Chess3DMoveDto playedMove);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess3D_RotateLayer(IntPtr handle, int axis, int layer, int quarterTurns);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetPositionText(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetLastInfo(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetCurrentRulesetId(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetGoalProfileType(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetCaptureProfileType(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetOccupancyProfileType(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetFusionProfileType(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess3D_GetLayerTurnProfileType(IntPtr handle, StringBuilder buffer, int capacity);

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
