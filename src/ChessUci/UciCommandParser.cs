namespace ChessUci;

internal enum UciCommandKind
{
    Uci,
    Debug,
    IsReady,
    SetOption,
    Register,
    NewGame,
    Position,
    Go,
    Stop,
    PonderHit,
    Quit
}

internal sealed record UciCommand(UciCommandKind Kind, IReadOnlyList<string> Arguments, string? OptionName = null,
    string? OptionValue = null);

internal sealed record UciParseResult(bool Success, UciCommand? Command, string Error)
{
    public static UciParseResult Fail(string error) => new(false, null, error);
    public static UciParseResult Ok(UciCommand command) => new(true, command, string.Empty);
}

internal static class UciCommandParser
{
    internal const int MaxLineLength = 16 * 1024;
    private const int MaxTokens = 256;

    public static UciParseResult Parse(string? line)
    {
        if (line is null) return UciParseResult.Fail("end of input");
        if (line.Length > MaxLineLength) return UciParseResult.Fail("command line exceeds 16 KiB");
        var tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return UciParseResult.Fail("empty command");
        if (tokens.Length > MaxTokens) return UciParseResult.Fail("command has too many tokens");
        var args = tokens.Skip(1).ToArray();
        return tokens[0] switch
        {
            "uci" => NoArguments(UciCommandKind.Uci, args),
            "debug" => ParseDebug(args),
            "isready" => NoArguments(UciCommandKind.IsReady, args),
            "setoption" => ParseSetOption(args),
            "register" => UciParseResult.Ok(new UciCommand(UciCommandKind.Register, args)),
            "ucinewgame" => NoArguments(UciCommandKind.NewGame, args),
            "position" => RequiredArguments(UciCommandKind.Position, args),
            "go" => UciParseResult.Ok(new UciCommand(UciCommandKind.Go, args)),
            "stop" => NoArguments(UciCommandKind.Stop, args),
            "ponderhit" => NoArguments(UciCommandKind.PonderHit, args),
            "quit" => NoArguments(UciCommandKind.Quit, args),
            _ => UciParseResult.Fail($"unknown command '{tokens[0]}'")
        };
    }

    private static UciParseResult ParseDebug(string[] args) =>
        args.Length == 1 && args[0] is "on" or "off"
            ? UciParseResult.Ok(new UciCommand(UciCommandKind.Debug, args))
            : UciParseResult.Fail("debug requires 'on' or 'off'");

    private static UciParseResult ParseSetOption(string[] args)
    {
        if (args.Length < 2 || args[0] != "name") return UciParseResult.Fail("setoption requires 'name'");
        var valueIndex = Array.IndexOf(args, "value", 1);
        var nameEnd = valueIndex < 0 ? args.Length : valueIndex;
        var name = string.Join(' ', args[1..nameEnd]);
        if (string.IsNullOrWhiteSpace(name)) return UciParseResult.Fail("setoption name is empty");
        var value = valueIndex < 0 ? null : string.Join(' ', args[(valueIndex + 1)..]);
        return UciParseResult.Ok(new UciCommand(UciCommandKind.SetOption, args, name, value));
    }

    private static UciParseResult NoArguments(UciCommandKind kind, string[] args) =>
        args.Length == 0 ? UciParseResult.Ok(new UciCommand(kind, args)) : UciParseResult.Fail($"{kind} takes no arguments");

    private static UciParseResult RequiredArguments(UciCommandKind kind, string[] args) =>
        args.Length > 0 ? UciParseResult.Ok(new UciCommand(kind, args)) : UciParseResult.Fail($"{kind} requires arguments");
}
