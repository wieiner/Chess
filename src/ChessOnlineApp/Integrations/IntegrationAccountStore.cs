using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessApp;

[Flags]
internal enum IntegrationConsumer
{
    None = 0,
    Chess2D = 1 << 0,
    Chess3D = 1 << 1,
    WebPlatform = 1 << 2
}

internal sealed class IntegrationAccountProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PortalId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Username { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string AccessMode { get; set; } = "";
    public IntegrationConsumer Consumers { get; set; } = IntegrationConsumer.None;
    public bool HasSecret { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, string> Settings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonIgnore]
    public string Summary
    {
        get
        {
            var identity = !string.IsNullOrWhiteSpace(Username) ? Username : Endpoint;
            if (string.IsNullOrWhiteSpace(identity))
            {
                identity = RoomId;
            }
            if (string.IsNullOrWhiteSpace(identity))
            {
                identity = "profile";
            }

            return $"{DisplayName} | {identity} | {Consumers}";
        }
    }
}

internal sealed class IntegrationSettingsDocument
{
    public int Version { get; set; } = 1;
    public List<IntegrationAccountProfile> Accounts { get; set; } = new();
}

internal interface IIntegrationAccountStore
{
    string StorePath { get; }
    Task<IntegrationSettingsDocument> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(IntegrationSettingsDocument document, CancellationToken cancellationToken = default);
    Task<IntegrationAccountProfile> UpsertAsync(IntegrationAccountProfile profile, CancellationToken cancellationToken = default);
}

internal sealed class JsonIntegrationAccountStore : IIntegrationAccountStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonIntegrationAccountStore(string? storePath = null)
    {
        StorePath = string.IsNullOrWhiteSpace(storePath)
            ? GetDefaultStorePath()
            : storePath;
    }

    public string StorePath { get; }

    public async Task<IntegrationSettingsDocument> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(StorePath))
        {
            return new IntegrationSettingsDocument();
        }

        await using var stream = File.OpenRead(StorePath);
        var document = await JsonSerializer.DeserializeAsync<IntegrationSettingsDocument>(stream, JsonOptions, cancellationToken);
        return document ?? new IntegrationSettingsDocument();
    }

    public async Task SaveAsync(IntegrationSettingsDocument document, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(StorePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{StorePath}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
        }

        File.Move(tempPath, StorePath, overwrite: true);
    }

    public async Task<IntegrationAccountProfile> UpsertAsync(IntegrationAccountProfile profile, CancellationToken cancellationToken = default)
    {
        Normalize(profile);
        var document = await LoadAsync(cancellationToken);
        var existing = FindExisting(document.Accounts, profile);
        var now = DateTimeOffset.UtcNow;

        if (existing == null)
        {
            profile.CreatedAt = profile.CreatedAt == default ? now : profile.CreatedAt;
            profile.UpdatedAt = now;
            document.Accounts.Add(profile);
        }
        else
        {
            existing.PortalId = profile.PortalId;
            existing.DisplayName = profile.DisplayName;
            existing.Username = profile.Username;
            existing.Endpoint = profile.Endpoint;
            existing.RoomId = profile.RoomId;
            existing.AccessMode = profile.AccessMode;
            existing.Consumers = profile.Consumers;
            existing.HasSecret = profile.HasSecret;
            existing.Settings = profile.Settings;
            existing.UpdatedAt = now;
            profile = existing;
        }

        document.Accounts = document.Accounts
            .OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Username, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Endpoint, StringComparer.OrdinalIgnoreCase)
            .ToList();

        await SaveAsync(document, cancellationToken);
        return profile;
    }

    private static IntegrationAccountProfile? FindExisting(IEnumerable<IntegrationAccountProfile> accounts, IntegrationAccountProfile profile)
    {
        return accounts.FirstOrDefault(account =>
            string.Equals(account.Id, profile.Id, StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(account.PortalId, profile.PortalId, StringComparison.OrdinalIgnoreCase) &&
             string.Equals(account.Username, profile.Username, StringComparison.OrdinalIgnoreCase) &&
             string.Equals(account.Endpoint, profile.Endpoint, StringComparison.OrdinalIgnoreCase) &&
             string.Equals(account.RoomId, profile.RoomId, StringComparison.OrdinalIgnoreCase)));
    }

    private static void Normalize(IntegrationAccountProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            profile.Id = Guid.NewGuid().ToString("N");
        }

        profile.PortalId = profile.PortalId.Trim();
        profile.DisplayName = profile.DisplayName.Trim();
        profile.Username = profile.Username.Trim();
        profile.Endpoint = profile.Endpoint.Trim();
        profile.RoomId = profile.RoomId.Trim();
        profile.AccessMode = profile.AccessMode.Trim();
        profile.Settings = new Dictionary<string, string>(
            profile.Settings.Where(pair => !string.IsNullOrWhiteSpace(pair.Key)),
            StringComparer.OrdinalIgnoreCase);
    }

    private static string GetDefaultStorePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(appData))
        {
            appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        return Path.Combine(appData, "ChessAdvisor", "integrations.json");
    }
}

internal static class IntegrationProfileFactory
{
    public static IntegrationAccountProfile FromPortal(
        ChessPortalDescriptor portal,
        string username,
        string alias,
        bool hasSecret,
        string accessMode = "")
    {
        return new IntegrationAccountProfile
        {
            PortalId = portal.Id,
            DisplayName = string.IsNullOrWhiteSpace(alias) ? portal.DisplayName : alias,
            Username = username,
            Endpoint = portal.HomeUri.ToString(),
            AccessMode = accessMode,
            Consumers = ConsumersForPortal(portal),
            HasSecret = hasSecret,
            Settings =
            {
                ["transport"] = portal.Transport,
                ["authKind"] = portal.AuthKind.ToString(),
                ["capabilities"] = portal.Capabilities.ToString()
            }
        };
    }

    public static IntegrationAccountProfile FromChess3DRelay(
        Uri endpoint,
        string roomId,
        int seat,
        int groupSlot,
        bool hasSecret)
    {
        return new IntegrationAccountProfile
        {
            PortalId = "chessadvisor3d",
            DisplayName = "ChessAdvisor 3D Web Platform",
            Endpoint = endpoint.ToString(),
            RoomId = roomId,
            AccessMode = groupSlot > 0 ? "group-bridge" : "player",
            Consumers = IntegrationConsumer.Chess3D | IntegrationConsumer.WebPlatform,
            HasSecret = hasSecret,
            Settings =
            {
                ["seat"] = seat.ToString(),
                ["groupSlot"] = groupSlot.ToString(),
                ["protocol"] = "chess3d.relay.v1"
            }
        };
    }

    private static IntegrationConsumer ConsumersForPortal(ChessPortalDescriptor portal)
    {
        if (string.Equals(portal.Id, "chessadvisor3d", StringComparison.OrdinalIgnoreCase) ||
            portal.Capabilities.HasFlag(ChessPortalCapability.Custom3DRelay))
        {
            return IntegrationConsumer.Chess3D | IntegrationConsumer.WebPlatform;
        }

        return portal.Capabilities.HasFlag(ChessPortalCapability.PublicProfile) ||
               portal.Capabilities.HasFlag(ChessPortalCapability.PublicGameArchive) ||
               portal.Capabilities.HasFlag(ChessPortalCapability.CurrentDailyGames) ||
               portal.Capabilities.HasFlag(ChessPortalCapability.LiveGameStream) ||
               portal.Capabilities.HasFlag(ChessPortalCapability.TextServer)
            ? IntegrationConsumer.Chess2D
            : IntegrationConsumer.None;
    }
}
