using System.Collections.Concurrent;
using System.Diagnostics;

var checks = new Checks();
var root = FindRepositoryRoot();
var executable = Path.Combine(root, "src", "ChessUci", "bin", "x64", "Release", "net8.0-windows", "ChessUci.exe");
checks.Check(File.Exists(executable), "ChessUci executable exists");
if (!File.Exists(executable)) return checks.Finish();

using var session = new UciProcess(executable, root);
try
{
    session.Send("uci");
    checks.Check(await session.WaitStdout(line => line == "uciok", 5000), "UCI handshake completes");
    checks.Check(session.Stdout.Any(line => line.StartsWith("id name Chess Native", StringComparison.Ordinal)), "handshake identifies engine");
    checks.Check(session.Stdout.Any(line => line.Contains("option name MoveOverhead", StringComparison.Ordinal)) &&
        session.Stdout.Any(line => line.Contains("option name OwnBook", StringComparison.Ordinal)), "handshake advertises supported options");
    checks.Check(!session.Stdout.Any(line => line.Contains("option name Hash", StringComparison.Ordinal) ||
        line.Contains("option name Threads", StringComparison.Ordinal)), "handshake omits unsupported options");

    session.Send("isready");
    checks.Check(await session.WaitStdout(line => line == "readyok", 3000), "isready returns readyok");
    session.Send("not-a-uci-command");
    checks.Check(await session.WaitStderr(line => line.Contains("unknown command", StringComparison.Ordinal), 3000),
        "malformed command reports stderr diagnostic");
    var readyCount = session.Stdout.Count(line => line == "readyok");
    session.Send("isready");
    checks.Check(await session.WaitStdoutCount("readyok", readyCount + 1, 3000), "engine remains alive after malformed command");

    session.Send("setoption name OwnBook value false");
    session.Send("position startpos moves e2e4 e7e5");
    var bestCount = session.Stdout.Count(line => line.StartsWith("bestmove ", StringComparison.Ordinal));
    session.Send("go depth 1");
    checks.Check(await session.WaitStdoutCount("bestmove ", bestCount + 1, 5000), "go depth returns bestmove");
    checks.Check(ValidateLatestSearch(session.Stdout), "depth search emits parseable real telemetry and matching PV");

    bestCount++;
    session.Send("go movetime 80");
    checks.Check(await session.WaitStdoutCount("bestmove ", bestCount + 1, 5000), "go movetime returns bestmove");

    session.Send("position startpos moves e2e5");
    checks.Check(await session.WaitStderr(line => line.Contains("illegal UCI move 'e2e5'", StringComparison.Ordinal), 3000),
        "illegal move reports stderr without process exit");
    session.Send("position fen 7k/5Q2/6K1/8/8/8/8/8 b - - 0 1");
    bestCount++;
    session.Send("go depth 2");
    checks.Check(await session.WaitStdoutCount("bestmove ", bestCount + 1, 5000) && session.Stdout.Last() == "bestmove 0000",
        "FEN terminal position returns bestmove 0000");

    session.Send("position startpos");
    bestCount++;
    session.Send("go infinite");
    await Task.Delay(100);
    session.Send("stop");
    checks.Check(await session.WaitStdoutCount("bestmove ", bestCount + 1, 5000), "go infinite stop cooperatively returns one bestmove");

    bestCount++;
    session.Send("go nodes 10");
    checks.Check(await session.WaitStdoutCount("bestmove ", bestCount + 1, 5000), "go nodes returns bounded bestmove");

    session.Send("quit");
    checks.Check(await session.WaitForExit(5000) && session.ExitCode == 0, "quit exits and cleans up process");
    checks.Check(session.Stdout.All(IsProtocolLine), "stdout contains protocol lines only");
    checks.Check(session.Stdout.Count(line => line.StartsWith("bestmove ", StringComparison.Ordinal)) == bestCount + 1,
        "each completed search emits exactly one bestmove");
}
finally
{
    session.KillIfRunning();
}

return checks.Finish();

static bool ValidateLatestSearch(IReadOnlyList<string> lines)
{
    var best = lines.LastOrDefault(line => line.StartsWith("bestmove ", StringComparison.Ordinal));
    if (best is null) return false;
    var move = best[9..];
    var info = lines.LastOrDefault(line => line.StartsWith("info depth ", StringComparison.Ordinal));
    return info is not null && info.Contains(" score ", StringComparison.Ordinal) &&
        info.Contains(" nodes ", StringComparison.Ordinal) && info.Contains(" nps ", StringComparison.Ordinal) &&
        info.Contains(" time ", StringComparison.Ordinal) && info.EndsWith($" pv {move}", StringComparison.Ordinal);
}

static bool IsProtocolLine(string line) =>
    line.StartsWith("id ", StringComparison.Ordinal) || line.StartsWith("option ", StringComparison.Ordinal) ||
    line is "uciok" or "readyok" || line.StartsWith("info ", StringComparison.Ordinal) ||
    line.StartsWith("bestmove ", StringComparison.Ordinal);

static string FindRepositoryRoot()
{
    var current = new DirectoryInfo(Environment.CurrentDirectory);
    while (current is not null && !File.Exists(Path.Combine(current.FullName, "Chess.sln"))) current = current.Parent;
    return current?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
}

internal sealed class UciProcess : IDisposable
{
    private readonly Process _process;
    private readonly ConcurrentQueue<string> _stdout = new();
    private readonly ConcurrentQueue<string> _stderr = new();

    public UciProcess(string executable, string workingDirectory)
    {
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };
        _process.OutputDataReceived += (_, e) => { if (e.Data is not null) _stdout.Enqueue(e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data is not null) _stderr.Enqueue(e.Data); };
        if (!_process.Start()) throw new InvalidOperationException("ChessUci process did not start.");
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    public IReadOnlyList<string> Stdout => _stdout.ToArray();
    public IReadOnlyList<string> Stderr => _stderr.ToArray();
    public int ExitCode => _process.HasExited ? _process.ExitCode : -1;

    public void Send(string command)
    {
        _process.StandardInput.WriteLine(command);
        _process.StandardInput.Flush();
    }

    public Task<bool> WaitStdout(Func<string, bool> predicate, int timeoutMs) => WaitFor(() => Stdout.Any(predicate), timeoutMs);
    public Task<bool> WaitStderr(Func<string, bool> predicate, int timeoutMs) => WaitFor(() => Stderr.Any(predicate), timeoutMs);
    public Task<bool> WaitStdoutCount(string prefix, int count, int timeoutMs) =>
        WaitFor(() => Stdout.Count(line => line.StartsWith(prefix, StringComparison.Ordinal)) >= count, timeoutMs);
    public async Task<bool> WaitForExit(int timeoutMs)
    {
        using var timeout = new CancellationTokenSource(timeoutMs);
        try { await _process.WaitForExitAsync(timeout.Token); return true; }
        catch (OperationCanceledException) { return false; }
    }

    public void KillIfRunning()
    {
        try { if (!_process.HasExited) _process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
    }

    public void Dispose()
    {
        KillIfRunning();
        _process.Dispose();
    }

    private static async Task<bool> WaitFor(Func<bool> predicate, int timeoutMs)
    {
        var deadline = Stopwatch.StartNew();
        while (deadline.ElapsedMilliseconds < timeoutMs)
        {
            if (predicate()) return true;
            await Task.Delay(20);
        }
        return predicate();
    }
}

internal sealed class Checks
{
    private int _failed;
    public void Check(bool condition, string name)
    {
        Console.WriteLine($"{(condition ? "PASS" : "FAIL")} {name}");
        if (!condition) _failed++;
    }
    public int Finish()
    {
        Console.WriteLine($"ChessUciSubprocessTests: {(_failed == 0 ? "PASS" : $"FAIL ({_failed})")}");
        return _failed == 0 ? 0 : 1;
    }
}
