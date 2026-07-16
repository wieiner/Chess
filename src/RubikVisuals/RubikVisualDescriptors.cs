namespace RubikVisuals;

public enum RubikFace
{
    U = 0,
    R = 1,
    F = 2,
    D = 3,
    L = 4,
    B = 5
}

public readonly record struct RubikCoordinate(int X, int Y, int Z);

public readonly record struct RubikAxisVector(int X, int Y, int Z)
{
    public RubikAxisVector Negated() => new(-X, -Y, -Z);

    public bool IsUnitAxis =>
        Math.Abs(X) + Math.Abs(Y) + Math.Abs(Z) == 1 &&
        X is >= -1 and <= 1 &&
        Y is >= -1 and <= 1 &&
        Z is >= -1 and <= 1;
}

public readonly record struct RubikCubieOrientation(
    RubikAxisVector LocalX,
    RubikAxisVector LocalY,
    RubikAxisVector LocalZ);

public sealed record RubikCubieVisualInput(
    RubikCoordinate Coordinate,
    int CubieId,
    int StickerMask,
    RubikCubieOrientation? Orientation);

public sealed record RubikStickerVisualDescriptor(
    int LocalFace,
    int WorldFace,
    RubikAxisVector WorldNormal,
    int ColorId);

public sealed record RubikCubieVisualDescriptor(
    RubikCoordinate Coordinate,
    int CubieId,
    bool BodyVisible,
    bool IsSelected,
    bool OrientationAvailable,
    int PhysicalStickerCount,
    IReadOnlyList<RubikStickerVisualDescriptor> Stickers);

public sealed record RubikSceneVisualSummary(
    int Size,
    int CubiesRendered,
    int PlasticBodies,
    int StickersRendered,
    int CornersRendered,
    int EdgesRendered,
    int CentersRendered,
    int InternalsRendered,
    int TotalCorners,
    int TotalEdges,
    int TotalCenters,
    int TotalInternals,
    int InvalidStickers,
    bool FaceletsSynchronized,
    bool OrientationAvailable,
    bool FallbackRendererActive,
    IReadOnlyList<RubikCubieVisualDescriptor> Cubies);

public static class RubikVisualDescriptorBuilder
{
    public const int MinimumSize = 2;
    public const int MaximumSize = 32;
    public const int FaceCount = 6;

    public static RubikSceneVisualSummary BuildScene(
        int size,
        IReadOnlyList<int>? facelets,
        IReadOnlyList<RubikCubieVisualInput> cubies,
        bool surfaceOnly,
        int selectedAxis = -1,
        int selectedLayer = -1)
    {
        if (size is < MinimumSize or > MaximumSize)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }
        ArgumentNullException.ThrowIfNull(cubies);

        var expectedCubies = checked(size * size * size);
        if (cubies.Count != expectedCubies)
        {
            throw new ArgumentException($"Expected {expectedCubies} cubies, received {cubies.Count}.", nameof(cubies));
        }

        var faceletsSynchronized = facelets?.Count == FaceCount * size * size;
        var descriptors = new List<RubikCubieVisualDescriptor>(surfaceOnly
            ? expectedCubies - Math.Max(0, size - 2) * Math.Max(0, size - 2) * Math.Max(0, size - 2)
            : expectedCubies);
        var stickersRendered = 0;
        var invalidStickers = 0;
        var corners = 0;
        var edges = 0;
        var centers = 0;
        var internals = 0;
        var orientationAvailable = true;
        var fallbackActive = false;

        foreach (var cubie in cubies)
        {
            ValidateCoordinate(size, cubie.Coordinate);
            var isSurface = IsSurface(size, cubie.Coordinate);
            if (surfaceOnly && !isSurface)
            {
                continue;
            }

            var physicalStickerCount = CountMaskBits(cubie.StickerMask);
            switch (physicalStickerCount)
            {
                case 3: corners++; break;
                case 2: edges++; break;
                case 1: centers++; break;
                case 0: internals++; break;
                default: invalidStickers++; break;
            }

            IReadOnlyList<RubikStickerVisualDescriptor> stickers;
            if (faceletsSynchronized && cubie.Orientation.HasValue && physicalStickerCount <= 3)
            {
                stickers = BuildOrientedStickers(size, facelets!, cubie, ref invalidStickers);
            }
            else
            {
                orientationAvailable = false;
                fallbackActive = true;
                stickers = BuildShellStickers(size, facelets, cubie.Coordinate, ref invalidStickers);
            }

            stickersRendered += stickers.Count;
            descriptors.Add(new RubikCubieVisualDescriptor(
                cubie.Coordinate,
                cubie.CubieId,
                BodyVisible: true,
                IsSelected(cubie.Coordinate, selectedAxis, selectedLayer),
                cubie.Orientation.HasValue,
                physicalStickerCount,
                stickers));
        }

        var inner = Math.Max(0, size - 2);
        return new RubikSceneVisualSummary(
            size,
            descriptors.Count,
            descriptors.Count,
            stickersRendered,
            corners,
            edges,
            centers,
            internals,
            TotalCorners: 8,
            TotalEdges: 12 * inner,
            TotalCenters: 6 * inner * inner,
            TotalInternals: inner * inner * inner,
            invalidStickers,
            faceletsSynchronized,
            orientationAvailable,
            fallbackActive,
            descriptors);
    }

    private static IReadOnlyList<RubikStickerVisualDescriptor> BuildOrientedStickers(
        int size,
        IReadOnlyList<int> facelets,
        RubikCubieVisualInput cubie,
        ref int invalidStickers)
    {
        var result = new List<RubikStickerVisualDescriptor>(3);
        for (var localFace = 0; localFace < FaceCount; localFace++)
        {
            if ((cubie.StickerMask & (1 << localFace)) == 0)
            {
                continue;
            }

            var normal = NormalForLocalFace(localFace, cubie.Orientation!.Value);
            var worldFace = WorldFaceForNormal(normal);
            if (worldFace < 0 || !IsOnWorldFace(size, cubie.Coordinate, worldFace))
            {
                invalidStickers++;
                continue;
            }

            var colorId = ReadFacelet(facelets, size, worldFace, cubie.Coordinate);
            if (colorId is < 1 or > 6)
            {
                invalidStickers++;
                continue;
            }
            result.Add(new RubikStickerVisualDescriptor(localFace, worldFace, normal, colorId));
        }
        return result;
    }

    private static IReadOnlyList<RubikStickerVisualDescriptor> BuildShellStickers(
        int size,
        IReadOnlyList<int>? facelets,
        RubikCoordinate coordinate,
        ref int invalidStickers)
    {
        var result = new List<RubikStickerVisualDescriptor>(3);
        for (var worldFace = 0; worldFace < FaceCount; worldFace++)
        {
            if (!IsOnWorldFace(size, coordinate, worldFace))
            {
                continue;
            }
            if (facelets == null || facelets.Count != FaceCount * size * size)
            {
                invalidStickers++;
                continue;
            }
            var colorId = ReadFacelet(facelets, size, worldFace, coordinate);
            if (colorId is < 1 or > 6)
            {
                invalidStickers++;
                continue;
            }
            result.Add(new RubikStickerVisualDescriptor(-1, worldFace, NormalForWorldFace(worldFace), colorId));
        }
        return result;
    }

    public static RubikAxisVector NormalForLocalFace(int localFace, RubikCubieOrientation orientation) => localFace switch
    {
        (int)RubikFace.U => orientation.LocalY,
        (int)RubikFace.R => orientation.LocalX,
        (int)RubikFace.F => orientation.LocalZ,
        (int)RubikFace.D => orientation.LocalY.Negated(),
        (int)RubikFace.L => orientation.LocalX.Negated(),
        (int)RubikFace.B => orientation.LocalZ.Negated(),
        _ => default
    };

    public static int WorldFaceForNormal(RubikAxisVector normal) => normal switch
    {
        (0, 1, 0) => (int)RubikFace.U,
        (1, 0, 0) => (int)RubikFace.R,
        (0, 0, 1) => (int)RubikFace.F,
        (0, -1, 0) => (int)RubikFace.D,
        (-1, 0, 0) => (int)RubikFace.L,
        (0, 0, -1) => (int)RubikFace.B,
        _ => -1
    };

    public static RubikAxisVector NormalForWorldFace(int worldFace) => worldFace switch
    {
        (int)RubikFace.U => new(0, 1, 0),
        (int)RubikFace.R => new(1, 0, 0),
        (int)RubikFace.F => new(0, 0, 1),
        (int)RubikFace.D => new(0, -1, 0),
        (int)RubikFace.L => new(-1, 0, 0),
        (int)RubikFace.B => new(0, 0, -1),
        _ => default
    };

    public static int CountMaskBits(int mask)
    {
        var count = 0;
        for (var bit = 0; bit < FaceCount; bit++)
        {
            if ((mask & (1 << bit)) != 0)
            {
                count++;
            }
        }
        return count;
    }

    private static int ReadFacelet(
        IReadOnlyList<int> facelets,
        int size,
        int worldFace,
        RubikCoordinate coordinate)
    {
        var maximum = size - 1;
        var (row, column) = worldFace switch
        {
            (int)RubikFace.U => (coordinate.Z, coordinate.X),
            (int)RubikFace.R => (maximum - coordinate.Y, maximum - coordinate.Z),
            (int)RubikFace.F => (maximum - coordinate.Y, coordinate.X),
            (int)RubikFace.D => (maximum - coordinate.Z, coordinate.X),
            (int)RubikFace.L => (maximum - coordinate.Y, coordinate.Z),
            (int)RubikFace.B => (maximum - coordinate.Y, maximum - coordinate.X),
            _ => (-1, -1)
        };
        var index = worldFace * size * size + row * size + column;
        return index >= 0 && index < facelets.Count ? facelets[index] : 0;
    }

    private static bool IsSurface(int size, RubikCoordinate coordinate)
    {
        var maximum = size - 1;
        return coordinate.X == 0 || coordinate.X == maximum ||
               coordinate.Y == 0 || coordinate.Y == maximum ||
               coordinate.Z == 0 || coordinate.Z == maximum;
    }

    private static bool IsOnWorldFace(int size, RubikCoordinate coordinate, int face)
    {
        var maximum = size - 1;
        return face switch
        {
            (int)RubikFace.U => coordinate.Y == maximum,
            (int)RubikFace.R => coordinate.X == maximum,
            (int)RubikFace.F => coordinate.Z == maximum,
            (int)RubikFace.D => coordinate.Y == 0,
            (int)RubikFace.L => coordinate.X == 0,
            (int)RubikFace.B => coordinate.Z == 0,
            _ => false
        };
    }

    private static bool IsSelected(RubikCoordinate coordinate, int axis, int layer) => axis switch
    {
        0 => coordinate.Z == layer,
        1 => coordinate.Y == layer,
        2 => coordinate.X == layer,
        _ => false
    };

    private static void ValidateCoordinate(int size, RubikCoordinate coordinate)
    {
        if (coordinate.X < 0 || coordinate.X >= size ||
            coordinate.Y < 0 || coordinate.Y >= size ||
            coordinate.Z < 0 || coordinate.Z >= size)
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate));
        }
    }
}
