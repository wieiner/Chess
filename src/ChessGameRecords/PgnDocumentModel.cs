using System.Collections.ObjectModel;

namespace ChessGameRecords;

public enum PgnResult
{
    Ongoing = 0,
    WhiteWin,
    BlackWin,
    Draw
}

public enum PgnCommentStyle
{
    Brace = 0,
    Semicolon
}

public static class PgnResultExtensions
{
    public static string ToMarker(this PgnResult result) => result switch
    {
        PgnResult.WhiteWin => "1-0",
        PgnResult.BlackWin => "0-1",
        PgnResult.Draw => "1/2-1/2",
        _ => "*"
    };

    public static bool TryParseMarker(string marker, out PgnResult result)
    {
        result = marker switch
        {
            "1-0" => PgnResult.WhiteWin,
            "0-1" => PgnResult.BlackWin,
            "1/2-1/2" => PgnResult.Draw,
            "*" => PgnResult.Ongoing,
            _ => (PgnResult)(-1)
        };
        return (int)result >= 0;
    }
}

public static class PgnSevenTagRoster
{
    public static readonly IReadOnlyList<string> Names = Array.AsReadOnly(
        new[] { "Event", "Site", "Date", "Round", "White", "Black", "Result" });
}

public sealed record PgnTagPair
{
    public PgnTagPair(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);
        if (!char.IsLetter(name[0]) || name.Any(character => !char.IsLetterOrDigit(character) && character != '_'))
        {
            throw new ArgumentException("A PGN tag name must start with a letter and contain only letters, digits, or underscore.", nameof(name));
        }

        Name = name;
        Value = value;
    }

    public string Name { get; }
    public string Value { get; }
}

public sealed record PgnComment
{
    public PgnComment(string text, PgnCommentStyle style = PgnCommentStyle.Brace)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
        Style = style;
    }

    public string Text { get; }
    public PgnCommentStyle Style { get; }
}

public readonly record struct PgnNag
{
    public PgnNag(int value)
    {
        if (value is < 0 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "A PGN NAG must be in the range 0..255.");
        }
        Value = value;
    }

    public int Value { get; }
    public override string ToString() => $"${Value}";
}

public sealed class PgnMoveNode
{
    public PgnMoveNode(
        int plyIndex,
        string san,
        IEnumerable<PgnComment>? leadingComments = null,
        IEnumerable<PgnNag>? nags = null,
        IEnumerable<PgnComment>? trailingComments = null,
        IEnumerable<PgnVariation>? variations = null,
        int? fullmoveNumber = null,
        bool? isBlackMove = null)
    {
        if (plyIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(plyIndex));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(san);

        PlyIndex = plyIndex;
        FullmoveNumber = fullmoveNumber ?? (plyIndex / 2) + 1;
        if (FullmoveNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(fullmoveNumber));
        }
        IsBlackMove = isBlackMove ?? (plyIndex & 1) != 0;
        San = san;
        LeadingComments = Freeze(leadingComments);
        Nags = Freeze(nags);
        TrailingComments = Freeze(trailingComments);
        Variations = Freeze(variations);
    }

    public int PlyIndex { get; }
    public int FullmoveNumber { get; }
    public bool IsBlackMove { get; }
    public string San { get; }
    public IReadOnlyList<PgnComment> LeadingComments { get; }
    public IReadOnlyList<PgnNag> Nags { get; }
    public IReadOnlyList<PgnComment> TrailingComments { get; }
    public IReadOnlyList<PgnVariation> Variations { get; }

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values) =>
        Array.AsReadOnly(values?.ToArray() ?? Array.Empty<T>());
}

public sealed class PgnVariation
{
    public PgnVariation(IEnumerable<PgnMoveNode>? moves = null)
    {
        Moves = Array.AsReadOnly(moves?.ToArray() ?? Array.Empty<PgnMoveNode>());
    }

    public IReadOnlyList<PgnMoveNode> Moves { get; }
}

public sealed class PgnGame
{
    public PgnGame(IEnumerable<PgnTagPair> tags, PgnVariation mainLine, PgnResult result)
    {
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(mainLine);
        Tags = Array.AsReadOnly(tags.ToArray());
        MainLine = mainLine;
        Result = result;
    }

    public IReadOnlyList<PgnTagPair> Tags { get; }
    public PgnVariation MainLine { get; }
    public PgnResult Result { get; }

    public string? FindTag(string name) =>
        Tags.FirstOrDefault(tag => string.Equals(tag.Name, name, StringComparison.Ordinal))?.Value;

    public IReadOnlyList<string> FindAllTags(string name) => new ReadOnlyCollection<string>(
        Tags.Where(tag => string.Equals(tag.Name, name, StringComparison.Ordinal)).Select(tag => tag.Value).ToArray());
}

public sealed class PgnDocument
{
    public PgnDocument(IEnumerable<PgnGame>? games = null)
    {
        Games = Array.AsReadOnly(games?.ToArray() ?? Array.Empty<PgnGame>());
    }

    public IReadOnlyList<PgnGame> Games { get; }
}
