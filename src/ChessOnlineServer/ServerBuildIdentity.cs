using System.Reflection;
using System.Text.Json;
using ChessOnlineProtocol;

namespace ChessOnlineServer;

public static class ServerBuildIdentity
{
    private const string FileName = "server-build.json";

    public static OnlineServerBuildIdentity Load(string baseDirectory)
    {
        var fallbackVersion = GetInformationalVersion();
        var path = Path.Combine(baseDirectory, FileName);
        if (!File.Exists(path))
        {
            return new OnlineServerBuildIdentity
            {
                InformationalVersion = fallbackVersion
            };
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            return new OnlineServerBuildIdentity
            {
                Commit = SanitizeIdentifier(ReadString(root, "commit")),
                BuiltUtc = SanitizeText(ReadString(root, "builtUtc")),
                PackageId = SanitizeIdentifier(ReadString(root, "packageId")),
                InformationalVersion = SanitizeText(ReadString(root, "informationalVersion"))
            }.WithFallbackVersion(fallbackVersion);
        }
        catch (JsonException)
        {
            return new OnlineServerBuildIdentity
            {
                InformationalVersion = fallbackVersion
            };
        }
        catch (IOException)
        {
            return new OnlineServerBuildIdentity
            {
                InformationalVersion = fallbackVersion
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new OnlineServerBuildIdentity
            {
                InformationalVersion = fallbackVersion
            };
        }
    }

    private static string ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? ""
            : "";
    }

    private static string SanitizeIdentifier(string value)
    {
        value = SanitizeText(value);
        return value.Contains('\\') || value.Contains('/') || value.Contains(':')
            ? ""
            : value;
    }

    private static string SanitizeText(string value)
    {
        value = value.Trim();
        return value.Length > 256 ? value[..256] : value;
    }

    private static string GetInformationalVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(ServerBuildIdentity).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "";
    }

    private static OnlineServerBuildIdentity WithFallbackVersion(this OnlineServerBuildIdentity identity, string fallbackVersion)
    {
        if (string.IsNullOrWhiteSpace(identity.InformationalVersion))
        {
            identity.InformationalVersion = fallbackVersion;
        }

        return identity;
    }
}
