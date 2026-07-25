using System.Text.Json;
using ModelAssets;

var failures = new List<string>();
Run("schema parses and closes root", TestSchema, failures);
Run("manifest strict roundtrip", TestStrictRoundtrip, failures);
Run("unknown member rejected", TestUnknownMember, failures);
Run("invalid paths and hashes rejected", TestUnsafeValues, failures);
Run("duplicate roles rejected", TestDuplicates, failures);
Run("v1 adapter exposes complete runtime view", TestV1Adapter, failures);
Run("validator accepts synthetic OBJ", TestValidatorAcceptsObj, failures);
Run("validator rejects file and SHA failures", TestValidatorRejectsFiles, failures);
Run("validator rejects malformed geometry", TestValidatorRejectsGeometry, failures);
Run("Khronos adapter skips when unavailable", TestKhronosSkip, failures);
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
