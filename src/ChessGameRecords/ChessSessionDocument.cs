using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessGameRecords;

public sealed record ChessSessionSquare(int File, int Rank);

public sealed record ChessSessionClock(
    long? WhiteRemainingMilliseconds,
    long? BlackRemainingMilliseconds,
    long? MoveElapsedMilliseconds);

public sealed record ChessSessionMove(
    int PlyIndex,
    int FullmoveNumber,
    int Side,
    ChessSessionSquare From,
    ChessSessionSquare To,
    int MovedPiece,
    int CapturedPiece,
    int PromotionPiece,
    ChessCastleKind Castle,
    bool IsEnPassant,
    bool IsCapture,
    bool IsCheck,
    bool IsCheckmate,
    string PreMoveFen,
    string PostMoveFen,
    string Uci,
    string San,
    ChessSessionClock? Clock,
    string? Comment);

public sealed record ChessSessionHeaders(
    string Event,
    string Site,
    string Date,
    string Round,
    string White,
    string Black,
    IReadOnlyDictionary<string, string> AdditionalTags);

public sealed record ChessSessionGame(
    ChessSessionHeaders Headers,
    IReadOnlyList<ChessSessionMove> Moves,
    ChessGameResult Result,
    ChessTerminationReason Termination);

public enum ChessSessionBoardOrientation { White, Black }
public enum ChessSessionUiMode { Board2D, Board3D }

public sealed record ChessSessionPresentation(
    ChessSessionBoardOrientation BoardOrientation,
    string PieceTheme,
    string? ModelSetId,
    ChessSessionUiMode UiMode);

public sealed record ChessSessionEngineOptions(
    bool Enabled,
    string Backend,
    int MaxDepth,
    int TimeLimitMilliseconds,
    long NodeLimit,
    int Threads);

public sealed record ChessSessionAutosaveMetadata(
    bool IsAutosave,
    Guid? SourceSessionId,
    long Sequence,
    DateTimeOffset SavedUtc);

public sealed record ChessSessionDocument(
    string Format,
    string Version,
    Guid SessionId,
    DateTimeOffset CreatedUtc,
    DateTimeOffset ModifiedUtc,
    string StartingFen,
    string CurrentFen,
    ChessSessionGame Game,
    ChessSessionPresentation Presentation,
    ChessSessionEngineOptions Engine,
    bool Dirty,
    ChessSessionAutosaveMetadata? Autosave = null)
{
    public const string CurrentFormat = "chess2d-session";
    public const string CurrentVersion = "1.0";

    public static ChessSessionDocument FromGame(
        ChessGameRecord game,
        string currentFen,
        ChessSessionPresentation presentation,
        ChessSessionEngineOptions engine,
        bool dirty,
        Guid? sessionId = null,
        ChessSessionAutosaveMetadata? autosave = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        var headers = new ChessSessionHeaders(
            game.Headers.Event, game.Headers.Site, game.Headers.Date, game.Headers.Round,
            game.Headers.White, game.Headers.Black,
            ToSortedDictionary(game.Headers.AdditionalTags));
        var moves = game.Moves.Select(ToSessionMove).ToArray();
        return new ChessSessionDocument(
            CurrentFormat, CurrentVersion, sessionId ?? Guid.NewGuid(), game.CreatedUtc, DateTimeOffset.UtcNow,
            game.InitialPosition.Fen, currentFen,
            new ChessSessionGame(headers, moves, game.Result, game.Termination),
            presentation, engine, dirty, autosave);
    }

    public ChessGameRecord ToGameRecord()
    {
        var headers = new ChessGameHeaders(
            Game.Headers.Event, Game.Headers.Site, Game.Headers.Date, Game.Headers.Round,
            Game.Headers.White, Game.Headers.Black,
            ToSortedDictionary(Game.Headers.AdditionalTags));
        var moves = Game.Moves.Select(ToMoveRecord).ToArray();
        return new ChessGameRecord(headers, ChessPositionRecord.FromFen(StartingFen), moves,
            Array.Empty<ChessMoveRecord>(), Game.Result, Game.Termination, CreatedUtc, ModifiedUtc);
    }

    private static ChessSessionMove ToSessionMove(ChessMoveRecord move) => new(
        move.PlyIndex, move.FullmoveNumber, move.Side,
        new ChessSessionSquare(move.From.File, move.From.Rank), new ChessSessionSquare(move.To.File, move.To.Rank),
        move.MovedPiece, move.CapturedPiece, move.PromotionPiece, move.Castle,
        move.IsEnPassant, move.IsCapture, move.IsCheck, move.IsCheckmate,
        move.PreMoveFen, move.PostMoveFen, move.Uci, move.San,
        move.Clock is null ? null : new ChessSessionClock(
            ToMilliseconds(move.Clock.WhiteRemaining), ToMilliseconds(move.Clock.BlackRemaining),
            ToMilliseconds(move.Clock.MoveElapsed)),
        move.Comment);

    private static ChessMoveRecord ToMoveRecord(ChessSessionMove move) => new(
        move.PlyIndex, move.FullmoveNumber, move.Side,
        new ChessSquare(move.From.File, move.From.Rank), new ChessSquare(move.To.File, move.To.Rank),
        move.MovedPiece, move.CapturedPiece, move.PromotionPiece, move.Castle,
        move.IsEnPassant, move.IsCapture, move.IsCheck, move.IsCheckmate,
        move.PreMoveFen, move.PostMoveFen, move.Uci, move.San,
        move.Clock is null ? null : new ChessClockSnapshot(
            ToTimeSpan(move.Clock.WhiteRemainingMilliseconds), ToTimeSpan(move.Clock.BlackRemainingMilliseconds),
            ToTimeSpan(move.Clock.MoveElapsedMilliseconds)),
        move.Comment, null);

    private static long? ToMilliseconds(TimeSpan? value) => value is null ? null : checked((long)value.Value.TotalMilliseconds);
    private static TimeSpan? ToTimeSpan(long? value) => value is null ? null : TimeSpan.FromMilliseconds(value.Value);

    private static SortedDictionary<string, string> ToSortedDictionary(IReadOnlyDictionary<string, string> source)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in source) result[key] = value;
        return result;
    }
}

public sealed record ChessSessionValidationResult(bool Success, string Error)
{
    public static ChessSessionValidationResult Ok { get; } = new(true, string.Empty);
    public static ChessSessionValidationResult Fail(string error) => new(false, error);
}

public static class ChessSessionValidator
{
    public const int MaxMoves = 8192;

    public static ChessSessionValidationResult Validate(ChessSessionDocument? document)
    {
        if (document is null) return ChessSessionValidationResult.Fail("Session document is missing.");
        if (document.Format != ChessSessionDocument.CurrentFormat || document.Version != ChessSessionDocument.CurrentVersion)
            return ChessSessionValidationResult.Fail("Session format or version is not supported.");
        if (document.SessionId == Guid.Empty) return ChessSessionValidationResult.Fail("Session ID is empty.");
        if (document.CreatedUtc > document.ModifiedUtc) return ChessSessionValidationResult.Fail("Session timestamps are inconsistent.");
        try
        {
            _ = ChessPositionRecord.FromFen(document.StartingFen);
            _ = ChessPositionRecord.FromFen(document.CurrentFen);
        }
        catch (ArgumentException ex)
        {
            return ChessSessionValidationResult.Fail(ex.Message);
        }
        if (document.Game is null || document.Game.Headers is null || document.Game.Moves is null)
            return ChessSessionValidationResult.Fail("Session game data is incomplete.");
        if (document.Game.Moves.Count > MaxMoves) return ChessSessionValidationResult.Fail("Session move limit exceeded.");
        if ((document.Game.Result == ChessGameResult.Ongoing) != (document.Game.Termination == ChessTerminationReason.None))
            return ChessSessionValidationResult.Fail("Game result and termination are inconsistent.");

        var expectedFen = document.StartingFen;
        for (var index = 0; index < document.Game.Moves.Count; index++)
        {
            var move = document.Game.Moves[index];
            if (move.PlyIndex != index || move.Side is not (1 or -1) || move.FullmoveNumber < 1 ||
                move.From is null || move.To is null || move.From.File is < 0 or > 7 || move.From.Rank is < 0 or > 7 ||
                move.To.File is < 0 or > 7 || move.To.Rank is < 0 or > 7 || move.PreMoveFen != expectedFen ||
                string.IsNullOrWhiteSpace(move.San) || string.IsNullOrWhiteSpace(move.Uci))
                return ChessSessionValidationResult.Fail($"Move {index + 1} breaks the structured game chain.");
            try { _ = ChessPositionRecord.FromFen(move.PostMoveFen); }
            catch (ArgumentException) { return ChessSessionValidationResult.Fail($"Move {index + 1} has invalid post-move FEN."); }
            expectedFen = move.PostMoveFen;
        }
        if (document.CurrentFen != expectedFen) return ChessSessionValidationResult.Fail("Current FEN does not match the final move.");
        if (document.Presentation is null || !IsSemanticId(document.Presentation.PieceTheme) ||
            (document.Presentation.ModelSetId is not null && !IsSemanticId(document.Presentation.ModelSetId)))
            return ChessSessionValidationResult.Fail("Presentation identifiers must be semantic names, not paths.");
        if (document.Engine is null || !IsSemanticId(document.Engine.Backend) || document.Engine.MaxDepth is < 0 or > 128 ||
            document.Engine.TimeLimitMilliseconds is < 0 or > 3_600_000 || document.Engine.NodeLimit < 0 ||
            document.Engine.Threads is < 1 or > 256)
            return ChessSessionValidationResult.Fail("Engine options are outside supported bounds.");
        if (document.Autosave is { IsAutosave: false } || document.Autosave is { Sequence: < 1 })
            return ChessSessionValidationResult.Fail("Autosave metadata is invalid.");
        return ChessSessionValidationResult.Ok;
    }

    private static bool IsSemanticId(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 128 && !Path.IsPathRooted(value) &&
        value.IndexOfAny(new[] { '/', '\\', ':' }) < 0;
}

public sealed record ChessSessionSerializationResult(
    bool Success,
    ChessSessionDocument? Document,
    string Json,
    string Hash,
    string Error);

public static class ChessSessionSerializer
{
    public const int MaxUtf8Bytes = 4 * 1024 * 1024;
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static ChessSessionSerializationResult Serialize(ChessSessionDocument document)
    {
        var validation = ChessSessionValidator.Validate(document);
        if (!validation.Success) return new(false, null, string.Empty, string.Empty, validation.Error);
        try
        {
            var normalized = Normalize(document);
            var json = JsonSerializer.Serialize(normalized, Options);
            var bytes = Encoding.UTF8.GetBytes(json);
            if (bytes.Length > MaxUtf8Bytes) return new(false, null, string.Empty, string.Empty, "Session exceeds the UTF-8 size limit.");
            return new(true, normalized, json, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), string.Empty);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or OverflowException)
        {
            return new(false, null, string.Empty, string.Empty, ex.Message);
        }
    }

    public static ChessSessionSerializationResult Deserialize(string json)
    {
        if (json is null) return new(false, null, string.Empty, string.Empty, "Session JSON is missing.");
        var bytes = Encoding.UTF8.GetByteCount(json);
        if (bytes > MaxUtf8Bytes) return new(false, null, string.Empty, string.Empty, "Session exceeds the UTF-8 size limit.");
        try
        {
            var document = JsonSerializer.Deserialize<ChessSessionDocument>(json, Options);
            var validation = ChessSessionValidator.Validate(document);
            if (!validation.Success) return new(false, null, string.Empty, string.Empty, validation.Error);
            return Serialize(document!);
        }
        catch (JsonException ex)
        {
            return new(false, null, string.Empty, string.Empty, $"Invalid session JSON: {ex.Message}");
        }
    }

    private static ChessSessionDocument Normalize(ChessSessionDocument source)
    {
        var headers = source.Game.Headers with
        {
            AdditionalTags = ToSortedDictionary(source.Game.Headers.AdditionalTags)
        };
        return source with
        {
            Game = source.Game with { Headers = headers, Moves = source.Game.Moves.ToArray() }
        };
    }

    private static SortedDictionary<string, string> ToSortedDictionary(IReadOnlyDictionary<string, string> source)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in source) result[key] = value;
        return result;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            WriteIndented = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

public enum ChessSessionFileStage
{
    AfterTempWrite,
    AfterFlush,
    AfterVerify,
    BeforeReplace,
    AfterReplace
}

public sealed record ChessSessionFileResult(bool Success, ChessSessionDocument? Document, string Hash, string Error);

public sealed class ChessSessionFileService
{
    private readonly Action<ChessSessionFileStage>? _stageHook;

    public ChessSessionFileService(Action<ChessSessionFileStage>? stageHook = null) => _stageHook = stageHook;

    public ChessSessionFileResult Load(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists) return new(false, null, string.Empty, "Session file does not exist.");
            if (info.Length > ChessSessionSerializer.MaxUtf8Bytes) return new(false, null, string.Empty, "Session exceeds the UTF-8 size limit.");
            var result = ChessSessionSerializer.Deserialize(File.ReadAllText(path, Encoding.UTF8));
            return new(result.Success, result.Document, result.Hash, result.Error);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new(false, null, string.Empty, ex.Message);
        }
    }

    public ChessSessionFileResult Save(string path, ChessSessionDocument document, bool keepBackup = true)
    {
        var serialized = ChessSessionSerializer.Serialize(document);
        if (!serialized.Success) return new(false, null, string.Empty, serialized.Error);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)!;
        var temporary = fullPath + ".tmp";
        var backup = fullPath + ".bak";
        try
        {
            Directory.CreateDirectory(directory);
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096,
                       FileOptions.WriteThrough))
            {
                var bytes = new UTF8Encoding(false).GetBytes(serialized.Json);
                stream.Write(bytes);
                _stageHook?.Invoke(ChessSessionFileStage.AfterTempWrite);
                stream.Flush(flushToDisk: true);
                _stageHook?.Invoke(ChessSessionFileStage.AfterFlush);
            }
            var verified = Load(temporary);
            if (!verified.Success || verified.Hash != serialized.Hash)
                throw new IOException($"Temporary session verification failed: {verified.Error}");
            _stageHook?.Invoke(ChessSessionFileStage.AfterVerify);
            _stageHook?.Invoke(ChessSessionFileStage.BeforeReplace);
            if (File.Exists(fullPath))
            {
                if (keepBackup)
                {
                    File.Replace(temporary, fullPath, backup, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(temporary, fullPath, overwrite: true);
                }
            }
            else
            {
                File.Move(temporary, fullPath);
            }
            _stageHook?.Invoke(ChessSessionFileStage.AfterReplace);
            return new(true, serialized.Document, serialized.Hash, string.Empty);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            TryDelete(temporary);
            return new(false, null, string.Empty, ex.Message);
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { }
    }
}
