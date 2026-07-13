using ChessOnlineProtocol;

namespace ChessOnlineServer;

public static class OnlineReadinessReasons
{
    public const string NativeUnavailable = "nativeUnavailable";
    public const string ProfileSetInvalid = "profileSetInvalid";
    public const string PersistenceUnavailable = "persistenceUnavailable";
    public const string KeyRingUnavailable = "keyRingUnavailable";
    public const string RegistryUnavailable = "registryUnavailable";
    public const string ConfigurationInvalid = "configurationInvalid";
}

public sealed record OnlineReadinessResult(bool IsReady, IReadOnlyList<string> Reasons);

public sealed class OnlineReadinessDependencies
{
    public Func<bool>? NativeAuthorityReady { get; init; }
    public Func<string, bool>? ProfileSetReady { get; init; }
    public Func<string, bool>? DirectoryWritable { get; init; }
    public Func<bool>? RegistryReady { get; init; }
    public Func<HostedOnlineOptions, bool>? ConfigurationValid { get; init; }
}

public sealed class OnlineReadinessProbe
{
    private readonly HostedOnlineOptions _options;
    private readonly OnlineRoomRegistry _registry;
    private readonly OnlineReadinessDependencies _dependencies;

    public OnlineReadinessProbe(HostedOnlineOptions options, OnlineRoomRegistry registry)
        : this(options, registry, new OnlineReadinessDependencies())
    {
    }

    public OnlineReadinessProbe(
        HostedOnlineOptions options,
        OnlineRoomRegistry registry,
        OnlineReadinessDependencies dependencies)
    {
        _options = options;
        _registry = registry;
        _dependencies = dependencies;
    }

    public OnlineReadinessResult Check()
    {
        var reasons = new List<string>();
        CheckDependency(
            _dependencies.ConfigurationValid?.Invoke(_options) ?? ConfigurationValid(_options),
            OnlineReadinessReasons.ConfigurationInvalid,
            reasons);
        CheckDependency(
            SafeInvoke(_dependencies.ProfileSetReady, _options.ProfileRoot, ProfileSetReady),
            OnlineReadinessReasons.ProfileSetInvalid,
            reasons);
        CheckDependency(
            SafeInvoke(_dependencies.NativeAuthorityReady, NativeAuthorityReady),
            OnlineReadinessReasons.NativeUnavailable,
            reasons);
        CheckDependency(
            SafeInvoke(_dependencies.RegistryReady, RegistryReady),
            OnlineReadinessReasons.RegistryUnavailable,
            reasons);

        var persistenceDirectory = Path.GetDirectoryName(_options.Persistence.StorePath) ?? ".";
        CheckDependency(
            SafeInvoke(_dependencies.DirectoryWritable, persistenceDirectory, DirectoryWritable),
            OnlineReadinessReasons.PersistenceUnavailable,
            reasons);
        CheckDependency(
            SafeInvoke(_dependencies.DirectoryWritable, _options.DataProtection.KeyRingPath, DirectoryWritable),
            OnlineReadinessReasons.KeyRingUnavailable,
            reasons);

        return new OnlineReadinessResult(reasons.Count == 0, reasons);
    }

    private bool NativeAuthorityReady()
    {
        var diagnostics = _registry.GetAuthorityDiagnostics();
        return diagnostics.IsSupported && _registry.ProbeAuthorityReady();
    }

    private bool RegistryReady()
    {
        _ = _registry.GetDiagnostics();
        return true;
    }

    private static bool ProfileSetReady(string profileRoot)
    {
        if (!Directory.Exists(profileRoot) || RuleProfileCatalog.All.Count != 5)
        {
            return false;
        }

        var expected = RuleProfileCatalog.All
            .Select(profile => profile.FileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actual = Directory.EnumerateFiles(profileRoot, "*_3d_v0_1.json", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return expected.SetEquals(actual);
    }

    private static bool DirectoryWritable(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        var marker = Path.Combine(directory, $".chessonline-ready-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(marker, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose))
            {
                stream.WriteByte(0x52);
                stream.Flush(flushToDisk: true);
            }
            return !File.Exists(marker);
        }
        finally
        {
            try { File.Delete(marker); } catch { }
        }
    }

    private static bool ConfigurationValid(HostedOnlineOptions options)
    {
        var urls = options.HostUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return urls.Length > 0 &&
            urls.All(url => Uri.TryCreate(url, UriKind.Absolute, out var parsed) &&
                (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps)) &&
            options.HubPath.StartsWith('/') &&
            !string.IsNullOrWhiteSpace(options.ProfileRoot) &&
            !string.IsNullOrWhiteSpace(options.Persistence.StorePath) &&
            !string.IsNullOrWhiteSpace(options.DataProtection.KeyRingPath) &&
            !string.IsNullOrWhiteSpace(options.DataProtection.ApplicationName) &&
            options.MaxReceiveMessageBytes >= 4096;
    }

    private static void CheckDependency(bool ready, string reason, ICollection<string> reasons)
    {
        if (!ready)
        {
            reasons.Add(reason);
        }
    }

    private static bool SafeInvoke(Func<bool>? dependency, Func<bool> fallback)
    {
        try { return (dependency ?? fallback)(); } catch { return false; }
    }

    private static bool SafeInvoke(Func<string, bool>? dependency, string value, Func<string, bool> fallback)
    {
        try { return (dependency ?? fallback)(value); } catch { return false; }
    }
}
