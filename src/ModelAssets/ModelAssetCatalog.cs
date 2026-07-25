namespace ModelAssets;

public sealed record ModelAssetSetDescriptor(
    ModelAssetManifest Manifest,
    string PackageRoot,
    string ManifestPath,
    bool IsLegacyV1,
    string Diagnostics)
{
    public string SetId => Manifest.SetId;
    public string DisplayName => Manifest.DisplayName;

    public ModelAssetEntry? FindRole(string role) =>
        Manifest.Assets.FirstOrDefault(asset =>
            string.Equals(asset.Role, role, StringComparison.Ordinal));

    public string ResolveAssetPath(ModelAssetEntry asset) =>
        RuntimeModelSecurity.ResolvePackageResource(PackageRoot, asset.Path);
}

public sealed record ModelAssetCatalogResult(
    IReadOnlyList<ModelAssetSetDescriptor> Sets,
    IReadOnlyList<string> Diagnostics);

public sealed record ModelAssetSetSelection(
    ModelAssetSetDescriptor? Set,
    bool UsedFallback,
    string Diagnostics);

public static class ModelAssetSetSelector
{
    public static ModelAssetSetSelection Select(
        IEnumerable<ModelAssetSetDescriptor> sets,
        string? requestedSetId)
    {
        var values = sets.ToArray();
        if (!string.IsNullOrWhiteSpace(requestedSetId))
        {
            var requested = values.FirstOrDefault(set =>
                string.Equals(set.SetId, requestedSetId, StringComparison.OrdinalIgnoreCase));
            if (requested is not null)
                return new(requested, false, $"selected '{requested.SetId}'");
        }

        var fallback = values.FirstOrDefault();
        return fallback is null
            ? new(null, true, "requested model set is unavailable; procedural fallback")
            : new(fallback, true,
                $"requested model set '{requestedSetId ?? "(none)"}' is unavailable; fallback '{fallback.SetId}'");
    }
}

public static class ModelAssetCatalog
{
    public const string V2ManifestName = "asset-manifest-v2.json";
    public const string LegacyPieceCatalogName = "piece_sets.json";

    public static ModelAssetCatalogResult Discover(
        IEnumerable<string> roots,
        string supportedApp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(supportedApp);
        var sets = new Dictionary<string, ModelAssetSetDescriptor>(StringComparer.OrdinalIgnoreCase);
        var diagnostics = new List<string>();

        foreach (var candidate in roots.Where(path => !string.IsNullOrWhiteSpace(path))
                     .Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(candidate)) continue;
            DiscoverV2(candidate, supportedApp, sets, diagnostics);
            DiscoverLegacy(candidate, supportedApp, sets, diagnostics);
        }

        return new(
            sets.Values.OrderBy(set => set.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray(),
            diagnostics.AsReadOnly());
    }

    private static void DiscoverV2(
        string root,
        string supportedApp,
        IDictionary<string, ModelAssetSetDescriptor> sets,
        ICollection<string> diagnostics)
    {
        foreach (var path in Directory.EnumerateFiles(root, V2ManifestName, SearchOption.AllDirectories))
        {
            try
            {
                var manifest = ModelAssetManifestJson.Deserialize(File.ReadAllText(path));
                if (!Supports(manifest, supportedApp)) continue;
                AddPreferred(sets, new(
                    manifest,
                    Path.GetDirectoryName(path)!,
                    path,
                    false,
                    $"manifest-v2; {manifest.Assets.Count} semantic assets"));
            }
            catch (Exception ex)
            {
                diagnostics.Add($"{path}: {ex.Message}");
            }
        }
    }

    private static void DiscoverLegacy(
        string root,
        string supportedApp,
        IDictionary<string, ModelAssetSetDescriptor> sets,
        ICollection<string> diagnostics)
    {
        foreach (var path in Directory.EnumerateFiles(root, LegacyPieceCatalogName, SearchOption.AllDirectories))
        {
            try
            {
                foreach (var manifest in PieceSetV1Adapter.Load(path).Where(item => Supports(item, supportedApp)))
                {
                    AddPreferred(sets, new(
                        manifest,
                        Path.GetDirectoryName(path)!,
                        path,
                        true,
                        $"legacy-v1 OBJ adapter; {manifest.Assets.Count} semantic assets"));
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add($"{path}: {ex.Message}");
            }
        }
    }

    private static bool Supports(ModelAssetManifest manifest, string app) =>
        manifest.SupportedApps.Any(value =>
            string.Equals(value, app, StringComparison.OrdinalIgnoreCase));

    private static void AddPreferred(
        IDictionary<string, ModelAssetSetDescriptor> sets,
        ModelAssetSetDescriptor candidate)
    {
        if (!sets.TryGetValue(candidate.SetId, out var current) ||
            current.IsLegacyV1 && !candidate.IsLegacyV1)
            sets[candidate.SetId] = candidate;
    }
}

public static class ChessModelRoles
{
    private static readonly string[] PieceNames =
        ["pawn", "knight", "bishop", "rook", "queen", "king"];

    public static IReadOnlyList<string> Chess2DRequired { get; } =
        PieceNames.SelectMany(name => new[] { $"chess.white.{name}", $"chess.black.{name}" })
            .Concat(["chess.board.lightTile", "chess.board.darkTile"])
            .ToArray();

    public static string Piece(int pieceCode)
    {
        var index = Math.Abs(pieceCode) - 1;
        if ((uint)index >= (uint)PieceNames.Length)
            throw new ArgumentOutOfRangeException(nameof(pieceCode));
        return $"chess.{(pieceCode > 0 ? "white" : "black")}.{PieceNames[index]}";
    }

    public static string BoardTile(bool light) =>
        light ? "chess.board.lightTile" : "chess.board.darkTile";

    public static string Chess3DCommonPiece(int pieceType)
    {
        var index = Math.Abs(pieceType) - 1;
        if ((uint)index >= (uint)PieceNames.Length)
            throw new ArgumentOutOfRangeException(nameof(pieceType));
        return $"chess3d.common.{PieceNames[index]}";
    }

    public static IReadOnlyList<string> MissingChess2DRoles(ModelAssetSetDescriptor set) =>
        Chess2DRequired.Where(role => set.FindRole(role) is null).ToArray();
}

public sealed record Chess3DProfileAssetPlan(
    string Mode,
    IReadOnlyList<string> CommonRoles,
    IReadOnlyList<string> OptionalRoles);

public static class Chess3DProfileAssetPlanner
{
    private static readonly string[] CommonRoles =
    [
        "chess3d.common.pawn", "chess3d.common.knight", "chess3d.common.bishop",
        "chess3d.common.rook", "chess3d.common.queen", "chess3d.common.king"
    ];

    public static Chess3DProfileAssetPlan Plan(string rulesetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rulesetId);
        if (rulesetId.Contains("rubik-convergence", StringComparison.OrdinalIgnoreCase))
            return New("rubik-convergence",
                "asgard.core", "asgard.anchor", "asgard.reserveSlot", "asgard.fusionMarker",
                "rubikConvergence.core", "rubikConvergence.layerMarker", "rubikConvergence.turnMarker");
        if (rulesetId.Contains("asgard-convergence", StringComparison.OrdinalIgnoreCase))
            return New("asgard",
                "asgard.core", "asgard.anchor", "asgard.reserveSlot", "asgard.fusionMarker");
        if (rulesetId.Contains("hodge-projection", StringComparison.OrdinalIgnoreCase))
            return New("hodge",
                "hodge.primaryMarker", "hodge.mirrorMarker", "hodge.projectionArrow");
        if (rulesetId.Contains("single-side", StringComparison.OrdinalIgnoreCase))
            return New("single-side");
        return New("classic");
    }

    private static Chess3DProfileAssetPlan New(string mode, params string[] optionalRoles) =>
        new(mode, CommonRoles, optionalRoles);
}
