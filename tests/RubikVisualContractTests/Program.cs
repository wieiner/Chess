using System.Runtime.InteropServices;
using System.Text.Json;
using RubikVisuals;

var tests = new ContractChecks();

try
{
    foreach (var size in new[] { 2, 3, 8, 11 })
    {
        using var cube = NativeCube.Create(size);
        CheckSolvedScene(tests, cube, size);
    }

    CheckRotatedScenes(tests);
    CheckFaceletFallback(tests);
    CheckFixture(tests);
}
catch (Exception exception)
{
    tests.Fail($"Unhandled exception: {exception}");
}

return tests.Finish();

static void CheckSolvedScene(ContractChecks tests, NativeCube cube, int size)
{
    var full = cube.BuildVisualSummary(surfaceOnly: false);
    var shell = cube.BuildVisualSummary(surfaceOnly: true);
    var inner = Math.Max(0, size - 2);
    var expectedStickers = 6 * size * size;
    var expectedEdges = 12 * inner;
    var expectedCenters = 6 * inner * inner;
    var expectedInternals = inner * inner * inner;
    var expectedShell = size * size * size - expectedInternals;

    tests.Check(full.CubiesRendered == size * size * size, $"full N={size} renders every cubie body");
    tests.Check(shell.CubiesRendered == expectedShell, $"surface N={size} renders only shell bodies");
    tests.Check(full.StickersRendered == expectedStickers && shell.StickersRendered == expectedStickers,
        $"N={size} renders exactly 6*N*N physical stickers");
    tests.Check(full.CornersRendered == 8 && full.EdgesRendered == expectedEdges &&
        full.CentersRendered == expectedCenters && full.InternalsRendered == expectedInternals,
        $"N={size} physical cubie classes match topology formulas");
    tests.Check(full.InvalidStickers == 0 && full.OrientationAvailable && !full.FallbackRendererActive,
        $"N={size} solved visual descriptors use exact orientation without fallback");

    var colorCounts = full.Cubies.SelectMany(cubie => cubie.Stickers)
        .GroupBy(sticker => sticker.ColorId)
        .ToDictionary(group => group.Key, group => group.Count());
    tests.Check(Enumerable.Range(1, 6).All(color => colorCounts.GetValueOrDefault(color) == size * size),
        $"N={size} has N*N descriptors for every color");
    tests.Check(full.Cubies.SelectMany(cubie => cubie.Stickers).All(sticker => sticker.WorldNormal.IsUnitAxis),
        $"N={size} sticker normals remain exact unit axes");
    tests.Check(full.Cubies.All(cubie => cubie.Stickers.Select(sticker => sticker.WorldNormal).Distinct().Count() == cubie.Stickers.Count),
        $"N={size} no cubie has duplicate world normals");
    tests.Check(full.Cubies.Where(cubie => cubie.PhysicalStickerCount == 3)
        .All(cubie => cubie.Stickers.Select(sticker => sticker.ColorId).Distinct().Count() == 3),
        $"N={size} every rendered corner has three distinct colors");
    tests.Check(full.Cubies.Where(cubie => cubie.PhysicalStickerCount == 2)
        .All(cubie => cubie.Stickers.Select(sticker => sticker.ColorId).Distinct().Count() == 2),
        $"N={size} every rendered edge or wing has two distinct colors");
}

static void CheckRotatedScenes(ContractChecks tests)
{
    using var cube = NativeCube.Create(11);
    var solved = cube.Facelets;

    cube.RotateLayer(0, 10, 1);
    CheckRotationInvariants(tests, cube, "outer turn");

    cube.RotateLayer(0, 1, 1);
    CheckRotationInvariants(tests, cube, "inner slice");

    cube.RotateLayer(2, 10, 1);
    cube.RotateLayer(2, 9, 1);
    CheckRotationInvariants(tests, cube, "wide turn");

    for (var layer = 0; layer < 11; layer++)
    {
        cube.RotateLayer(1, layer, 1);
    }
    CheckRotationInvariants(tests, cube, "whole-cube rotation");

    tests.Check(cube.Scramble(20260716, 18), "deterministic N=11 scramble succeeds");
    CheckRotationInvariants(tests, cube, "scramble");

    var reverse = cube.ReverseHistory;
    tests.Check(reverse.Length > 0 && cube.ApplyMoves(reverse), "reverse-history playback applies after visual scenarios");
    tests.Check(cube.Facelets.SequenceEqual(solved), "reverse-history playback restores solved facelets");
    CheckRotationInvariants(tests, cube, "reverse-history playback");
}

static void CheckRotationInvariants(ContractChecks tests, NativeCube cube, string scenario)
{
    var summary = cube.BuildVisualSummary(surfaceOnly: true);
    tests.Check(summary.StickersRendered == 6 * cube.Size * cube.Size,
        $"{scenario} preserves physical sticker descriptor count");
    tests.Check(summary.InvalidStickers == 0 && summary.OrientationAvailable && !summary.FallbackRendererActive,
        $"{scenario} preserves exact descriptor orientation");
    tests.Check(summary.Cubies.SelectMany(cubie => cubie.Stickers)
        .GroupBy(sticker => sticker.ColorId)
        .All(group => group.Count() == cube.Size * cube.Size),
        $"{scenario} preserves color multiplicity");
}

static void CheckFaceletFallback(ContractChecks tests)
{
    using var cube = NativeCube.Create(3);
    var facelets = cube.Facelets;
    tests.Check(cube.SetFacelets(facelets), "facelet-only import succeeds for fallback test");
    var summary = cube.BuildVisualSummary(surfaceOnly: true);
    tests.Check(summary.FallbackRendererActive && !summary.OrientationAvailable,
        "facelet-only state reports shell fallback and unavailable cubie orientation");
    tests.Check(summary.StickersRendered == 54 && summary.InvalidStickers == 0,
        "facelet-only shell fallback renders all canonical facelets without invention");
}

static void CheckFixture(ContractChecks tests)
{
    var fixturePath = Path.Combine(AppContext.BaseDirectory, "fixtures", "solved-3x3.visual.json");
    using var fixture = JsonDocument.Parse(File.ReadAllBytes(fixturePath));
    using var cube = NativeCube.Create(3);
    var summary = cube.BuildVisualSummary(surfaceOnly: false);
    var root = fixture.RootElement;
    tests.Check(root.GetProperty("size").GetInt32() == summary.Size &&
        root.GetProperty("stickers").GetInt32() == summary.StickersRendered,
        "solved 3x3 visual fixture summary matches descriptors");

    foreach (var expected in root.GetProperty("representatives").EnumerateArray())
    {
        var coordinateValues = expected.GetProperty("coordinate").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        var coordinate = new RubikCoordinate(coordinateValues[0], coordinateValues[1], coordinateValues[2]);
        var actual = summary.Cubies.Single(cubie => cubie.Coordinate == coordinate);
        var expectedLocalFaces = expected.GetProperty("localFaces").EnumerateArray().Select(item => item.GetInt32()).Order().ToArray();
        var expectedColors = expected.GetProperty("colorIds").EnumerateArray().Select(item => item.GetInt32()).Order().ToArray();
        tests.Check(actual.Stickers.Select(sticker => sticker.LocalFace).Order().SequenceEqual(expectedLocalFaces) &&
            actual.Stickers.Select(sticker => sticker.ColorId).Order().SequenceEqual(expectedColors),
            $"fixture representative {coordinate} matches physical local faces and colors");
    }
}

internal sealed class NativeCube : IDisposable
{
    private const string DllName = "RubikEngine.dll";
    private IntPtr _handle;

    private NativeCube(IntPtr handle, int size)
    {
        _handle = handle;
        Size = size;
    }

    public int Size { get; }

    public int[] Facelets
    {
        get
        {
            var result = new int[6 * Size * Size];
            if (Rubik_GetFacelets(_handle, result, result.Length) != result.Length)
            {
                throw new InvalidOperationException("Native facelets are unavailable.");
            }
            return result;
        }
    }

    public NativeMove[] ReverseHistory
    {
        get
        {
            var count = Rubik_SolveByReverseHistory(_handle, null, 0);
            var moves = new NativeMove[Math.Max(0, count)];
            if (moves.Length > 0)
            {
                Rubik_SolveByReverseHistory(_handle, moves, moves.Length);
            }
            return moves;
        }
    }

    public static NativeCube Create(int size)
    {
        var handle = Rubik_CreateSized(size);
        return handle != IntPtr.Zero
            ? new NativeCube(handle, size)
            : throw new InvalidOperationException($"Could not create native N={size} cube.");
    }

    public RubikSceneVisualSummary BuildVisualSummary(bool surfaceOnly)
    {
        var cells = new int[Size * Size * Size];
        if (Rubik_GetCells(_handle, cells) == 0)
        {
            throw new InvalidOperationException("Could not read native cubies.");
        }

        int[]? facelets;
        try { facelets = Facelets; }
        catch (InvalidOperationException) { facelets = null; }

        var cubies = new List<RubikCubieVisualInput>(cells.Length);
        for (var z = 0; z < Size; z++)
        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++)
        {
            var index = z * Size * Size + y * Size + x;
            RubikCubieOrientation? orientation = null;
            if (Rubik_GetCubieOrientation(_handle, x, y, z, out var nativeOrientation) != 0)
            {
                orientation = new RubikCubieOrientation(
                    new(nativeOrientation.LocalXWorldX, nativeOrientation.LocalXWorldY, nativeOrientation.LocalXWorldZ),
                    new(nativeOrientation.LocalYWorldX, nativeOrientation.LocalYWorldY, nativeOrientation.LocalYWorldZ),
                    new(nativeOrientation.LocalZWorldX, nativeOrientation.LocalZWorldY, nativeOrientation.LocalZWorldZ));
            }
            cubies.Add(new RubikCubieVisualInput(
                new RubikCoordinate(x, y, z),
                cells[index],
                Rubik_GetCubieStickerMask(_handle, x, y, z),
                orientation));
        }
        return RubikVisualDescriptorBuilder.BuildScene(Size, facelets, cubies, surfaceOnly);
    }

    public void RotateLayer(int axis, int layer, int turns)
    {
        if (Rubik_RotateLayer(_handle, axis, layer, turns) == 0)
        {
            throw new InvalidOperationException($"Native turn failed: axis={axis}, layer={layer}, turns={turns}.");
        }
    }

    public bool Scramble(int seed, int length) => Rubik_Scramble(_handle, seed, length) != 0;
    public bool ApplyMoves(NativeMove[] moves) => Rubik_ApplyMoves(_handle, moves, moves.Length) != 0;
    public bool SetFacelets(int[] facelets) => Rubik_SetFacelets(_handle, facelets, facelets.Length) != 0;

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            Rubik_Destroy(_handle);
            _handle = IntPtr.Zero;
        }
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr Rubik_CreateSized(int size);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void Rubik_Destroy(IntPtr handle);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_GetCells(IntPtr handle, [Out] int[] cells);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_GetFacelets(IntPtr handle, [Out] int[] facelets, int capacity);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_SetFacelets(IntPtr handle, [In] int[] facelets, int count);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_GetCubieStickerMask(IntPtr handle, int x, int y, int z);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_GetCubieOrientation(IntPtr handle, int x, int y, int z, out NativeOrientation orientation);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_RotateLayer(IntPtr handle, int axis, int layer, int turns);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_Scramble(IntPtr handle, int seed, int length);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_SolveByReverseHistory(IntPtr handle, [Out] NativeMove[]? moves, int capacity);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int Rubik_ApplyMoves(IntPtr handle, [In] NativeMove[] moves, int count);

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeMove
    {
        public int Axis;
        public int Layer;
        public int QuarterTurns;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeOrientation
    {
        public int LocalXWorldX;
        public int LocalXWorldY;
        public int LocalXWorldZ;
        public int LocalYWorldX;
        public int LocalYWorldY;
        public int LocalYWorldZ;
        public int LocalZWorldX;
        public int LocalZWorldY;
        public int LocalZWorldZ;
    }
}

internal sealed class ContractChecks
{
    private int _failures;

    public void Check(bool condition, string message)
    {
        Console.WriteLine($"{(condition ? "PASS" : "FAIL")} {message}");
        if (!condition) _failures++;
    }

    public void Fail(string message) => Check(false, message);

    public int Finish()
    {
        Console.WriteLine($"RubikVisualContractTests: {(_failures == 0 ? "PASS" : $"FAIL ({_failures})")}");
        return _failures == 0 ? 0 : 1;
    }
}
