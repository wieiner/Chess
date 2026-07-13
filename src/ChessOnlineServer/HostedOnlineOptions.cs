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
    public HostedAuthOptions Auth { get; set; } = new();
    public HostedPersistenceOptions Persistence { get; set; } = new();
    public HostedDataProtectionOptions DataProtection { get; set; } = new();
    public HostedSecurityOptions Security { get; set; } = new();
    public HostedCleanupOptions Cleanup { get; set; } = new();

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
        Auth.Normalize();
        Persistence.Normalize();
        DataProtection.Normalize();
        Security.Normalize();
        Cleanup.Normalize();
    }
}

public sealed class HostedCleanupOptions
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 300;
    public int MaxRemovalsPerRun { get; set; } = 32;
    public int WaitingIdleMinutes { get; set; } = 360;
    public int CompletedRetentionHours { get; set; } = 168;
    public int AbandonedRetentionHours { get; set; } = 168;
    public int MalformedOrphanMinutes { get; set; } = 60;
    public int SpectatorOrphanMinutes { get; set; } = 5;

    public void Normalize()
    {
        IntervalSeconds = Math.Clamp(IntervalSeconds <= 0 ? 300 : IntervalSeconds, 30, 86400);
        MaxRemovalsPerRun = Math.Clamp(MaxRemovalsPerRun <= 0 ? 32 : MaxRemovalsPerRun, 1, 256);
        WaitingIdleMinutes = Math.Clamp(WaitingIdleMinutes <= 0 ? 360 : WaitingIdleMinutes, 30, 43200);
        CompletedRetentionHours = Math.Clamp(CompletedRetentionHours <= 0 ? 168 : CompletedRetentionHours, 1, 8760);
        AbandonedRetentionHours = Math.Clamp(AbandonedRetentionHours <= 0 ? 168 : AbandonedRetentionHours, 1, 8760);
        MalformedOrphanMinutes = Math.Clamp(MalformedOrphanMinutes <= 0 ? 60 : MalformedOrphanMinutes, 10, 10080);
        SpectatorOrphanMinutes = Math.Clamp(SpectatorOrphanMinutes <= 0 ? 5 : SpectatorOrphanMinutes, 1, 1440);
    }
}

public sealed class HostedAuthOptions
{
    public bool EnableAuthentication { get; set; } = false;
    public bool AllowDevAnonymousSessions { get; set; } = true;
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
    public bool RequireHttpsForTokens { get; set; } = false;

    public void Normalize()
    {
        AccessTokenMinutes = Math.Clamp(AccessTokenMinutes, 1, 24 * 60);
        RefreshTokenDays = Math.Clamp(RefreshTokenDays, 1, 365);
    }
}

public sealed class HostedPersistenceOptions
{
    public string Provider { get; set; } = "json";
    public string StorePath { get; set; } = "";
    public bool AutoCreate { get; set; } = true;
    public bool RestoreRoomsOnStartup { get; set; } = false;

    public void Normalize()
    {
        Provider = string.IsNullOrWhiteSpace(Provider) ? "json" : Provider.Trim();
        if (string.IsNullOrWhiteSpace(StorePath))
        {
            StorePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Chess3D",
                "online-dev",
                "chess3d-online-store.json");
        }
    }
}

public sealed class HostedDataProtectionOptions
{
    public string ApplicationName { get; set; } = "Chess3D.Online";
    public string KeyRingPath { get; set; } = "";

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(ApplicationName))
        {
            ApplicationName = "Chess3D.Online";
        }
        if (string.IsNullOrWhiteSpace(KeyRingPath))
        {
            KeyRingPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Chess3D",
                "online-dev",
                "keys");
        }
    }
}

public sealed class HostedSecurityOptions
{
    public int PasswordMinLength { get; set; } = 8;
    public int PasswordMaxLength { get; set; } = 256;
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 5;

    public void Normalize()
    {
        PasswordMinLength = Math.Clamp(PasswordMinLength, 6, 128);
        PasswordMaxLength = Math.Clamp(PasswordMaxLength, PasswordMinLength, 4096);
        MaxLoginAttempts = Math.Clamp(MaxLoginAttempts, 1, 100);
        LockoutMinutes = Math.Clamp(LockoutMinutes, 1, 1440);
    }
}
