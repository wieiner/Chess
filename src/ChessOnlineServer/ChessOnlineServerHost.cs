using ChessOnlineProtocol;
using ChessOnlinePersistence;
using ChessOnlinePersistence.Repositories;
using ChessOnlineServer.Matchmaking;
using ChessOnlineServer.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Net;

namespace ChessOnlineServer;

public static class ChessOnlineServerHost
{
    public static WebApplication BuildApp(string[] args, Action<HostedOnlineOptions>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Warning);
        builder.Configuration.AddEnvironmentVariables("CHESS3D_ONLINE_");

        var options = new HostedOnlineOptions();
        builder.Configuration.GetSection("HostedOnline").Bind(options);
        configure?.Invoke(options);
        options.ProfileRoot = ResolveProfileRoot(options.ProfileRoot);
        options.Normalize();

        builder.WebHost.UseUrls(options.HostUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        builder.Services.AddSingleton(options);
        Directory.CreateDirectory(Path.GetDirectoryName(options.Persistence.StorePath) ?? ".");
        Directory.CreateDirectory(options.DataProtection.KeyRingPath);
        builder.Services.AddDataProtection()
            .SetApplicationName(options.DataProtection.ApplicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(options.DataProtection.KeyRingPath));
        var storeOptions = new OnlineStoreOptions
        {
            Provider = options.Persistence.Provider,
            StorePath = options.Persistence.StorePath,
            AutoCreate = options.Persistence.AutoCreate,
            RestoreRoomsOnStartup = options.Persistence.RestoreRoomsOnStartup
        };
        var securityOptions = new OnlineSecurityOptions
        {
            PasswordMinLength = options.Security.PasswordMinLength,
            PasswordMaxLength = options.Security.PasswordMaxLength,
            MaxLoginAttempts = options.Security.MaxLoginAttempts,
            LockoutMinutes = options.Security.LockoutMinutes
        };
        builder.Services.AddSingleton(storeOptions);
        builder.Services.AddSingleton(securityOptions);
        builder.Services.AddSingleton<JsonOnlineStore>();
        builder.Services.AddSingleton<IOnlineIdentityStore>(sp => sp.GetRequiredService<JsonOnlineStore>());
        builder.Services.AddSingleton<IOnlineSessionStore>(sp => sp.GetRequiredService<JsonOnlineStore>());
        builder.Services.AddSingleton<IOnlineRoomPersistenceStore>(sp => sp.GetRequiredService<JsonOnlineStore>());
        builder.Services.AddSingleton<OnlineAccountService>();
        builder.Services.AddSingleton(new OnlineTokenOptions
        {
            AccessTokenMinutes = options.Auth.AccessTokenMinutes,
            RefreshTokenDays = options.Auth.RefreshTokenDays
        });
        builder.Services.AddSingleton<OnlineTokenService>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(sp => new OnlineRoomRegistry(
            options.ProfileRoot,
            timeProvider: sp.GetRequiredService<TimeProvider>()));
        builder.Services.AddSingleton(sp => new OnlineHubConnectionRegistry(sp.GetRequiredService<TimeProvider>()));
        builder.Services.AddSingleton(sp => new OnlineSpectatorRegistry(sp.GetRequiredService<TimeProvider>()));
        builder.Services.AddSingleton<OnlineReadinessProbe>();
        builder.Services.AddSingleton<OnlineMatchmakingService>();
        builder.Services.AddSingleton<OnlineRoomCleanupCoordinator>();
        builder.Services.AddHostedService<OnlineRoomCleanupService>();
        if (options.Auth.EnableAuthentication)
        {
            builder.Services
                .AddAuthentication(ChessOnlineAuthenticationHandler.AuthScheme)
                .AddScheme<AuthenticationSchemeOptions, ChessOnlineAuthenticationHandler>(ChessOnlineAuthenticationHandler.AuthScheme, _ => { });
            builder.Services.AddAuthorization();
        }
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
        builder.Services.Configure<ForwardedHeadersOptions>(forwarded =>
        {
            forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            forwarded.KnownProxies.Add(IPAddress.Loopback);
            forwarded.KnownProxies.Add(IPAddress.IPv6Loopback);
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddRateLimiter(limits =>
        {
            limits.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limits.OnRejected = async (context, cancellationToken) =>
            {
                if (!context.HttpContext.Response.HasStarted)
                {
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        errorCode = "rateLimited",
                        errorText = "Too many requests."
                    }, cancellationToken);
                }
            };
            limits.AddPolicy(OnlineRateLimitPolicies.Register, context =>
                OnlineRateLimitPolicies.FixedWindow(
                    context,
                    options.RateLimits.RegisterPermitLimit,
                    options.RateLimits.RegisterWindowSeconds));
            limits.AddPolicy(OnlineRateLimitPolicies.Login, context =>
                OnlineRateLimitPolicies.FixedWindow(
                    context,
                    options.RateLimits.LoginPermitLimit,
                    options.RateLimits.LoginWindowSeconds));
            limits.AddPolicy(OnlineRateLimitPolicies.Session, context =>
                OnlineRateLimitPolicies.FixedWindow(
                    context,
                    options.RateLimits.SessionPermitLimit,
                    options.RateLimits.SessionWindowSeconds));
            limits.AddPolicy(OnlineRateLimitPolicies.Diagnostics, context =>
                OnlineRateLimitPolicies.FixedWindow(
                    context,
                    options.RateLimits.DiagnosticsPermitLimit,
                    options.RateLimits.DiagnosticsWindowSeconds));
        });

        var app = builder.Build();
        app.UseForwardedHeaders();
        app.UseCors("local-dev");
        if (options.Auth.EnableAuthentication)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }
        app.UseRateLimiter();
        app.MapHealthChecks("/healthz/live");
        app.MapGet("/healthz/ready", (OnlineReadinessProbe readiness) =>
        {
            var result = readiness.Check();
            return result.IsReady
                ? Results.Json(new
                {
                    status = "ready",
                    protocolId = OnlineProtocolVersion.ProtocolId,
                    protocolVersion = OnlineProtocolVersion.ProtocolVersion,
                    profileCount = RuleProfileCatalog.All.Count,
                    authEnabled = options.Auth.EnableAuthentication,
                    persistenceProvider = options.Persistence.Provider
                })
                : Results.Json(new { status = "notReady", reasons = result.Reasons }, statusCode: 503);
        });
        MapAuthEndpoints(app, options);
        var diagnosticsEndpoint = app.MapGet("/chess3d/diagnostics", (
            OnlineRoomRegistry registry,
            OnlineHubConnectionRegistry connections,
            OnlineMatchmakingService matchmaking,
            OnlineSpectatorRegistry spectators) =>
        {
            var diagnostics = registry.GetDiagnostics();
            var authority = registry.GetAuthorityDiagnostics();
            var build = ServerBuildIdentity.Load(AppContext.BaseDirectory);
            return Results.Json(new
            {
                protocolId = OnlineProtocolVersion.ProtocolId,
                protocolVersion = OnlineProtocolVersion.ProtocolVersion,
                serverCommit = string.IsNullOrWhiteSpace(build.Commit) ? diagnostics.ServerCommit : build.Commit,
                build,
                requestLegalPreview = diagnostics.RequestLegalPreviewSupported,
                realtimeResync = diagnostics.RealtimeResyncSupported,
                actionLog = diagnostics.ActionLogSupported,
                matchmaking = diagnostics.MatchmakingSupported,
                resumeMatch = diagnostics.ResumeMatchSupported,
                spectatorMode = diagnostics.SpectatorModeSupported,
                lobbySnapshot = diagnostics.LobbySnapshotSupported,
                supportedHubMethods = diagnostics.SupportedHubMethods,
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
                diagnostics.LastRejectReason,
                diagnostics.ActiveTableCount,
                diagnostics.ResumableTableCount,
                diagnostics.CompletedTableCount,
                diagnostics.ExpiredTableCount,
                spectatorCount = spectators.TotalCount,
                diagnostics.CleanupRunCount,
                diagnostics.LastCleanupUtc,
                diagnostics.LastCleanupRemovedCount,
                authorityRuntimeKind = authority.RuntimeKindName,
                authorityIsPortableRuntime = authority.IsPortableRuntime,
                authorityIsSupported = authority.IsSupported,
                authorityPlatform = authority.Platform,
                authorityProcessArchitecture = authority.ProcessArchitecture,
                authorityNativeLibraryName = authority.NativeLibraryName,
                authorityNativeLibraryPath = authority.NativeLibraryPath,
                authEnabled = options.Auth.EnableAuthentication,
                persistenceProvider = options.Persistence.Provider,
                matchmakingQueueCount = matchmaking.ActiveQueueCount
            });
        });
        if (options.RateLimits.Enabled)
        {
            diagnosticsEndpoint.RequireRateLimiting(OnlineRateLimitPolicies.Diagnostics);
        }
        app.MapHub<Chess3DRelayHub>(options.HubPath, hub =>
        {
            hub.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
            hub.ApplicationMaxBufferSize = options.MaxReceiveMessageBytes;
            hub.TransportMaxBufferSize = options.MaxReceiveMessageBytes;
        });

        return app;
    }

    private static void MapAuthEndpoints(WebApplication app, HostedOnlineOptions options)
    {
        var register = app.MapPost("/api/auth/register", async (AuthRegisterRequest request, HttpContext http, OnlineAccountService accounts, OnlineTokenService tokens, CancellationToken cancellationToken) =>
        {
            if (options.Auth.RequireHttpsForTokens && !IsHttpsOrLoopback(http))
            {
                return Results.Json(AuthTokenResponse.Fail("httpsRequired", "HTTPS is required for token issuance."), statusCode: 400);
            }
            var result = await accounts.RegisterAsync(request.UserName, request.DisplayName, request.Password, cancellationToken);
            if (!result.Success || result.Account == null)
            {
                return Results.Json(AuthTokenResponse.Fail(result.ErrorCode, result.ErrorText), statusCode: 400);
            }
            return Results.Json(await IssueTokensAsync(result.Account, request.ClientName, accounts, tokens, options, cancellationToken));
        });
        if (options.RateLimits.Enabled)
        {
            register.RequireRateLimiting(OnlineRateLimitPolicies.Register);
        }

        var login = app.MapPost("/api/auth/login", async (AuthLoginRequest request, HttpContext http, OnlineAccountService accounts, OnlineTokenService tokens, CancellationToken cancellationToken) =>
        {
            if (options.Auth.RequireHttpsForTokens && !IsHttpsOrLoopback(http))
            {
                return Results.Json(AuthTokenResponse.Fail("httpsRequired", "HTTPS is required for token issuance."), statusCode: 400);
            }
            var result = await accounts.AuthenticateAsync(request.UserName, request.Password, cancellationToken);
            if (!result.Success || result.Account == null)
            {
                return Results.Json(AuthTokenResponse.Fail(result.ErrorCode, result.ErrorText), statusCode: 401);
            }
            return Results.Json(await IssueTokensAsync(result.Account, request.ClientName, accounts, tokens, options, cancellationToken));
        });
        if (options.RateLimits.Enabled)
        {
            login.RequireRateLimiting(OnlineRateLimitPolicies.Login);
        }

        var refresh = app.MapPost("/api/auth/refresh", async (AuthRefreshRequest request, OnlineTokenService tokens, OnlineAccountService accounts, CancellationToken cancellationToken) =>
        {
            var validation = await tokens.ValidateRefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (!validation.Success || validation.Account == null || validation.Session == null)
            {
                return Results.Json(AuthTokenResponse.Fail(validation.ErrorCode, "Refresh token is invalid."), statusCode: 401);
            }
            var accessToken = tokens.CreateAccessToken(validation.Account, validation.Session);
            return Results.Json(AuthTokenResponse.Ok(validation.Account, validation.Session, accessToken, request.RefreshToken, validation.Session.ExpiresAtUtc));
        });
        if (options.RateLimits.Enabled)
        {
            refresh.RequireRateLimiting(OnlineRateLimitPolicies.Session);
        }

        var logout = app.MapPost("/api/auth/logout", async (AuthRefreshRequest request, OnlineTokenService tokens, OnlineAccountService accounts, CancellationToken cancellationToken) =>
        {
            var validation = await tokens.ValidateRefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (validation.Session != null)
            {
                await accounts.RevokeSessionAsync(validation.Session.SessionId, cancellationToken);
            }
            return Results.Json(new { success = true });
        });
        if (options.RateLimits.Enabled)
        {
            logout.RequireRateLimiting(OnlineRateLimitPolicies.Session);
        }

        var me = app.MapGet("/api/auth/me", async (HttpContext http, OnlineAccountService accounts) =>
        {
            var playerId = http.User.FindFirst("playerId")?.Value ?? "";
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return Results.Json(AuthTokenResponse.Fail("anonymous", "No authenticated player."), statusCode: 401);
            }
            var account = await accounts.FindPlayerAsync(playerId, http.RequestAborted);
            return account == null
                ? Results.Json(AuthTokenResponse.Fail("notFound", "Authenticated player was not found."), statusCode: 404)
                : Results.Json(new { success = true, account.PlayerId, account.UserName, account.DisplayName });
        });
        if (options.RateLimits.Enabled)
        {
            me.RequireRateLimiting(OnlineRateLimitPolicies.Session);
        }
    }

    private static async Task<AuthTokenResponse> IssueTokensAsync(
        ChessOnlinePersistence.Entities.PlayerAccountEntity account,
        string clientName,
        OnlineAccountService accounts,
        OnlineTokenService tokens,
        HostedOnlineOptions options,
        CancellationToken cancellationToken)
    {
        var placeholderRefreshHash = "";
        var expires = DateTime.UtcNow.AddDays(options.Auth.RefreshTokenDays);
        var session = await accounts.CreateSessionAsync(account, placeholderRefreshHash, expires, clientName, cancellationToken);
        var refreshToken = tokens.CreateRefreshToken(account, session);
        session.RefreshTokenHash = OnlineTokenService.HashToken(refreshToken);
        await accounts.UpdateSessionAsync(session, cancellationToken);
        var accessToken = tokens.CreateAccessToken(account, session);
        return AuthTokenResponse.Ok(account, session, accessToken, refreshToken, expires);
    }

    private static bool IsHttpsOrLoopback(HttpContext http)
    {
        if (http.Request.IsHttps)
        {
            return true;
        }
        var host = http.Connection.RemoteIpAddress;
        return host != null && System.Net.IPAddress.IsLoopback(host);
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

public sealed class AuthRegisterRequest
{
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Password { get; set; } = "";
    public string ClientName { get; set; } = "";
}

public sealed class AuthLoginRequest
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string ClientName { get; set; } = "";
}

public sealed class AuthRefreshRequest
{
    public string RefreshToken { get; set; } = "";
}

public sealed class AuthTokenResponse
{
    public bool Success { get; set; }
    public string PlayerId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public string SessionId { get; set; } = "";
    public string ErrorCode { get; set; } = "";
    public string ErrorText { get; set; } = "";

    public static AuthTokenResponse Ok(
        ChessOnlinePersistence.Entities.PlayerAccountEntity account,
        ChessOnlinePersistence.Entities.PlayerSessionEntity session,
        string accessToken,
        string refreshToken,
        DateTime expiresAtUtc) => new()
    {
        Success = true,
        PlayerId = account.PlayerId,
        UserName = account.UserName,
        DisplayName = account.DisplayName,
        SessionId = session.SessionId,
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresAtUtc = expiresAtUtc
    };

    public static AuthTokenResponse Fail(string code, string text) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorText = text
    };
}

internal static class AppStart
{
    public static string StartedUtc { get; } = DateTime.UtcNow.ToString("O");
}
