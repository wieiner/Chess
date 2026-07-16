using System.Text;
using System.Text.Json;

namespace RubikState;

public sealed record RubikMoveSequenceDocument(
    string Format,
    int Version,
    int Size,
    string InputHash,
    string SolverId,
    bool Complete,
    bool Verified,
    string? FinalHash,
    IReadOnlyList<RubikMove> Moves);

public static class RubikMoveSequenceFile
{
    public const string Format = "rubik.move-sequence";
    public const int Version = 1;
    public const int MaximumBytes = 4 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static RubikMoveSequenceDocument Create(int size, string inputHash, string solverId,
        IReadOnlyList<RubikMove> moves, bool complete, bool verified, string? finalHash = null) =>
        Validate(new(Format, Version, size, inputHash, solverId, complete, verified, finalHash, moves.ToArray()));

    public static string Serialize(RubikMoveSequenceDocument document) => JsonSerializer.Serialize(Validate(document), JsonOptions);

    public static RubikMoveSequenceDocument Parse(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > MaximumBytes) throw new InvalidDataException("Rubik move file exceeds the size limit.");
        try
        {
            return Validate(JsonSerializer.Deserialize<RubikMoveSequenceDocument>(json) ??
                throw new InvalidDataException("Rubik move file is empty."));
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Rubik move file JSON is malformed.", exception);
        }
    }

    public static void SaveAtomic(string path, RubikMoveSequenceDocument document) =>
        AtomicTextFile.Write(path, Serialize(document));

    public static RubikMoveSequenceDocument Load(string path) => Parse(AtomicTextFile.ReadBounded(path, MaximumBytes));

    private static RubikMoveSequenceDocument Validate(RubikMoveSequenceDocument document)
    {
        if (document.Format != Format || document.Version != Version) throw new InvalidDataException("Unsupported Rubik move format/version.");
        if (document.Size is < RubikStateDocument.MinimumSize or > RubikStateDocument.MaximumSize)
            throw new InvalidDataException("Rubik move file size is outside supported bounds.");
        ValidateHash(document.InputHash, "input");
        if (string.IsNullOrWhiteSpace(document.SolverId) || document.SolverId.Length > 128)
            throw new InvalidDataException("Rubik move file solver id is invalid.");
        if (document.Moves.Count > 1_000_000 || document.Moves.Any(move => !move.IsValidFor(document.Size)))
            throw new InvalidDataException("Rubik move file contains too many or invalid moves.");
        if (document.Verified && (!document.Complete || document.FinalHash is null))
            throw new InvalidDataException("A verified move file must be complete and contain a final hash.");
        if (document.FinalHash is not null) ValidateHash(document.FinalHash, "final");
        return document;
    }

    private static void ValidateHash(string hash, string name)
    {
        if (hash.Length != 64 || hash.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidDataException($"Rubik move file {name} hash is invalid.");
    }
}

internal static class AtomicTextFile
{
    public static void Write(string path, string text)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)!;
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                       64 * 1024, FileOptions.SequentialScan))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(text);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }
            if (File.Exists(fullPath)) File.Replace(temporary, fullPath, null, ignoreMetadataErrors: true);
            else File.Move(temporary, fullPath);
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
        }
    }

    public static string ReadBounded(string path, int maximumBytes)
    {
        var fullPath = Path.GetFullPath(path);
        using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
            FileOptions.SequentialScan);
        if (stream.Length > maximumBytes) throw new InvalidDataException("Text artifact exceeds the size limit.");
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
