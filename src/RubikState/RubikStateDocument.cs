using System.Text.Json;

namespace RubikState;

public sealed record RubikColorScheme(
    string U = "white",
    string R = "red",
    string F = "green",
    string D = "yellow",
    string L = "orange",
    string B = "blue")
{
    public IEnumerable<KeyValuePair<string, string>> InFaceOrder()
    {
        yield return new("U", U);
        yield return new("R", R);
        yield return new("F", F);
        yield return new("D", D);
        yield return new("L", L);
        yield return new("B", B);
    }
}

public sealed record RubikFaces(int[] U, int[] R, int[] F, int[] D, int[] L, int[] B)
{
    public IEnumerable<KeyValuePair<string, int[]>> InFaceOrder()
    {
        yield return new("U", U);
        yield return new("R", R);
        yield return new("F", F);
        yield return new("D", D);
        yield return new("L", L);
        yield return new("B", B);
    }

    public int[] Flatten() => InFaceOrder().SelectMany(face => face.Value).ToArray();
}

public sealed record RubikStateDocument(
    string Format,
    int Version,
    int Size,
    IReadOnlyList<string> FaceOrder,
    RubikColorScheme ColorScheme,
    RubikFaces Faces,
    string StateHash,
    string Source,
    string CreatedUtc,
    JsonElement? Metadata = null)
{
    public const string CurrentFormat = "rubik.state";
    public const int CurrentVersion = 1;
    public const int MinimumSize = 2;
    public const int MaximumSize = 32;

    public static readonly IReadOnlyList<string> CanonicalFaceOrder =
        Array.AsReadOnly(new[] { "U", "R", "F", "D", "L", "B" });

    public static RubikStateDocument Create(int size, int[] facelets, string source = "generated", JsonElement? metadata = null)
    {
        var faceArea = checked(size * size);
        if (facelets.Length != checked(6 * faceArea))
        {
            throw new ArgumentException($"Expected {6 * faceArea} facelets for N={size}.", nameof(facelets));
        }

        int[] Face(int index) => facelets.AsSpan(index * faceArea, faceArea).ToArray();
        return new RubikStateDocument(
            CurrentFormat,
            CurrentVersion,
            size,
            CanonicalFaceOrder,
            new RubikColorScheme(),
            new RubikFaces(Face(0), Face(1), Face(2), Face(3), Face(4), Face(5)),
            string.Empty,
            source,
            DateTimeOffset.UtcNow.ToString("O"),
            metadata?.Clone());
    }
}

public sealed record RubikStateLoadPlan(RubikStateDocument Document, int[] Facelets, string StateHash);
