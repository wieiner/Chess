namespace ChessOnlineServer;

public sealed class HostedOnlineOptions
{
    public string HostUrls { get; set; } = "http://127.0.0.1:5077";
    public string HubPath { get; set; } = "/chess3d/relay";
    public string[] AllowedOrigins { get; set; } =
    {
        "http://127.0.0.1:5077",
        "http://localhost:5077"
    };
    public int MaxReceiveMessageBytes { get; set; } = 65536;
    public int KeepAliveIntervalSeconds { get; set; } = 15;
    public int ClientTimeoutSeconds { get; set; } = 45;
    public bool EnableDetailedErrors { get; set; } = false;
    public int RateLimitPermitLimit { get; set; } = 120;
    public int RateLimitWindowSeconds { get; set; } = 60;
    public int MaxRooms { get; set; } = 128;
    public int MaxTablesPerRoom { get; set; } = 32;
    public int MaxConnections { get; set; } = 256;
    public int MaxMessageLogLength { get; set; } = 512;
    public bool DiagnosticsEnabled { get; set; } = true;
    public string ProfileRoot { get; set; } = "";

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(HostUrls))
        {
            HostUrls = "http://127.0.0.1:5077";
        }
        if (string.IsNullOrWhiteSpace(HubPath) || !HubPath.StartsWith('/'))
        {
            HubPath = "/chess3d/relay";
        }
        MaxReceiveMessageBytes = Math.Clamp(MaxReceiveMessageBytes, 4096, 1024 * 1024);
        KeepAliveIntervalSeconds = Math.Clamp(KeepAliveIntervalSeconds, 5, 120);
        ClientTimeoutSeconds = Math.Clamp(ClientTimeoutSeconds, 10, 300);
        RateLimitPermitLimit = Math.Clamp(RateLimitPermitLimit, 1, 10000);
        RateLimitWindowSeconds = Math.Clamp(RateLimitWindowSeconds, 1, 3600);
        MaxRooms = Math.Clamp(MaxRooms, 1, 10000);
        MaxTablesPerRoom = Math.Clamp(MaxTablesPerRoom, 1, 1000);
        MaxConnections = Math.Clamp(MaxConnections, 1, 10000);
        MaxMessageLogLength = Math.Clamp(MaxMessageLogLength, 8, 100000);
    }
}
