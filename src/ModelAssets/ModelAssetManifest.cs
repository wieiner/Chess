using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelAssets;

public sealed record ModelAssetManifest
{
    public required string Format { get; init; }
    public required string Version { get; init; }
    public required string SetId { get; init; }
    public required string DisplayName { get; init; }
    public required string Author { get; init; }
    public required ModelAssetLicense License { get; init; }
    public required ModelAssetSource Source { get; init; }
    public required IReadOnlyList<string> SupportedApps { get; init; }
    public required string Units { get; init; }
    public required string CoordinateSystem { get; init; }
    public required double DefaultScale { get; init; }
    public required IReadOnlyList<ModelAssetEntry> Assets { get; init; }
}

public sealed record ModelAssetEntry
{
    public required string AssetId { get; init; }
    public required string Role { get; init; }
    public required string Format { get; init; }
    public required string Path { get; init; }
    public required string Sha256 { get; init; }
    public double Scale { get; init; } = 1.0;
    public ModelAssetVector3 Rotation { get; init; } = ModelAssetVector3.Zero;
    public ModelAssetVector3 Origin { get; init; } = ModelAssetVector3.Zero;
    public ModelAssetBounds? Bounds { get; init; }
    public long? TriangleCount { get; init; }
    public long? VertexCount { get; init; }
    public IReadOnlyList<ModelAssetMaterial> Materials { get; init; } = [];
    public IReadOnlyList<ModelAssetTexture> Textures { get; init; } = [];
    public IReadOnlyList<ModelAssetLod> Lod { get; init; } = [];
    public string? FallbackRole { get; init; }
    public ModelAssetLicense? LicenseOverride { get; init; }
}

public sealed record ModelAssetMaterial
{
    public required string MaterialId { get; init; }
    public string? BaseColor { get; init; }
    public bool DoubleSided { get; init; }
}

public sealed record ModelAssetTexture
{
    public required string TextureId { get; init; }
    public required string Usage { get; init; }
    public required string Path { get; init; }
    public required string Sha256 { get; init; }
}

public sealed record ModelAssetLod
{
    public required int Level { get; init; }
    public required string Path { get; init; }
    public required string Sha256 { get; init; }
}

public sealed record ModelAssetLicense
{
    public required string SpdxId { get; init; }
    public required string Status { get; init; }
    public string? NoticePath { get; init; }
    public string? Notes { get; init; }
}

public sealed record ModelAssetSource
{
    public required string Provenance { get; init; }
    public string? SourceUri { get; init; }
    public string? SourceSha256 { get; init; }
}

public readonly record struct ModelAssetVector3(double X, double Y, double Z)
{
    public static ModelAssetVector3 Zero => new(0, 0, 0);
}

public sealed record ModelAssetBounds
{
    public required ModelAssetVector3 Minimum { get; init; }
    public required ModelAssetVector3 Maximum { get; init; }
}

public static class ModelAssetManifestJson
{
    public const string CurrentFormat = "chess-model-assets";
    public const string CurrentVersion = "2.0";

    public static JsonSerializerOptions StrictOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        WriteIndented = true
    };

    public static ModelAssetManifest Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var manifest = JsonSerializer.Deserialize<ModelAssetManifest>(json, StrictOptions)
            ?? throw new JsonException("Model asset manifest is empty.");
        ModelAssetManifestRules.ThrowIfInvalid(manifest);
        return manifest;
    }

    public static string Serialize(ModelAssetManifest manifest)
    {
        ModelAssetManifestRules.ThrowIfInvalid(manifest);
        return JsonSerializer.Serialize(manifest, StrictOptions) + Environment.NewLine;
    }
}

public static class ModelAssetRoles
{
    public static IReadOnlySet<string> Known { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "chess.white.pawn", "chess.white.knight", "chess.white.bishop",
            "chess.white.rook", "chess.white.queen", "chess.white.king",
            "chess.black.pawn", "chess.black.knight", "chess.black.bishop",
            "chess.black.rook", "chess.black.queen", "chess.black.king",
            "chess.board.lightTile", "chess.board.darkTile", "chess.board.frame",
            "chess3d.common.pawn", "chess3d.common.knight", "chess3d.common.bishop",
            "chess3d.common.rook", "chess3d.common.queen", "chess3d.common.king",
            "asgard.core", "asgard.anchor", "asgard.reserveSlot", "asgard.fusionMarker",
            "rubikConvergence.core", "rubikConvergence.layerMarker", "rubikConvergence.turnMarker",
            "hodge.primaryMarker", "hodge.mirrorMarker", "hodge.projectionArrow",
            "rubik.cubieBody", "rubik.sticker", "rubik.core"
        };
}

public static class ModelAssetManifestRules
{
    private static readonly HashSet<string> KnownFormats =
        new(StringComparer.OrdinalIgnoreCase) { "glb", "obj" };
    private static readonly HashSet<string> KnownUnits =
        new(StringComparer.Ordinal) { "meter", "centimeter", "millimeter", "unit" };
    private static readonly HashSet<string> KnownCoordinateSystems =
        new(StringComparer.Ordinal) { "right-handed-y-up", "right-handed-z-up", "left-handed-y-up" };

    public static void ThrowIfInvalid(ModelAssetManifest manifest)
    {
        if (manifest.Format != ModelAssetManifestJson.CurrentFormat)
            throw new FormatException($"Unsupported manifest format '{manifest.Format}'.");
        if (manifest.Version != ModelAssetManifestJson.CurrentVersion)
            throw new FormatException($"Unsupported manifest version '{manifest.Version}'.");
        RequireToken(manifest.SetId, "setId");
        RequireText(manifest.DisplayName, "displayName");
        RequireText(manifest.Author, "author");
        RequireText(manifest.License.SpdxId, "license.spdxId");
        RequireText(manifest.License.Status, "license.status");
        RequireText(manifest.Source.Provenance, "source.provenance");
        if (manifest.SupportedApps.Count == 0)
            throw new FormatException("supportedApps must not be empty.");
        if (!KnownUnits.Contains(manifest.Units))
            throw new FormatException($"Unknown units '{manifest.Units}'.");
        if (!KnownCoordinateSystems.Contains(manifest.CoordinateSystem))
            throw new FormatException($"Unknown coordinate system '{manifest.CoordinateSystem}'.");
        if (!double.IsFinite(manifest.DefaultScale) || manifest.DefaultScale <= 0)
            throw new FormatException("defaultScale must be finite and positive.");
        if (manifest.Assets.Count == 0)
            throw new FormatException("assets must not be empty.");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var roles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var asset in manifest.Assets)
        {
            RequireToken(asset.AssetId, "assetId");
            if (!ids.Add(asset.AssetId))
                throw new FormatException($"Duplicate assetId '{asset.AssetId}'.");
            if (!ModelAssetRoles.Known.Contains(asset.Role))
                throw new FormatException($"Unknown semantic role '{asset.Role}'.");
            if (!roles.Add(asset.Role))
                throw new FormatException($"Duplicate semantic role '{asset.Role}'.");
            if (!KnownFormats.Contains(asset.Format))
                throw new FormatException($"Unsupported asset format '{asset.Format}'.");
            ValidateRelativePath(asset.Path, "asset.path");
            ValidateSha(asset.Sha256, "asset.sha256");
            if (!double.IsFinite(asset.Scale) || asset.Scale <= 0)
                throw new FormatException($"Asset '{asset.AssetId}' scale must be finite and positive.");
            ValidateVector(asset.Rotation, "rotation");
            ValidateVector(asset.Origin, "origin");
            foreach (var texture in asset.Textures)
            {
                ValidateRelativePath(texture.Path, "texture.path");
                ValidateSha(texture.Sha256, "texture.sha256");
            }
            foreach (var lod in asset.Lod)
            {
                ValidateRelativePath(lod.Path, "lod.path");
                ValidateSha(lod.Sha256, "lod.sha256");
            }
        }
    }

    public static void ValidateRelativePath(string value, string field)
    {
        RequireText(value, field);
        if (Path.IsPathRooted(value) || value.Contains('\\'))
            throw new FormatException($"{field} must be a normalized forward-slash relative path.");
        var parts = value.Split('/');
        if (parts.Any(part => part is "" or "." or ".."))
            throw new FormatException($"{field} contains an invalid path segment.");
    }

    private static void ValidateSha(string value, string field)
    {
        if (value.Length != 64 || value.Any(c => !Uri.IsHexDigit(c)))
            throw new FormatException($"{field} must contain 64 hexadecimal SHA-256 characters.");
    }

    private static void ValidateVector(ModelAssetVector3 value, string field)
    {
        if (!double.IsFinite(value.X) || !double.IsFinite(value.Y) || !double.IsFinite(value.Z))
            throw new FormatException($"{field} must contain finite values.");
    }

    private static void RequireText(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException($"{field} is required.");
    }

    private static void RequireToken(string value, string field)
    {
        RequireText(value, field);
        if (value.Length > 128 || value.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')))
            throw new FormatException($"{field} contains unsupported characters.");
    }
}
