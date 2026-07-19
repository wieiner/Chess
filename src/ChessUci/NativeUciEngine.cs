using System.Runtime.InteropServices;
using System.Text;

namespace ChessUci;

internal sealed class NativeUciEngine : IDisposable
{
    internal const int Queen = 5;
    internal const int Rook = 4;
    internal const int Bishop = 3;
    internal const int Knight = 2;
    private const string Library = "ChessEngine.dll";
    private IntPtr _handle;

    public NativeUciEngine()
    {
        _handle = Chess_Create();
        if (_handle == IntPtr.Zero) throw new InvalidOperationException("ChessEngine.dll failed to create a game.");
    }

    public void Reset() => Chess_Reset(Handle);
    public bool SetFen(string fen) => Chess_SetFen(Handle, fen) != 0;

    public string GetFen()
    {
        var buffer = new StringBuilder(256);
        var required = Chess_GetFen(Handle, buffer, buffer.Capacity);
        if (required > buffer.Capacity)
        {
            buffer = new StringBuilder(required);
            Chess_GetFen(Handle, buffer, buffer.Capacity);
        }
        return buffer.ToString();
    }

    public bool TryMakeMove(UciCoordinateMove move, out NativeMove played) =>
        Chess_TryMakeMove(Handle, move.FromFile, move.FromRank, move.ToFile, move.ToRank, move.Promotion, out played) != 0;

    public bool MakeBestMove(NativeSearchOptions options, out NativeMove played) =>
        Chess_MakeBestMoveEx(Handle, ref options, out played) != 0;

    public NativeSearchInfo GetSearchInfo() =>
        Chess_GetLastSearchStats(Handle, out var info) != 0 ? info : default;
    public NativeState GetState() => Chess_GetState(Handle, out var state) != 0 ? state : default;

    public void CancelSearch() => Chess_CancelSearch(Handle);
    public bool SetSearchNodeLimit(long limit) => Chess_SetSearchNodeLimit(Handle, limit) != 0;

    public void Dispose()
    {
        if (_handle == IntPtr.Zero) return;
        Chess_Destroy(_handle);
        _handle = IntPtr.Zero;
    }

    private IntPtr Handle => _handle != IntPtr.Zero ? _handle : throw new ObjectDisposedException(nameof(NativeUciEngine));

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr Chess_Create();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void Chess_Destroy(IntPtr handle);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void Chess_Reset(IntPtr handle);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int Chess_SetFen(IntPtr handle, string fen);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int Chess_GetFen(IntPtr handle, StringBuilder buffer, int capacity);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int Chess_TryMakeMove(IntPtr handle,
        int fromFile, int fromRank, int toFile, int toRank, int promotion, out NativeMove playedMove);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int Chess_MakeBestMoveEx(IntPtr handle,
        ref NativeSearchOptions options, out NativeMove playedMove);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int Chess_GetLastSearchStats(IntPtr handle,
        out NativeSearchInfo info);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int Chess_GetState(IntPtr handle,
        out NativeState state);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void Chess_CancelSearch(IntPtr handle);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int Chess_SetSearchNodeLimit(IntPtr handle, long nodeLimit);
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeMove
{
    public int FromFile;
    public int FromRank;
    public int ToFile;
    public int ToRank;
    public int Promotion;
    public int Flags;
    public int Score;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeSearchOptions
{
    public int Depth;
    public int TimeLimitMs;
    public int AutomaticDepth;
    public int UseQuiescence;
    public int UseTranspositionTable;
    public int UseMoveOrdering;
    public int UsePieceSquareTables;
    public int UseBishopPairBonus;
    public int UseKingSafetyBonus;
    public int UseGpuEvaluation;
    public int UseEndgameTables;
    public int OpeningRandomness;
    public int OpeningMaxPly;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeSearchInfo
{
    public int RequestedDepth;
    public int CompletedDepth;
    public int StoppedByTime;
    public int ReachedRequestedDepth;
    public int TimeLimitMs;
    public int ElapsedMs;
    public long Nodes;
    public int BestScore;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeState
{
    public int SideToMove;
    public int Status;
    public int IsCheck;
    public int HalfmoveClock;
    public int FullmoveNumber;
    public int LegalMoveCount;
    public int LastFromFile;
    public int LastFromRank;
    public int LastToFile;
    public int LastToRank;
    public int LastPromotion;
    public int LastFlags;
    public int RepetitionCount;
    public int CanClaimRepetition;
    public int CanClaimFiftyMove;
}
