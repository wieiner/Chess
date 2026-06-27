using System.Text.Json;
using ChessOnlineProtocol;

namespace ChessOnlineClient;

public sealed class OnlineChess3DBoardSnapshot
{
    private readonly OnlineChess3DBoardCell[] _cells;

    public OnlineChess3DBoardSnapshot(
        string rulesetId,
        string roomId,
        string tableId,
        long serverSeq,
        string stateHash,
        int actionCount,
        string lastActionNotation,
        int width,
        int height,
        int depth,
        int currentSide,
        int currentMacroPlayer,
        int currentTurnKind,
        IReadOnlyList<int> projectedBoard)
    {
        if (width <= 0 || height <= 0 || depth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Board dimensions must be positive.");
        }

        var expected = width * height * depth;
        if (projectedBoard.Count != expected)
        {
            throw new ArgumentException($"Projected board must contain {expected} cells.", nameof(projectedBoard));
        }

        RulesetId = rulesetId;
        RoomId = roomId;
        TableId = tableId;
        ServerSeq = serverSeq;
        StateHash = stateHash;
        ActionCount = actionCount;
        LastActionNotation = lastActionNotation;
        Width = width;
        Height = height;
        Depth = depth;
        CurrentSide = currentSide;
        CurrentMacroPlayer = currentMacroPlayer;
        CurrentTurnKind = currentTurnKind;

        _cells = new OnlineChess3DBoardCell[expected];
        for (var index = 0; index < projectedBoard.Count; index++)
        {
            var x = index % width;
            var y = index / width % height;
            var z = index / (width * height);
            _cells[index] = new OnlineChess3DBoardCell(index, x, y, z, projectedBoard[index]);
        }
    }

    public string RulesetId { get; }
    public string RoomId { get; }
    public string TableId { get; }
    public long ServerSeq { get; }
    public string StateHash { get; }
    public int ActionCount { get; }
    public string LastActionNotation { get; }
    public int Width { get; }
    public int Height { get; }
    public int Depth { get; }
    public int CurrentSide { get; }
    public int CurrentMacroPlayer { get; }
    public int CurrentTurnKind { get; }
    public IReadOnlyList<OnlineChess3DBoardCell> Cells => _cells;
    public IEnumerable<OnlineChess3DBoardCell> OccupiedCells => _cells.Where(cell => cell.IsOccupied);

    public OnlineChess3DBoardCell GetCell(int x, int y, int z)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"Cell ({x},{y},{z}) is outside {Width}x{Height}x{Depth}.");
        }

        return _cells[z * Width * Height + y * Width + x];
    }
}

public sealed record OnlineChess3DBoardCell(int Index, int X, int Y, int Z, int PieceCode)
{
    public bool IsOccupied => PieceCode != 0;
    public int Side => PieceCode / 10;
    public int PieceType => PieceCode % 10;
    public string Coordinate => $"({X},{Y},{Z})";
}

public static class OnlineChess3DBoardSnapshotParser
{
    public static bool TryParse(OnlineSnapshot? snapshot, out OnlineChess3DBoardSnapshot board, out string error)
    {
        board = null!;
        error = "";

        if (snapshot == null)
        {
            error = "Snapshot is missing.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(snapshot.SaveGameJson))
        {
            error = "Snapshot does not contain SaveGameJson.";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(snapshot.SaveGameJson);
            var root = document.RootElement;

            if (!TryGetString(root, "format", out var format) || format != "chess3d-savegame")
            {
                error = "Snapshot savegame format is not chess3d-savegame.";
                return false;
            }

            var width = 8;
            var height = 8;
            var depth = 8;
            if (root.TryGetProperty("board", out var boardElement))
            {
                width = GetIntOrDefault(boardElement, "width", width);
                height = GetIntOrDefault(boardElement, "height", height);
                depth = GetIntOrDefault(boardElement, "depth", depth);
            }

            if (width <= 0 || height <= 0 || depth <= 0)
            {
                error = $"Invalid board dimensions {width}x{height}x{depth}.";
                return false;
            }

            if (!root.TryGetProperty("projectedBoard", out var projectedBoardElement) ||
                projectedBoardElement.ValueKind != JsonValueKind.Array)
            {
                error = "Snapshot savegame does not contain projectedBoard array.";
                return false;
            }

            var projectedBoard = new List<int>(width * height * depth);
            foreach (var cell in projectedBoardElement.EnumerateArray())
            {
                if (cell.ValueKind != JsonValueKind.Number || !cell.TryGetInt32(out var pieceCode))
                {
                    error = "projectedBoard contains a non-integer cell.";
                    return false;
                }

                projectedBoard.Add(pieceCode);
            }

            var expected = width * height * depth;
            if (projectedBoard.Count != expected)
            {
                error = $"projectedBoard contains {projectedBoard.Count} cells, expected {expected}.";
                return false;
            }

            board = new OnlineChess3DBoardSnapshot(
                snapshot.RulesetId,
                snapshot.RoomId,
                snapshot.TableId,
                snapshot.ServerSeq,
                snapshot.StateHash,
                snapshot.ActionCount,
                snapshot.LastActionNotation,
                width,
                height,
                depth,
                GetIntOrDefault(root, "currentSide", 0),
                GetIntOrDefault(root, "currentMacroPlayer", 0),
                GetIntOrDefault(root, "currentTurnKind", 0),
                projectedBoard);
            return true;
        }
        catch (JsonException ex)
        {
            error = $"Snapshot savegame JSON is invalid: {ex.Message}";
            return false;
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = "";
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? "";
        return true;
    }

    private static int GetIntOrDefault(JsonElement element, string propertyName, int defaultValue)
    {
        return element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt32(out var value)
            ? value
            : defaultValue;
    }
}
