using System.Diagnostics;

var startedUtc = DateTimeOffset.UtcNow;
var stopwatch = Stopwatch.StartNew();
var childPid = "";
var timedOut = false;
var exitCode = 125;

try
{
    var spec = WatchdogSpec.Parse(args);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(spec.StdoutPath)) ?? ".");
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(spec.StderrPath)) ?? ".");

    await using var stdout = File.Create(spec.StdoutPath);
    await using var stderr = File.Create(spec.StderrPath);

    using var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = spec.File,
            Arguments = spec.Arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(spec.WorkingDirectory)
                ? Environment.CurrentDirectory
                : spec.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        },
        EnableRaisingEvents = true
    };

    if (!process.Start())
    {
        throw new InvalidOperationException("Child process did not start.");
    }

    childPid = process.Id.ToString();
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(spec.TimeoutSeconds));
    var stdoutTask = process.StandardOutput.BaseStream.CopyToAsync(stdout, timeout.Token);
    var stderrTask = process.StandardError.BaseStream.CopyToAsync(stderr, timeout.Token);

    try
    {
        await process.WaitForExitAsync(timeout.Token);
        await Task.WhenAll(stdoutTask, stderrTask);
        exitCode = process.ExitCode;
    }
    catch (OperationCanceledException)
    {
        timedOut = true;
        exitCode = 124;
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best effort: the process may have exited between timeout and kill.
        }

        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await process.WaitForExitAsync(cleanup.Token);
        }
        catch
        {
            // Do not let cleanup defeat the watchdog timeout contract.
        }
    }
    finally
    {
        await stdout.FlushAsync();
        await stderr.FlushAsync();
    }
}
catch (Exception ex)
{
    exitCode = 125;
    Console.Error.WriteLine(ex.ToString());
}
finally
{
    stopwatch.Stop();
    Console.WriteLine($"ChildPid: {childPid}");
    Console.WriteLine($"StartedUtc: {startedUtc:O}");
    Console.WriteLine($"TimeoutSeconds: {GetArgValue(args, "--timeout") ?? ""}");
    Console.WriteLine($"Duration: {stopwatch.Elapsed:c}");
    Console.WriteLine($"TimedOut: {timedOut}");
    Console.WriteLine($"ExitCode: {exitCode}");
    Console.WriteLine($"StdoutPath: {GetArgValue(args, "--stdout") ?? ""}");
    Console.WriteLine($"StderrPath: {GetArgValue(args, "--stderr") ?? ""}");
}

return exitCode;

static string? GetArgValue(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }
    return null;
}

sealed record WatchdogSpec(
    string File,
    string Arguments,
    string WorkingDirectory,
    int TimeoutSeconds,
    string StdoutPath,
    string StderrPath)
{
    public static WatchdogSpec Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var recognized = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "--file", "--args", "--workdir", "--timeout", "--stdout", "--stderr"
        };

        for (var i = 0; i < args.Length; i++)
        {
            var key = args[i];
            if (!recognized.Contains(key))
            {
                throw new ArgumentException($"Unexpected argument '{key}'. Expected one of: {string.Join(", ", recognized)}.");
            }

            if (key.Equals("--args", StringComparison.OrdinalIgnoreCase))
            {
                var parts = new List<string>();
                while (i + 1 < args.Length && !recognized.Contains(args[i + 1]))
                {
                    parts.Add(args[++i]);
                }
                values[key] = string.Join(" ", parts);
                continue;
            }

            if (i + 1 >= args.Length || recognized.Contains(args[i + 1]))
            {
                throw new ArgumentException($"Missing value for '{key}'.");
            }
            values[key] = args[++i];
        }

        var file = Required(values, "--file");
        var timeoutText = Required(values, "--timeout");
        if (!int.TryParse(timeoutText, out var timeoutSeconds) || timeoutSeconds <= 0)
        {
            throw new ArgumentException("--timeout must be a positive integer number of seconds.");
        }

        return new WatchdogSpec(
            file,
            values.TryGetValue("--args", out var childArgs) ? childArgs : string.Empty,
            values.TryGetValue("--workdir", out var workdir) ? workdir : Environment.CurrentDirectory,
            timeoutSeconds,
            Required(values, "--stdout"),
            Required(values, "--stderr"));
    }
    static string Required(IReadOnlyDictionary<string, string> values, string key)
    {
        if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
        throw new ArgumentException($"Missing required argument {key}.");
    }
}

