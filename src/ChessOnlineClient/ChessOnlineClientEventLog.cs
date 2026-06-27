namespace ChessOnlineClient;

public sealed class ChessOnlineClientEventLog
{
    private readonly List<string> _events = new();

    public IReadOnlyList<string> Events => _events;

    public void Add(string message)
    {
        _events.Add(ChessOnlineSecretRedactor.Redact(message));
    }

    public void Clear() => _events.Clear();
}
