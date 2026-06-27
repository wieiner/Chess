using ChessOnlineProtocol;

namespace ChessOnlineClient;

public sealed class OnlineSeatTurnState
{
    public string RulesetId { get; init; } = "";
    public string PrimaryPlayerId { get; init; } = "";
    public string OpponentPlayerId { get; init; } = "";
    public int PrimarySeatIndex { get; init; }
    public int OpponentSeatIndex { get; init; }
    public int PrimarySideId { get; init; }
    public int PrimaryMacroPlayer { get; init; }
    public int CurrentSide { get; init; }
    public int CurrentMacroPlayer { get; init; }
    public int CurrentTurnKind { get; init; }
    public bool IsHodge { get; init; }
    public bool HasMatch { get; init; }
    public bool HasSnapshot { get; init; }
    public bool CanPrimaryAct { get; init; }
    public string DisabledReason { get; init; } = "";
    public string Summary { get; init; } = "Turn: no online match.";

    public static OnlineSeatTurnState Empty(string reason = "no online match") => new()
    {
        DisabledReason = reason,
        Summary = $"Turn: canAct=no reason={reason}"
    };

    public static OnlineSeatTurnState FromMatch(
        string rulesetId,
        string primaryPlayerId,
        string opponentPlayerId,
        int primarySeatIndex,
        int opponentSeatIndex,
        OnlineChess3DBoardSnapshot? board)
    {
        var isHodge = IsHodgeRuleset(rulesetId);
        var hasMatch = primarySeatIndex > 0;
        var hasSnapshot = board != null;
        var currentSide = board?.CurrentSide ?? 0;
        var currentMacro = board?.CurrentMacroPlayer ?? 0;
        var currentTurnKind = board?.CurrentTurnKind ?? 0;
        var primarySide = isHodge ? 0 : primarySeatIndex;
        var primaryMacro = isHodge ? primarySeatIndex : 0;
        var canAct = false;
        var reason = "";

        if (!hasMatch)
        {
            reason = "no primary seat assigned";
        }
        else if (!hasSnapshot)
        {
            reason = "no authoritative snapshot";
        }
        else if (isHodge)
        {
            canAct = currentMacro != 0 && currentMacro == primaryMacro;
            reason = canAct ? "" : $"waiting for macro-player {currentMacro}";
        }
        else
        {
            canAct = currentSide != 0 && currentSide == primarySide;
            reason = canAct ? "" : $"waiting for side {currentSide}";
        }

        var primaryActor = isHodge ? $"macro={primaryMacro}" : $"side={primarySide}";
        var currentActor = isHodge ? $"currentMacro={currentMacro}" : $"currentSide={currentSide}";
        var summary = $"Turn: me={ShortId(primaryPlayerId)} opponent={ShortId(opponentPlayerId)} " +
            $"seat={DisplaySeat(primarySeatIndex)} opponentSeat={DisplaySeat(opponentSeatIndex)} " +
            $"{primaryActor} {currentActor} turnKind={currentTurnKind} canAct={(canAct ? "yes" : "no")}";

        if (!canAct && !string.IsNullOrWhiteSpace(reason))
        {
            summary += $" reason={reason}";
        }

        return new OnlineSeatTurnState
        {
            RulesetId = rulesetId,
            PrimaryPlayerId = primaryPlayerId,
            OpponentPlayerId = opponentPlayerId,
            PrimarySeatIndex = primarySeatIndex,
            OpponentSeatIndex = opponentSeatIndex,
            PrimarySideId = primarySide,
            PrimaryMacroPlayer = primaryMacro,
            CurrentSide = currentSide,
            CurrentMacroPlayer = currentMacro,
            CurrentTurnKind = currentTurnKind,
            IsHodge = isHodge,
            HasMatch = hasMatch,
            HasSnapshot = hasSnapshot,
            CanPrimaryAct = canAct,
            DisabledReason = reason,
            Summary = summary
        };
    }

    public static OnlineSeatTurnState FromStatus(
        OnlineMatchmakingStatus? status,
        string primaryPlayerId,
        string opponentPlayerId,
        OnlineChess3DBoardSnapshot? board)
    {
        var primarySeat = FindSeat(status, primaryPlayerId);
        var opponentSeat = FindSeat(status, opponentPlayerId);
        var rulesetId = board?.RulesetId ??
            status?.Tickets.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.RequestedRulesetId))?.RequestedRulesetId ??
            status?.RequestedRulesetId ??
            "";

        return FromMatch(rulesetId, primaryPlayerId, opponentPlayerId, primarySeat, opponentSeat, board);
    }

    public static int FindSeat(OnlineMatchmakingStatus? status, string playerId)
    {
        if (status == null || string.IsNullOrWhiteSpace(playerId))
        {
            return 0;
        }

        var ticketSeat = status.Tickets.FirstOrDefault(t =>
            string.Equals(t.PlayerId, playerId, StringComparison.OrdinalIgnoreCase))?.SeatIndex ?? 0;
        if (ticketSeat > 0)
        {
            return ticketSeat;
        }

        return string.Equals(status.PlayerId, playerId, StringComparison.OrdinalIgnoreCase)
            ? status.SeatIndex
            : 0;
    }

    public static bool IsHodgeRuleset(string rulesetId) =>
        rulesetId.Contains("hodge-projection-duel", StringComparison.OrdinalIgnoreCase);

    private static string DisplaySeat(int seat) => seat > 0 ? seat.ToString() : "none";

    private static string ShortId(string value) =>
        string.IsNullOrWhiteSpace(value) ? "none" : value.Length <= 8 ? value : value[..8];
}
