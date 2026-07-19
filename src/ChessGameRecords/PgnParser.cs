namespace ChessGameRecords;

public enum PgnParserMode
{
    ImportTolerant = 0,
    ExportStrict
}

public sealed record PgnParserOptions(PgnParserMode Mode = PgnParserMode.ImportTolerant, int MaxVariationDepth = 32);
public sealed record PgnParseResult(bool Success, PgnDocument? Document, IReadOnlyList<PgnDiagnostic> Diagnostics);

public static class PgnParser
{
    public static PgnParseResult Parse(string input, PgnParserOptions? options = null, PgnTokenizerOptions? tokenizerOptions = null)
    {
        options ??= new PgnParserOptions();
        if (options.MaxVariationDepth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options));
        }
        var tokenization = PgnTokenizer.Tokenize(input, tokenizerOptions);
        if (!tokenization.Success)
        {
            return new PgnParseResult(false, null, tokenization.Diagnostics);
        }
        return new Reader(tokenization.Tokens, options).ParseDocument();
    }

    private sealed class Reader
    {
        private readonly IReadOnlyList<PgnToken> _tokens;
        private readonly PgnParserOptions _options;
        private int _index;

        public Reader(IReadOnlyList<PgnToken> tokens, PgnParserOptions options)
        {
            _tokens = tokens;
            _options = options;
        }

        public PgnParseResult ParseDocument()
        {
            var games = new List<PgnGame>();
            while (Current.Kind != PgnTokenKind.EndOfFile)
            {
                var game = ParseGame();
                if (game.Diagnostic is not null)
                {
                    return Failed(game.Diagnostic);
                }
                games.Add(game.Value!);
            }
            if (games.Count == 0)
            {
                return Failed(Diagnostic("emptyDocument", "PGN document contains no games.", Current));
            }
            return new PgnParseResult(true, new PgnDocument(games), Array.Empty<PgnDiagnostic>());
        }

        private ParseValue<PgnGame> ParseGame()
        {
            var tags = new List<PgnTagPair>();
            while (Current.Kind == PgnTokenKind.LeftBracket)
            {
                var tag = ParseTag();
                if (tag.Diagnostic is not null)
                {
                    return ParseValue<PgnGame>.Failed(tag.Diagnostic);
                }
                tags.Add(tag.Value!);
            }

            var duplicateRequired = tags.Where(tag => PgnSevenTagRoster.Names.Contains(tag.Name, StringComparer.Ordinal))
                .GroupBy(tag => tag.Name, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
            if (duplicateRequired is not null)
            {
                return Error<PgnGame>("duplicateRequiredTag", $"Required PGN tag '{duplicateRequired.Key}' appears more than once.", Current);
            }
            if (_options.Mode == PgnParserMode.ExportStrict)
            {
                var missing = PgnSevenTagRoster.Names.FirstOrDefault(name => tags.All(tag => tag.Name != name));
                if (missing is not null)
                {
                    return Error<PgnGame>("missingRequiredTag", $"Strict PGN requires tag '{missing}'.", Current);
                }
            }

            var sequence = ParseSequence(depth: 0, stopAtRightParenthesis: false);
            if (sequence.Diagnostic is not null)
            {
                return ParseValue<PgnGame>.Failed(sequence.Diagnostic);
            }
            if (Current.Kind != PgnTokenKind.Result || !PgnResultExtensions.TryParseMarker(Current.Value, out var result))
            {
                return Error<PgnGame>("missingResult", "PGN movetext must end with a result marker.", Current);
            }
            var resultToken = Current;
            _index++;
            var resultTag = tags.FirstOrDefault(tag => tag.Name == "Result");
            if (resultTag is not null && resultTag.Value != result.ToMarker())
            {
                return Error<PgnGame>("resultMismatch", "The Result tag does not match the movetext marker.", resultToken);
            }
            return ParseValue<PgnGame>.Success(new PgnGame(tags, sequence.Value!, result));
        }

        private ParseValue<PgnTagPair> ParseTag()
        {
            _index++;
            if (Current.Kind != PgnTokenKind.TagName)
            {
                return Error<PgnTagPair>("expectedTagName", "Expected a PGN tag name after '['.", Current);
            }
            var name = Current.Value;
            _index++;
            if (Current.Kind != PgnTokenKind.QuotedString)
            {
                return Error<PgnTagPair>("expectedTagValue", $"Expected a quoted value for tag '{name}'.", Current);
            }
            var value = Current.Value;
            _index++;
            if (Current.Kind != PgnTokenKind.RightBracket)
            {
                return Error<PgnTagPair>("expectedTagClose", $"Expected ']' after tag '{name}'.", Current);
            }
            _index++;
            try
            {
                return ParseValue<PgnTagPair>.Success(new PgnTagPair(name, value));
            }
            catch (ArgumentException exception)
            {
                return Error<PgnTagPair>("invalidTag", exception.Message, Current);
            }
        }

        private ParseValue<PgnVariation> ParseSequence(int depth, bool stopAtRightParenthesis)
        {
            if (depth > _options.MaxVariationDepth)
            {
                return Error<PgnVariation>("variationDepth", "PGN variation nesting exceeds the configured limit.", Current);
            }
            var moves = new List<MoveBuilder>();
            var pendingComments = new List<PgnComment>();
            int? moveNumber = null;
            bool? isBlackMove = null;
            var expectLeadingComment = moves.Count == 0;

            while (Current.Kind is not (PgnTokenKind.EndOfFile or PgnTokenKind.Result))
            {
                if (Current.Kind == PgnTokenKind.RightParenthesis)
                {
                    if (!stopAtRightParenthesis)
                    {
                        return Error<PgnVariation>("unexpectedVariationClose", "Unexpected ')' outside a PGN variation.", Current);
                    }
                    _index++;
                    return ParseValue<PgnVariation>.Success(BuildVariation(moves));
                }
                switch (Current.Kind)
                {
                    case PgnTokenKind.Integer:
                        if (!int.TryParse(Current.Value, out var parsedMoveNumber) || parsedMoveNumber < 1)
                        {
                            return Error<PgnVariation>("invalidMoveNumber", "PGN move number must be positive.", Current);
                        }
                        moveNumber = parsedMoveNumber;
                        _index++;
                        var periods = 0;
                        while (Current.Kind == PgnTokenKind.Period)
                        {
                            periods++;
                            _index++;
                        }
                        if (periods is not (1 or 3))
                        {
                            return Error<PgnVariation>("invalidMoveNumber", "PGN move number requires one or three periods.", Current);
                        }
                        isBlackMove = periods == 3;
                        expectLeadingComment = true;
                        break;
                    case PgnTokenKind.Comment:
                    case PgnTokenKind.SemicolonComment:
                        var comment = new PgnComment(Current.Value,
                            Current.Kind == PgnTokenKind.SemicolonComment ? PgnCommentStyle.Semicolon : PgnCommentStyle.Brace);
                        if (moves.Count > 0 && pendingComments.Count == 0 && !expectLeadingComment)
                        {
                            moves[^1].TrailingComments.Add(comment);
                        }
                        else
                        {
                            pendingComments.Add(comment);
                        }
                        _index++;
                        break;
                    case PgnTokenKind.Nag:
                        if (moves.Count == 0 || !int.TryParse(Current.Value, out var nag))
                        {
                            return Error<PgnVariation>("orphanNag", "A PGN NAG must follow a move.", Current);
                        }
                        moves[^1].Nags.Add(new PgnNag(nag));
                        _index++;
                        break;
                    case PgnTokenKind.LeftParenthesis:
                        if (moves.Count == 0)
                        {
                            return Error<PgnVariation>("orphanVariation", "A PGN variation must follow a move.", Current);
                        }
                        _index++;
                        var child = ParseSequence(depth + 1, stopAtRightParenthesis: true);
                        if (child.Diagnostic is not null)
                        {
                            return ParseValue<PgnVariation>.Failed(child.Diagnostic);
                        }
                        moves[^1].Variations.Add(child.Value!);
                        expectLeadingComment = true;
                        break;
                    case PgnTokenKind.SanSymbol:
                        var inferredBlack = isBlackMove ?? (moves.Count > 0 ? !moves[^1].IsBlackMove : false);
                        var inferredNumber = moveNumber ?? (moves.Count > 0
                            ? moves[^1].FullmoveNumber + (moves[^1].IsBlackMove ? 1 : 0)
                            : 1);
                        moves.Add(new MoveBuilder(moves.Count, inferredNumber, inferredBlack, Current.Value, pendingComments));
                        pendingComments = new List<PgnComment>();
                        moveNumber = null;
                        isBlackMove = null;
                        expectLeadingComment = false;
                        _index++;
                        break;
                    default:
                        return Error<PgnVariation>("unexpectedToken", $"Unexpected token '{Current.Value}' in PGN movetext.", Current);
                }
            }
            if (stopAtRightParenthesis)
            {
                return Error<PgnVariation>("unterminatedVariation", "PGN variation is not terminated with ')'.", Current);
            }
            if (pendingComments.Count > 0 && moves.Count > 0)
            {
                moves[^1].TrailingComments.AddRange(pendingComments);
            }
            return ParseValue<PgnVariation>.Success(BuildVariation(moves));
        }

        private static PgnVariation BuildVariation(IEnumerable<MoveBuilder> moves) => new(moves.Select(move => new PgnMoveNode(
            move.PlyIndex, move.San, move.LeadingComments, move.Nags, move.TrailingComments, move.Variations,
            move.FullmoveNumber, move.IsBlackMove)));

        private PgnToken Current => _tokens[Math.Min(_index, _tokens.Count - 1)];
        private PgnDiagnostic Diagnostic(string code, string message, PgnToken token) => new(code, message, token.Location);
        private ParseValue<T> Error<T>(string code, string message, PgnToken token) => ParseValue<T>.Failed(Diagnostic(code, message, token));
        private PgnParseResult Failed(PgnDiagnostic diagnostic) => new(false, null, new[] { diagnostic });

        private sealed class MoveBuilder
        {
            public MoveBuilder(int plyIndex, int fullmoveNumber, bool isBlackMove, string san, IEnumerable<PgnComment> leadingComments)
            {
                PlyIndex = plyIndex;
                FullmoveNumber = fullmoveNumber;
                IsBlackMove = isBlackMove;
                San = san;
                LeadingComments.AddRange(leadingComments);
            }
            public int PlyIndex { get; }
            public int FullmoveNumber { get; }
            public bool IsBlackMove { get; }
            public string San { get; }
            public List<PgnComment> LeadingComments { get; } = new();
            public List<PgnNag> Nags { get; } = new();
            public List<PgnComment> TrailingComments { get; } = new();
            public List<PgnVariation> Variations { get; } = new();
        }

        private sealed record ParseValue<T>(T? Value, PgnDiagnostic? Diagnostic)
        {
            public static ParseValue<T> Success(T value) => new(value, null);
            public static ParseValue<T> Failed(PgnDiagnostic diagnostic) => new(default, diagnostic);
        }
    }
}
