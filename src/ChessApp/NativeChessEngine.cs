using System.Runtime.InteropServices;
using System.Text;

namespace ChessApp;

internal sealed class NativeChessEngine : IDisposable
{
    public const int White = 1;
    public const int Black = -1;

    public const int Pawn = 1;
    public const int Knight = 2;
    public const int Bishop = 3;
    public const int Rook = 4;
    public const int Queen = 5;
    public const int King = 6;

    public const int MoveCapture = 1;
    public const int MoveCastle = 2;
    public const int MoveEnPassant = 4;
    public const int MovePromotion = 8;
    public const int MoveCheck = 16;

    public const int StatusPlaying = 0;
    public const int StatusCheckmate = 1;
    public const int StatusStalemate = 2;
    public const int StatusFiftyMoveClaim = 3;
    public const int StatusRepetitionClaim = 4;
    public const int StatusRepetitionDraw = 5;
    public const int StatusSeventyFiveMoveDraw = 6;

    private const string DllName = "ChessEngine.dll";
    private IntPtr _handle;

    public NativeChessEngine()
    {
        _handle = Chess_Create();
        if (_handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("ChessEngine.dll did not create a game instance.");
        }
    }

    public void Reset()
    {
        ThrowIfDisposed();
        Chess_Reset(_handle);
    }

    public bool SetFen(string fen)
    {
        ThrowIfDisposed();
        return Chess_SetFen(_handle, fen) != 0;
    }

    public string GetFen()
    {
        ThrowIfDisposed();
        var buffer = new StringBuilder(256);
        var needed = Chess_GetFen(_handle, buffer, buffer.Capacity);
        if (needed > buffer.Capacity)
        {
            buffer = new StringBuilder(needed);
            Chess_GetFen(_handle, buffer, buffer.Capacity);
        }
        return buffer.ToString();
    }

    public string GetLastSearchInfo()
    {
        ThrowIfDisposed();
        var buffer = new StringBuilder(256);
        var needed = Chess_GetLastSearchInfo(_handle, buffer, buffer.Capacity);
        if (needed > buffer.Capacity)
        {
            buffer = new StringBuilder(needed);
            Chess_GetLastSearchInfo(_handle, buffer, buffer.Capacity);
        }
        return buffer.ToString();
    }

    public ChessSearchInfoDto GetLastSearchStats()
    {
        ThrowIfDisposed();
        return Chess_GetLastSearchStats(_handle, out var info) != 0 ? info : default;
    }

    public int[] GetBoard()
    {
        ThrowIfDisposed();
        var board = new int[64];
        if (Chess_GetBoard(_handle, board) == 0)
        {
            throw new InvalidOperationException("Could not read board from ChessEngine.dll.");
        }
        return board;
    }

    public ChessStateDto GetState()
    {
        ThrowIfDisposed();
        if (Chess_GetState(_handle, out var state) == 0)
        {
            throw new InvalidOperationException("Could not read game state from ChessEngine.dll.");
        }
        return state;
    }

    public ChessDrawRulesDto GetDrawRules()
    {
        ThrowIfDisposed();
        if (Chess_GetDrawRules(_handle, out var rules) == 0)
        {
            throw new InvalidOperationException("Could not read draw rules from ChessEngine.dll.");
        }
        return rules;
    }

    public bool SetDrawRules(ChessDrawRulesDto rules)
    {
        ThrowIfDisposed();
        return Chess_SetDrawRules(_handle, ref rules) != 0;
    }

    public bool SetTablebasePath(string path)
    {
        ThrowIfDisposed();
        return Chess_SetTablebasePath(_handle, path) != 0;
    }

    public ChessTablebaseInfoDto GetTablebaseInfo()
    {
        ThrowIfDisposed();
        return Chess_GetTablebaseInfo(_handle, out var info) != 0 ? info : default;
    }

    public bool ClaimDraw()
    {
        ThrowIfDisposed();
        return Chess_ClaimDraw(_handle) != 0;
    }

    public ChessMoveDto[] GetLegalMoves()
    {
        ThrowIfDisposed();
        var buffer = new ChessMoveDto[256];
        var count = Chess_GetLegalMoves(_handle, buffer, buffer.Length);
        if (count <= buffer.Length)
        {
            return buffer.Take(count).ToArray();
        }

        buffer = new ChessMoveDto[count];
        Chess_GetLegalMoves(_handle, buffer, buffer.Length);
        return buffer;
    }

    public bool TryMakeMove(int fromFile, int fromRank, int toFile, int toRank, int promotion, out ChessMoveDto move)
    {
        ThrowIfDisposed();
        return Chess_TryMakeMove(_handle, fromFile, fromRank, toFile, toRank, promotion, out move) != 0;
    }

    public bool MakeBestMove(ChessSearchOptionsDto options, out ChessMoveDto move)
    {
        ThrowIfDisposed();
        return Chess_MakeBestMoveEx(_handle, ref options, out move) != 0;
    }

    public bool Undo()
    {
        ThrowIfDisposed();
        return Chess_Undo(_handle) != 0;
    }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            Chess_Destroy(_handle);
            _handle = IntPtr.Zero;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_handle == IntPtr.Zero, this);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr Chess_Create();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void Chess_Destroy(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void Chess_Reset(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess_SetFen(IntPtr handle, string fen);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess_GetFen(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess_GetBoard(IntPtr handle, [Out] int[] pieces64);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess_GetState(IntPtr handle, out ChessStateDto state);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess_GetDrawRules(IntPtr handle, out ChessDrawRulesDto rules);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess_SetDrawRules(IntPtr handle, ref ChessDrawRulesDto rules);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess_SetTablebasePath(IntPtr handle, string path);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess_GetTablebaseInfo(IntPtr handle, out ChessTablebaseInfoDto info);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess_ClaimDraw(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess_GetLegalMoves(IntPtr handle, [Out] ChessMoveDto[] buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess_TryMakeMove(IntPtr handle, int fromFile, int fromRank, int toFile, int toRank, int promotion, out ChessMoveDto playedMove);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess_MakeBestMove(IntPtr handle, int depth, out ChessMoveDto playedMove);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess_MakeBestMoveEx(IntPtr handle, ref ChessSearchOptionsDto options, out ChessMoveDto playedMove);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess_GetLastSearchStats(IntPtr handle, out ChessSearchInfoDto info);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Chess_Undo(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Chess_GetLastSearchInfo(IntPtr handle, StringBuilder buffer, int capacity);
}

[StructLayout(LayoutKind.Sequential)]
internal struct ChessMoveDto
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
internal struct ChessStateDto
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

[StructLayout(LayoutKind.Sequential)]
internal struct ChessSearchOptionsDto
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
internal struct ChessDrawRulesDto
{
    public int RepetitionClaimCount;
    public int RepetitionAutoDrawCount;
    public int AutoClaimThreefold;
    public int FiftyMoveClaimPlies;
    public int SeventyFiveMoveAutoPlies;
    public int AutoClaimFiftyMove;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ChessSearchInfoDto
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
internal struct ChessTablebaseInfoDto
{
    public int Enabled;
    public int SyzygyWdlFiles;
    public int SyzygyDtzFiles;
    public int MaxPieces;
    public int BuiltInEndgameTables;
}
