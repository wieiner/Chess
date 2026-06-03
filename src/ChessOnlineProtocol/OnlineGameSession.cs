using ChessApp;

namespace ChessOnlineProtocol;

public sealed class OnlineGameSession : IDisposable
{
    private readonly NativeChess3DEngine _engine = new();
    private readonly RuleProfileInfo _profile;

    public OnlineGameSession(RuleProfileInfo profile, string profileRoot)
    {
        _profile = profile;
        var profilePath = Path.Combine(profileRoot, profile.FileName);
        var json = File.ReadAllText(profilePath);
        if (!_engine.LoadRuleProfileJson(json))
        {
            throw new InvalidOperationException($"Could not load Chess3D profile: {profile.FileName}");
        }
    }

    public string RulesetId => _profile.RulesetId;
    public string StateHash => _engine.GetStateHash();
    public int ActionCount => _engine.GetActionCount();
    public string LastActionNotation => _engine.GetLastActionNotation();
    public int GamePhase => _engine.GetGamePhase();
    public int GameOutcome => _engine.GetGameOutcome();
    public string TurnSummary => _engine.GetCurrentTurnSummary();

    public OnlineActionCommand? FirstLegalNormalMoveCommand(int actorSide)
    {
        var moves = _engine.GetLegalMoves();
        var move = moves.FirstOrDefault(m => m.Piece / 10 == actorSide);
        if (move.Piece == 0)
        {
            return null;
        }
        return new OnlineActionCommand
        {
            ActionKind = OnlineActionKinds.NormalMove,
            ActorSide = actorSide,
            FromX = move.FromX,
            FromY = move.FromY,
            FromZ = move.FromZ,
            ToX = move.ToX,
            ToY = move.ToY,
            ToZ = move.ToZ,
            PromotionType = move.PromotionType
        };
    }

    public OnlineActionCommand? FirstAiCandidateCommand(string preferredKind = "")
    {
        _engine.BuildAiActionCandidates();
        foreach (var candidate in _engine.GetAiActionCandidates())
        {
            var actionKind = candidate.Kind switch
            {
                1 => OnlineActionKinds.NormalMove,
                2 => OnlineActionKinds.RubikLayerTurn,
                3 => OnlineActionKinds.ReserveRestore,
                5 => OnlineActionKinds.HodgeProjectedMove,
                _ => ""
            };
            if (!string.IsNullOrWhiteSpace(preferredKind) &&
                !string.Equals(preferredKind, actionKind, StringComparison.Ordinal))
            {
                continue;
            }
            return new OnlineActionCommand
            {
                ActionKind = actionKind,
                ActorSide = candidate.Side,
                MacroPlayer = candidate.MacroPlayer,
                FromX = candidate.FromX,
                FromY = candidate.FromY,
                FromZ = candidate.FromZ,
                ToX = candidate.ToX,
                ToY = candidate.ToY,
                ToZ = candidate.ToZ,
                PromotionType = candidate.PromotionType,
                Side = candidate.Side,
                PieceType = candidate.ReservePieceType,
                X = candidate.RestoreX,
                Y = candidate.RestoreY,
                Z = candidate.RestoreZ,
                PrimarySide = candidate.PrimarySide,
                Axis = candidate.Axis,
                Layer = candidate.Layer,
                QuarterTurns = candidate.QuarterTurns
            };
        }
        return null;
    }

    public bool TryApply(OnlineActionCommand command, out string rejectReason, out string rejectText)
    {
        rejectReason = OnlineRejectReasons.None;
        rejectText = "";

        try
        {
            var ok = command.ActionKind switch
            {
                OnlineActionKinds.NormalMove => _engine.TryMakeMove(
                    command.FromX, command.FromY, command.FromZ,
                    command.ToX, command.ToY, command.ToZ,
                    command.PromotionType,
                    out _),
                OnlineActionKinds.HodgeProjectedMove => _engine.TryMakeProjectedMove(
                    command.PrimarySide != 0 ? command.PrimarySide : command.ActorSide,
                    command.FromX, command.FromY, command.FromZ,
                    command.ToX, command.ToY, command.ToZ,
                    command.PromotionType,
                    out _),
                OnlineActionKinds.RubikLayerTurn => _engine.RotateLayer(command.Axis, command.Layer, command.QuarterTurns),
                OnlineActionKinds.ReserveRestore => _engine.RestoreReservePiece(command.Side, command.PieceType, command.X, command.Y, command.Z),
                OnlineActionKinds.AiActionRequest => false,
                _ => false
            };

            if (ok)
            {
                return true;
            }

            rejectReason = command.ActionKind == OnlineActionKinds.AiActionRequest
                ? OnlineRejectReasons.UnsupportedAction
                : OnlineRejectReasons.IllegalAction;
            rejectText = BuildRejectText(command);
            return false;
        }
        catch (Exception ex)
        {
            rejectReason = OnlineRejectReasons.InternalError;
            rejectText = ex.Message;
            return false;
        }
    }

    public OnlineSnapshot CreateSnapshot(string roomId, string tableId, long serverSeq)
    {
        return new OnlineSnapshot
        {
            RoomId = roomId,
            TableId = tableId,
            RulesetId = _profile.RulesetId,
            ProfileSummary = $"{_profile.DisplayName}; goal={_engine.GetGoalProfileType()}; capture={_engine.GetCaptureProfileType()}; layer={_engine.GetLayerTurnProfileType()}",
            ServerSeq = serverSeq,
            StateHash = StateHash,
            GamePhase = GamePhase,
            GameOutcome = GameOutcome,
            TurnSummary = TurnSummary,
            SaveGameJson = _engine.ExportSaveGameJson(),
            ActionCount = ActionCount,
            LastActionNotation = LastActionNotation
        };
    }

    public void Dispose()
    {
        _engine.Dispose();
    }

    private string BuildRejectText(OnlineActionCommand command)
    {
        return command.ActionKind switch
        {
            OnlineActionKinds.NormalMove => _engine.GetLastMoveLegalityReason(),
            OnlineActionKinds.HodgeProjectedMove => _engine.GetLastProjectionError(),
            OnlineActionKinds.RubikLayerTurn => _engine.GetLayerTurnResultName(_engine.GetLastLayerTurnInfo().ResultCode),
            OnlineActionKinds.ReserveRestore => _engine.GetLastReserveRestoreInfo(),
            OnlineActionKinds.AiActionRequest => "Server-side AI search is disabled in P3E by default.",
            _ => "Unsupported action kind."
        };
    }
}

public sealed record RuleProfileInfo(string FileName, string RulesetId, string DisplayName, int SeatCount);

public static class RuleProfileCatalog
{
    private static readonly RuleProfileInfo[] Profiles =
    {
        new("classic_six_side_3d_v0_1.json", "classic-six-side-3d-8x8x8-v0.1", "Classic Six-Side 3D", 6),
        new("single_side_3d_v0_1.json", "single-side-3d-8x8x8-v0.1", "Single-Side Training", 1),
        new("asgard_convergence_3d_v0_1.json", "asgard-convergence-3d-8x8x8-v0.1", "Asgard / Meru Convergence", 6),
        new("rubik_convergence_3d_v0_1.json", "rubik-convergence-3d-8x8x8-v0.1", "Rubik Convergence", 6),
        new("hodge_projection_duel_3d_v0_1.json", "hodge-projection-duel-3d-8x8x8-v0.1", "Hodge Projection Duel", 2)
    };

    public static IReadOnlyList<RuleProfileInfo> All => Profiles;

    public static bool TryResolve(string profileRoot, string idOrFileName, out RuleProfileInfo profile)
    {
        profile = Profiles.FirstOrDefault(p =>
            string.Equals(p.RulesetId, idOrFileName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.FileName, idOrFileName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileName(p.FileName), Path.GetFileName(idOrFileName), StringComparison.OrdinalIgnoreCase))!;
        if (profile == null)
        {
            return false;
        }
        return File.Exists(Path.Combine(profileRoot, profile.FileName));
    }

    public static RuleProfileInfo ResolveRequired(string profileRoot, string idOrFileName)
    {
        if (!TryResolve(profileRoot, idOrFileName, out var profile))
        {
            throw new InvalidOperationException($"Unsupported or missing Chess3D RuleProfile: {idOrFileName}");
        }
        return profile;
    }
}
