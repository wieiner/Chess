using ChessOnlineProtocol;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace ChessOnlineServer;

public static class ChessOnlineServerHost
{
    public static WebApplication BuildApp(string[] args, Action<HostedOnlineOptions>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddEnvironmentVariables("CHESS3D_ONLINE_");

        var options = new HostedOnlineOptions();
        builder.Configuration.GetSection("HostedOnline").Bind(options);
        configure?.Invoke(options);
        options.ProfileRoot = ResolveProfileRoot(options.ProfileRoot);
        options.Normalize();

        builder.WebHost.UseUrls(options.HostUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(new OnlineRoomRegistry(options.ProfileRoot));
        builder.Services.AddSingleton<OnlineHubConnectionRegistry>();
        builder.Services.AddCors(cors =>
        {
            cors.AddPolicy("local-dev", policy =>
            {
                if (options.AllowedOrigins.Length == 0)
                {
                    policy.WithOrigins("http://127.0.0.1:5077", "http://localhost:5077");
                }
                else
                {
                    policy.WithOrigins(options.AllowedOrigins);
                }
                policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            });
        });
        builder.Services.AddSignalR(hub =>
        {
            hub.EnableDetailedErrors = options.EnableDetailedErrors;
            hub.MaximumReceiveMessageSize = options.MaxReceiveMessageBytes;
            hub.KeepAliveInterval = TimeSpan.FromSeconds(options.KeepAliveIntervalSeconds);
            hub.ClientTimeoutInterval = TimeSpan.FromSeconds(options.ClientTimeoutSeconds);
        });
        builder.Services.AddHealthChecks();

        var app = builder.Build();
        app.UseCors("local-dev");
        app.MapHealthChecks("/healthz/live");
        app.MapGet("/healthz/ready", (OnlineRoomRegistry registry) =>
        {
            var profileOk = RuleProfileCatalog.All.All(p => File.Exists(Path.Combine(options.ProfileRoot, p.FileName)));
            return profileOk
                ? Results.Json(new { status = "ready", protocolId = OnlineProtocolVersion.ProtocolId, protocolVersion = OnlineProtocolVersion.ProtocolVersion, profileCount = RuleProfileCatalog.All.Count })
                : Results.Json(new { status = "notReady", reason = "missingProfile" }, statusCode: 503);
        });
        app.MapGet("/chess3d/diagnostics", (OnlineRoomRegistry registry, OnlineHubConnectionRegistry connections) =>
        {
            var diagnostics = registry.GetDiagnostics();
            return Results.Json(new
            {
                protocolId = OnlineProtocolVersion.ProtocolId,
                protocolVersion = OnlineProtocolVersion.ProtocolVersion,
                startedUtc = AppStart.StartedUtc,
                diagnostics.RoomCount,
                diagnostics.TableCount,
                activeConnections = connections.ActiveConnectionCount,
                diagnostics.ConnectionCount,
                diagnostics.LastServerSeq,
                diagnostics.AcceptedActionCount,
                diagnostics.RejectedActionCount,
                diagnostics.ResyncCount,
                diagnostics.ActionLogLength,
                diagnostics.ProtocolErrorCount,
                diagnostics.LastRejectReason
            });
        });
        app.MapHub<Chess3DRelayHub>(options.HubPath, hub =>
        {
            hub.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
            hub.ApplicationMaxBufferSize = options.MaxReceiveMessageBytes;
            hub.TransportMaxBufferSize = options.MaxReceiveMessageBytes;
        });

        return app;
    }

    public static string ResolveProfileRoot(string configured)
    {
        if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
        {
            return configured;
        }

        var outputRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "Rules3D", "Profiles");
        if (Directory.Exists(outputRoot))
        {
            return outputRoot;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "assets", "rules", "profiles");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }

        return outputRoot;
    }
}

internal static class AppStart
{
    public static string StartedUtc { get; } = DateTime.UtcNow.ToString("O");
}
