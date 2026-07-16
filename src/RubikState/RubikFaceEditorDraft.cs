using System.Text.Json;

namespace RubikState;

public sealed record RubikFaceEditorSummary(
    int Size,
    IReadOnlyDictionary<int, int> ColorCounts,
    int EmptyCells,
    bool BasicCountsValid,
    string OrientationGuidance);

public sealed class RubikFaceEditorDraft
{
    private const int HistoryLimit = 200;
    private static readonly string[] FaceNames = ["U", "R", "F", "D", "L", "B"];
    private readonly int[][] _faces;
    private readonly Stack<int[]> _undo = new();
    private readonly Stack<int[]> _redo = new();

    public RubikFaceEditorDraft(int size, IReadOnlyList<int>? facelets = null)
    {
        if (size is < RubikStateDocument.MinimumSize or > RubikStateDocument.MaximumSize)
            throw new ArgumentOutOfRangeException(nameof(size));
        Size = size;
        var area = checked(size * size);
        _faces = Enumerable.Range(0, 6).Select(_ => new int[area]).ToArray();
        if (facelets is not null)
        {
            if (facelets.Count != 6 * area) throw new ArgumentException($"Expected {6 * area} facelets.", nameof(facelets));
            for (var face = 0; face < 6; face++)
                for (var index = 0; index < area; index++)
                    _faces[face][index] = ValidateColor(facelets[face * area + index], allowEmpty: true);
        }
    }

    public int Size { get; }
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public IReadOnlyList<int> GetFace(int face) => _faces[ValidateFace(face)];
    public int GetCell(int face, int row, int column) => _faces[ValidateFace(face)][Index(row, column)];

    public void Paint(int face, int row, int column, int color)
    {
        face = ValidateFace(face);
        color = ValidateColor(color, allowEmpty: true);
        var index = Index(row, column);
        if (_faces[face][index] == color) return;
        RecordUndo();
        _faces[face][index] = color;
    }

    public void FillFace(int face, int color)
    {
        face = ValidateFace(face);
        color = ValidateColor(color, allowEmpty: true);
        if (_faces[face].All(value => value == color)) return;
        RecordUndo();
        Array.Fill(_faces[face], color);
    }

    public void ClearFace(int face) => FillFace(face, 0);

    public void ClearAll()
    {
        if (_faces.All(face => face.All(value => value == 0))) return;
        RecordUndo();
        foreach (var face in _faces) Array.Fill(face, 0);
    }

    public void RotateFaceClockwise(int face)
    {
        face = ValidateFace(face);
        RecordUndo();
        var source = _faces[face].ToArray();
        for (var row = 0; row < Size; row++)
        for (var column = 0; column < Size; column++)
            _faces[face][column * Size + (Size - 1 - row)] = source[row * Size + column];
    }

    public bool Undo()
    {
        if (_undo.Count == 0) return false;
        _redo.Push(Flatten());
        Restore(_undo.Pop());
        return true;
    }

    public bool Redo()
    {
        if (_redo.Count == 0) return false;
        _undo.Push(Flatten());
        Restore(_redo.Pop());
        return true;
    }

    public string CopyFaceText(int face) => string.Join(' ', _faces[ValidateFace(face)]);

    public void PasteFaceText(int face, string text)
    {
        face = ValidateFace(face);
        var values = text.Split([' ', '\t', '\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(token => int.TryParse(token, out var value) ? ValidateColor(value, allowEmpty: true)
                : throw new FormatException($"Invalid draft color token '{token}'."))
            .ToArray();
        if (values.Length != Size * Size) throw new FormatException($"Expected {Size * Size} values, found {values.Length}.");
        if (_faces[face].SequenceEqual(values)) return;
        RecordUndo();
        values.CopyTo(_faces[face], 0);
    }

    public RubikFaceEditorSummary Summarize()
    {
        var counts = Enumerable.Range(0, 7).ToDictionary(color => color, _ => 0);
        foreach (var value in Flatten()) counts[value]++;
        var expected = Size * Size;
        var valid = counts[0] == 0 && Enumerable.Range(1, 6).All(color => counts[color] == expected);
        string guidance;
        if (Size % 2 == 0)
        {
            guidance = "Even N has no single fixed center. Confirm the explicit U/R/F/D/L/B orientation; it will not be inferred or changed.";
        }
        else
        {
            var middle = Size / 2;
            var centers = Enumerable.Range(0, 6).Select(face => GetCell(face, middle, middle)).ToArray();
            guidance = centers.All(color => color is >= 1 and <= 6) && centers.Distinct().Count() == 6
                ? $"Odd-N center suggestion: {string.Join(", ", FaceNames.Select((face, index) => $"{face}={centers[index]}"))}. Confirm before apply."
                : "Odd N can use six distinct center stickers as an orientation suggestion after they are filled.";
        }
        return new RubikFaceEditorSummary(Size, counts, counts[0], valid, guidance);
    }

    public RubikStateDocument ToStateDocument(string source = "physical-editor")
    {
        var summary = Summarize();
        if (!summary.BasicCountsValid) throw new InvalidOperationException("Draft must have exactly N*N stickers of each color and no empty cells.");
        return RubikStateDocument.Create(Size, Flatten(), source);
    }

    public int[] Flatten() => _faces.SelectMany(face => face).ToArray();

    private void RecordUndo()
    {
        _undo.Push(Flatten());
        if (_undo.Count > HistoryLimit)
        {
            var kept = _undo.Take(HistoryLimit).Reverse().ToArray();
            _undo.Clear();
            foreach (var item in kept) _undo.Push(item);
        }
        _redo.Clear();
    }

    private void Restore(int[] values)
    {
        var area = Size * Size;
        for (var face = 0; face < 6; face++) Array.Copy(values, face * area, _faces[face], 0, area);
    }

    private int Index(int row, int column)
    {
        if (row < 0 || row >= Size) throw new ArgumentOutOfRangeException(nameof(row));
        if (column < 0 || column >= Size) throw new ArgumentOutOfRangeException(nameof(column));
        return row * Size + column;
    }

    private static int ValidateFace(int face) => face is >= 0 and < 6 ? face : throw new ArgumentOutOfRangeException(nameof(face));
    private static int ValidateColor(int color, bool allowEmpty) => color is >= 1 and <= 6 || allowEmpty && color == 0
        ? color : throw new ArgumentOutOfRangeException(nameof(color));
}

public static class RubikFaceEditorDraftSerializer
{
    public const int MaximumBytes = 1024 * 1024;

    public static string Serialize(RubikFaceEditorDraft draft) => JsonSerializer.Serialize(new
    {
        format = "rubik.editor-draft",
        version = 1,
        size = draft.Size,
        faceOrder = RubikStateDocument.CanonicalFaceOrder,
        faces = new Dictionary<string, IReadOnlyList<int>>
        {
            ["U"] = draft.GetFace(0), ["R"] = draft.GetFace(1), ["F"] = draft.GetFace(2),
            ["D"] = draft.GetFace(3), ["L"] = draft.GetFace(4), ["B"] = draft.GetFace(5)
        }
    }, new JsonSerializerOptions { WriteIndented = true });

    public static RubikFaceEditorDraft Parse(string json)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaximumBytes) throw new InvalidDataException("Draft exceeds the one-megabyte limit.");
        using var parsed = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
        var root = parsed.RootElement;
        if (root.ValueKind != JsonValueKind.Object || root.GetProperty("format").GetString() != "rubik.editor-draft" || root.GetProperty("version").GetInt32() != 1)
            throw new InvalidDataException("Unsupported Rubik editor draft.");
        var size = root.GetProperty("size").GetInt32();
        var values = new List<int>();
        var faces = root.GetProperty("faces");
        foreach (var name in new[] { "U", "R", "F", "D", "L", "B" })
            values.AddRange(faces.GetProperty(name).EnumerateArray().Select(value => value.GetInt32()));
        return new RubikFaceEditorDraft(size, values);
    }

    public static void SaveAtomic(string path, RubikFaceEditorDraft draft)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("Draft path has no directory.");
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
        var temp = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(Serialize(draft));
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(true);
            }
            _ = Parse(File.ReadAllText(temp));
            if (File.Exists(fullPath)) File.Replace(temp, fullPath, null, true); else File.Move(temp, fullPath);
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
        }
    }

    public static RubikFaceEditorDraft Load(string path)
    {
        var info = new FileInfo(path);
        if (info.Length > MaximumBytes) throw new InvalidDataException("Draft exceeds the one-megabyte limit.");
        return Parse(File.ReadAllText(path));
    }
}
