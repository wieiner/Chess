namespace RubikState;

public readonly record struct RubikSurfaceCoordinate(int X, int Y, int Z);
public sealed record RubikObservedSticker(string Face, int ColorId);
public sealed record RubikCornerObservation(RubikSurfaceCoordinate Coordinate, IReadOnlyList<RubikObservedSticker> Stickers,
    string ColorSignature, int Orientation);
public sealed record RubikWingObservation(RubikSurfaceCoordinate Coordinate, IReadOnlyList<RubikObservedSticker> Stickers,
    string ColorSignature, int OrbitIndex, int WingIndex, bool Flipped);
public sealed record RubikCenterObservation(RubikSurfaceCoordinate Coordinate, RubikObservedSticker Sticker,
    int OrbitA, int OrbitB);

public sealed record RubikCubieDecompositionResult(
    int Size,
    bool Complete,
    IReadOnlyList<RubikCornerObservation> Corners,
    IReadOnlyList<RubikWingObservation> Wings,
    IReadOnlyList<RubikCenterObservation> Centers,
    IReadOnlyList<RubikValidationIssue> Issues);

public static class RubikCubieDecomposer
{
    private static readonly string[] FaceNames = ["U", "R", "F", "D", "L", "B"];

    public static RubikCubieDecompositionResult Decompose(RubikStateDocument document)
    {
        var basic = RubikStateValidator.Validate(document);
        if (!basic.IsValid)
        {
            var issues = basic.Issues.Select(issue => new RubikValidationIssue(RubikValidationSeverity.Error,
                issue.Code == RubikStateErrorCode.HashMismatch ? RubikValidationCodes.HashMismatch : RubikValidationCodes.WrongFaceSize,
                null, null, null, null, issue.Message, "Fix basic state validation before cubie decomposition.")).ToArray();
            return new(document.Size, false, [], [], [], issues);
        }
        return Decompose(document.Size, document.Faces.Flatten());
    }

    public static RubikCubieDecompositionResult Decompose(int size, IReadOnlyList<int> facelets)
    {
        if (size is < 2 or > 32) throw new ArgumentOutOfRangeException(nameof(size));
        if (facelets.Count != 6 * size * size) throw new ArgumentException("Facelet count does not match N.", nameof(facelets));
        var corners = new List<RubikCornerObservation>(8);
        var wings = new List<RubikWingObservation>(12 * Math.Max(0, size - 2));
        var centers = new List<RubikCenterObservation>(6 * Math.Max(0, size - 2) * Math.Max(0, size - 2));
        var maximum = size - 1;

        for (var z = 0; z < size; z++)
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var coordinate = new RubikSurfaceCoordinate(x, y, z);
            var exposedFaces = FacesAt(coordinate, maximum);
            if (exposedFaces.Count == 0) continue;
            var stickers = exposedFaces.Select(face => new RubikObservedSticker(FaceNames[face], ReadFacelet(facelets, size, face, coordinate))).ToArray();
            if (stickers.Length == 3)
            {
                var ud = stickers.FirstOrDefault(sticker => sticker.ColorId is 1 or 4);
                var orientation = ud is null ? -1 : ud.Face is "U" or "D" ? 0 :
                    ud.ColorId == 1 ? (ud.Face is "R" or "L" ? 1 : 2) :
                    (ud.Face is "R" or "L" ? 2 : 1);
                corners.Add(new(coordinate, stickers, Signature(stickers.Select(sticker => sticker.ColorId)), orientation));
            }
            else if (stickers.Length == 2)
            {
                var freeAxisValue = x > 0 && x != maximum ? x :
                    y > 0 && y != maximum ? y : z;
                var orbit = Math.Min(freeAxisValue, maximum - freeAxisValue);
                var primary = stickers.FirstOrDefault(sticker => sticker.ColorId is 1 or 4) ??
                              stickers.FirstOrDefault(sticker => sticker.ColorId is 3 or 6);
                var flipped = primary is not null && (primary.ColorId is 1 or 4
                    ? primary.Face is not ("U" or "D")
                    : primary.Face is not ("F" or "B"));
                wings.Add(new(coordinate, stickers, Signature(stickers.Select(sticker => sticker.ColorId)), orbit, freeAxisValue, flipped));
            }
            else if (stickers.Length == 1)
            {
                var (row, column) = FaceRowColumn(size, exposedFaces[0], coordinate);
                var distances = new[] { row, maximum - row, column, maximum - column }.OrderBy(value => value).ToArray();
                centers.Add(new(coordinate, stickers[0], distances[0], distances[1]));
            }
        }

        var expected = BuildSolvedInventory(size);
        var issues = new List<RubikValidationIssue>();
        CompareInventory(corners.Select(corner => corner.ColorSignature), expected.Corners, "corner", issues);
        CompareInventory(wings.Select(wing => $"{wing.ColorSignature}@{wing.OrbitIndex}"), expected.Wings, "wing", issues);
        CompareInventory(centers.Select(center => $"{center.Sticker.ColorId}@{center.OrbitA},{center.OrbitB}"), expected.Centers, "center", issues);

        foreach (var corner in corners.Where(corner => corner.ColorSignature.Split('-').Distinct().Count() != 3 || corner.Orientation < 0))
            issues.Add(Issue(RubikValidationCodes.DuplicateSticker, "corner", corner.Coordinate,
                $"Impossible corner colors {corner.ColorSignature}.", "Check the three stickers meeting at this corner."));

        return new(size, issues.Count == 0, corners, wings, centers, issues);
    }

    private static (string[] Corners, string[] Wings, string[] Centers) BuildSolvedInventory(int size)
    {
        var facelets = Enumerable.Range(1, 6).SelectMany(color => Enumerable.Repeat(color, size * size)).ToArray();
        var maximum = size - 1;
        var corners = new List<string>();
        var wings = new List<string>();
        var centers = new List<string>();
        for (var z = 0; z < size; z++)
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var coordinate = new RubikSurfaceCoordinate(x, y, z);
            var faces = FacesAt(coordinate, maximum);
            if (faces.Count == 3) corners.Add(Signature(faces.Select(face => ReadFacelet(facelets, size, face, coordinate))));
            else if (faces.Count == 2)
            {
                var t = x > 0 && x != maximum ? x : y > 0 && y != maximum ? y : z;
                wings.Add($"{Signature(faces.Select(face => ReadFacelet(facelets, size, face, coordinate)))}@{Math.Min(t, maximum - t)}");
            }
            else if (faces.Count == 1)
            {
                var (row, column) = FaceRowColumn(size, faces[0], coordinate);
                var distances = new[] { row, maximum - row, column, maximum - column }.OrderBy(value => value).ToArray();
                centers.Add($"{ReadFacelet(facelets, size, faces[0], coordinate)}@{distances[0]},{distances[1]}");
            }
        }
        return (corners.ToArray(), wings.ToArray(), centers.ToArray());
    }

    private static void CompareInventory(IEnumerable<string> actual, IEnumerable<string> expected, string cubieClass,
        ICollection<RubikValidationIssue> issues)
    {
        var actualCounts = actual.GroupBy(value => value).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var expectedCounts = expected.GroupBy(value => value).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        foreach (var item in actualCounts.Keys.Union(expectedCounts.Keys, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal))
        {
            var actualCount = actualCounts.GetValueOrDefault(item);
            var expectedCount = expectedCounts.GetValueOrDefault(item);
            if (actualCount > expectedCount)
                issues.Add(new(RubikValidationSeverity.Error, RubikValidationCodes.DuplicateSticker, null, null, null,
                    cubieClass, $"Inventory has {actualCount} '{item}' {cubieClass}(s); expected {expectedCount}.", "Correct duplicated or impossible sticker combinations."));
            if (actualCount < expectedCount)
                issues.Add(new(RubikValidationSeverity.Error, RubikValidationCodes.MissingSticker, null, null, null,
                    cubieClass, $"Inventory has {actualCount} '{item}' {cubieClass}(s); expected {expectedCount}.", "Restore the missing physical piece/orbit combination."));
        }
    }

    private static RubikValidationIssue Issue(string code, string cubieClass, RubikSurfaceCoordinate coordinate,
        string message, string action) => new(RubikValidationSeverity.Error, code, null, coordinate.Y, coordinate.X,
            cubieClass, message, action);

    private static List<int> FacesAt(RubikSurfaceCoordinate coordinate, int maximum)
    {
        var faces = new List<int>(3);
        if (coordinate.Y == maximum) faces.Add(0);
        if (coordinate.X == maximum) faces.Add(1);
        if (coordinate.Z == maximum) faces.Add(2);
        if (coordinate.Y == 0) faces.Add(3);
        if (coordinate.X == 0) faces.Add(4);
        if (coordinate.Z == 0) faces.Add(5);
        return faces;
    }

    private static string Signature(IEnumerable<int> colors) => string.Join('-', colors.OrderBy(color => color));

    private static int ReadFacelet(IReadOnlyList<int> facelets, int size, int face, RubikSurfaceCoordinate coordinate)
    {
        var (row, column) = FaceRowColumn(size, face, coordinate);
        return facelets[face * size * size + row * size + column];
    }

    private static (int Row, int Column) FaceRowColumn(int size, int face, RubikSurfaceCoordinate coordinate)
    {
        var maximum = size - 1;
        return face switch
        {
            0 => (coordinate.Z, coordinate.X),
            1 => (maximum - coordinate.Y, maximum - coordinate.Z),
            2 => (maximum - coordinate.Y, coordinate.X),
            3 => (maximum - coordinate.Z, coordinate.X),
            4 => (maximum - coordinate.Y, coordinate.Z),
            5 => (maximum - coordinate.Y, maximum - coordinate.X),
            _ => throw new ArgumentOutOfRangeException(nameof(face))
        };
    }
}
