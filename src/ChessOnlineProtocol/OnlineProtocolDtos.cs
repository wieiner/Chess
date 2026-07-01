using System.Text.Json.Serialization;

namespace ChessOnlineProtocol;

public static class OnlineProtocolVersion
{
    public const string ProtocolId = "chess3d.relay.v1";
    public const string ProtocolVersion = "0.1";
    public const int MaxMessageBytes = 65536;
}

public static class OnlineMessageTypes
{
    public const string Hello = "Hello";
    public const string CreateRoom = "CreateRoom";
    public const string JoinRoom = "JoinRoom";
    public const string LeaveRoom = "LeaveRoom";
    public const string ListRooms = "ListRooms";
    public const string CreateTable = "CreateTable";
    public const string JoinTableSeat = "JoinTableSeat";
    public const string LeaveTableSeat = "LeaveTableSeat";
    public const string Ready = "Ready";
    public const string StartGame = "StartGame";
    public const string SubmitAction = "SubmitAction";
    public const string RequestSnapshot = "RequestSnapshot";
    public const string RequestActionLog = "RequestActionLog";
    public const string RequestResumeMatch = "RequestResumeMatch";
    public const string JoinSpectator = "JoinSpectator";
    public const string RequestDiagnostics = "RequestDiagnostics";
    public const string RequestLegalPreview = "RequestLegalPreview";
    public const string JoinMatchmaking = "JoinMatchmaking";
    public const string CancelMatchmaking = "CancelMatchmaking";
    public const string GetMatchmakingStatus = "GetMatchmakingStatus";
    public const string ListMatchmakingQueues = "ListMatchmakingQueues";
    public const string Ping = "Ping";
    public const string ChatMessage = "ChatMessage";
    public const string Welcome = "Welcome";
    public const string RoomCreated = "RoomCreated";
    public const string RoomJoined = "RoomJoined";
    public const string RoomLeft = "RoomLeft";
    public const string RoomList = "RoomList";
    public const string TableCreated = "TableCreated";
    public const string TableState = "TableState";
    public const string SeatAssigned = "SeatAssigned";
    public const string GameStarted = "GameStarted";
    public const string ActionAccepted = "ActionAccepted";
    public const string ActionRejected = "ActionRejected";
    public const string AuthoritativeSnapshot = "AuthoritativeSnapshot";
    public const string ActionLogChunk = "ActionLogChunk";
    public const string ResumeMatchResult = "ResumeMatchResult";
    public const string JoinSpectatorResult = "JoinSpectatorResult";
    public const string LegalPreviewResult = "LegalPreviewResult";
    public const string ResyncRequired = "ResyncRequired";
    public const string Pong = "Pong";
    public const string Error = "Error";
    public const string Diagnostics = "Diagnostics";
    public const string ChatBroadcast = "ChatBroadcast";
    public const string MatchmakingJoined = "MatchmakingJoined";
    public const string MatchmakingCancelled = "MatchmakingCancelled";
    public const string MatchmakingStatus = "MatchmakingStatus";
    public const string MatchFound = "MatchFound";
    public const string MatchmakingError = "MatchmakingError";
}

public static class OnlineActionKinds
{
    public const string NormalMove = "NormalMove";
    public const string ReserveRestore = "ReserveRestore";
    public const string RubikLayerTurn = "RubikLayerTurn";
    public const string HodgeProjectedMove = "HodgeProjectedMove";
    public const string AiActionRequest = "AiActionRequest";
    public const string Resign = "Resign";
    public const string OfferDraw = "OfferDraw";
}

public static class OnlineRejectReasons
{
    public const string None = "none";
    public const string InvalidJson = "invalidJson";
    public const string OversizedMessage = "oversizedMessage";
    public const string WrongProtocol = "wrongProtocol";
    public const string UnsupportedVersion = "unsupportedVersion";
    public const string UnknownMessageType = "unknownMessageType";
    public const string MissingRequiredField = "missingRequiredField";
    public const string RoomNotFound = "roomNotFound";
    public const string TableNotFound = "tableNotFound";
    public const string SeatOccupied = "seatOccupied";
    public const string PlayerNotSeated = "playerNotSeated";
    public const string WrongActor = "wrongActor";
    public const string TableNotInGame = "tableNotInGame";
    public const string UnsupportedRuleset = "unsupportedRuleset";
    public const string UnsupportedAction = "unsupportedAction";
    public const string IllegalAction = "illegalAction";
    public const string StaleStateHash = "staleStateHash";
    public const string InternalError = "internalError";
    public const string AlreadyQueued = "alreadyQueued";
    public const string NotQueued = "notQueued";
}

public static class OnlineResumeFailureReasons
{
    public const string None = "none";
    public const string NotAuthenticated = "notAuthenticated";
    public const string TableNotFound = "tableNotFound";
    public const string PlayerNotInTable = "playerNotInTable";
    public const string RulesetMismatch = "rulesetMismatch";
    public const string StaleState = "staleState";
    public const string TableNotActive = "tableNotActive";
    public const string CannotResumeAfterServerRestartYet = "cannotResumeAfterServerRestartYet";
}

public static class OnlineSpectatorFailureReasons
{
    public const string None = "none";
    public const string NotAuthenticated = "notAuthenticated";
    public const string RoomNotFound = "roomNotFound";
    public const string TableNotFound = "tableNotFound";
    public const string RulesetMismatch = "rulesetMismatch";
    public const string TableNotActive = "tableNotActive";
    public const string Unsupported = "unsupported";
}

public sealed class OnlineMessageEnvelope
{
    public string ProtocolId { get; set; } = OnlineProtocolVersion.ProtocolId;
    public string ProtocolVersion { get; set; } = OnlineProtocolVersion.ProtocolVersion;
    public string MessageType { get; set; } = "";
    public string MessageId { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string SessionToken { get; set; } = "";
    public long ClientSeq { get; set; }
    public long ServerSeq { get; set; }
    public string SentAtUtc { get; set; } = "";
}

public sealed class OnlineProtocolMessage
{
    public OnlineMessageEnvelope Envelope { get; set; } = new();
    public OnlineActionCommand? Action { get; set; }
    public OnlineRoomCommand? Room { get; set; }
    public OnlineTableCommand? Table { get; set; }
    public OnlineSnapshot? Snapshot { get; set; }
    public OnlineActionLogChunk? ActionLog { get; set; }
    public OnlineResumeRequest? ResumeRequest { get; set; }
    public OnlineResumeResult? ResumeResult { get; set; }
    public OnlineJoinSpectatorRequest? SpectatorRequest { get; set; }
    public OnlineJoinSpectatorResult? SpectatorResult { get; set; }
    public OnlineLegalPreviewRequest? LegalPreviewRequest { get; set; }
    public OnlineLegalPreviewResult? LegalPreview { get; set; }
    public OnlineMatchmakingCommand? Matchmaking { get; set; }
    public OnlineMatchmakingStatus? MatchmakingStatus { get; set; }
    public OnlineDiagnostics? Diagnostics { get; set; }
    public OnlineError? Error { get; set; }
    public string Text { get; set; } = "";
}

public sealed class OnlineMatchmakingCommand
{
    public string TicketId { get; set; } = "";
    public string RequestedRulesetId { get; set; } = "";
    public int PreferredSeat { get; set; }
    public int ExpireSeconds { get; set; } = 120;
}

public sealed class OnlineMatchmakingStatus
{
    public string TicketId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string RequestedRulesetId { get; set; } = "";
    public string State { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public int SeatIndex { get; set; }
    public int QueueCount { get; set; }
    public string ErrorCode { get; set; } = "";
    public string ErrorText { get; set; } = "";
    public List<OnlineMatchmakingTicket> Tickets { get; set; } = new();
}

public sealed class OnlineMatchmakingTicket
{
    public string TicketId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string RequestedRulesetId { get; set; } = "";
    public string State { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public int SeatIndex { get; set; }
    public string CreatedAtUtc { get; set; } = "";
    public string ExpiresAtUtc { get; set; } = "";
}

public sealed class OnlineRoomCommand
{
    public string RoomId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int MaxTables { get; set; } = 8;
}

public sealed class OnlineTableCommand
{
    public string TableId { get; set; } = "";
    public string RulesetId { get; set; } = "";
    public int SeatIndex { get; set; }
    public bool Ready { get; set; }
}

public sealed class OnlineActionCommand
{
    public string ActionKind { get; set; } = "";
    public int ActorSide { get; set; }
    public int MacroPlayer { get; set; }
    public string ExpectedStateHashBefore { get; set; } = "";
    public int FromX { get; set; }
    public int FromY { get; set; }
    public int FromZ { get; set; }
    public int ToX { get; set; }
    public int ToY { get; set; }
    public int ToZ { get; set; }
    public int PromotionType { get; set; }
    public int Side { get; set; }
    public int PieceType { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public int PrimarySide { get; set; }
    public int Axis { get; set; }
    public int Layer { get; set; }
    public int QuarterTurns { get; set; }
    public int Depth { get; set; }
    public int NodeLimit { get; set; }
    public int TimeLimitMs { get; set; }
}

public sealed class OnlineLegalPreviewRequest
{
    public string PlayerId { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public int SourceX { get; set; }
    public int SourceY { get; set; }
    public int SourceZ { get; set; }
    public int ActorSide { get; set; }
    public int MacroPlayer { get; set; }
    public string ExpectedStateHash { get; set; } = "";
    public string ActionKindFilter { get; set; } = "";
}

public sealed class OnlineResumeRequest
{
    public string PlayerId { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public int SeatIndex { get; set; }
    public string ExpectedRulesetId { get; set; } = "";
    public string LastKnownStateHash { get; set; } = "";
    public long LastKnownServerSeq { get; set; }
}

public sealed class OnlineResumeResult
{
    public bool Success { get; set; }
    public string FailureReason { get; set; } = OnlineResumeFailureReasons.None;
    public string FailureText { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public int SeatIndex { get; set; }
    public string RulesetId { get; set; } = "";
    public OnlineSnapshot? Snapshot { get; set; }
    public OnlineActionLogChunk? ActionLog { get; set; }
    public List<OnlineResumeCandidate> Candidates { get; set; } = new();
}

public sealed class OnlineResumeCandidate
{
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public int SeatIndex { get; set; }
    public string RulesetId { get; set; } = "";
    public string StateHash { get; set; } = "";
    public long ServerSeq { get; set; }
    public string TableState { get; set; } = "";
    public string UpdatedAtUtc { get; set; } = "";
}

public sealed class OnlineJoinSpectatorRequest
{
    public string PlayerId { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public string ExpectedRulesetId { get; set; } = "";
    public long LastKnownServerSeq { get; set; }
}

public sealed class OnlineJoinSpectatorResult
{
    public bool Success { get; set; }
    public string FailureReason { get; set; } = OnlineSpectatorFailureReasons.None;
    public string FailureText { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public string RulesetId { get; set; } = "";
    public string SpectatorId { get; set; } = "";
    public OnlineSpectatorState State { get; set; } = new();
    public OnlineSnapshot? Snapshot { get; set; }
    public OnlineActionLogChunk? ActionLog { get; set; }
}

public sealed class OnlineSpectatorState
{
    public bool IsSpectator { get; set; }
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public string RulesetId { get; set; } = "";
    public string SpectatorId { get; set; } = "";
    public string ViewerPlayerId { get; set; } = "";
    public long LastKnownServerSeq { get; set; }
    public string SubmitDisabledReason { get; set; } = "Spectator mode is read-only.";
}

public sealed class OnlineLegalPreviewResult
{
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public string RulesetId { get; set; } = "";
    public string StateHash { get; set; } = "";
    public long ServerSeq { get; set; }
    public int SourceX { get; set; }
    public int SourceY { get; set; }
    public int SourceZ { get; set; }
    public int ActorSide { get; set; }
    public int MacroPlayer { get; set; }
    public bool IsStale { get; set; }
    public string NoLegalActionReason { get; set; } = "";
    public List<OnlineLegalActionOption> Options { get; set; } = new();
    public OnlineLegalPreviewError? Error { get; set; }
}

public sealed class OnlineLegalActionOption
{
    public string ActionKind { get; set; } = "";
    public int ActorSide { get; set; }
    public int MacroPlayer { get; set; }
    public OnlineLegalTarget From { get; set; } = new();
    public OnlineLegalTarget To { get; set; } = new();
    public int PromotionType { get; set; }
    public int Side { get; set; }
    public int PieceType { get; set; }
    public OnlineLegalTarget ReserveTarget { get; set; } = new();
    public int PrimarySide { get; set; }
    public int Axis { get; set; }
    public int Layer { get; set; }
    public int QuarterTurns { get; set; }
    public string Notation { get; set; } = "";
    public string DisplayLabel { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Capability { get; set; } = "";
    public int Flags { get; set; }
    public int PieceCode { get; set; }
    public int CapturedPieceCode { get; set; }
    public int ReasonCode { get; set; }
    public bool IsCapture { get; set; }
    public bool IsSpecial { get; set; }
    public bool IsRecommendedSafeTestAction { get; set; }
}

public sealed class OnlineLegalTarget
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
}

public sealed class OnlineLegalPreviewError
{
    public string ReasonCode { get; set; } = OnlineRejectReasons.None;
    public string ReasonText { get; set; } = "";
    public bool RequiresResync { get; set; }
}

public sealed class OnlineActionEvent
{
    public long ServerSeq { get; set; }
    public int ActionIndex { get; set; }
    public string PlayerId { get; set; } = "";
    public int SeatIndex { get; set; }
    public string ActionKind { get; set; } = "";
    public OnlineActionCommand Command { get; set; } = new();
    public string Notation { get; set; } = "";
    public string StateHashAfter { get; set; } = "";
    public string GamePhase { get; set; } = "";
    public string GameOutcome { get; set; } = "";
    public string CreatedAtUtc { get; set; } = "";
}

public sealed class OnlineSnapshot
{
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public string RulesetId { get; set; } = "";
    public string ProfileSummary { get; set; } = "";
    public long ServerSeq { get; set; }
    public string StateHash { get; set; } = "";
    public int GamePhase { get; set; }
    public int GameOutcome { get; set; }
    public string TurnSummary { get; set; } = "";
    public string SaveGameJson { get; set; } = "";
    public int ActionCount { get; set; }
    public string LastActionNotation { get; set; } = "";
}

public sealed class OnlineActionLogChunk
{
    public string RoomId { get; set; } = "";
    public string TableId { get; set; } = "";
    public long FromServerSeq { get; set; }
    public long ToServerSeq { get; set; }
    public List<OnlineActionEvent> Events { get; set; } = new();
}

public sealed class OnlineError
{
    public string ReasonCode { get; set; } = OnlineRejectReasons.None;
    public string ReasonText { get; set; } = "";
    public string StateHash { get; set; } = "";
    public long ServerSeq { get; set; }
}

public sealed class OnlineDiagnostics
{
    public string ServerCommit { get; set; } = "";
    public string ProtocolVersion { get; set; } = OnlineProtocolVersion.ProtocolVersion;
    public bool RequestLegalPreviewSupported { get; set; } = true;
    public bool RealtimeResyncSupported { get; set; } = true;
    public bool ActionLogSupported { get; set; } = true;
    public bool MatchmakingSupported { get; set; } = true;
    public bool ResumeMatchSupported { get; set; }
    public bool SpectatorModeSupported { get; set; }
    public List<string> SupportedHubMethods { get; set; } = new();
    public int RoomCount { get; set; }
    public int TableCount { get; set; }
    public int ConnectionCount { get; set; }
    public int ActiveConnectionCount { get; set; }
    public long LastServerSeq { get; set; }
    public string LastAcceptedAction { get; set; } = "";
    public string LastRejectReason { get; set; } = "";
    public string LastStateHash { get; set; } = "";
    public int LastSnapshotBytes { get; set; }
    public int ActionLogLength { get; set; }
    public int ProtocolErrorCount { get; set; }
    public int AcceptedActionCount { get; set; }
    public int RejectedActionCount { get; set; }
    public int ResyncCount { get; set; }
}

[JsonSerializable(typeof(OnlineProtocolMessage))]
[JsonSerializable(typeof(OnlineMessageEnvelope))]
[JsonSerializable(typeof(OnlineActionCommand))]
[JsonSerializable(typeof(OnlineResumeRequest))]
[JsonSerializable(typeof(OnlineResumeResult))]
[JsonSerializable(typeof(OnlineResumeCandidate))]
[JsonSerializable(typeof(OnlineJoinSpectatorRequest))]
[JsonSerializable(typeof(OnlineJoinSpectatorResult))]
[JsonSerializable(typeof(OnlineSpectatorState))]
[JsonSerializable(typeof(OnlineLegalPreviewRequest))]
[JsonSerializable(typeof(OnlineLegalPreviewResult))]
[JsonSerializable(typeof(OnlineLegalActionOption))]
[JsonSerializable(typeof(OnlineLegalTarget))]
[JsonSerializable(typeof(OnlineLegalPreviewError))]
[JsonSerializable(typeof(OnlineActionEvent))]
[JsonSerializable(typeof(OnlineSnapshot))]
[JsonSerializable(typeof(OnlineActionLogChunk))]
[JsonSerializable(typeof(OnlineMatchmakingCommand))]
[JsonSerializable(typeof(OnlineMatchmakingStatus))]
[JsonSerializable(typeof(OnlineMatchmakingTicket))]
[JsonSerializable(typeof(OnlineDiagnostics))]
internal partial class OnlineProtocolJsonContext : JsonSerializerContext
{
}
