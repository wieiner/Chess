namespace ChessUci;

internal readonly record struct UciCoordinateMove(int FromFile, int FromRank, int ToFile, int ToRank, int Promotion)
{
    public static bool TryParse(string text, out UciCoordinateMove move)
    {
        move = default;
        if (text.Length is not (4 or 5) || !IsFile(text[0]) || !IsRank(text[1]) ||
            !IsFile(text[2]) || !IsRank(text[3])) return false;
        var promotion = text.Length == 4 ? 0 : char.ToLowerInvariant(text[4]) switch
        {
            'q' => NativeUciEngine.Queen,
            'r' => NativeUciEngine.Rook,
            'b' => NativeUciEngine.Bishop,
            'n' => NativeUciEngine.Knight,
            _ => -1
        };
        if (promotion < 0) return false;
        move = new UciCoordinateMove(text[0] - 'a', text[1] - '1', text[2] - 'a', text[3] - '1', promotion);
        return true;
    }

    public override string ToString() => $"{(char)('a' + FromFile)}{FromRank + 1}{(char)('a' + ToFile)}{ToRank + 1}" +
        (Promotion switch { NativeUciEngine.Queen => "q", NativeUciEngine.Rook => "r", NativeUciEngine.Bishop => "b", NativeUciEngine.Knight => "n", _ => "" });

    private static bool IsFile(char value) => value is >= 'a' and <= 'h';
    private static bool IsRank(char value) => value is >= '1' and <= '8';
}

internal sealed class UciPositionController : IDisposable
{
    public NativeUciEngine Authority { get; } = new();

    public bool TryApply(IReadOnlyList<string> args, out string error)
    {
        error = string.Empty;
        if (args.Count == 0) { error = "position requires startpos or fen"; return false; }
        using var candidate = new NativeUciEngine();
        var index = 0;
        if (args[index] == "startpos")
        {
            candidate.Reset();
            index++;
        }
        else if (args[index] == "fen")
        {
            if (args.Count < 7) { error = "position fen requires six FEN fields"; return false; }
            var fen = string.Join(' ', args.Skip(1).Take(6));
            if (!candidate.SetFen(fen)) { error = "position contains invalid FEN"; return false; }
            index = 7;
        }
        else
        {
            error = "position requires startpos or fen";
            return false;
        }

        if (index < args.Count)
        {
            if (args[index] != "moves") { error = $"unexpected position token '{args[index]}'"; return false; }
            index++;
            for (; index < args.Count; index++)
            {
                if (!UciCoordinateMove.TryParse(args[index], out var move))
                {
                    error = $"invalid UCI move '{args[index]}'";
                    return false;
                }
                if (!candidate.TryMakeMove(move, out _))
                {
                    error = $"illegal UCI move '{args[index]}'";
                    return false;
                }
            }
        }

        if (!Authority.SetFen(candidate.GetFen()))
        {
            error = "candidate position could not be committed";
            return false;
        }
        return true;
    }

    public void Dispose() => Authority.Dispose();
}
