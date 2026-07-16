using System.Text.Json;

namespace RubikState;

public enum RubikReductionPhase
{
    NormalizeOrientation,
    SolveCenters,
    PairWings,
    BuildReduced3x3,
    SolveReduced3x3,
    ResolveParity,
    VerifySolved
}

public enum RubikReductionStatus
{
    Planned,
    InProgress,
    Incomplete,
    Complete,
    Failed,
    Cancelled
}

public sealed record RubikReductionPhaseDescriptor(
    RubikReductionPhase Phase,
    string Goal,
    string EntryInvariant,
    string ExitInvariant,
    bool Implemented);

public sealed record RubikReductionCheckpoint(
    string Format,
    int Version,
    string SolverId,
    int Size,
    string InputHash,
    RubikReductionPhase CurrentPhase,
    RubikReductionStatus Status,
    IReadOnlyList<RubikMove> EmittedMoves,
    IReadOnlyList<string> Log);

public sealed record RubikReductionPlanResult(
    bool Success,
    RubikReductionStatus Status,
    IReadOnlyList<RubikReductionPhaseDescriptor> Phases,
    RubikReductionCheckpoint? Checkpoint,
    RubikSolveFailure? Failure);

public static class RubikNxnReductionFramework
{
    public const string SolverId = "owned-nxn-reduction-framework-v1";
    public const string Format = "rubik.reduction-checkpoint";
    public const int Version = 1;
    public const int MaximumCheckpointBytes = 1024 * 1024;
    public const int MaximumLogEntries = 128;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static IReadOnlyList<RubikReductionPhaseDescriptor> PhasePlan { get; } =
    [
        new(RubikReductionPhase.NormalizeOrientation, "Normalize the declared U/R/F/D/L/B frame.",
            "Portable face order and cubie inventory are valid.", "Orientation frame is explicit and stable.", true),
        new(RubikReductionPhase.SolveCenters, "Arrange center stickers by target face and orbit.",
            "Orientation frame is stable.", "Every center has its target face color.", false),
        new(RubikReductionPhase.PairWings, "Pair matching wing orbits into reduced edges.",
            "Centers are solved.", "All twelve reduced edges are paired.", false),
        new(RubikReductionPhase.BuildReduced3x3, "Project corners, paired edges, and centers to a 3x3 state.",
            "Centers and wings are reduced.", "A validated reduced 3x3 state exists.", false),
        new(RubikReductionPhase.SolveReduced3x3, "Solve the reduced 3x3 state with an approved backend.",
            "Reduced 3x3 is valid.", "Reduced 3x3 is solved or parity is classified.", false),
        new(RubikReductionPhase.ResolveParity, "Apply size/parity-specific correction if required.",
            "Reduction parity is classified.", "Reduction parity obstruction is cleared.", false),
        new(RubikReductionPhase.VerifySolved, "Replay all emitted moves and verify the solved hash.",
            "A complete candidate move list exists.", "Independent replay reaches canonical solved state.", true)
    ];

    public static RubikReductionPlanResult CreatePlan(RubikStateDocument state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (cancellationToken.IsCancellationRequested)
            return Failure(RubikSolveFailureKind.Cancelled, "Reduction planning was cancelled.", RubikReductionStatus.Cancelled);
        if (state.Size < 4)
            return Failure(RubikSolveFailureKind.UnsupportedSize, "NxN reduction starts at size 4.", RubikReductionStatus.Failed);
        var validation = RubikSolvabilityValidator.Validate(state);
        if (!validation.BasicCountsValid || !validation.CubieInventoryValid)
            return Failure(RubikSolveFailureKind.InvalidState, "State failed basic or cubie-inventory validation.", RubikReductionStatus.Failed);

        var inputHash = RubikStateHasher.Calculate(state);
        var log = BoundedLog([
            $"N={state.Size} basic counts and cubie inventory validated.",
            "Reduction guidance created; center/wing move generation is not implemented."
        ]);
        var checkpoint = new RubikReductionCheckpoint(Format, Version, SolverId, state.Size, inputHash,
            RubikReductionPhase.NormalizeOrientation, RubikReductionStatus.Incomplete, [], log);
        return new(true, RubikReductionStatus.Incomplete, PhasePlan, checkpoint, null);
    }

    public static string SerializeCheckpoint(RubikReductionCheckpoint checkpoint) =>
        JsonSerializer.Serialize(ValidateCheckpointShape(checkpoint), JsonOptions);

    public static RubikReductionCheckpoint ParseCheckpoint(string json, RubikStateDocument expectedState)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(expectedState);
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaximumCheckpointBytes)
            throw new InvalidDataException("Reduction checkpoint exceeds the size limit.");
        RubikReductionCheckpoint checkpoint;
        try
        {
            checkpoint = JsonSerializer.Deserialize<RubikReductionCheckpoint>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            }) ?? throw new InvalidDataException("Reduction checkpoint is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Reduction checkpoint JSON is malformed.", exception);
        }
        ValidateCheckpointShape(checkpoint);
        var expectedHash = RubikStateHasher.Calculate(expectedState);
        if (checkpoint.Size != expectedState.Size || !StringComparer.Ordinal.Equals(checkpoint.InputHash, expectedHash))
            throw new InvalidDataException("Reduction checkpoint does not match the input size/hash.");
        return checkpoint;
    }

    public static IReadOnlyList<string> BoundedLog(IEnumerable<string> entries) => entries
        .Where(entry => !string.IsNullOrWhiteSpace(entry))
        .Select(entry => entry.Length <= 512 ? entry : entry[..512])
        .TakeLast(MaximumLogEntries)
        .ToArray();

    public static void SaveCheckpointAtomic(string path, RubikReductionCheckpoint checkpoint)
    {
        var json = SerializeCheckpoint(checkpoint);
        AtomicTextFile.Write(path, json);
    }

    public static RubikReductionCheckpoint LoadCheckpoint(string path, RubikStateDocument expectedState)
    {
        var json = AtomicTextFile.ReadBounded(path, MaximumCheckpointBytes);
        return ParseCheckpoint(json, expectedState);
    }

    private static RubikReductionCheckpoint ValidateCheckpointShape(RubikReductionCheckpoint checkpoint)
    {
        if (checkpoint.Format != Format || checkpoint.Version != Version || checkpoint.SolverId != SolverId)
            throw new InvalidDataException("Unsupported reduction checkpoint format, version, or solver id.");
        if (checkpoint.Size is < 4 or > RubikStateDocument.MaximumSize)
            throw new InvalidDataException("Reduction checkpoint size is outside supported bounds.");
        if (checkpoint.InputHash.Length != 64 || checkpoint.InputHash.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidDataException("Reduction checkpoint input hash is invalid.");
        if (checkpoint.EmittedMoves.Any(move => !move.IsValidFor(checkpoint.Size)))
            throw new InvalidDataException("Reduction checkpoint contains an invalid move.");
        if (checkpoint.Log.Count > MaximumLogEntries)
            throw new InvalidDataException("Reduction checkpoint log exceeds the entry limit.");
        return checkpoint;
    }

    private static RubikReductionPlanResult Failure(RubikSolveFailureKind kind, string message, RubikReductionStatus status) =>
        new(false, status, PhasePlan, null, new(kind, message));
}
