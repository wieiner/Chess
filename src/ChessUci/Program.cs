using ChessUci;

var debug = false;
var moveOverhead = 30;
var ownBook = true;
using var position = new UciPositionController();
using var search = new UciSearchController(Console.Out);

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
            search.Cancel(suppressResult: true);
            position.Authority.Reset();
            break;
        case UciCommandKind.Position:
            search.Cancel(suppressResult: true);
            if (!position.TryApply(command.Arguments, out var positionError))
                Console.Error.WriteLine($"UCI error: {positionError}");
            break;
        case UciCommandKind.Go:
            if (!UciGoParameters.TryParse(command.Arguments, out var go, out var goError) ||
                !search.Start(position.Authority.GetFen(), go, moveOverhead, ownBook, out goError))
                Console.Error.WriteLine($"UCI error: {goError}");
            break;
        case UciCommandKind.Stop:
            search.Stop();
            break;
        case UciCommandKind.Quit:
            search.Cancel(suppressResult: true);
            search.WaitForIdle(TimeSpan.FromSeconds(5));
            return 0;
    }
}

GC.KeepAlive(moveOverhead);
GC.KeepAlive(ownBook);
return 0;
