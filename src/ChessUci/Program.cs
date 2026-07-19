using ChessUci;

var debug = false;
var moveOverhead = 30;
var ownBook = true;
using var position = new UciPositionController();

while (Console.ReadLine() is { } line)
{
    var parsed = UciCommandParser.Parse(line);
    if (!parsed.Success || parsed.Command is null)
    {
        Console.Error.WriteLine($"UCI error: {parsed.Error}");
        continue;
    }

    var command = parsed.Command;
    switch (command.Kind)
    {
        case UciCommandKind.Uci:
            Console.WriteLine("id name Chess Native P4M");
            Console.WriteLine("id author wieiner/Chess");
            Console.WriteLine("option name MoveOverhead type spin default 30 min 0 max 5000");
            Console.WriteLine("option name OwnBook type check default true");
            Console.WriteLine("uciok");
            break;
        case UciCommandKind.IsReady:
            Console.WriteLine("readyok");
            break;
        case UciCommandKind.Debug:
            debug = command.Arguments[0] == "on";
            break;
        case UciCommandKind.SetOption:
            if (string.Equals(command.OptionName, "MoveOverhead", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(command.OptionValue, out var overhead))
                moveOverhead = Math.Clamp(overhead, 0, 5000);
            else if (string.Equals(command.OptionName, "OwnBook", StringComparison.OrdinalIgnoreCase) &&
                     bool.TryParse(command.OptionValue, out var book))
                ownBook = book;
            else
                Console.Error.WriteLine($"UCI error: unsupported option '{command.OptionName}'");
            break;
        case UciCommandKind.Register:
        case UciCommandKind.PonderHit:
            if (debug) Console.Error.WriteLine($"UCI debug: {command.Kind} is a compatibility no-op");
            break;
        case UciCommandKind.NewGame:
            position.Authority.Reset();
            break;
        case UciCommandKind.Position:
            if (!position.TryApply(command.Arguments, out var positionError))
                Console.Error.WriteLine($"UCI error: {positionError}");
            break;
        case UciCommandKind.Quit:
            return 0;
        case UciCommandKind.Go:
        case UciCommandKind.Stop:
            Console.Error.WriteLine($"UCI error: {command.Kind} is recognized but not active in this build stage");
            break;
    }
}

GC.KeepAlive(moveOverhead);
GC.KeepAlive(ownBook);
return 0;
