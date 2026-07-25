using System.Text.Json;
using System.Numerics;
using ModelAssets;

var failures = new List<string>();
Run("schema parses and closes root", TestSchema, failures);
Run("manifest strict roundtrip", TestStrictRoundtrip, failures);
Run("unknown member rejected", TestUnknownMember, failures);
Run("invalid paths and hashes rejected", TestUnsafeValues, failures);
Run("duplicate roles rejected", TestDuplicates, failures);
Run("v1 adapter exposes complete runtime view", TestV1Adapter, failures);
Run("catalog discovers complete legacy Chess2D set", TestCatalogDiscovery, failures);
Run("catalog discovers complete GLB Chess2D set", TestGlbCatalogDiscovery, failures);
Run("incomplete Chess2D set reports missing roles", TestIncompleteSet, failures);
Run("model set selection falls back by semantic id", TestSetSelectionFallback, failures);
Run("Chess3D profile asset plans remain isolated", TestChess3DProfilePlans, failures);
Run("validator accepts synthetic OBJ", TestValidatorAcceptsObj, failures);
Run("validator rejects file and SHA failures", TestValidatorRejectsFiles, failures);
Run("validator rejects malformed geometry", TestValidatorRejectsGeometry, failures);
Run("Khronos adapter skips when unavailable", TestKhronosSkip, failures);
Run("runtime model boundary freezes collections", TestRuntimeBoundary, failures);
Run("runtime resource policy rejects external paths", TestRuntimeResourcePolicy, failures);
Run("runtime checked arithmetic rejects overflow", TestRuntimeArithmetic, failures);
Run("GLB loader reads triangle hierarchy and material", TestGlbTriangle, failures);
Run("GLB loader reads embedded texture and multiple primitives", TestGlbTextureAndPrimitives, failures);
Run("GLB loader rejects corrupt and unsafe accessors", TestGlbFailures, failures);
Run("GLB loader reports optional and rejects required extensions", TestGlbExtensions, failures);
Run("GLB loader enforces declared limits", TestGlbLimits, failures);
Run("exactly five Chess3D rule profiles remain", TestFiveProfiles, failures);

if (failures.Count > 0)
{
    Console.Error.WriteLine($"Model asset contracts failed: {failures.Count}");
    foreach (var failure in failures) Console.Error.WriteLine(failure);
    return 1;
}

Console.WriteLine("Model asset contracts passed.");
return 0;

static void Run(string name, Action test, ICollection<string> failures)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"FAIL {name}: {ex.Message}");
    }
}

static string Root()
{
    var current = new DirectoryInfo(AppContext.BaseDirectory);
    while (current is not null && !File.Exists(Path.Combine(current.FullName, "Chess.sln")))
        current = current.Parent;
    return current?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
}

static void TestSchema()
{
    using var schema = JsonDocument.Parse(File.ReadAllText(
        Path.Combine(Root(), "assets", "models", "asset-manifest-v2.schema.json")));
    Equal("object", schema.RootElement.GetProperty("type").GetString(), "schema root type");
    if (schema.RootElement.GetProperty("additionalProperties").GetBoolean())
        throw new InvalidOperationException("Schema root must reject unknown properties.");
}

static void TestStrictRoundtrip()
{
    var manifest = Sample();
    var json = ModelAssetManifestJson.Serialize(manifest);
    var parsed = ModelAssetManifestJson.Deserialize(json);
    Equal(manifest.SetId, parsed.SetId, "set id");
    Equal(manifest.Assets[0].Role, parsed.Assets[0].Role, "role");
}

static void TestUnknownMember()
{
    var json = ModelAssetManifestJson.Serialize(Sample()).TrimEnd();
    json = json[..^1] + ",\"surprise\":true}";
    Throws<JsonException>(() => ModelAssetManifestJson.Deserialize(json));
}

static void TestUnsafeValues()
{
    Throws<FormatException>(() => ModelAssetManifestRules.ThrowIfInvalid(
        Sample() with { Assets = [Sample().Assets[0] with { Path = "../escape.glb" }] }));
    Throws<FormatException>(() => ModelAssetManifestRules.ThrowIfInvalid(
        Sample() with { Assets = [Sample().Assets[0] with { Path = "C:/private.glb" }] }));
    Throws<FormatException>(() => ModelAssetManifestRules.ThrowIfInvalid(
        Sample() with { Assets = [Sample().Assets[0] with { Sha256 = "abc" }] }));
}

static void TestDuplicates()
{
    var entry = Sample().Assets[0];
    Throws<FormatException>(() => ModelAssetManifestRules.ThrowIfInvalid(
        Sample() with { Assets = [entry, entry with { AssetId = "another" }] }));
}

static void TestV1Adapter()
{
    var path = Path.Combine(Root(), "assets", "models", "chess", "pieces", "piece_sets.json");
    var manifests = PieceSetV1Adapter.Load(path);
    Equal(1, manifests.Count, "v1 set count");
    var manifest = manifests[0];
    Equal("default-obj", manifest.SetId, "adapted set id");
    Equal(14, manifest.Assets.Count, "adapted asset count");
    Equal("pending-review", manifest.License.Status, "legacy license status");
    if (manifest.Assets.Any(asset => asset.Sha256.Length != 64))
        throw new InvalidOperationException("Adapter did not calculate SHA-256.");
}

static void TestCatalogDiscovery()
{
    var result = ModelAssetCatalog.Discover(
        [Path.Combine(Root(), "assets", "models")], "Chess2D");
    var set = result.Sets.Single(item => item.SetId == "default-obj");
    Equal(true, set.IsLegacyV1, "legacy marker");
    Equal(0, ChessModelRoles.MissingChess2DRoles(set).Count, "required Chess2D roles");
    Equal("obj", set.FindRole("chess.white.king")?.Format, "white king format");
    Equal("obj", set.FindRole("chess.board.darkTile")?.Format, "dark tile format");
}

static void TestGlbCatalogDiscovery()
{
    using var fixture = GlbFixture.Create(
        requiredExtension: false,
        invalidIndex: false,
        nanPosition: false);
    var root = Path.GetDirectoryName(fixture.Path)!;
    var assets = ChessModelRoles.Chess2DRequired.Select((role, index) => new ModelAssetEntry
    {
        AssetId = $"asset-{index}",
        Role = role,
        Format = "glb",
        Path = Path.GetFileName(fixture.Path),
        Sha256 = fixture.Sha256
    }).ToArray();
    var manifest = Sample() with
    {
        SetId = "synthetic-complete-glb",
        DisplayName = "Synthetic Complete GLB",
        SupportedApps = ["Chess2D"],
        Assets = assets
    };
    File.WriteAllText(
        Path.Combine(root, ModelAssetCatalog.V2ManifestName),
        ModelAssetManifestJson.Serialize(manifest));

    var catalog = ModelAssetCatalog.Discover([root], "Chess2D");
    var set = catalog.Sets.Single();
    Equal(false, set.IsLegacyV1, "v2 marker");
    Equal(0, ChessModelRoles.MissingChess2DRoles(set).Count, "complete GLB role set");
    Equal("glb", set.FindRole("chess.black.queen")?.Format, "GLB role format");
}

static void TestIncompleteSet()
{
    var manifest = Sample() with
    {
        SupportedApps = ["Chess2D"],
        Assets =
        [
            Sample().Assets[0] with
            {
                AssetId = "white-pawn",
                Role = "chess.white.pawn"
            }
        ]
    };
    var set = new ModelAssetSetDescriptor(
        manifest, Root(), "synthetic", false, "synthetic");
    Equal(13, ChessModelRoles.MissingChess2DRoles(set).Count, "missing role count");
}

static void TestSetSelectionFallback()
{
    var catalog = ModelAssetCatalog.Discover(
        [Path.Combine(Root(), "assets", "models")], "Chess2D");
    var selected = ModelAssetSetSelector.Select(catalog.Sets, "default-obj");
    Equal(false, selected.UsedFallback, "known set selection");
    Equal("default-obj", selected.Set?.SetId, "known set id");

    var missing = ModelAssetSetSelector.Select(catalog.Sets, "deleted-set");
    Equal(true, missing.UsedFallback, "missing set fallback");
    Equal("default-obj", missing.Set?.SetId, "fallback set id");
}

static void TestChess3DProfilePlans()
{
    var plans = new[]
    {
        Chess3DProfileAssetPlanner.Plan("classic-six-side-3d-8x8x8-v0.1"),
        Chess3DProfileAssetPlanner.Plan("single-side-3d-8x8x8-v0.1"),
        Chess3DProfileAssetPlanner.Plan("asgard-convergence-3d-8x8x8-v0.1"),
        Chess3DProfileAssetPlanner.Plan("rubik-convergence-3d-8x8x8-v0.1"),
        Chess3DProfileAssetPlanner.Plan("hodge-projection-duel-3d-8x8x8-v0.1")
    };
    Equal(6, plans.SelectMany(plan => plan.CommonRoles).Distinct().Count(), "common piece roles");
    Equal(0, plans[0].OptionalRoles.Count, "classic optional roles");
    Equal(0, plans[1].OptionalRoles.Count, "single optional roles");
    Equal(true, plans[2].OptionalRoles.Contains("asgard.core"), "Asgard core role");
    Equal(true, plans[3].OptionalRoles.Contains("rubikConvergence.layerMarker"), "Rubik layer role");
    Equal(true, plans[4].OptionalRoles.Contains("hodge.projectionArrow"), "Hodge arrow role");
    Equal(false, plans[4].OptionalRoles.Any(role => role.StartsWith("asgard.", StringComparison.Ordinal)),
        "Hodge isolation");
}

static void TestValidatorAcceptsObj()
{
    using var fixture = ValidatorFixture.Create("v 0 0 0\nv 1 0 0\nv 0 1 0\nvn 0 0 1\nf 1//1 2//1 3//1\n");
    var report = new ModelAssetValidator().Validate(new()
    {
        ManifestPath = fixture.ManifestPath,
        RequireNormals = true,
        RequiredRoles = new HashSet<string> { "chess.white.king" }
    });
    if (!report.IsValid) throw new InvalidOperationException(string.Join("; ", report.Issues.Select(i => i.Message)));
    Equal(3L, report.Stats[0].VertexCount, "validator vertices");
    Equal(1L, report.Stats[0].TriangleCount, "validator triangles");
}

static void TestValidatorRejectsFiles()
{
    using var fixture = ValidatorFixture.Create("v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n");
    File.AppendAllText(fixture.AssetPath, "# changed");
    var report = new ModelAssetValidator().Validate(new() { ManifestPath = fixture.ManifestPath });
    if (report.IsValid || report.Issues.All(i => i.Code != "asset.invalid"))
        throw new InvalidOperationException("SHA mismatch was not rejected.");
    File.Delete(fixture.AssetPath);
    report = new ModelAssetValidator().Validate(new() { ManifestPath = fixture.ManifestPath });
    if (report.IsValid) throw new InvalidOperationException("Missing file was not rejected.");
}

static void TestValidatorRejectsGeometry()
{
    using var fixture = ValidatorFixture.Create("v NaN 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 9\n");
    var report = new ModelAssetValidator().Validate(new() { ManifestPath = fixture.ManifestPath });
    if (report.IsValid) throw new InvalidOperationException("NaN/out-of-range geometry was accepted.");
}

static void TestKhronosSkip()
{
    var old = Environment.GetEnvironmentVariable("KHRONOS_GLTF_VALIDATOR");
    try
    {
        Environment.SetEnvironmentVariable("KHRONOS_GLTF_VALIDATOR", null);
        var result = new KhronosGltfValidatorAdapter()
            .ValidateAsync("not-used.glb", TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
        Equal(KhronosValidatorStatus.Skipped, result.Status, "Khronos status");
    }
    finally
    {
        Environment.SetEnvironmentVariable("KHRONOS_GLTF_VALIDATOR", old);
    }
}

static void TestRuntimeBoundary()
{
    var nodes = new List<RuntimeNode>
    {
        new(0, "root", null, Array.Empty<int>(), RuntimeTransform.Identity, RuntimeTransform.Identity)
    };
    var model = RuntimeModelAsset.Freeze(
        new string('a', 64),
        nodes,
        Array.Empty<RuntimeMesh>(),
        Array.Empty<RuntimeMaterial>(),
        Array.Empty<RuntimeTexture>(),
        new RuntimeBounds(Vector3.Zero, Vector3.One),
        new RuntimeModelDiagnostics(TimeSpan.Zero, 0, Array.Empty<string>(), Array.Empty<RuntimeUnsupportedFeature>()));
    nodes.Add(new(1, "late", null, Array.Empty<int>(), RuntimeTransform.Identity, RuntimeTransform.Identity));
    Equal(1, model.Nodes.Count, "frozen node count");
    if (!model.Bounds.IsFinite) throw new InvalidOperationException("Finite bounds reported invalid.");
}

static void TestRuntimeResourcePolicy()
{
    var root = Path.Combine(Path.GetTempPath(), "runtime-model-root");
    Throws<FormatException>(() => RuntimeModelSecurity.ResolvePackageResource(root, "https://example.invalid/a.bin"));
    Throws<FormatException>(() => RuntimeModelSecurity.ResolvePackageResource(root, "../escape.bin"));
    var local = RuntimeModelSecurity.ResolvePackageResource(root, "textures/base.png");
    if (!local.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Local resource did not stay below package root.");
}

static void TestRuntimeArithmetic()
{
    Equal(32, RuntimeModelSecurity.CheckedRangeEnd(0, 3, 12, 8), "checked range");
    Throws<OverflowException>(() => RuntimeModelSecurity.CheckedRangeEnd(
        int.MaxValue - 1, 2, int.MaxValue, 4));
    Throws<FormatException>(() => RuntimeModelSecurity.CheckedRangeEnd(0, 1, 4, 8));
}

static void TestGlbTriangle()
{
    using var fixture = GlbFixture.Create(requiredExtension: false, invalidIndex: false, nanPosition: false);
    var model = new GlbRuntimeModelLoader().LoadAsync(new()
    {
        Path = fixture.Path,
        ExpectedSha256 = fixture.Sha256
    }).GetAwaiter().GetResult();
    Equal(1, model.Meshes.Count, "GLB mesh count");
    Equal(1, model.Meshes[0].Primitives.Count, "GLB primitive count");
    Equal(3, model.Meshes[0].Primitives[0].Vertices.Positions.Count, "GLB vertices");
    Equal(3, model.Meshes[0].Primitives[0].Indices.Indices.Count, "GLB indices");
    Equal(1, model.Materials.Count, "GLB materials");
    if (!model.Bounds.IsFinite || model.Bounds.Minimum.X < 1.9f)
        throw new InvalidOperationException("Nested node translation did not affect world bounds.");
}

static void TestGlbTextureAndPrimitives()
{
    using var fixture = GlbFixture.Create(false, false, false, textured: true, multiplePrimitives: true);
    var model = Load(fixture);
    Equal(2, model.Meshes[0].Primitives.Count, "multiple primitive count");
    Equal(1, model.Textures.Count, "embedded texture count");
    Equal("image/png", model.Textures[0].MimeType, "embedded texture MIME");
    Equal(0, model.Materials[0].BaseColorTextureIndex, "base color texture index");
}

static void TestGlbFailures()
{
    using (var fixture = GlbFixture.Create(false, invalidIndex: true, nanPosition: false))
        Throws<FormatException>(() => Load(fixture));
    using (var fixture = GlbFixture.Create(false, invalidIndex: false, nanPosition: true))
        Throws<FormatException>(() => Load(fixture));
    using (var fixture = GlbFixture.Create(false, false, false))
    {
        var bytes = File.ReadAllBytes(fixture.Path);
        bytes[0] = 0;
        File.WriteAllBytes(fixture.Path, bytes);
        var sha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
        Throws<FormatException>(() => new GlbRuntimeModelLoader().LoadAsync(new()
        {
            Path = fixture.Path,
            ExpectedSha256 = sha
        }).GetAwaiter().GetResult());
    }
}

static void TestGlbExtensions()
{
    using (var optional = GlbFixture.Create(false, false, false, optionalExtension: true))
    {
        var model = Load(optional);
        Equal(1, model.Diagnostics.UnsupportedFeatures.Count, "optional extension diagnostics");
    }
    using var required = GlbFixture.Create(requiredExtension: true, false, false);
    Throws<NotSupportedException>(() => Load(required));
}

static void TestGlbLimits()
{
    using var fixture = GlbFixture.Create(false, false, false);
    Throws<FormatException>(() => new GlbRuntimeModelLoader().LoadAsync(new()
    {
        Path = fixture.Path,
        ExpectedSha256 = fixture.Sha256,
        Limits = new RuntimeModelLoadLimits { MaxFileBytes = 20 }
    }).GetAwaiter().GetResult());
}

static RuntimeModelAsset Load(GlbFixture fixture) =>
    new GlbRuntimeModelLoader().LoadAsync(new()
    {
        Path = fixture.Path,
        ExpectedSha256 = fixture.Sha256
    }).GetAwaiter().GetResult();

static void TestFiveProfiles()
{
    var profileRoot = Path.Combine(Root(), "assets", "rules", "profiles");
    var rulesetIds = Directory.EnumerateFiles(profileRoot, "*.json")
        .Select(path =>
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.TryGetProperty("rulesetId", out var id) ? id.GetString() : null;
        })
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .ToArray();
    Equal(5, rulesetIds.Length, "ruleset profile count");
    Equal(5, rulesetIds.Distinct(StringComparer.Ordinal).Count(), "unique ruleset profile count");
}

static ModelAssetManifest Sample() => new()
{
    Format = ModelAssetManifestJson.CurrentFormat,
    Version = ModelAssetManifestJson.CurrentVersion,
    SetId = "synthetic-test",
    DisplayName = "Synthetic Test",
    Author = "Repository test",
    License = new ModelAssetLicense { SpdxId = "CC0-1.0", Status = "approved" },
    Source = new ModelAssetSource { Provenance = "Generated by contract test" },
    SupportedApps = ["Chess2D"],
    Units = "unit",
    CoordinateSystem = "right-handed-y-up",
    DefaultScale = 1,
    Assets =
    [
        new ModelAssetEntry
        {
            AssetId = "synthetic.white.king",
            Role = "chess.white.king",
            Format = "glb",
            Path = "models/king.glb",
            Sha256 = new string('a', 64)
        }
    ]
};

static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

static void Equal<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{label}: expected '{expected}', got '{actual}'.");
}

sealed class ValidatorFixture : IDisposable
{
    private readonly string _root;
    public string ManifestPath { get; }
    public string AssetPath { get; }

    private ValidatorFixture(string root, string manifestPath, string assetPath)
    {
        _root = root;
        ManifestPath = manifestPath;
        AssetPath = assetPath;
    }

    public static ValidatorFixture Create(string obj)
    {
        var root = Path.Combine(Path.GetTempPath(), "chess-model-assets", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "assets"));
        var assetPath = Path.Combine(root, "assets", "king.obj");
        File.WriteAllText(assetPath, obj);
        var sha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            File.ReadAllBytes(assetPath))).ToLowerInvariant();
        var sample = CreateSampleManifest();
        var manifest = sample with
        {
            Assets = [sample.Assets[0] with
            {
                Format = "obj",
                Path = "assets/king.obj",
                Sha256 = sha
            }]
        };
        var manifestPath = Path.Combine(root, "asset-manifest-v2.json");
        File.WriteAllText(manifestPath, ModelAssetManifestJson.Serialize(manifest));
        return new(root, manifestPath, assetPath);
    }

    private static ModelAssetManifest CreateSampleManifest() => new()
    {
        Format = ModelAssetManifestJson.CurrentFormat,
        Version = ModelAssetManifestJson.CurrentVersion,
        SetId = "synthetic-validator",
        DisplayName = "Synthetic Validator",
        Author = "Repository test",
        License = new ModelAssetLicense { SpdxId = "CC0-1.0", Status = "approved" },
        Source = new ModelAssetSource { Provenance = "Generated by contract test" },
        SupportedApps = ["Chess2D"],
        Units = "unit",
        CoordinateSystem = "right-handed-y-up",
        DefaultScale = 1,
        Assets =
        [
            new ModelAssetEntry
            {
                AssetId = "synthetic.validator.king",
                Role = "chess.white.king",
                Format = "obj",
                Path = "assets/king.obj",
                Sha256 = new string('0', 64)
            }
        ]
    };

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}

sealed class GlbFixture : IDisposable
{
    private readonly string _root;
    public string Path { get; }
    public string Sha256 { get; }

    private GlbFixture(string root, string path, string sha256)
    {
        _root = root;
        Path = path;
        Sha256 = sha256;
    }

    public static GlbFixture Create(
        bool requiredExtension,
        bool invalidIndex,
        bool nanPosition,
        bool optionalExtension = false,
        bool textured = false,
        bool multiplePrimitives = false)
    {
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chess-glb", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = System.IO.Path.Combine(root, "fixture.glb");
        var binary = new List<byte>();
        void Float(float value) => binary.AddRange(BitConverter.GetBytes(value));
        Float(nanPosition ? float.NaN : 0); Float(0); Float(0);
        Float(1); Float(0); Float(0);
        Float(0); Float(1); Float(0);
        var indexOffset = binary.Count;
        binary.AddRange(BitConverter.GetBytes((ushort)0));
        binary.AddRange(BitConverter.GetBytes((ushort)1));
        binary.AddRange(BitConverter.GetBytes((ushort)(invalidIndex ? 9 : 2)));
        while (binary.Count % 4 != 0) binary.Add(0);
        var imageOffset = binary.Count;
        if (textured) binary.AddRange(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a });

        var bufferViews = new List<object>
        {
            new Dictionary<string, object?> { ["buffer"] = 0, ["byteOffset"] = 0, ["byteLength"] = 36 },
            new Dictionary<string, object?> { ["buffer"] = 0, ["byteOffset"] = indexOffset, ["byteLength"] = 6 }
        };
        if (textured)
            bufferViews.Add(new Dictionary<string, object?> { ["buffer"] = 0, ["byteOffset"] = imageOffset, ["byteLength"] = 8 });
        var primitive = new Dictionary<string, object?>
        {
            ["attributes"] = new Dictionary<string, object?> { ["POSITION"] = 0 },
            ["indices"] = 1,
            ["material"] = 0
        };
        var primitives = multiplePrimitives ? new object[] { primitive, new Dictionary<string, object?>(primitive) } : new object[] { primitive };
        var pbr = new Dictionary<string, object?>
        {
            ["baseColorFactor"] = new[] { 0.9, 0.8, 0.7, 1.0 }
        };
        if (textured) pbr["baseColorTexture"] = new Dictionary<string, object?> { ["index"] = 0 };

        var gltf = new Dictionary<string, object?>
        {
            ["asset"] = new Dictionary<string, object?> { ["version"] = "2.0" },
            ["buffers"] = new object[] { new Dictionary<string, object?> { ["byteLength"] = binary.Count } },
            ["bufferViews"] = bufferViews,
            ["accessors"] = new object[]
            {
                new Dictionary<string, object?> { ["bufferView"] = 0, ["componentType"] = 5126, ["count"] = 3, ["type"] = "VEC3" },
                new Dictionary<string, object?> { ["bufferView"] = 1, ["componentType"] = 5123, ["count"] = 3, ["type"] = "SCALAR" }
            },
            ["materials"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["name"] = "Ivory",
                    ["pbrMetallicRoughness"] = pbr
                }
            },
            ["meshes"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["name"] = "Triangle",
                    ["primitives"] = primitives
                }
            },
            ["nodes"] = new object[]
            {
                new Dictionary<string, object?> { ["name"] = "Parent", ["translation"] = new[] { 2, 0, 0 }, ["children"] = new[] { 1 } },
                new Dictionary<string, object?> { ["name"] = "Piece", ["mesh"] = 0 }
            },
            ["scenes"] = new object[] { new Dictionary<string, object?> { ["nodes"] = new[] { 0 } } },
            ["scene"] = 0
        };
        if (optionalExtension || requiredExtension)
            gltf["extensionsUsed"] = new[] { "KHR_example_unsupported" };
        if (requiredExtension)
            gltf["extensionsRequired"] = new[] { "KHR_example_unsupported" };
        if (textured)
        {
            gltf["images"] = new object[]
            {
                new Dictionary<string, object?> { ["bufferView"] = 2, ["mimeType"] = "image/png", ["name"] = "Tiny" }
            };
            gltf["textures"] = new object[]
            {
                new Dictionary<string, object?> { ["source"] = 0 }
            };
        }
        var json = JsonSerializer.Serialize(gltf);
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
        var paddedJson = Pad(jsonBytes, 0x20);
        var paddedBin = Pad(binary.ToArray(), 0);
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write((uint)0x46546C67);
        writer.Write((uint)2);
        writer.Write((uint)(12 + 8 + paddedJson.Length + 8 + paddedBin.Length));
        writer.Write((uint)paddedJson.Length);
        writer.Write((uint)0x4E4F534A);
        writer.Write(paddedJson);
        writer.Write((uint)paddedBin.Length);
        writer.Write((uint)0x004E4942);
        writer.Write(paddedBin);
        writer.Flush();
        writer.Dispose();
        stream.Dispose();
        var sha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            File.ReadAllBytes(path))).ToLowerInvariant();
        return new(root, path, sha);
    }

    private static byte[] Pad(byte[] input, byte value)
    {
        var length = (input.Length + 3) & ~3;
        var result = Enumerable.Repeat(value, length).Select(item => (byte)item).ToArray();
        input.CopyTo(result, 0);
        return result;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
