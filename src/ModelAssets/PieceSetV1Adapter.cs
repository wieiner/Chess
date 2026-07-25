using System.Security.Cryptography;
using System.Text.Json;

namespace ModelAssets;

public static class PieceSetV1Adapter
{
    private static readonly IReadOnlyDictionary<string, string> PieceRoleNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["pawn"] = "pawn",
            ["rook"] = "rook",
            ["knight"] = "knight",
            ["bishop"] = "bishop",
            ["queen"] = "queen",
            ["king"] = "king"
        };

    public static IReadOnlyList<ModelAssetManifest> Load(string catalogPath)
    {
        var fullCatalogPath = Path.GetFullPath(catalogPath);
        var root = Path.GetDirectoryName(fullCatalogPath)
            ?? throw new ArgumentException("Catalog path has no parent directory.", nameof(catalogPath));
        using var document = JsonDocument.Parse(
            File.ReadAllText(fullCatalogPath),
            new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow });
        if (document.RootElement.GetProperty("version").GetInt32() != 1)
            throw new FormatException("Only piece_sets.json version 1 can be adapted.");

        var manifests = new List<ModelAssetManifest>();
        foreach (var set in document.RootElement.GetProperty("sets").EnumerateArray())
        {
            var setId = set.GetProperty("setId").GetString() ?? throw new FormatException("v1 setId is missing.");
            var assets = new List<ModelAssetEntry>();
            var pieces = set.GetProperty("pieces");
            foreach (var mapping in PieceRoleNames)
            {
                var piece = pieces.GetProperty(mapping.Key);
                AddPiece(assets, root, setId, mapping.Value, "white", piece.GetProperty("white"));
                AddPiece(assets, root, setId, mapping.Value, "black", piece.GetProperty("black"));
            }

            if (set.TryGetProperty("board", out var board))
            {
                AddBoard(assets, root, setId, "lightTile", board.GetProperty("lightTile"));
                AddBoard(assets, root, setId, "darkTile", board.GetProperty("darkTile"));
            }

            var manifest = new ModelAssetManifest
            {
                Format = ModelAssetManifestJson.CurrentFormat,
                Version = ModelAssetManifestJson.CurrentVersion,
                SetId = setId,
                DisplayName = set.GetProperty("displayName").GetString() ?? setId,
                Author = "Unknown legacy source",
                License = new ModelAssetLicense
                {
                    SpdxId = "NOASSERTION",
                    Status = "pending-review",
                    Notes = "Adapted from v1; redistribution rights are not inferred."
                },
                Source = new ModelAssetSource
                {
                    Provenance = set.GetProperty("source").GetString() ?? "legacy-v1-catalog"
                },
                SupportedApps = set.GetProperty("supportedApps").EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .Where(item => item.Length > 0)
                    .ToArray(),
                Units = "unit",
                CoordinateSystem = "right-handed-y-up",
                DefaultScale = set.TryGetProperty("scale", out var scale) ? scale.GetDouble() : 1.0,
                Assets = assets
            };
            ModelAssetManifestRules.ThrowIfInvalid(manifest);
            manifests.Add(manifest);
        }
        return manifests;
    }

    private static void AddPiece(
        ICollection<ModelAssetEntry> assets,
        string root,
        string setId,
        string piece,
        string color,
        JsonElement descriptor)
    {
        AddAsset(assets, root, $"{setId}.{color}.{piece}", $"chess.{color}.{piece}", descriptor);
    }

    private static void AddBoard(
        ICollection<ModelAssetEntry> assets,
        string root,
        string setId,
        string role,
        JsonElement descriptor)
    {
        AddAsset(assets, root, $"{setId}.board.{role}", $"chess.board.{role}", descriptor);
    }

    private static void AddAsset(
        ICollection<ModelAssetEntry> assets,
        string root,
        string assetId,
        string role,
        JsonElement descriptor)
    {
        var relativePath = descriptor.GetProperty("obj").GetString()
            ?? throw new FormatException($"v1 asset '{assetId}' has no OBJ path.");
        var normalized = relativePath.Replace('\\', '/');
        ModelAssetManifestRules.ValidateRelativePath(normalized, "v1 obj");
        var fullPath = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var boundedRoot = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(boundedRoot, StringComparison.OrdinalIgnoreCase))
            throw new FormatException($"v1 asset '{assetId}' escapes the catalog root.");
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"v1 asset '{assetId}' is missing.", fullPath);

        assets.Add(new ModelAssetEntry
        {
            AssetId = assetId,
            Role = role,
            Format = "obj",
            Path = normalized,
            Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fullPath))).ToLowerInvariant()
        });
    }
}
