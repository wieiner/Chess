namespace RubikState;

public readonly record struct RubikMove(int Axis, int Layer, int QuarterTurns)
{
    public bool IsValidFor(int size) => Axis is >= 0 and <= 2 && Layer >= 0 && Layer < size &&
        NormalizeQuarterTurns(QuarterTurns) != 0;

    public RubikMove Inverse() => this with { QuarterTurns = 4 - NormalizeQuarterTurns(QuarterTurns) };

    public static int NormalizeQuarterTurns(int value)
    {
        var normalized = value % 4;
        return normalized < 0 ? normalized + 4 : normalized;
    }
}

public sealed record RubikSolverCapabilities(
    string SolverId,
    string DisplayName,
    int MinimumSize,
    int MaximumSize,
    bool SupportsArbitraryState,
    bool RequiresTrustedHistory,
    bool SupportsCheckpoints,
    bool SupportsPauseResume);

public enum RubikSolvePhase
{
    Validate,
    ReverseTrustedHistory,
    Search,
    SolveCenters,
    PairWings,
    CorrectParity,
    SolveReducedCube,
    Verify,
    Complete
}

public sealed record RubikSolveProgress(
    RubikSolvePhase Phase,
    double Fraction,
    long NodesVisited,
    int MovesFound,
    TimeSpan Elapsed,
    string Message);

public sealed record RubikSolveCheckpoint(
    string SolverId,
    int Size,
    string InputHash,
    RubikSolvePhase Phase,
    string Payload);

public sealed record RubikSolveRequest(
    RubikStateDocument State,
    TimeSpan TimeLimit,
    long MemoryLimitBytes,
    int MaximumDepth,
    CancellationToken CancellationToken = default,
    RubikSolveCheckpoint? Checkpoint = null,
    IReadOnlyList<RubikMove>? TrustedHistory = null,
    IProgress<RubikSolveProgress>? Progress = null);

public enum RubikSolveFailureKind
{
    None,
    InvalidRequest,
    InvalidState,
    UnsupportedSize,
    UnsupportedState,
    InvalidCheckpoint,
    ResourceLimit,
    Timeout,
    Cancelled,
    InternalError
}

public sealed record RubikSolveFailure(RubikSolveFailureKind Kind, string Message);

public enum RubikSolutionVerificationStatus
{
    NotRun,
    Verified,
    Failed
}

public sealed record RubikSolutionVerification(
    RubikSolutionVerificationStatus Status,
    bool Solved,
    string? FinalHash,
    int AppliedMoveCount,
    string Message)
{
    public static readonly RubikSolutionVerification NotRun =
        new(RubikSolutionVerificationStatus.NotRun, false, null, 0, "Independent replay verification has not run.");
}

public sealed record RubikSolveResult(
    bool Success,
    IReadOnlyList<RubikMove> Moves,
    IReadOnlyList<RubikSolvePhase> Phases,
    TimeSpan Elapsed,
    string InputHash,
    string? FinalHash,
    RubikSolutionVerification Verification,
    RubikSolveFailure? Failure)
{
    public int MoveCount => Moves.Count;
}

public interface IRubikSolver
{
    RubikSolverCapabilities Capabilities { get; }
    Task<RubikSolveResult> SolveAsync(RubikSolveRequest request);
}

public static class RubikSolutionVerifier
{
    public static RubikSolutionVerification NotRun() => RubikSolutionVerification.NotRun;
}

public sealed class ReverseHistorySolver : IRubikSolver
{
    public RubikSolverCapabilities Capabilities { get; } = new(
        "trusted-history-reverse-v1",
        "Trusted history reverse",
        RubikStateDocument.MinimumSize,
        RubikStateDocument.MaximumSize,
        SupportsArbitraryState: false,
        RequiresTrustedHistory: true,
        SupportsCheckpoints: false,
        SupportsPauseResume: false);

    public Task<RubikSolveResult> SolveAsync(RubikSolveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var started = DateTimeOffset.UtcNow;
        var inputHash = RubikStateHasher.Calculate(request.State);
        RubikSolveResult Failure(RubikSolveFailureKind kind, string message, params RubikSolvePhase[] phases) =>
            new(false, [], phases, DateTimeOffset.UtcNow - started, inputHash, null,
                RubikSolutionVerification.NotRun, new(kind, message));

        if (request.TimeLimit <= TimeSpan.Zero || request.MemoryLimitBytes <= 0 || request.MaximumDepth < 0)
            return Task.FromResult(Failure(RubikSolveFailureKind.InvalidRequest, "Positive time/memory limits and a non-negative maximum depth are required."));
        if (request.CancellationToken.IsCancellationRequested)
            return Task.FromResult(Failure(RubikSolveFailureKind.Cancelled, "Solve was cancelled before it started."));
        var validation = RubikStateValidator.Validate(request.State);
        if (!validation.IsValid)
            return Task.FromResult(Failure(RubikSolveFailureKind.InvalidState,
                string.Join("; ", validation.Issues.Select(issue => issue.Message)), RubikSolvePhase.Validate));
        if (request.TrustedHistory is null)
            return Task.FromResult(Failure(RubikSolveFailureKind.UnsupportedState,
                "Trusted engine history is required; this solver does not solve arbitrary imported states.", RubikSolvePhase.Validate));
        if (request.TrustedHistory.Any(move => !move.IsValidFor(request.State.Size)))
            return Task.FromResult(Failure(RubikSolveFailureKind.InvalidRequest,
                "Trusted history contains an invalid axis, layer, or quarter-turn value.", RubikSolvePhase.Validate));
        if (request.TrustedHistory.Count > request.MaximumDepth)
            return Task.FromResult(Failure(RubikSolveFailureKind.ResourceLimit,
                "Trusted history exceeds the requested maximum solution depth.", RubikSolvePhase.Validate));

        request.Progress?.Report(new(RubikSolvePhase.ReverseTrustedHistory, 0, 0, 0,
            DateTimeOffset.UtcNow - started, "Reversing trusted engine history."));
        var moves = request.TrustedHistory.Reverse().Select(move => move.Inverse()).ToArray();
        request.Progress?.Report(new(RubikSolvePhase.Complete, 1, 0, moves.Length,
            DateTimeOffset.UtcNow - started, "Trusted history reversed; independent verification is pending."));
        return Task.FromResult(new RubikSolveResult(true, moves,
            [RubikSolvePhase.Validate, RubikSolvePhase.ReverseTrustedHistory, RubikSolvePhase.Complete],
            DateTimeOffset.UtcNow - started, inputHash, null, RubikSolutionVerification.NotRun, null));
    }
}
