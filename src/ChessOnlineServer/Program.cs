namespace ChessOnlineServer;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var app = ChessOnlineServerHost.BuildApp(args);
        await app.RunAsync();
    }
}
