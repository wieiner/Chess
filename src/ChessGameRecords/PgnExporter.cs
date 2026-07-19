using System.Text;

namespace ChessGameRecords;

public sealed record PgnExportOptions(
    int LineWidth = 80,
    bool IncludeComments = true,
    bool IncludeNags = true,
    bool IncludeVariations = true);

public sealed record PgnExportResult(bool Success, string Text, string Error)
{
    internal static PgnExportResult Failed(string error) => new(false, string.Empty, error);
}

public static class PgnGameFactory
{
    public static PgnGame FromGameRecord(ChessGameRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var result = record.Result switch
        {
            ChessGameResult.WhiteWin => PgnResult.WhiteWin,
            ChessGameResult.BlackWin => PgnResult.BlackWin,
            ChessGameResult.Draw => PgnResult.Draw,
            _ => PgnResult.Ongoing
        };
        var tags = new List<PgnTagPair>
        {
            new("Event", record.Headers.Event),
            new("Site", record.Headers.Site),
            new("Date", record.Headers.Date),
            new("Round", record.Headers.Round),
            new("White", record.Headers.White),
            new("Black", record.Headers.Black),
            new("Result", result.ToMarker())
        };

        foreach (var tag in record.Headers.AdditionalTags.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!PgnSevenTagRoster.Names.Contains(tag.Key, StringComparer.Ordinal) && tag.Key is not ("SetUp" or "FEN"))
            {
                tags.Add(new PgnTagPair(tag.Key, tag.Value));
            }
        }
        if (!string.Equals(record.InitialPosition.Fen, ChessGameHistory.StandardInitialFen, StringComparison.Ordinal))
        {
            tags.Add(new PgnTagPair("SetUp", "1"));
            tags.Add(new PgnTagPair("FEN", record.InitialPosition.Fen));
        }

        var moves = record.Moves.Select(move => new PgnMoveNode(
            move.PlyIndex,
            move.San,
            trailingComments: string.IsNullOrWhiteSpace(move.Comment) ? null : new[] { new PgnComment(move.Comment) },
            fullmoveNumber: move.FullmoveNumber,
            isBlackMove: move.Side < 0));
        return new PgnGame(tags, new PgnVariation(moves), result);
    }
}

public static class PgnExporter
{
    public static PgnExportResult Export(PgnGame game, PgnExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        options ??= new PgnExportOptions();
        if (options.LineWidth is < 40 or > 240)
        {
            return PgnExportResult.Failed("PGN line width must be in the range 40..240.");
        }
        if (!TryValidate(game, out var error))
        {
            return PgnExportResult.Failed(error);
        }

        var builder = new StringBuilder();
        foreach (var name in PgnSevenTagRoster.Names)
        {
            var tag = game.Tags.Single(item => item.Name == name);
            builder.Append('[').Append(tag.Name).Append(" \"").Append(EscapeTagValue(tag.Value)).Append("\"]\n");
        }
        foreach (var tag in game.Tags.Where(tag => !PgnSevenTagRoster.Names.Contains(tag.Name, StringComparer.Ordinal)))
        {
            builder.Append('[').Append(tag.Name).Append(" \"").Append(EscapeTagValue(tag.Value)).Append("\"]\n");
        }
        builder.Append('\n');

        var tokens = new List<string>();
        AppendVariation(tokens, game.MainLine, options, true);
        tokens.Add(game.Result.ToMarker());
        AppendWrapped(builder, tokens, options.LineWidth);
        return new PgnExportResult(true, builder.ToString(), string.Empty);
    }

    public static PgnExportResult Export(ChessGameRecord record, PgnExportOptions? options = null) =>
        Export(PgnGameFactory.FromGameRecord(record), options);

    public static void WriteUtf8(string path, PgnGame game, PgnExportOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var result = Export(game, options);
        if (!result.Success)
        {
            throw new InvalidOperationException(result.Error);
        }
        File.WriteAllText(path, result.Text, new UTF8Encoding(false));
    }

    private static bool TryValidate(PgnGame game, out string error)
    {
        var duplicate = game.Tags.GroupBy(tag => tag.Name, StringComparer.Ordinal).FirstOrDefault(group => group.Count() != 1);
        if (duplicate is not null)
        {
            error = $"PGN export requires unique tag names; duplicate '{duplicate.Key}'.";
            return false;
        }
        foreach (var required in PgnSevenTagRoster.Names)
        {
            if (game.Tags.All(tag => tag.Name != required))
            {
                error = $"PGN export requires the Seven Tag Roster entry '{required}'.";
                return false;
            }
        }
        if (game.FindTag("Result") != game.Result.ToMarker())
        {
            error = "The Result tag and movetext termination marker must agree.";
            return false;
        }
        var setup = game.FindTag("SetUp");
        var fen = game.FindTag("FEN");
        if ((fen is not null && setup != "1") || (setup == "1" && string.IsNullOrWhiteSpace(fen)))
        {
            error = "A nonstandard PGN start requires matching SetUp \"1\" and FEN tags.";
            return false;
        }
        if (game.Tags.Any(tag => tag.Value.Contains('\r') || tag.Value.Contains('\n')))
        {
            error = "PGN export tag values cannot contain line breaks.";
            return false;
        }
        if (!ValidateVariation(game.MainLine, out error))
        {
            return false;
        }
        error = string.Empty;
        return true;
    }

    private static bool ValidateVariation(PgnVariation variation, out string error)
    {
        foreach (var move in variation.Moves)
        {
            if (move.LeadingComments.Concat(move.TrailingComments).Any(comment => comment.Text.Contains('}')))
            {
                error = "PGN brace comments cannot contain a closing brace.";
                return false;
            }
            foreach (var child in move.Variations)
            {
                if (!ValidateVariation(child, out error))
                {
                    return false;
                }
            }
        }
        error = string.Empty;
        return true;
    }

    private static void AppendVariation(List<string> tokens, PgnVariation variation, PgnExportOptions options, bool isMainLine)
    {
        var previousWasBlack = false;
        var first = true;
        foreach (var move in variation.Moves)
        {
            if (options.IncludeComments)
            {
                tokens.AddRange(move.LeadingComments.Select(CommentToken));
            }
            if (!move.IsBlackMove)
            {
                tokens.Add($"{move.FullmoveNumber}.");
            }
            else if (first || previousWasBlack)
            {
                tokens.Add($"{move.FullmoveNumber}...");
            }
            tokens.Add(move.San);
            if (options.IncludeNags)
            {
                tokens.AddRange(move.Nags.Select(nag => nag.ToString()));
            }
            if (options.IncludeComments)
            {
                tokens.AddRange(move.TrailingComments.Select(CommentToken));
            }
            if (options.IncludeVariations)
            {
                foreach (var child in move.Variations)
                {
                    tokens.Add("(");
                    AppendVariation(tokens, child, options, false);
                    tokens.Add(")");
                }
            }
            previousWasBlack = move.IsBlackMove;
            first = false;
        }
    }

    private static string CommentToken(PgnComment comment) => $"{{{comment.Text.Replace("\r", " ").Replace("\n", " ")}}}";

    private static string EscapeTagValue(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static void AppendWrapped(StringBuilder builder, IEnumerable<string> tokens, int width)
    {
        var column = 0;
        foreach (var token in tokens)
        {
            var needed = (column == 0 ? 0 : 1) + token.Length;
            if (column > 0 && column + needed > width)
            {
                builder.Append('\n');
                column = 0;
            }
            if (column > 0)
            {
                builder.Append(' ');
                column++;
            }
            builder.Append(token);
            column += token.Length;
        }
        builder.Append('\n');
    }
}
