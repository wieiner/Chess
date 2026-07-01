using ChessOnlineProtocol;

namespace ChessOnlineClient;

public sealed class OnlineLobbyFilterState
{
    public string RulesetIdFilter { get; set; } = "";
    public bool IncludeWaitingTables { get; set; } = true;
    public bool IncludeInGameTables { get; set; } = true;
    public bool IncludeFinishedTables { get; set; }

    public OnlineLobbySnapshotRequest ToRequest()
    {
        return new OnlineLobbySnapshotRequest
        {
            RulesetIdFilter = RulesetIdFilter.Trim(),
            IncludeWaitingTables = IncludeWaitingTables,
            IncludeInGameTables = IncludeInGameTables,
            IncludeFinishedTables = IncludeFinishedTables
        };
    }
}

public sealed class OnlineLobbyTableDisplayRow
{
    public string RoomId { get; init; } = "";
    public string TableId { get; init; } = "";
    public string RulesetId { get; init; } = "";
    public string TableState { get; init; } = "";
    public int SeatsOccupied { get; init; }
    public int MaxSeats { get; init; }
    public int SpectatorCount { get; init; }
    public long LastServerSeq { get; init; }
    public string UpdatedUtc { get; init; } = "";
    public string SeatSummary { get; init; } = "";
    public bool CanJoinAsPlayer => SeatsOccupied < MaxSeats && !string.Equals(TableState, "Finished", StringComparison.OrdinalIgnoreCase);
    public bool CanSpectate => !string.IsNullOrWhiteSpace(RoomId) && !string.IsNullOrWhiteSpace(TableId);
    public string DisplayLabel => $"{RulesetId} {RoomId}/{TableId} {TableState} seats={SeatsOccupied}/{MaxSeats} spectators={SpectatorCount} seq={LastServerSeq}";

    public static OnlineLobbyTableDisplayRow FromProtocol(OnlineLobbyTableRow row)
    {
        return new OnlineLobbyTableDisplayRow
        {
            RoomId = row.RoomId,
            TableId = row.TableId,
            RulesetId = row.RulesetId,
            TableState = row.TableState,
            SeatsOccupied = row.SeatsOccupied,
            MaxSeats = row.MaxSeats,
            SpectatorCount = row.SpectatorCount,
            LastServerSeq = row.LastServerSeq,
            UpdatedUtc = row.UpdatedUtc,
            SeatSummary = string.Join(", ", row.SeatSummaries.Select(s => $"#{s.SeatIndex}:{s.PlayerLabel}{(s.Ready ? ":ready" : "")}"))
        };
    }

    public static IReadOnlyList<OnlineLobbyTableDisplayRow> FromSnapshot(OnlineLobbySnapshot? snapshot)
    {
        return snapshot?.Tables.Select(FromProtocol).ToArray() ?? Array.Empty<OnlineLobbyTableDisplayRow>();
    }
}
