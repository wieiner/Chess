using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using RubikState;
using RubikVisuals;

var checks = new ContractChecks();

try
{
    CheckRoundtrips(checks);
    CheckHashContract(checks);
    CheckNegativeInputs(checks);
    CheckExamples(checks);
    CheckTransactionalNativeApply(checks);
    CheckAtomicFileService(checks);
    CheckElevenByElevenFileRoundtrips(checks);
    CheckPhysicalEditorDraft(checks);
    CheckStructuredValidationDiagnostics(checks);
    CheckFaceletDecomposition(checks);
    CheckSolvabilityValidation(checks);
    CheckPhysicalElevenByElevenWorkflow(checks);
    CheckSolverContracts(checks);
    CheckSolutionVerification(checks);
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

static void CheckAtomicFileService(ContractChecks checks)
{
    var directory = Path.Combine(Path.GetTempPath(), "Chess-RubikStateContracts", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var service = new RubikStateFileService();
        var path = Path.Combine(directory, "cube.rubik.json");
        var original = SolvedDocument(3) with { Source = "atomic-original" };
        var initialSave = service.Save(path, original);
        checks.Check(initialSave.Success && File.Exists(path), "atomic service creates first destination");
        var originalBytes = File.ReadAllBytes(path);

        var changedFacelets = original.Faces.Flatten();
        (changedFacelets[0], changedFacelets[9]) = (changedFacelets[9], changedFacelets[0]);
        var changed = RubikStateDocument.Create(3, changedFacelets, "atomic-replacement") with { CreatedUtc = "2026-07-16T00:00:00Z" };

        foreach (var stage in Enum.GetValues<RubikFileWriteStage>())
        {
            var result = service.Save(path, changed, failureInjector: new ThrowAtStage(stage));
            checks.Check(!result.Success, $"injected {stage} failure is reported");
            checks.Check(File.ReadAllBytes(path).SequenceEqual(originalBytes), $"injected {stage} preserves destination bytes");
            checks.Check(service.Read(path).Success, $"destination remains readable after injected {stage}");
            checks.Check(!Directory.EnumerateFiles(directory, ".*.tmp").Any(), $"injected {stage} leaves no sibling temp");
        }

        var replacement = service.Save(path, changed, retainBackup: true);
        checks.Check(replacement.Success && replacement.BackupPath is not null && File.Exists(replacement.BackupPath),
            "successful replacement optionally retains backup");
        checks.Check(service.Read(path).LoadPlan?.Facelets.SequenceEqual(changedFacelets) == true,
            "replacement destination contains new validated state");
        checks.Check(replacement.BackupPath is not null && File.ReadAllBytes(replacement.BackupPath).SequenceEqual(originalBytes),
            "backup contains previous destination bytes");

        File.WriteAllBytes(Path.Combine(directory, "oversized.rubik.json"), new byte[128]);
        var oversized = service.Read(Path.Combine(directory, "oversized.rubik.json"), maximumBytes: 64);
        checks.Check(!oversized.Success && oversized.ErrorCode == RubikFileErrorCode.InputTooLarge,
            "bounded file read rejects oversized input before allocation/parsing");

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var cancelledResult = service.Save(Path.Combine(directory, "cancelled.rubik.json"), original, cancellationToken: cancelled.Token);
        checks.Check(!cancelledResult.Success && cancelledResult.ErrorCode == RubikFileErrorCode.Cancelled,
            "cancelled write reports a stable error and creates no destination");
    }
    finally
    {
        try { Directory.Delete(directory, recursive: true); } catch { }
    }
}

static void CheckElevenByElevenFileRoundtrips(ContractChecks checks)
{
    var scenarios = new (string Name, Action<NativeCube> Mutate)[]
    {
        ("solved", _ => { }),
        ("short-scramble", cube => checks.Check(cube.Scramble(20260716, 12), "N=11 short scramble succeeds")),
        ("inner-slice", cube => checks.Check(cube.RotateLayer(0, 1, 1), "N=11 inner slice succeeds")),
        ("wide-move", cube =>
        {
            checks.Check(cube.RotateLayer(2, 10, 1) && cube.RotateLayer(2, 9, 1), "N=11 two-layer wide move succeeds");
        }),
        ("whole-cube", cube =>
        {
            var success = true;
            for (var layer = 0; layer < 11; layer++) success &= cube.RotateLayer(1, layer, 1);
            checks.Check(success, "N=11 whole-cube layer sequence succeeds");
        })
    };

    var directory = Path.Combine(Path.GetTempPath(), "Chess-Rubik11Roundtrips", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var service = new RubikStateFileService();
        foreach (var scenario in scenarios)
        {
            using var source = NativeCube.Create(11);
            scenario.Mutate(source);
            var sourceFacelets = source.Facelets;
            var document = RubikStateDocument.Create(11, sourceFacelets, $"regression-{scenario.Name}") with
            {
                CreatedUtc = "2026-07-16T00:00:00Z"
            };
            var sourceHash = RubikStateHasher.Calculate(document);
            var sourceVisual = ShellSignature(11, sourceFacelets);
            var path = Path.Combine(directory, $"{scenario.Name}.rubik.json");

            checks.Check(service.Save(path, document).Success, $"{scenario.Name} atomic save succeeds");
            var read = service.Read(path);
            checks.Check(read.Success && read.LoadPlan is not null, $"{scenario.Name} bounded read succeeds");
            checks.Check(read.LoadPlan?.StateHash == sourceHash, $"{scenario.Name} hash survives file roundtrip");

            using var loaded = NativeCube.Create(11);
            checks.Check(read.LoadPlan is not null && loaded.SetFacelets(read.LoadPlan.Facelets), $"{scenario.Name} validated file applies to fresh native cube");
            checks.Check(loaded.Facelets.SequenceEqual(sourceFacelets), $"{scenario.Name} exact facelets survive reset/load");
            checks.Check(RubikStateHasher.Calculate(RubikStateDocument.Create(11, loaded.Facelets)) == sourceHash,
                $"{scenario.Name} exported loaded hash equals saved hash");
            var loadedState = loaded.State;
            checks.Check(loadedState.ManualState == 1 && loadedState.HistoryCount == 0,
                $"{scenario.Name} physical import does not manufacture trusted history");
            checks.Check(!loaded.HasOrientationAt(0, 0, 0), $"{scenario.Name} physical import reports decomposition unavailable");
            var loadedVisual = ShellSignature(11, loaded.Facelets);
            checks.Check(loadedVisual.StickerCount == 726 && loadedVisual.InvalidCount == 0 && loadedVisual.Fallback,
                $"{scenario.Name} loaded renderer uses complete honest facelet shell");
            checks.Check(loadedVisual.Signature.SequenceEqual(sourceVisual.Signature),
                $"{scenario.Name} world-face/color visual descriptors survive file roundtrip");
        }
    }
    finally
    {
        try { Directory.Delete(directory, recursive: true); } catch { }
    }
}

static (int StickerCount, int InvalidCount, bool Fallback, string[] Signature) ShellSignature(int size, int[] facelets)
{
    var cubies = new List<RubikCubieVisualInput>(size * size * size);
    var maximum = size - 1;
    for (var z = 0; z < size; z++)
    for (var y = 0; y < size; y++)
    for (var x = 0; x < size; x++)
    {
        var mask = 0;
        if (y == maximum) mask |= 1 << (int)RubikFace.U;
        if (x == maximum) mask |= 1 << (int)RubikFace.R;
        if (z == maximum) mask |= 1 << (int)RubikFace.F;
        if (y == 0) mask |= 1 << (int)RubikFace.D;
        if (x == 0) mask |= 1 << (int)RubikFace.L;
        if (z == 0) mask |= 1 << (int)RubikFace.B;
        cubies.Add(new RubikCubieVisualInput(new RubikCoordinate(x, y, z), z * size * size + y * size + x, mask, null));
    }
    var summary = RubikVisualDescriptorBuilder.BuildScene(size, facelets, cubies, surfaceOnly: true);
    var signature = summary.Cubies.SelectMany(cubie => cubie.Stickers.Select(sticker =>
            $"{cubie.Coordinate.X},{cubie.Coordinate.Y},{cubie.Coordinate.Z}:{sticker.WorldFace}:{sticker.ColorId}"))
        .OrderBy(value => value, StringComparer.Ordinal)
        .ToArray();
    return (summary.StickersRendered, summary.InvalidStickers, summary.FallbackRendererActive, signature);
}

static void CheckPhysicalEditorDraft(ContractChecks checks)
{
    var draft = new RubikFaceEditorDraft(11);
    checks.Check(draft.Summarize().EmptyCells == 6 * 121 && !draft.Summarize().BasicCountsValid,
        "empty N=11 editor draft is isolated and incomplete");
    draft.Paint(0, 0, 0, 1);
    checks.Check(draft.GetCell(0, 0, 0) == 1 && draft.CanUndo, "paint updates only managed draft and records undo");
    checks.Check(draft.Undo() && draft.GetCell(0, 0, 0) == 0, "editor undo restores prior cell");
    checks.Check(draft.Redo() && draft.GetCell(0, 0, 0) == 1, "editor redo restores paint");
    draft.ClearAll();
    for (var face = 0; face < 6; face++) draft.FillFace(face, face + 1);
    var summary = draft.Summarize();
    checks.Check(summary.BasicCountsValid && summary.EmptyCells == 0, "six solved N=11 faces satisfy basic color counts");
    checks.Check(summary.OrientationGuidance.Contains("U=1", StringComparison.Ordinal), "odd N reports explicit center orientation suggestion");
    checks.Check(draft.ToStateDocument().Faces.Flatten().SequenceEqual(draft.Flatten()), "valid draft converts to portable physical document");

    var copied = draft.CopyFaceText(0);
    draft.ClearFace(1);
    draft.PasteFaceText(1, copied);
    checks.Check(draft.GetFace(1).All(value => value == 1), "copy/paste face uses exactly N*N bounded color IDs");

    var rotation = new RubikFaceEditorDraft(3);
    rotation.PasteFaceText(0, "1 2 3 4 5 6 1 2 3");
    rotation.RotateFaceClockwise(0);
    checks.Check(rotation.GetFace(0).SequenceEqual(new[] { 1, 4, 1, 2, 5, 2, 3, 6, 3 }), "face view rotation maps matrix clockwise");

    var even = new RubikFaceEditorDraft(4);
    checks.Check(even.Summarize().OrientationGuidance.Contains("no single fixed center", StringComparison.Ordinal),
        "even N requires explicit orientation and is never silently inferred");

    var directory = Path.Combine(Path.GetTempPath(), "Chess-RubikDraftContracts", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var path = Path.Combine(directory, "incomplete.rubikdraft.json");
        var incomplete = new RubikFaceEditorDraft(11);
        incomplete.Paint(2, 4, 7, 3);
        RubikFaceEditorDraftSerializer.SaveAtomic(path, incomplete);
        var loaded = RubikFaceEditorDraftSerializer.Load(path);
        checks.Check(loaded.Size == 11 && loaded.Flatten().SequenceEqual(incomplete.Flatten()),
            "incomplete N=11 draft saves and loads without touching portable state rules");
    }
    finally
    {
        try { Directory.Delete(directory, recursive: true); } catch { }
    }
}

static void CheckStructuredValidationDiagnostics(ContractChecks checks)
{
    var empty = new RubikFaceEditorDraft(11);
    var emptyReport = RubikPhysicalStateDiagnostics.ValidateDraft(empty, maximumCellIssues: 4);
    var first = emptyReport.Issues.FirstOrDefault(issue => issue.Code == RubikValidationCodes.MissingSticker);
    checks.Check(first is { Face: "U", Row: 0, Column: 0, Severity: RubikValidationSeverity.Error },
        "structured missing-sticker issue identifies exact U cell");
    checks.Check(emptyReport.Issues.Count(issue => issue.Code == RubikValidationCodes.MissingSticker) == 4 &&
                 emptyReport.Issues.Count(issue => issue.Code == RubikValidationCodes.ColorUnderflow) == 6,
        "cell diagnostics are bounded while all color underflow summaries remain present");

    var imbalanced = new RubikFaceEditorDraft(3);
    for (var face = 0; face < 6; face++) imbalanced.FillFace(face, 1);
    var imbalanceReport = RubikPhysicalStateDiagnostics.ValidateDraft(imbalanced);
    checks.Check(imbalanceReport.Issues.Any(issue => issue.Code == RubikValidationCodes.ColorOverflow) &&
                 imbalanceReport.Issues.Any(issue => issue.Code == RubikValidationCodes.ColorUnderflow),
        "structured diagnostics distinguish color overflow and underflow");
    checks.Check(imbalanceReport.Issues.Any(issue => issue.Code == RubikValidationCodes.InvalidCenterScheme),
        "odd-N duplicate centers produce invalidCenterScheme");

    var solved = new RubikFaceEditorDraft(3);
    for (var face = 0; face < 6; face++) solved.FillFace(face, face + 1);
    var solvedReport = RubikPhysicalStateDiagnostics.ValidateDraft(solved);
    checks.Check(solvedReport.BasicCountsValid && solvedReport.ErrorCount == 0 && solvedReport.Issues.Count == 0,
        "solved editor draft has a clean structured report");
    using var json = JsonDocument.Parse(emptyReport.ToSanitizedJson());
    checks.Check(json.RootElement.GetProperty("format").GetString() == "rubik.validation-report" &&
                 !emptyReport.ToSanitizedJson().Contains("facelets", StringComparison.OrdinalIgnoreCase) &&
                 !emptyReport.ToSanitizedJson().Contains("path", StringComparison.OrdinalIgnoreCase),
        "sanitized validation report contains diagnostics but no payload or local path");
}

static void CheckFaceletDecomposition(ContractChecks checks)
{
    foreach (var size in new[] { 2, 3, 4, 5, 8, 11 })
    {
        var solved = SolvedDocument(size);
        var decomposition = RubikCubieDecomposer.Decompose(solved);
        var inner = Math.Max(0, size - 2);
        checks.Check(decomposition.Complete, $"solved N={size} decomposes without invented inventory");
        checks.Check(decomposition.Corners.Count == 8 && decomposition.Wings.Count == 12 * inner &&
                     decomposition.Centers.Count == 6 * inner * inner,
            $"N={size} corner/wing/center topology counts are exact");

        using var cube = NativeCube.Create(size);
        checks.Check(cube.Scramble(1800 + size, Math.Min(12, size + 4)), $"N={size} legal decomposition scramble succeeds");
        var scrambled = RubikCubieDecomposer.Decompose(size, cube.Facelets);
        checks.Check(scrambled.Complete, $"legal native scramble N={size} preserves decomposable inventory");
        checks.Check(scrambled.Wings.Select(wing => wing.Coordinate).Distinct().Count() == scrambled.Wings.Count &&
                     scrambled.Wings.All(wing => wing.OrbitIndex >= 1 && wing.WingIndex is > 0 && wing.WingIndex < size - 1),
            $"N={size} duplicate color-pair wings retain distinct coordinates and bounded orbit/index observations");
    }

    var badCorner = SolvedDocument(3).Faces.Flatten();
    var dCorner = 3 * 9 + 2 * 3;
    var lCenter = 4 * 9 + 1 * 3 + 1;
    (badCorner[dCorner], badCorner[lCenter]) = (badCorner[lCenter], badCorner[dCorner]);
    var badCornerResult = RubikCubieDecomposer.Decompose(3, badCorner);
    checks.Check(!badCornerResult.Complete && badCornerResult.Issues.Any(issue => issue.CubieClass == "corner"),
        "duplicate/impossible corner colors fail decomposition without assigning cubie IDs");

    var badCenterOrbit = SolvedDocument(5).Faces.Flatten();
    var uOrbitOne = 1 * 5 + 1;
    var rFixedCenter = 25 + 2 * 5 + 2;
    (badCenterOrbit[uOrbitOne], badCenterOrbit[rFixedCenter]) = (badCenterOrbit[rFixedCenter], badCenterOrbit[uOrbitOne]);
    var badCenterResult = RubikCubieDecomposer.Decompose(5, badCenterOrbit);
    checks.Check(!badCenterResult.Complete && badCenterResult.Issues.Any(issue => issue.CubieClass == "center"),
        "count-preserving center orbit corruption is detected");
}

static void CheckSolvabilityValidation(ContractChecks checks)
{
    foreach (var size in new[] { 2, 3, 4, 5, 8, 11 })
    {
        var solved = RubikSolvabilityValidator.Validate(SolvedDocument(size));
        checks.Check(solved.BasicCountsValid && solved.CubieInventoryValid && solved.OrientationValid,
            $"solved N={size} passes counts, inventory, and orientation invariants");
        checks.Check(size <= 3
                ? solved is { SolverReady: true, OrientationProven: true, ParityProven: true, ValidationLevel: RubikValidationLevel.FullSmallCube }
                : solved is { SolverReady: false, OrientationProven: false, ParityProven: false, ValidationLevel: RubikValidationLevel.CubieInventory },
            $"N={size} reports honest small/full versus NxN/partial proof level");

        using var cube = NativeCube.Create(size);
        checks.Check(cube.Scramble(1900 + size, Math.Min(14, size + 5)), $"N={size} solvability scramble succeeds");
        var legal = RubikSolvabilityValidator.Validate(RubikStateDocument.Create(size, cube.Facelets));
        checks.Check(legal.CubieInventoryValid && legal.OrientationValid && legal.ParityValid,
            $"legal scramble N={size} has no known solvability violation " +
            $"(inventory={legal.CubieInventoryValid}, orientation={legal.OrientationValid}, parity={legal.ParityValid}, " +
            $"issues={string.Join(',', legal.Issues.Select(issue => issue.Code))})");
    }


    using (var canonicalThree = NativeCube.Create(3))
    {
        checks.Check(canonicalThree.RotateLayer(2, 2, 1) && canonicalThree.RotateLayer(1, 2, 1) &&
                     canonicalThree.RotateLayer(0, 2, 3) && canonicalThree.RotateLayer(2, 0, 1),
            "canonical 3x3 outer-face scramble succeeds");
        var canonicalResult = RubikSolvabilityValidator.Validate(RubikStateDocument.Create(3, canonicalThree.Facelets));
        checks.Check(canonicalResult is { SolverReady: true, OrientationProven: true, ParityProven: true },
            "canonical 3x3 outer-face scramble receives full small-cube proof");
    }

    var twistedCorner = SolvedDocument(3).Faces.Flatten();
    var d = 3 * 9 + 6;
    var l = 4 * 9 + 6;
    var b = 5 * 9 + 8;
    (twistedCorner[d], twistedCorner[l], twistedCorner[b]) = (twistedCorner[l], twistedCorner[b], twistedCorner[d]);
    var twistedResult = RubikSolvabilityValidator.Validate(RubikStateDocument.Create(3, twistedCorner));
    checks.Check(!twistedResult.OrientationValid && !twistedResult.SolverReady &&
                 twistedResult.Issues.Any(issue => issue.Code == "cornerOrientation"),
        "single twisted corner is rejected by orientation sum");

    var flippedEdge = SolvedDocument(3).Faces.Flatten();
    var uFrontEdge = 7;
    var fTopEdge = 2 * 9 + 1;
    (flippedEdge[uFrontEdge], flippedEdge[fTopEdge]) = (flippedEdge[fTopEdge], flippedEdge[uFrontEdge]);
    var flippedResult = RubikSolvabilityValidator.Validate(RubikStateDocument.Create(3, flippedEdge));
    checks.Check(!flippedResult.OrientationValid && flippedResult.Issues.Any(issue => issue.Code == "edgeOrientation"),
        "single flipped 3x3 edge is rejected");

    var swappedCorners = SolvedDocument(3).Faces.Flatten();
    var rTopFront = 9;
    var lTopFront = 4 * 9 + 2;
    (swappedCorners[rTopFront], swappedCorners[lTopFront]) = (swappedCorners[lTopFront], swappedCorners[rTopFront]);
    var swappedResult = RubikSolvabilityValidator.Validate(RubikStateDocument.Create(3, swappedCorners));
    checks.Check(!swappedResult.ParityValid && !swappedResult.SolverReady &&
                 swappedResult.Issues.Any(issue => issue.Code == "permutationParity"),
        "two-corner swap is rejected by 3x3 parity equality");

    using var evenCube = NativeCube.Create(4);
    checks.Check(evenCube.RotateLayer(0, 1, 1), "legal 4x4 inner turn creates an even-cube parity-boundary fixture");
    var evenResult = RubikSolvabilityValidator.Validate(RubikStateDocument.Create(4, evenCube.Facelets));
    checks.Check(evenResult.CubieInventoryValid && evenResult.OrientationValid && evenResult.ParityValid &&
                 !evenResult.ParityProven && !evenResult.SolverReady,
        "legal even-cube state is accepted without a false full parity proof");
}

static void CheckPhysicalElevenByElevenWorkflow(ContractChecks checks)
{
    const int size = 11;
    var directory = Path.Combine(Path.GetTempPath(), "Chess-RubikPhysical11Workflow", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var service = new RubikStateFileService();
        var draft = new RubikFaceEditorDraft(size);
        for (var face = 0; face < 6; face++) draft.FillFace(face, face + 1);

        var diagnostic = RubikPhysicalStateDiagnostics.ValidateDraft(draft);
        checks.Check(diagnostic.BasicCountsValid && diagnostic.ErrorCount == 0,
            "physical N=11 solved draft passes structured validation");
        var document = draft.ToStateDocument("physical-11x11-workflow");
        var solvability = RubikSolvabilityValidator.Validate(document);
        checks.Check(solvability is
            {
                BasicCountsValid: true,
                CubieInventoryValid: true,
                SolverReady: false,
                OrientationProven: false,
                ParityProven: false,
                ValidationLevel: RubikValidationLevel.CubieInventory
            }, "physical N=11 solved input reports honest inventory-only solver readiness");

        using var applied = NativeCube.Create(size);
        checks.Check(applied.SetFacelets(document.Faces.Flatten()), "physical N=11 solved document applies to native cube");
        var rendered = ShellSignature(size, applied.Facelets);
        checks.Check(rendered is { StickerCount: 726, InvalidCount: 0, Fallback: true },
            "physical N=11 solved document renders a complete facelet shell");

        var path = Path.Combine(directory, "physical-solved-11x11.rubik.json");
        var originalHash = RubikStateHasher.Calculate(document);
        checks.Check(service.Save(path, document).Success, "physical N=11 solved document saves atomically");

        using var reset = NativeCube.Create(size);
        var loaded = service.Read(path);
        checks.Check(loaded.Success && loaded.LoadPlan is not null, "physical N=11 saved document reloads after reset");
        checks.Check(loaded.LoadPlan?.StateHash == originalHash, "physical N=11 load preserves canonical hash");
        checks.Check(loaded.LoadPlan is not null && reset.SetFacelets(loaded.LoadPlan.Facelets),
            "physical N=11 reloaded plan applies to reset cube");
        checks.Check(reset.Facelets.SequenceEqual(document.Faces.Flatten()),
            "physical N=11 reset/load preserves every facelet");

        using var scramble = NativeCube.Create(size);
        checks.Check(scramble.Scramble(20260720, 20), "legal N=11 physical workflow scramble fixture is generated");
        var scrambleDocument = RubikStateDocument.Create(size, scramble.Facelets, "legal-native-11x11-fixture") with
        {
            CreatedUtc = "2026-07-20T00:00:00Z"
        };
        var scrambleSolvability = RubikSolvabilityValidator.Validate(scrambleDocument);
        checks.Check(scrambleSolvability.CubieInventoryValid && !scrambleSolvability.SolverReady,
            "legal N=11 scramble validates at the honest inventory-only level");
        var scrambleVisual = ShellSignature(size, scrambleDocument.Faces.Flatten());
        checks.Check(scrambleVisual is { StickerCount: 726, InvalidCount: 0 },
            "legal N=11 scramble renders all physical stickers");

        var fixturePath = Path.Combine(directory, "legal-scramble-fixture.rubik.json");
        var copyPath = Path.Combine(directory, "legal-scramble-copy.rubik.json");
        checks.Check(service.Save(fixturePath, scrambleDocument).Success, "legal N=11 scramble fixture saves atomically");
        var fixture = service.Read(fixturePath);
        checks.Check(fixture.LoadPlan is not null && service.Save(copyPath, fixture.LoadPlan.Document).Success,
            "loaded legal N=11 scramble saves as an independent copy");
        var copy = service.Read(copyPath);
        checks.Check(copy.LoadPlan?.StateHash == fixture.LoadPlan?.StateHash &&
                     copy.LoadPlan?.Facelets.SequenceEqual(scrambleDocument.Faces.Flatten()) == true,
            "legal N=11 scramble copy preserves hash and exact facelets");
    }
    finally
    {
        try { Directory.Delete(directory, recursive: true); } catch { }
    }
}

static void CheckSolverContracts(ContractChecks checks)
{
    var solver = new ReverseHistorySolver();
    checks.Check(!solver.Capabilities.SupportsArbitraryState && solver.Capabilities.RequiresTrustedHistory &&
                 solver.Capabilities is { MinimumSize: 2, MaximumSize: 32 },
        "reverse-history solver advertises trusted-history capability honestly");

    var state = SolvedDocument(3);
    var history = new[] { new RubikMove(2, 2, 1), new RubikMove(0, 0, 2), new RubikMove(1, 2, 3) };
    var request = new RubikSolveRequest(state, TimeSpan.FromSeconds(5), 16 * 1024 * 1024, 20,
        TrustedHistory: history);
    var result = solver.SolveAsync(request).GetAwaiter().GetResult();
    checks.Check(result.Success && result.Moves.SequenceEqual(new[]
        {
            new RubikMove(1, 2, 1), new RubikMove(0, 0, 2), new RubikMove(2, 2, 3)
        }), "reverse-history solver returns the exact inverse sequence");
    checks.Check(result is { FinalHash: null, Verification.Status: RubikSolutionVerificationStatus.NotRun },
        "solver result does not claim verification before independent replay");

    var imported = solver.SolveAsync(request with { TrustedHistory = null }).GetAwaiter().GetResult();
    checks.Check(!imported.Success && imported.Failure?.Kind == RubikSolveFailureKind.UnsupportedState,
        "reverse-history solver rejects arbitrary imported state without trusted history");

    using var cancelled = new CancellationTokenSource();
    cancelled.Cancel();
    var cancelledResult = solver.SolveAsync(request with { CancellationToken = cancelled.Token }).GetAwaiter().GetResult();
    checks.Check(!cancelledResult.Success && cancelledResult.Failure?.Kind == RubikSolveFailureKind.Cancelled,
        "solver contract reports pre-cancelled request without work");
}

static void CheckSolutionVerification(ContractChecks checks)
{
    var history = new[]
    {
        new RubikMove(2, 2, 1), new RubikMove(0, 0, 2), new RubikMove(1, 2, 3), new RubikMove(2, 0, 1)
    };
    using var source = NativeCube.Create(3);
    foreach (var move in history)
        checks.Check(source.RotateLayer(move.Axis, move.Layer, move.QuarterTurns), "verification fixture move applies");
    var inputFacelets = source.Facelets;
    var input = RubikStateDocument.Create(3, inputFacelets, "verification-fixture");
    var solver = new ReverseHistorySolver();
    var request = new RubikSolveRequest(input, TimeSpan.FromSeconds(5), 16 * 1024 * 1024, 20,
        TrustedHistory: history);
    var solution = solver.SolveAsync(request).GetAwaiter().GetResult();
    var factory = new NativeMoveExecutorFactory();

    var verified = RubikSolutionVerifier.Verify(input, solution.Moves, factory);
    var solvedHash = RubikStateHasher.Calculate(SolvedDocument(3));
    checks.Check(verified is { Status: RubikSolutionVerificationStatus.Verified, Solved: true, AppliedMoveCount: 4 } &&
                 verified.FinalHash == solvedHash, "valid reverse solution replays independently to solved hash");
    checks.Check(input.Faces.Flatten().SequenceEqual(inputFacelets), "solution verification does not mutate input facelets");

    var malformed = RubikSolutionVerifier.Verify(input, [new RubikMove(9, 0, 1)], factory);
    checks.Check(malformed.Status == RubikSolutionVerificationStatus.Failed && malformed.AppliedMoveCount == 0,
        "malformed move fails before replay mutation");
    var illegalLayer = RubikSolutionVerifier.Verify(input, [new RubikMove(0, 3, 1)], factory);
    checks.Check(illegalLayer.Status == RubikSolutionVerificationStatus.Failed && illegalLayer.AppliedMoveCount == 0,
        "out-of-range layer fails before replay mutation");

    var truncated = RubikSolutionVerifier.Verify(input, solution.Moves.Take(solution.Moves.Count - 1).ToArray(), factory);
    checks.Check(truncated.Status == RubikSolutionVerificationStatus.Failed && !truncated.Solved &&
                 truncated.AppliedMoveCount == solution.Moves.Count - 1,
        "truncated solution applies cleanly but fails solved-state proof");
    var incorrect = solution.Moves.ToArray();
    incorrect[0] = incorrect[0] with { QuarterTurns = 2 };
    var incorrectResult = RubikSolutionVerifier.Verify(input, incorrect, factory);
    checks.Check(incorrectResult.Status == RubikSolutionVerificationStatus.Failed && !incorrectResult.Solved,
        "syntactically valid incorrect sequence fails solved-state proof");

    using var cancelled = new CancellationTokenSource();
    cancelled.Cancel();
    var cancelledResult = RubikSolutionVerifier.Verify(input, solution.Moves, factory, cancelled.Token);
    checks.Check(cancelledResult.Status == RubikSolutionVerificationStatus.Failed &&
                 cancelledResult.Message.Contains("cancelled", StringComparison.OrdinalIgnoreCase),
        "cancelled verification returns promptly with an explicit result");
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
    public NativeState State { get { if (Rubik_GetState(_handle, out var state) == 0) throw new InvalidOperationException("State read failed."); return state; } }
    public bool RotateLayer(int axis, int layer, int turns) => Rubik_RotateLayer(_handle, axis, layer, turns) != 0;
    public bool Scramble(int seed, int length) => Rubik_Scramble(_handle, seed, length) != 0;
    public bool HasOrientationAt(int x, int y, int z) => Rubik_GetCubieOrientation(_handle, x, y, z, out _) != 0;
    public void Dispose() { if (_handle != IntPtr.Zero) { Rubik_Destroy(_handle); _handle = IntPtr.Zero; } }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern IntPtr Rubik_CreateSized(int size);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern void Rubik_Destroy(IntPtr handle);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern int Rubik_GetFacelets(IntPtr handle, [Out] int[] facelets, int capacity);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern int Rubik_SetFacelets(IntPtr handle, [In] int[] facelets, int count);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern int Rubik_GetState(IntPtr handle, out NativeState state);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern int Rubik_RotateLayer(IntPtr handle, int axis, int layer, int turns);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern int Rubik_Scramble(IntPtr handle, int seed, int length);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern int Rubik_GetCubieOrientation(IntPtr handle, int x, int y, int z, out NativeOrientation orientation);

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeState
    {
        public int Size, CellCount, HistoryCount, IsSolved, ManualState, LastAxis, LastLayer, LastQuarterTurns;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeOrientation
    {
        public int Xx, Xy, Xz, Yx, Yy, Yz, Zx, Zy, Zz;
    }
}

internal sealed class NativeMoveExecutorFactory : IRubikMoveExecutorFactory
{
    public IRubikMoveExecutor Create(RubikStateDocument state) => new NativeMoveExecutor(state);
}

internal sealed class NativeMoveExecutor : IRubikMoveExecutor
{
    private readonly NativeCube _cube;

    public NativeMoveExecutor(RubikStateDocument state)
    {
        _cube = NativeCube.Create(state.Size);
        if (!_cube.SetFacelets(state.Faces.Flatten()))
        {
            _cube.Dispose();
            throw new InvalidOperationException("Native verifier could not import facelets.");
        }
    }

    public int Size => _cube.Size;
    public int[] GetFacelets() => _cube.Facelets;
    public bool TryApply(RubikMove move, out string error)
    {
        var success = _cube.RotateLayer(move.Axis, move.Layer, move.QuarterTurns);
        error = success ? string.Empty : "native layer rotation failed";
        return success;
    }
    public void Dispose() => _cube.Dispose();
}

internal sealed class ThrowAtStage(RubikFileWriteStage target) : IRubikFileFailureInjector
{
    public void AtStage(RubikFileWriteStage stage)
    {
        if (stage == target) throw new IOException($"Injected failure at {stage}.");
    }
}

internal sealed class ContractChecks
{
    private int _failures;
    public void Check(bool condition, string message) { Console.WriteLine($"{(condition ? "PASS" : "FAIL")} {message}"); if (!condition) _failures++; }
    public void Fail(string message) => Check(false, message);
    public int Finish() { Console.WriteLine($"RubikStateContractTests: {(_failures == 0 ? "PASS" : $"FAIL ({_failures})")}"); return _failures == 0 ? 0 : 1; }
}
