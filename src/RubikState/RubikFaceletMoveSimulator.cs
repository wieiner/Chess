namespace RubikState;

public static class RubikFaceletMoveSimulator
{
    private readonly record struct Vector(int X, int Y, int Z);
    private readonly record struct Sticker(Vector Position, Vector Normal);

    public static int[] Apply(int size, IReadOnlyList<int> facelets, RubikMove move)
    {
        if (size is < RubikStateDocument.MinimumSize or > RubikStateDocument.MaximumSize)
            throw new ArgumentOutOfRangeException(nameof(size));
        if (facelets.Count != 6 * size * size)
            throw new ArgumentException("Facelet count does not match cube size.", nameof(facelets));
        if (!move.IsValidFor(size))
            throw new ArgumentException("Move axis, layer, or quarter turns are invalid.", nameof(move));

        var turns = RubikMove.NormalizeQuarterTurns(move.QuarterTurns);
        var result = facelets.ToArray();
        for (var face = 0; face < 6; face++)
        for (var row = 0; row < size; row++)
        for (var column = 0; column < size; column++)
        {
            var sticker = ToSticker(size, face, row, column);
            var coordinate = move.Axis == 0 ? sticker.Position.Z : move.Axis == 1 ? sticker.Position.Y : sticker.Position.X;
            if (coordinate != move.Layer) continue;
            var rotated = new Sticker(
                RotatePosition(size, move.Axis, move.Layer, turns, sticker.Position),
                RotateNormal(move.Axis, turns, sticker.Normal));
            var (targetFace, targetRow, targetColumn) = ToFacelet(size, rotated);
            result[Index(size, targetFace, targetRow, targetColumn)] = facelets[Index(size, face, row, column)];
        }
        return result;
    }

    private static Sticker ToSticker(int size, int face, int row, int column)
    {
        var maximum = size - 1;
        return face switch
        {
            0 => new(new(column, maximum, row), new(0, 1, 0)),
            1 => new(new(maximum, maximum - row, maximum - column), new(1, 0, 0)),
            2 => new(new(column, maximum - row, maximum), new(0, 0, 1)),
            3 => new(new(column, 0, maximum - row), new(0, -1, 0)),
            4 => new(new(0, maximum - row, column), new(-1, 0, 0)),
            5 => new(new(maximum - column, maximum - row, 0), new(0, 0, -1)),
            _ => throw new ArgumentOutOfRangeException(nameof(face))
        };
    }

    private static Vector RotatePosition(int size, int axis, int layer, int turns, Vector value)
    {
        var (u, v) = axis switch
        {
            0 => (value.X, value.Y),
            1 => (value.X, value.Z),
            _ => (value.Y, value.Z)
        };
        for (var turn = 0; turn < turns; turn++) (u, v) = (size - 1 - v, u);
        return axis switch
        {
            0 => new(u, v, layer),
            1 => new(u, layer, v),
            _ => new(layer, u, v)
        };
    }

    private static Vector RotateNormal(int axis, int turns, Vector value)
    {
        for (var turn = 0; turn < turns; turn++)
            value = axis switch
            {
                0 => new(-value.Y, value.X, value.Z),
                1 => new(-value.Z, value.Y, value.X),
                _ => new(value.X, -value.Z, value.Y)
            };
        return value;
    }

    private static (int Face, int Row, int Column) ToFacelet(int size, Sticker sticker)
    {
        var maximum = size - 1;
        if (sticker.Normal.Y == 1) return (0, sticker.Position.Z, sticker.Position.X);
        if (sticker.Normal.X == 1) return (1, maximum - sticker.Position.Y, maximum - sticker.Position.Z);
        if (sticker.Normal.Z == 1) return (2, maximum - sticker.Position.Y, sticker.Position.X);
        if (sticker.Normal.Y == -1) return (3, maximum - sticker.Position.Z, sticker.Position.X);
        if (sticker.Normal.X == -1) return (4, maximum - sticker.Position.Y, sticker.Position.Z);
        if (sticker.Normal.Z == -1) return (5, maximum - sticker.Position.Y, maximum - sticker.Position.X);
        throw new InvalidOperationException("Rotated sticker has no canonical surface normal.");
    }

    private static int Index(int size, int face, int row, int column) => face * size * size + row * size + column;
}
