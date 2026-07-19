namespace ChessUci;

internal sealed record UciGoParameters(
    int? Depth,
    int? MoveTime,
    long? Nodes,
    int? WhiteTime,
    int? BlackTime,
    int WhiteIncrement,
    int BlackIncrement,
    bool Infinite)
{
    public static bool TryParse(IReadOnlyList<string> args, out UciGoParameters parameters, out string error)
    {
        int? depth = null, moveTime = null, whiteTime = null, blackTime = null;
        long? nodes = null;
        var whiteIncrement = 0;
        var blackIncrement = 0;
        var infinite = false;
        error = string.Empty;
        for (var index = 0; index < args.Count; index++)
        {
            var key = args[index];
            if (key == "infinite") { infinite = true; continue; }
            if (key is not ("depth" or "movetime" or "nodes" or "wtime" or "btime" or "winc" or "binc"))
            {
                error = $"unsupported go token '{key}'";
                parameters = null!;
                return false;
            }
            if (++index >= args.Count || !long.TryParse(args[index], out var value) || value < 0 || value > int.MaxValue)
            {
                error = $"go {key} requires a bounded non-negative integer";
                parameters = null!;
                return false;
            }
            switch (key)
            {
                case "depth": depth = Math.Clamp((int)value, 1, 64); break;
                case "movetime": moveTime = (int)value; break;
                case "nodes": nodes = value; break;
                case "wtime": whiteTime = (int)value; break;
                case "btime": blackTime = (int)value; break;
                case "winc": whiteIncrement = (int)value; break;
                case "binc": blackIncrement = (int)value; break;
            }
        }
        parameters = new UciGoParameters(depth, moveTime, nodes, whiteTime, blackTime, whiteIncrement, blackIncrement, infinite);
        return true;
    }
}

internal sealed class UciSearchController : IDisposable
{
    private readonly object _gate = new();
    private readonly TextWriter _output;
    private NativeUciEngine? _activeEngine;
    private Task? _activeTask;
    private long _generation;
    private bool _disposed;

    public UciSearchController(TextWriter output) => _output = output;

    public bool Start(string fen, UciGoParameters go, int moveOverhead, bool ownBook, out string error)
    {
        error = string.Empty;
        Cancel(suppressResult: true);
        var generation = Interlocked.Increment(ref _generation);
        var engine = new NativeUciEngine();
        if (!engine.SetFen(fen))
        {
            engine.Dispose();
            error = "search position FEN was rejected";
            return false;
        }
        if (!engine.SetSearchNodeLimit(go.Nodes ?? 0))
        {
            engine.Dispose();
            error = "search node limit was rejected";
            return false;
        }
        var options = BuildOptions(fen, go, moveOverhead, ownBook);
        lock (_gate)
        {
            if (_disposed) { engine.Dispose(); error = "search controller is disposed"; return false; }
            _activeEngine = engine;
            _activeTask = Task.Run(() => RunSearch(engine, options, generation));
        }
        return true;
    }

    public void Stop() => Cancel(suppressResult: false);

    public void Cancel(bool suppressResult)
    {
        if (suppressResult) Interlocked.Increment(ref _generation);
        lock (_gate) _activeEngine?.CancelSearch();
    }

    public bool WaitForIdle(TimeSpan timeout)
    {
        Task? task;
        lock (_gate) task = _activeTask;
        return task is null || task.Wait(timeout);
    }

    private void RunSearch(NativeUciEngine engine, NativeSearchOptions options, long generation)
    {
        try
        {
            var success = engine.MakeBestMove(options, out var best);
            if (generation != Interlocked.Read(ref _generation)) return;
            var move = success ? ToUci(best) : "0000";
            lock (_output) { _output.WriteLine($"bestmove {move}"); _output.Flush(); }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"UCI search error: {ex.Message}");
            if (generation == Interlocked.Read(ref _generation))
            {
                lock (_output) { _output.WriteLine("bestmove 0000"); _output.Flush(); }
            }
        }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_activeEngine, engine)) _activeEngine = null;
            }
            engine.Dispose();
        }
    }

    private static NativeSearchOptions BuildOptions(string fen, UciGoParameters go, int overhead, bool ownBook)
    {
        var sideWhite = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1) == "w";
        var remaining = sideWhite ? go.WhiteTime : go.BlackTime;
        var increment = sideWhite ? go.WhiteIncrement : go.BlackIncrement;
        var time = go.MoveTime ?? (remaining is null ? 0 : Math.Max(1, remaining.Value / 30 + increment / 2 - overhead));
        if (go.MoveTime is not null) time = Math.Max(1, time - overhead);
        var iterative = go.Infinite || go.Nodes is not null || time > 0;
        return new NativeSearchOptions
        {
            Depth = go.Depth ?? (iterative ? 64 : 6),
            TimeLimitMs = go.Infinite ? 0 : time,
            AutomaticDepth = iterative ? 1 : 0,
            UseQuiescence = 1,
            UseTranspositionTable = 1,
            UseMoveOrdering = 1,
            UsePieceSquareTables = 1,
            UseBishopPairBonus = 1,
            UseKingSafetyBonus = 1,
            UseGpuEvaluation = 0,
            UseEndgameTables = 1,
            OpeningRandomness = ownBook ? 20 : 0,
            OpeningMaxPly = 16
        };
    }

    private static string ToUci(NativeMove move) => new UciCoordinateMove(
        move.FromFile, move.FromRank, move.ToFile, move.ToRank, move.Promotion).ToString();

    public void Dispose()
    {
        lock (_gate) _disposed = true;
        Cancel(suppressResult: true);
        WaitForIdle(TimeSpan.FromSeconds(5));
    }
}
