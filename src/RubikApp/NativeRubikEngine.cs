using System.Runtime.InteropServices;
using System.Text;

namespace RubikApp;

internal sealed class NativeRubikEngine : IDisposable
{
    private const string DllName = "RubikEngine.dll";
    private IntPtr _handle;

    public NativeRubikEngine()
    {
        _handle = Rubik_Create();
        if (_handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("RubikEngine.dll did not create an engine instance.");
        }
    }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            Rubik_Destroy(_handle);
            _handle = IntPtr.Zero;
        }
    }

    public void Reset()
    {
        Rubik_Reset(_handle);
    }

    public RubikStateDto GetState()
    {
        return Rubik_GetState(_handle, out var state) != 0 ? state : default;
    }

    public int[] GetCells()
    {
        var state = GetState();
        var cells = new int[Math.Max(0, state.CellCount)];
        if (cells.Length > 0)
        {
            Rubik_GetCells(_handle, cells);
        }
        return cells;
    }

    public int FaceletSchemaVersion => Rubik_GetFaceletSchemaVersion(_handle);

    public int FaceletCount => Rubik_GetFaceletCount(_handle);

    public int[] GetFacelets()
    {
        var facelets = new int[Math.Max(0, FaceletCount)];
        if (facelets.Length == 0)
        {
            return facelets;
        }

        if (Rubik_GetFacelets(_handle, facelets, facelets.Length) < 0)
        {
            throw new InvalidOperationException(GetLastInfo());
        }
        return facelets;
    }

    public bool SetFacelets(int[] facelets)
    {
        ArgumentNullException.ThrowIfNull(facelets);
        return Rubik_SetFacelets(_handle, facelets, facelets.Length) != 0;
    }

    public int GetFacelet(RubikFace face, int row, int column)
    {
        return Rubik_GetFacelet(_handle, (int)face, row, column);
    }

    public bool SetFacelet(RubikFace face, int row, int column, int colorId)
    {
        return Rubik_SetFacelet(_handle, (int)face, row, column, colorId) != 0;
    }

    public string GetColorScheme()
    {
        return GetText(Rubik_GetColorScheme);
    }

    public bool ValidateFacelets(int[] facelets)
    {
        ArgumentNullException.ThrowIfNull(facelets);
        return Rubik_ValidateFacelets(_handle, facelets, facelets.Length) != 0;
    }

    public bool SetSize(int size)
    {
        return Rubik_SetSize(_handle, size) != 0;
    }

    public bool SetCell(int x, int y, int z, int value)
    {
        return Rubik_SetCell(_handle, x, y, z, value) != 0;
    }

    public bool SetCells(int[] cells)
    {
        var expected = GetState().CellCount;
        if (cells.Length != expected)
        {
            throw new ArgumentException($"Rubik state must contain exactly {expected} cells.", nameof(cells));
        }
        return Rubik_SetCells(_handle, cells) != 0;
    }

    public bool RotateLayer(int axis, int layer, int quarterTurns)
    {
        return Rubik_RotateLayer(_handle, axis, layer, quarterTurns) != 0;
    }

    public bool Scramble(int seed, int length)
    {
        return Rubik_Scramble(_handle, seed, length) != 0;
    }

    public RubikMoveDto[] SolveByReverseHistory()
    {
        var count = Rubik_SolveByReverseHistory(_handle, null, 0);
        if (count <= 0)
        {
            return count == 0 ? Array.Empty<RubikMoveDto>() : Array.Empty<RubikMoveDto>();
        }

        var moves = new RubikMoveDto[count];
        Rubik_SolveByReverseHistory(_handle, moves, moves.Length);
        return moves;
    }

    public RubikMoveDto[] GetHistory()
    {
        var count = Rubik_GetHistory(_handle, null, 0);
        if (count <= 0)
        {
            return Array.Empty<RubikMoveDto>();
        }

        var moves = new RubikMoveDto[count];
        Rubik_GetHistory(_handle, moves, moves.Length);
        return moves;
    }

    public bool ApplyMoves(RubikMoveDto[] moves)
    {
        return Rubik_ApplyMoves(_handle, moves, moves.Length) != 0;
    }

    public string GetCommandText()
    {
        return GetText(Rubik_GetCommandText);
    }

    public string GetLastInfo()
    {
        return GetText(Rubik_GetLastInfo);
    }

    private string GetText(TextCallback callback)
    {
        var buffer = new StringBuilder(1024);
        var needed = callback(_handle, buffer, buffer.Capacity);
        if (needed > buffer.Capacity)
        {
            buffer = new StringBuilder(needed);
            callback(_handle, buffer, buffer.Capacity);
        }
        return buffer.ToString();
    }

    private delegate int TextCallback(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr Rubik_Create();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void Rubik_Destroy(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void Rubik_Reset(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_SetSize(IntPtr handle, int size);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_GetState(IntPtr handle, out RubikStateDto state);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_GetCells(IntPtr handle, [Out] int[] cells512);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_SetCells(IntPtr handle, [In] int[] cells512);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_SetCell(IntPtr handle, int x, int y, int z, int value);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_RotateLayer(IntPtr handle, int axis, int layer, int quarterTurns);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_Scramble(IntPtr handle, int seed, int length);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_SolveByReverseHistory(IntPtr handle, [Out] RubikMoveDto[]? buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_GetHistory(IntPtr handle, [Out] RubikMoveDto[]? buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_ApplyMoves(IntPtr handle, [In] RubikMoveDto[] moves, int count);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Rubik_GetCommandText(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Rubik_GetLastInfo(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_GetFaceletSchemaVersion(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_GetFaceletCount(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_GetFacelets(IntPtr handle, [Out] int[] facelets, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_SetFacelets(IntPtr handle, [In] int[] facelets, int count);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_GetFacelet(IntPtr handle, int face, int row, int column);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_SetFacelet(IntPtr handle, int face, int row, int column, int colorId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int Rubik_GetColorScheme(IntPtr handle, StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_ValidateFacelets(IntPtr handle, [In] int[] facelets, int count);

    [StructLayout(LayoutKind.Sequential)]
    public struct RubikMoveDto
    {
        public int Axis;
        public int Layer;
        public int QuarterTurns;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RubikStateDto
    {
        public int Size;
        public int CellCount;
        public int HistoryCount;
        public int IsSolved;
        public int ManualState;
        public int LastAxis;
        public int LastLayer;
        public int LastQuarterTurns;
    }

    public enum RubikFace
    {
        U = 0,
        R = 1,
        F = 2,
        D = 3,
        L = 4,
        B = 5
    }
}
