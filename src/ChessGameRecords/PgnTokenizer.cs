namespace ChessGameRecords;

public enum PgnTokenKind
{
    LeftBracket = 0,
    RightBracket,
    TagName,
    QuotedString,
    Integer,
    Period,
    SanSymbol,
    Comment,
    SemicolonComment,
    Nag,
    LeftParenthesis,
    RightParenthesis,
    Result,
    EndOfFile
}

public sealed record PgnSourceLocation(int Offset, int Line, int Column, int Length);
public sealed record PgnToken(PgnTokenKind Kind, string Value, PgnSourceLocation Location);
public sealed record PgnDiagnostic(string Code, string Message, PgnSourceLocation Location);
public sealed record PgnTokenizerOptions(
    int MaxInputLength = 1_000_000,
    int MaxTokens = 200_000,
    int MaxTokenLength = 4_096,
    int MaxCommentLength = 65_536);
public sealed record PgnTokenizationResult(
    bool Success,
    IReadOnlyList<PgnToken> Tokens,
    IReadOnlyList<PgnDiagnostic> Diagnostics);

public static class PgnTokenizer
{
    public static PgnTokenizationResult Tokenize(string input, PgnTokenizerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        options ??= new PgnTokenizerOptions();
        if (options.MaxInputLength < 1 || options.MaxTokens < 1 || options.MaxTokenLength < 1 || options.MaxCommentLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "PGN tokenizer bounds must be positive.");
        }
        if (input.Length > options.MaxInputLength)
        {
            return Failure("inputTooLarge", $"PGN input exceeds {options.MaxInputLength} characters.", 0, 1, 1, input.Length);
        }

        var scanner = new Scanner(input, options);
        return scanner.Run();
    }

    private static PgnTokenizationResult Failure(string code, string message, int offset, int line, int column, int length) =>
        new(false, Array.Empty<PgnToken>(), new[] { new PgnDiagnostic(code, message, new PgnSourceLocation(offset, line, column, length)) });

    private sealed class Scanner
    {
        private readonly string _input;
        private readonly PgnTokenizerOptions _options;
        private readonly List<PgnToken> _tokens = new();
        private int _offset;
        private int _line = 1;
        private int _column = 1;
        private bool _expectTagName;

        public Scanner(string input, PgnTokenizerOptions options)
        {
            _input = input;
            _options = options;
        }

        public PgnTokenizationResult Run()
        {
            while (_offset < _input.Length)
            {
                if (char.IsWhiteSpace(Current))
                {
                    Advance();
                    continue;
                }
                if (Current == '%' && _column == 1)
                {
                    SkipLine();
                    continue;
                }

                var start = Mark();
                PgnDiagnostic? diagnostic = Current switch
                {
                    '[' => AddSingle(PgnTokenKind.LeftBracket, "[", start, expectTagName: true),
                    ']' => AddSingle(PgnTokenKind.RightBracket, "]", start),
                    '.' => AddSingle(PgnTokenKind.Period, ".", start),
                    '(' => AddSingle(PgnTokenKind.LeftParenthesis, "(", start),
                    ')' => AddSingle(PgnTokenKind.RightParenthesis, ")", start),
                    '"' => ReadQuotedString(start),
                    '{' => ReadBraceComment(start),
                    ';' => ReadSemicolonComment(start),
                    '$' => ReadNag(start),
                    _ => ReadSymbol(start)
                };
                if (diagnostic is not null)
                {
                    return new PgnTokenizationResult(false, Array.Empty<PgnToken>(), new[] { diagnostic });
                }
                if (_tokens.Count > _options.MaxTokens)
                {
                    return new PgnTokenizationResult(false, Array.Empty<PgnToken>(), new[]
                    {
                        Diagnostic("tooManyTokens", $"PGN input exceeds {_options.MaxTokens} tokens.", start)
                    });
                }
            }

            _tokens.Add(new PgnToken(PgnTokenKind.EndOfFile, string.Empty, new PgnSourceLocation(_offset, _line, _column, 0)));
            return new PgnTokenizationResult(true, Array.AsReadOnly(_tokens.ToArray()), Array.Empty<PgnDiagnostic>());
        }

        private PgnDiagnostic? AddSingle(PgnTokenKind kind, string value, SourceMark start, bool expectTagName = false)
        {
            Advance();
            Add(kind, value, start);
            _expectTagName = expectTagName;
            return null;
        }

        private PgnDiagnostic? ReadQuotedString(SourceMark start)
        {
            Advance();
            var value = new System.Text.StringBuilder();
            while (_offset < _input.Length && Current != '"')
            {
                if (Current is '\r' or '\n')
                {
                    return Diagnostic("unterminatedString", "PGN tag strings cannot cross a line boundary.", start);
                }
                if (Current == '\\')
                {
                    Advance();
                    if (_offset >= _input.Length || Current is not ('\\' or '"'))
                    {
                        return Diagnostic("invalidEscape", "PGN tag strings only escape backslash or quote.", start);
                    }
                }
                value.Append(Current);
                Advance();
                if (value.Length > _options.MaxTokenLength)
                {
                    return Diagnostic("tokenTooLong", "PGN tag string exceeds the token limit.", start);
                }
            }
            if (_offset >= _input.Length)
            {
                return Diagnostic("unterminatedString", "PGN tag string is not terminated.", start);
            }
            Advance();
            Add(PgnTokenKind.QuotedString, value.ToString(), start);
            _expectTagName = false;
            return null;
        }

        private PgnDiagnostic? ReadBraceComment(SourceMark start)
        {
            Advance();
            var value = new System.Text.StringBuilder();
            while (_offset < _input.Length && Current != '}')
            {
                value.Append(Current);
                Advance();
                if (value.Length > _options.MaxCommentLength)
                {
                    return Diagnostic("commentTooLong", "PGN comment exceeds the comment limit.", start);
                }
            }
            if (_offset >= _input.Length)
            {
                return Diagnostic("unterminatedComment", "PGN brace comment is not terminated.", start);
            }
            Advance();
            Add(PgnTokenKind.Comment, value.ToString(), start);
            return null;
        }

        private PgnDiagnostic? ReadSemicolonComment(SourceMark start)
        {
            Advance();
            var value = new System.Text.StringBuilder();
            while (_offset < _input.Length && Current is not ('\r' or '\n'))
            {
                value.Append(Current);
                Advance();
                if (value.Length > _options.MaxCommentLength)
                {
                    return Diagnostic("commentTooLong", "PGN semicolon comment exceeds the comment limit.", start);
                }
            }
            Add(PgnTokenKind.SemicolonComment, value.ToString(), start);
            return null;
        }

        private PgnDiagnostic? ReadNag(SourceMark start)
        {
            Advance();
            var valueStart = _offset;
            while (_offset < _input.Length && char.IsAsciiDigit(Current))
            {
                Advance();
            }
            if (_offset == valueStart)
            {
                return Diagnostic("invalidNag", "A PGN NAG requires decimal digits after '$'.", start);
            }
            var value = _input[valueStart.._offset];
            if (!int.TryParse(value, out var numeric) || numeric > 255)
            {
                return Diagnostic("invalidNag", "A PGN NAG must be in the range 0..255.", start);
            }
            Add(PgnTokenKind.Nag, value, start);
            return null;
        }

        private PgnDiagnostic? ReadSymbol(SourceMark start)
        {
            var begin = _offset;
            while (_offset < _input.Length && !char.IsWhiteSpace(Current) && !IsDelimiter(Current))
            {
                Advance();
                if (_offset - begin > _options.MaxTokenLength)
                {
                    return Diagnostic("tokenTooLong", "PGN symbol exceeds the token limit.", start);
                }
            }
            if (_offset == begin)
            {
                Advance();
                return Diagnostic("unexpectedCharacter", $"Unexpected PGN character '{_input[begin]}'.", start);
            }
            var value = _input[begin.._offset];
            var kind = _expectTagName
                ? PgnTokenKind.TagName
                : IsResult(value) ? PgnTokenKind.Result
                : value.All(char.IsAsciiDigit) ? PgnTokenKind.Integer
                : PgnTokenKind.SanSymbol;
            Add(kind, value, start);
            _expectTagName = false;
            return null;
        }

        private void Add(PgnTokenKind kind, string value, SourceMark start) =>
            _tokens.Add(new PgnToken(kind, value, new PgnSourceLocation(start.Offset, start.Line, start.Column, _offset - start.Offset)));

        private PgnDiagnostic Diagnostic(string code, string message, SourceMark start) =>
            new(code, message, new PgnSourceLocation(start.Offset, start.Line, start.Column, Math.Max(1, _offset - start.Offset)));

        private SourceMark Mark() => new(_offset, _line, _column);
        private char Current => _input[_offset];

        private void Advance()
        {
            if (_input[_offset] == '\r')
            {
                _offset++;
                if (_offset < _input.Length && _input[_offset] == '\n')
                {
                    _offset++;
                }
                _line++;
                _column = 1;
                return;
            }
            if (_input[_offset++] == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
        }

        private void SkipLine()
        {
            while (_offset < _input.Length && Current is not ('\r' or '\n'))
            {
                Advance();
            }
        }

        private static bool IsDelimiter(char value) => value is '[' or ']' or '.' or '(' or ')' or '"' or '{' or '}' or ';' or '$';
        private static bool IsResult(string value) => value is "1-0" or "0-1" or "1/2-1/2" or "*";
        private readonly record struct SourceMark(int Offset, int Line, int Column);
    }
}
