using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using RubikState;

var checks = new ContractChecks();

try
{
    CheckRoundtrips(checks);
    CheckHashContract(checks);
    CheckNegativeInputs(checks);
    CheckExamples(checks);
    CheckTransactionalNativeApply(checks);
}
catch (Exception exception)
{
    checks.Fail($"Unhandled exception: {exception}");
}

return checks.Finish();

static void CheckRoundtrips(ContractChecks checks)
{
    foreach (var size in new[] { 2, 3, 4, 8, 11, 32 })
    {
        var document = SolvedDocument(size);
        var first = RubikStateSerializer.SerializeToUtf8(document);
        var second = RubikStateSerializer.SerializeToUtf8(document);
        var parsed = RubikStateSerializer.Parse(first);

        checks.Check(first.SequenceEqual(second), $"N={size} serialization is deterministic");
        checks.Check(parsed.Success && parsed.Plan is not null, $"N={size} serialized document parses and validates");
        checks.Check(parsed.Plan?.Facelets.SequenceEqual(document.Faces.Flatten()) == true,
            $"N={size} normalized load plan preserves U/R/F/D/L/B facelets");
        checks.Check(parsed.Plan?.StateHash == RubikStateHasher.Calculate(document),
            $"N={size} canonical state hash survives roundtrip");

        using var cube = NativeCube.Create(size);
        checks.Check(parsed.Plan is not null && cube.SetFacelets(parsed.Plan.Facelets), $"N={size} validated plan applies to native cube");
        checks.Check(cube.Facelets.SequenceEqual(document.Faces.Flatten()), $"N={size} native export equals portable input");
    }
}

static void CheckHashContract(ContractChecks checks)
{
    using var metadataAJson = JsonDocument.Parse("{\"z\":2,\"a\":1}");
    using var metadataBJson = JsonDocument.Parse("{\"a\":1,\"z\":2}");
    var baseline = SolvedDocument(3) with { CreatedUtc = "2026-01-01T00:00:00Z", Metadata = metadataAJson.RootElement.Clone() };
    var incidental = baseline with { CreatedUtc = "2027-02-02T03:04:05Z", Source = "other", Metadata = metadataBJson.RootElement.Clone() };
    checks.Check(RubikStateHasher.Calculate(baseline) == RubikStateHasher.Calculate(incidental),
        "hash excludes timestamp, source, metadata, and metadata property order");

    var changedFacelets = baseline.Faces.Flatten();
    (changedFacelets[0], changedFacelets[9]) = (changedFacelets[9], changedFacelets[0]);
    var changed = RubikStateDocument.Create(3, changedFacelets);
    checks.Check(RubikStateHasher.Calculate(baseline) != RubikStateHasher.Calculate(changed),
        "hash changes when normalized physical state changes");

    var canonicalA = RubikStateSerializer.Serialize(baseline);
    var canonicalB = RubikStateSerializer.Serialize(incidental);
    checks.Check(canonicalA.Contains("\"a\": 1") && canonicalA.IndexOf("\"a\": 1", StringComparison.Ordinal) < canonicalA.IndexOf("\"z\": 2", StringComparison.Ordinal),
        "serializer writes metadata object properties deterministically");
    checks.Check(RubikStateSerializer.Parse(canonicalA).Plan?.StateHash == RubikStateSerializer.Parse(canonicalB).Plan?.StateHash,
        "formatting-incidental documents normalize to one hash");
}

static void CheckNegativeInputs(ContractChecks checks)
{
    var valid = RubikStateSerializer.Serialize(SolvedDocument(3));
    ExpectCode(checks, valid[..^8], RubikStateErrorCode.MalformedJson, "truncated JSON");
    ExpectCode(checks, valid.Insert(valid.IndexOf('{') + 1, "\"format\":\"rubik.state\","), RubikStateErrorCode.DuplicateProperty, "duplicate property");
    ExpectMutationCode(checks, valid, root => root["version"] = 2, RubikStateErrorCode.UnsupportedVersion, "unsupported version");
    ExpectMutationCode(checks, valid, root => root["size"] = 33, RubikStateErrorCode.UnsupportedSize, "unsupported size");
    ExpectMutationCode(checks, valid, root => ((JsonObject)root["faces"]!).Remove("B"), RubikStateErrorCode.MissingProperty, "missing face");
    ExpectMutationCode(checks, valid, root => ((JsonObject)root["faces"]!)["X"] = new JsonArray(), RubikStateErrorCode.UnknownProperty, "extra face");
    ExpectMutationCode(checks, valid, root => ((JsonArray)root["faces"]!["U"]!).RemoveAt(0), RubikStateErrorCode.WrongFaceSize, "short face");
    ExpectMutationCode(checks, valid, root => root["faces"]!["U"]![0] = 2, RubikStateErrorCode.WrongColorCount, "wrong color count");
    ExpectMutationCode(checks, valid, root => root["faces"]!["U"]![0] = 99, RubikStateErrorCode.InvalidValue, "invalid color");
    ExpectMutationCode(checks, valid, root => root["stateHash"] = new string('0', 64), RubikStateErrorCode.HashMismatch, "hash mismatch");
    ExpectMutationCode(checks, valid, root => root["unexpected"] = true, RubikStateErrorCode.UnknownProperty, "unknown root member");
    ExpectMutationCode(checks, valid, root => root["source"] = "C:\\secret\\cube.json", RubikStateErrorCode.InvalidValue, "absolute source path");
    ExpectMutationCode(checks, valid, root => root["metadata"] = new JsonObject { ["command"] = "run" }, RubikStateErrorCode.InvalidValue, "executable metadata");

    var oversized = Encoding.UTF8.GetBytes(valid + new string(' ', 1024));
    var oversizedResult = RubikStateSerializer.Parse(oversized, valid.Length);
    checks.Check(!oversizedResult.Success && oversizedResult.Issues.Any(issue => issue.Code == RubikStateErrorCode.InputTooLarge),
        "oversized input is rejected before JSON parsing");
}

static void CheckExamples(ContractChecks checks)
{
    var root = Path.Combine(AppContext.BaseDirectory, "examples");
    foreach (var name in new[] { "solved-3x3.rubik.json", "solved-11x11.rubik.json", "scrambled-3x3.rubik.json" })
    {
        var result = RubikStateSerializer.Parse(File.ReadAllBytes(Path.Combine(root, name)));
        checks.Check(result.Success, $"example {name} parses and validates");
    }

    var wrongCount = RubikStateSerializer.Parse(File.ReadAllBytes(Path.Combine(root, "invalid", "wrong-face-count.rubik.json")));
    checks.Check(!wrongCount.Success && wrongCount.Issues.Any(issue => issue.Code is RubikStateErrorCode.WrongFaceSize or RubikStateErrorCode.WrongColorCount),
        "malformed short-face fixture is rejected");
    var wrongVersion = RubikStateSerializer.Parse(File.ReadAllBytes(Path.Combine(root, "invalid", "unsupported-version.rubik.json")));
    checks.Check(!wrongVersion.Success && wrongVersion.Issues.Any(issue => issue.Code == RubikStateErrorCode.UnsupportedVersion),
        "unsupported-version fixture is rejected");
}

static void CheckTransactionalNativeApply(ContractChecks checks)
{
    using var cube = NativeCube.Create(3);
    var before = cube.Facelets;
    var invalid = RubikStateSerializer.Serialize(SolvedDocument(3)).Replace("\"version\": 1", "\"version\": 9", StringComparison.Ordinal);
    var parsed = RubikStateSerializer.Parse(invalid);
    if (parsed.Success && parsed.Plan is not null)
        cube.SetFacelets(parsed.Plan.Facelets);
    checks.Check(!parsed.Success && cube.Facelets.SequenceEqual(before), "invalid document cannot reach native commit and leaves cube unchanged");

    var valid = RubikStateSerializer.Parse(RubikStateSerializer.Serialize(SolvedDocument(3)));
    checks.Check(valid.Plan is not null && cube.SetFacelets(valid.Plan.Facelets), "fully validated load plan reaches native commit");
}

static RubikStateDocument SolvedDocument(int size)
{
    var area = size * size;
    var facelets = Enumerable.Range(1, 6).SelectMany(color => Enumerable.Repeat(color, area)).ToArray();
    return RubikStateDocument.Create(size, facelets, "contract-test") with { CreatedUtc = "2026-07-16T00:00:00Z" };
}

static void ExpectMutationCode(ContractChecks checks, string valid, Action<JsonObject> mutate, RubikStateErrorCode code, string name)
{
    var root = JsonNode.Parse(valid)!.AsObject();
    mutate(root);
    ExpectCode(checks, root.ToJsonString(), code, name);
}

static void ExpectCode(ContractChecks checks, string json, RubikStateErrorCode code, string name)
{
    var result = RubikStateSerializer.Parse(json);
    checks.Check(!result.Success && result.Issues.Any(issue => issue.Code == code), $"{name} returns {code}");
}

internal sealed class NativeCube : IDisposable
{
    private const string DllName = "RubikEngine.dll";
    private IntPtr _handle;

    private NativeCube(IntPtr handle, int size) { _handle = handle; Size = size; }
    public int Size { get; }
    public static NativeCube Create(int size)
    {
        var handle = Rubik_CreateSized(size);
        return handle != IntPtr.Zero ? new NativeCube(handle, size) : throw new InvalidOperationException($"Could not create N={size} cube.");
    }
    public int[] Facelets
    {
        get
        {
            var values = new int[6 * Size * Size];
            if (Rubik_GetFacelets(_handle, values, values.Length) != values.Length) throw new InvalidOperationException("Facelet read failed.");
            return values;
        }
    }
    public bool SetFacelets(int[] values) => Rubik_SetFacelets(_handle, values, values.Length) != 0;
    public void Dispose() { if (_handle != IntPtr.Zero) { Rubik_Destroy(_handle); _handle = IntPtr.Zero; } }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern IntPtr Rubik_CreateSized(int size);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern void Rubik_Destroy(IntPtr handle);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern int Rubik_GetFacelets(IntPtr handle, [Out] int[] facelets, int capacity);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern int Rubik_SetFacelets(IntPtr handle, [In] int[] facelets, int count);
}

internal sealed class ContractChecks
{
    private int _failures;
    public void Check(bool condition, string message) { Console.WriteLine($"{(condition ? "PASS" : "FAIL")} {message}"); if (!condition) _failures++; }
    public void Fail(string message) => Check(false, message);
    public int Finish() { Console.WriteLine($"RubikStateContractTests: {(_failures == 0 ? "PASS" : $"FAIL ({_failures})")}"); return _failures == 0 ? 0 : 1; }
}
