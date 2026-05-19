using System.Runtime.InteropServices;
using System.Text;

namespace ChessApp;

internal static class NativeGpuBackend
{
    private const string DllName = "ChessGpuBackend.dll";

    public static bool IsAvailable()
    {
        try
        {
            return ChessGpu_IsAvailable() != 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    public static string GetInfo()
    {
        try
        {
            var buffer = new StringBuilder(512);
            var needed = ChessGpu_GetBackendInfo(buffer, buffer.Capacity);
            if (needed > buffer.Capacity)
            {
                buffer = new StringBuilder(needed);
                ChessGpu_GetBackendInfo(buffer, buffer.Capacity);
            }
            return buffer.ToString();
        }
        catch (DllNotFoundException)
        {
            return "ChessGpuBackend.dll not found.";
        }
        catch (EntryPointNotFoundException)
        {
            return "ChessGpuBackend.dll has incompatible exports.";
        }
    }

    public static int EvaluateBoard(int[] board, int sideToMove)
    {
        if (board.Length != 64)
        {
            throw new ArgumentException("Board must contain exactly 64 integers.", nameof(board));
        }
        var scores = new int[1];
        try
        {
            return ChessGpu_EvaluateBatch(board, 1, sideToMove, scores) == 1 ? scores[0] : 0;
        }
        catch (DllNotFoundException)
        {
            return 0;
        }
        catch (EntryPointNotFoundException)
        {
            return 0;
        }
    }

    public static int Evaluate3DBoard(int[] board, int perspectiveSide)
    {
        if (board.Length != 512)
        {
            throw new ArgumentException("3D board must contain exactly 512 integers.", nameof(board));
        }
        var scores = new int[1];
        try
        {
            return ChessGpu_Evaluate3DBatch(board, 1, perspectiveSide, scores) == 1 ? scores[0] : 0;
        }
        catch (DllNotFoundException)
        {
            return 0;
        }
        catch (EntryPointNotFoundException)
        {
            return 0;
        }
    }

    public static int[] GenerateRubikBoards(int[] board, int[] actions3)
    {
        if (board.Length != 512)
        {
            throw new ArgumentException("3D board must contain exactly 512 integers.", nameof(board));
        }
        if (actions3.Length % 3 != 0)
        {
            throw new ArgumentException("Rubik actions must be axis/layer/turn triplets.", nameof(actions3));
        }
        var actionCount = actions3.Length / 3;
        var result = new int[actionCount * 512];
        try
        {
            ChessGpu_GenerateRubikBatch(board, actions3, actionCount, result);
            return result;
        }
        catch (DllNotFoundException)
        {
            return result;
        }
        catch (EntryPointNotFoundException)
        {
            return result;
        }
    }

    public static ChessGpuKernelStatsDto GetKernelStats()
    {
        try
        {
            return ChessGpu_GetKernelStats(out var stats) != 0 ? stats : default;
        }
        catch (DllNotFoundException)
        {
            return default;
        }
        catch (EntryPointNotFoundException)
        {
            return default;
        }
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int ChessGpu_IsAvailable();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int ChessGpu_GetBackendInfo(StringBuilder buffer, int capacity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int ChessGpu_EvaluateBatch([In] int[] boards64, int boardCount, int sideToMove, [Out] int[] scores);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int ChessGpu_Evaluate3DBatch([In] int[] boards512, int boardCount, int perspectiveSide, [Out] int[] scores);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int ChessGpu_GenerateRubikBatch([In] int[] board512, [In] int[] actions3, int actionCount, [Out] int[] outBoards512);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int ChessGpu_GetKernelStats(out ChessGpuKernelStatsDto stats);
}

[StructLayout(LayoutKind.Sequential)]
internal struct ChessGpuKernelStatsDto
{
    public int Backend;
    public int LastBoardCount;
    public int TotalGpuBatches;
    public int TotalCpuFallbackBatches;
    public int EvaluatorVersion;
}
