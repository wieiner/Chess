using System.Diagnostics;

namespace RubikState;

public sealed class BoundedTwoByTwoSolver : IRubikSolver
{
    private static readonly RubikMove[] Moves =
        (from axis in Enumerable.Range(0, 3)
         from layer in Enumerable.Range(0, 2)
         from turns in Enumerable.Range(1, 3)
         select new RubikMove(axis, layer, turns)).ToArray();

    public RubikSolverCapabilities Capabilities { get; } = new(
        "owned-bounded-2x2-iddfs-v1", "Bounded arbitrary 2x2", 2, 2,
        SupportsArbitraryState: true, RequiresTrustedHistory: false,
        SupportsCheckpoints: false, SupportsPauseResume: false);

    public Task<RubikSolveResult> SolveAsync(RubikSolveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.Run(() => Solve(request), CancellationToken.None);
    }

    private static RubikSolveResult Solve(RubikSolveRequest request)
    {
        var timer = Stopwatch.StartNew();
        var inputHash = RubikStateHasher.Calculate(request.State);
        RubikSolveResult Fail(RubikSolveFailureKind kind, string message, params RubikSolvePhase[] phases) =>
            new(false, [], phases, timer.Elapsed, inputHash, null, RubikSolutionVerification.NotRun, new(kind, message));

        if (request.State.Size != 2)
            return Fail(RubikSolveFailureKind.UnsupportedSize, "This backend supports arbitrary 2x2 states only.", RubikSolvePhase.Validate);
        if (request.TimeLimit <= TimeSpan.Zero || request.MemoryLimitBytes < 1024 * 1024 || request.MaximumDepth < 0)
            return Fail(RubikSolveFailureKind.InvalidRequest, "Positive time, at least 1 MiB memory, and non-negative depth are required.");
        var solvability = RubikSolvabilityValidator.Validate(request.State);
        if (!solvability.SolverReady)
            return Fail(RubikSolveFailureKind.InvalidState,
                "Input did not pass the full 2x2 inventory/orientation solvability proof.", RubikSolvePhase.Validate);
        if (request.CancellationToken.IsCancellationRequested)
            return Fail(RubikSolveFailureKind.Cancelled, "Solve was cancelled before search.", RubikSolvePhase.Validate);

        var start = request.State.Faces.Flatten();
        var solved = SolvedFacelets();
        if (start.SequenceEqual(solved))
            return new(true, [], [RubikSolvePhase.Validate, RubikSolvePhase.Complete], timer.Elapsed,
                inputHash, RubikStateHasher.Calculate(SolvedDocument()), RubikSolutionVerification.NotRun, null);

        var nodeLimit = Math.Max(1_000L, request.MemoryLimitBytes / 160L);
        long nodes = 0;
        var path = new List<RubikMove>();
        for (var depth = 1; depth <= request.MaximumDepth; depth++)
        {
            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            if (Search(start, solved, depth, null, path, seen, request, timer, nodeLimit, ref nodes))
            {
                request.Progress?.Report(new(RubikSolvePhase.Complete, 1, nodes, path.Count, timer.Elapsed,
                    "Bounded 2x2 solution found; independent replay verification is pending."));
                return new(true, path.ToArray(), [RubikSolvePhase.Validate, RubikSolvePhase.Search, RubikSolvePhase.Complete],
                    timer.Elapsed, inputHash, null, RubikSolutionVerification.NotRun, null);
            }
            if (request.CancellationToken.IsCancellationRequested)
                return Fail(RubikSolveFailureKind.Cancelled, "Solve was cancelled during search.", RubikSolvePhase.Validate, RubikSolvePhase.Search);
            if (timer.Elapsed >= request.TimeLimit)
                return Fail(RubikSolveFailureKind.Timeout, "2x2 search reached its time limit.", RubikSolvePhase.Validate, RubikSolvePhase.Search);
            if (nodes >= nodeLimit)
                return Fail(RubikSolveFailureKind.ResourceLimit, "2x2 search reached its memory-derived node limit.", RubikSolvePhase.Validate, RubikSolvePhase.Search);
            request.Progress?.Report(new(RubikSolvePhase.Search,
                request.MaximumDepth == 0 ? 1 : (double)depth / request.MaximumDepth, nodes, 0, timer.Elapsed,
                $"Completed depth {depth}."));
        }
        return Fail(RubikSolveFailureKind.ResourceLimit, "No solution was found within the requested depth bound.",
            RubikSolvePhase.Validate, RubikSolvePhase.Search);
    }

    private static bool Search(int[] state, int[] solved, int remaining, RubikMove? previous, List<RubikMove> path,
        Dictionary<string, int> seen, RubikSolveRequest request, Stopwatch timer, long nodeLimit, ref long nodes)
    {
        if (state.SequenceEqual(solved)) return true;
        if (remaining == 0 || request.CancellationToken.IsCancellationRequested || timer.Elapsed >= request.TimeLimit || nodes >= nodeLimit)
            return false;
        var misplaced = state.Where((color, index) => color != solved[index]).Count();
        if ((misplaced + 11) / 12 > remaining) return false;
        var priorKey = previous is { } priorMove ? $"{priorMove.Axis}:{priorMove.Layer}" : "root";
        var key = Convert.ToBase64String(state.Select(value => (byte)value).ToArray()) + "|" + priorKey;
        if (seen.TryGetValue(key, out var priorRemaining) && priorRemaining >= remaining) return false;
        seen[key] = remaining;

        foreach (var move in Moves)
        {
            if (previous is { } prior)
            {
                if (move.Axis == prior.Axis && move.Layer == prior.Layer) continue;
                if (move.Axis == prior.Axis && move.Layer < prior.Layer) continue;
            }
            nodes++;
            var next = RubikFaceletMoveSimulator.Apply(2, state, move);
            path.Add(move);
            if (Search(next, solved, remaining - 1, move, path, seen, request, timer, nodeLimit, ref nodes)) return true;
            path.RemoveAt(path.Count - 1);
            if (request.CancellationToken.IsCancellationRequested || timer.Elapsed >= request.TimeLimit || nodes >= nodeLimit) return false;
        }
        return false;
    }

    private static int[] SolvedFacelets() => Enumerable.Range(1, 6).SelectMany(color => Enumerable.Repeat(color, 4)).ToArray();
    private static RubikStateDocument SolvedDocument() => RubikStateDocument.Create(2, SolvedFacelets(), "bounded-2x2-solved");
}
