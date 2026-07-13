using ChessOnlineProtocol;

namespace ChessOnlineServer;

public sealed class OnlineRoomCleanupCoordinator
{
    private readonly OnlineRoomRegistry _rooms;
    private readonly OnlineSpectatorRegistry _spectators;
    private readonly HostedCleanupOptions _options;
    private readonly TimeProvider _timeProvider;

    public OnlineRoomCleanupCoordinator(
        OnlineRoomRegistry rooms,
        OnlineSpectatorRegistry spectators,
        HostedOnlineOptions options,
        TimeProvider timeProvider)
    {
        _rooms = rooms;
        _spectators = spectators;
        _options = options.Cleanup;
        _timeProvider = timeProvider;
    }

    public OnlineRoomCleanupRun RunOnce()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var result = _rooms.RunCleanup(new OnlineRoomCleanupPolicy
        {
            NowUtc = now,
            MaxRemovals = _options.MaxRemovalsPerRun,
            WaitingIdle = TimeSpan.FromMinutes(_options.WaitingIdleMinutes),
            CompletedRetention = TimeSpan.FromHours(_options.CompletedRetentionHours),
            AbandonedRetention = TimeSpan.FromHours(_options.AbandonedRetentionHours),
            MalformedOrphanGrace = TimeSpan.FromMinutes(_options.MalformedOrphanMinutes)
        });
        var remaining = Math.Max(0, _options.MaxRemovalsPerRun - result.RemovedTableCount);
        var removedSpectators = remaining == 0
            ? 0
            : _spectators.PruneOrphans(
                _rooms.ContainsTable,
                TimeSpan.FromMinutes(_options.SpectatorOrphanMinutes),
                now,
                remaining);
        return new OnlineRoomCleanupRun(result, removedSpectators);
    }
}

public sealed record OnlineRoomCleanupRun(
    OnlineRoomCleanupResult Rooms,
    int RemovedSpectatorCount);

public sealed class OnlineRoomCleanupService : BackgroundService
{
    private readonly OnlineRoomCleanupCoordinator _coordinator;
    private readonly HostedCleanupOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OnlineRoomCleanupService> _logger;

    public OnlineRoomCleanupService(
        OnlineRoomCleanupCoordinator coordinator,
        HostedOnlineOptions options,
        TimeProvider timeProvider,
        ILogger<OnlineRoomCleanupService> logger)
    {
        _coordinator = coordinator;
        _options = options.Cleanup;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds), _timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var run = _coordinator.RunOnce();
            if (run.Rooms.RemovedTableCount > 0 || run.RemovedSpectatorCount > 0)
            {
                _logger.LogInformation(
                    "Online cleanup removed {TableCount} table(s) and {SpectatorCount} orphan spectator record(s).",
                    run.Rooms.RemovedTableCount,
                    run.RemovedSpectatorCount);
            }
        }
    }
}
