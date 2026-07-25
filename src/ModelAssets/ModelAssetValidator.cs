using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ModelAssets;

public enum ModelAssetValidationSeverity
{
    Information,
    Warning,
    Error
}

public sealed record ModelAssetValidationIssue(
    ModelAssetValidationSeverity Severity,
    string Code,
    string Message,
    string? AssetId = null);

public sealed record ModelAssetStats(
    string AssetId,
    long Bytes,
    long VertexCount,
    long TriangleCount,
    long NormalCount,
    long TextureCoordinateCount,
    long DegenerateTriangleCount);

public sealed record ModelAssetValidationRequest
{
    public required string ManifestPath { get; init; }
    public long MaxAssetBytes { get; init; } = 64L * 1024 * 1024;
    public long MaxTextureBytes { get; init; } = 16L * 1024 * 1024;
    public bool RequireNormals { get; init; }
    public IReadOnlySet<string> RequiredRoles { get; init; } = new HashSet<string>();
    public CancellationToken CancellationToken { get; init; }
}

public sealed record ModelAssetValidationReport
{
    public required string SetId { get; init; }
    public required IReadOnlyList<ModelAssetValidationIssue> Issues { get; init; }
    public required IReadOnlyList<ModelAssetStats> Stats { get; init; }
    public bool IsValid => Issues.All(issue => issue.Severity != ModelAssetValidationSeverity.Error);
}

public sealed class ModelAssetValidator
{
    public ModelAssetValidationReport Validate(ModelAssetValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var issues = new List<ModelAssetValidationIssue>();
        var stats = new List<ModelAssetStats>();
        ModelAssetManifest? manifest = null;
        string? packageRoot = null;

        try
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            var manifestPath = Path.GetFullPath(request.ManifestPath);
            packageRoot = Path.GetDirectoryName(manifestPath)
                ?? throw new FormatException("Manifest path has no package root.");
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("Manifest file is missing.", manifestPath);
            if (new FileInfo(manifestPath).Length > 1024 * 1024)
                throw new FormatException("Manifest exceeds the 1 MiB limit.");
            manifest = ModelAssetManifestJson.Deserialize(File.ReadAllText(manifestPath));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            issues.Add(new(ModelAssetValidationSeverity.Error, "manifest.invalid", ex.Message));
            return Report(manifest?.SetId ?? "unknown", issues, stats);
        }

        ValidateLicense(manifest, packageRoot, issues);
        var roles = manifest.Assets.Select(asset => asset.Role).ToHashSet(StringComparer.Ordinal);
        foreach (var required in request.RequiredRoles.Order(StringComparer.Ordinal))
        {
            if (!roles.Contains(required))
                issues.Add(new(ModelAssetValidationSeverity.Error, "role.requiredMissing",
                    $"Required role '{required}' is missing."));
        }

        foreach (var asset in manifest.Assets)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            try
            {
                var path = ResolvePackagePath(packageRoot, asset.Path);
                if (!File.Exists(path))
                    throw new FileNotFoundException($"Asset file '{asset.Path}' is missing.");
                var info = new FileInfo(path);
                if (info.Length == 0)
                    throw new FormatException("Mesh file is empty.");
                if (info.Length > request.MaxAssetBytes)
                    throw new FormatException($"Asset exceeds the {request.MaxAssetBytes}-byte limit.");
                var sha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
                if (!sha.Equals(asset.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new FormatException("SHA-256 does not match the manifest.");

                ModelAssetStats assetStats = asset.Format.ToLowerInvariant() switch
                {
                    "obj" => InspectObj(asset.AssetId, path, request.RequireNormals,
                        request.CancellationToken, issues),
                    "glb" => InspectGlb(asset.AssetId, path, request.CancellationToken, issues),
                    _ => throw new FormatException($"Unsupported format '{asset.Format}'.")
                };
                stats.Add(assetStats);
                ValidateDeclaredStats(asset, assetStats, issues);
                ValidateBounds(asset, issues);
                ValidateTextures(asset, packageRoot, request.MaxTextureBytes, issues);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                issues.Add(new(ModelAssetValidationSeverity.Error, "asset.invalid", ex.Message, asset.AssetId));
            }
        }

        return Report(manifest.SetId, issues, stats);
    }

    private static ModelAssetValidationReport Report(
        string setId,
        IReadOnlyList<ModelAssetValidationIssue> issues,
        IReadOnlyList<ModelAssetStats> stats) =>
        new() { SetId = setId, Issues = issues, Stats = stats };

    private static void ValidateLicense(
        ModelAssetManifest manifest,
        string packageRoot,
        ICollection<ModelAssetValidationIssue> issues)
    {
        if (manifest.License.SpdxId == "NOASSERTION" ||
            manifest.License.Status.Contains("pending", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new(ModelAssetValidationSeverity.Warning, "license.pending",
                "License/provenance is declared but still pending review."));
        }
        if (manifest.License.NoticePath is { Length: > 0 } notice)
        {
            try
            {
                var path = ResolvePackagePath(packageRoot, notice);
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                    issues.Add(new(ModelAssetValidationSeverity.Error, "license.noticeMissing",
                        $"License notice '{notice}' is missing or empty."));
            }
            catch (Exception ex)
            {
                issues.Add(new(ModelAssetValidationSeverity.Error, "license.noticeInvalid", ex.Message));
            }
        }
    }

    private static string ResolvePackagePath(string packageRoot, string relativePath)
    {
        ModelAssetManifestRules.ValidateRelativePath(relativePath, "package path");
        var root = Path.GetFullPath(packageRoot).TrimEnd(Path.DirectorySeparatorChar) +
                   Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(packageRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new FormatException("Package path escapes its root.");
        var item = new FileInfo(path);
        if (item.Exists && (item.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new FormatException("Package path resolves to a symlink or reparse point.");
        return path;
    }

    private static ModelAssetStats InspectObj(
        string assetId,
        string path,
        bool requireNormals,
        CancellationToken cancellationToken,
        ICollection<ModelAssetValidationIssue> issues)
    {
        long vertices = 0, normals = 0, uvs = 0, triangles = 0, degenerate = 0;
        var lineNumber = 0;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            64 * 1024, FileOptions.SequentialScan);
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 64 * 1024);
        while (reader.ReadLine() is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lineNumber++;
            if (line.Length > 1024 * 1024)
                throw new FormatException($"OBJ line {lineNumber} exceeds 1 MiB.");
            var trimmed = line.Trim();
            if (trimmed.StartsWith("v ", StringComparison.Ordinal))
            {
                ParseFiniteTuple(trimmed, 3, lineNumber);
                vertices++;
            }
            else if (trimmed.StartsWith("vn ", StringComparison.Ordinal))
            {
                ParseFiniteTuple(trimmed, 3, lineNumber);
                normals++;
            }
            else if (trimmed.StartsWith("vt ", StringComparison.Ordinal))
            {
                ParseFiniteTuple(trimmed, 2, lineNumber, allowExtra: true);
                uvs++;
            }
            else if (trimmed.StartsWith("f ", StringComparison.Ordinal))
            {
                var tokens = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 4) throw new FormatException($"OBJ face at line {lineNumber} has fewer than three vertices.");
                var indices = new List<long>(tokens.Length - 1);
                for (var i = 1; i < tokens.Length; i++)
                {
                    var sections = tokens[i].Split('/');
                    if (!long.TryParse(sections[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) ||
                        index == 0)
                        throw new FormatException($"OBJ face index at line {lineNumber} is invalid.");
                    var resolved = index > 0 ? index : vertices + index + 1;
                    if (resolved < 1 || resolved > vertices)
                        throw new FormatException($"OBJ face index at line {lineNumber} is out of range.");
                    indices.Add(resolved);
                    if (sections.Length > 1 && sections[1].Length > 0)
                        ValidateObjReference(sections[1], uvs, "UV", lineNumber);
                    if (sections.Length > 2 && sections[2].Length > 0)
                        ValidateObjReference(sections[2], normals, "normal", lineNumber);
                }
                triangles += tokens.Length - 3;
                for (var i = 1; i < indices.Count - 1; i++)
                {
                    if (indices[0] == indices[i] || indices[0] == indices[i + 1] ||
                        indices[i] == indices[i + 1]) degenerate++;
                }
            }
        }
        if (vertices == 0 || triangles == 0) throw new FormatException("OBJ contains no triangle mesh.");
        if (normals == 0)
        {
            issues.Add(new(requireNormals ? ModelAssetValidationSeverity.Error : ModelAssetValidationSeverity.Warning,
                "mesh.normalsMissing", "OBJ has no normals.", assetId));
        }
        if (degenerate > 0)
            issues.Add(new(ModelAssetValidationSeverity.Warning, "mesh.degenerate",
                $"OBJ contains {degenerate} degenerate triangle(s).", assetId));
        return new(assetId, new FileInfo(path).Length, vertices, triangles, normals, uvs, degenerate);
    }

    private static void ParseFiniteTuple(string line, int required, int lineNumber, bool allowExtra = false)
    {
        var tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if ((!allowExtra && tokens.Length != required + 1) || tokens.Length < required + 1)
            throw new FormatException($"OBJ numeric tuple at line {lineNumber} has the wrong size.");
        for (var i = 1; i <= required; i++)
        {
            if (!double.TryParse(tokens[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                !double.IsFinite(value))
                throw new FormatException($"OBJ numeric value at line {lineNumber} is not finite.");
        }
    }

    private static void ValidateObjReference(string token, long count, string kind, int lineNumber)
    {
        if (!long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) || index == 0)
            throw new FormatException($"OBJ {kind} index at line {lineNumber} is invalid.");
        var resolved = index > 0 ? index : count + index + 1;
        if (resolved < 1 || resolved > count)
            throw new FormatException($"OBJ {kind} index at line {lineNumber} is out of range.");
    }

    private static ModelAssetStats InspectGlb(
        string assetId,
        string path,
        CancellationToken cancellationToken,
        ICollection<ModelAssetValidationIssue> issues)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        if (stream.Length < 20) throw new FormatException("GLB is shorter than its header and JSON chunk.");
        if (reader.ReadUInt32() != 0x46546C67) throw new FormatException("GLB magic is invalid.");
        if (reader.ReadUInt32() != 2) throw new FormatException("Only GLB 2.0 is supported.");
        var declaredLength = reader.ReadUInt32();
        if (declaredLength != stream.Length) throw new FormatException("GLB declared length does not match the file.");
        var foundJson = false;
        while (stream.Position < stream.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stream.Length - stream.Position < 8) throw new FormatException("GLB chunk header is truncated.");
            var length = reader.ReadUInt32();
            var type = reader.ReadUInt32();
            if (length > stream.Length - stream.Position) throw new FormatException("GLB chunk exceeds the container.");
            if (type == 0x4E4F534A)
            {
                if (foundJson) throw new FormatException("GLB contains multiple JSON chunks.");
                if (length > 8 * 1024 * 1024) throw new FormatException("GLB JSON chunk exceeds 8 MiB.");
                using var json = JsonDocument.Parse(reader.ReadBytes(checked((int)length)));
                if (json.RootElement.GetProperty("asset").GetProperty("version").GetString() != "2.0")
                    throw new FormatException("GLB JSON asset version is not 2.0.");
                foundJson = true;
            }
            else
            {
                stream.Position += length;
            }
        }
        if (!foundJson) throw new FormatException("GLB has no JSON chunk.");
        issues.Add(new(ModelAssetValidationSeverity.Information, "glb.deepValidationDeferred",
            "Container is valid; accessor/mesh validation is performed by the bounded GLB loader.", assetId));
        return new(assetId, stream.Length, 0, 0, 0, 0, 0);
    }

    private static void ValidateDeclaredStats(
        ModelAssetEntry asset,
        ModelAssetStats stats,
        ICollection<ModelAssetValidationIssue> issues)
    {
        if (asset.VertexCount is { } vertices && stats.VertexCount != 0 && vertices != stats.VertexCount)
            issues.Add(new(ModelAssetValidationSeverity.Error, "stats.vertexMismatch",
                $"Declared vertex count {vertices} does not match {stats.VertexCount}.", asset.AssetId));
        if (asset.TriangleCount is { } triangles && stats.TriangleCount != 0 && triangles != stats.TriangleCount)
            issues.Add(new(ModelAssetValidationSeverity.Error, "stats.triangleMismatch",
                $"Declared triangle count {triangles} does not match {stats.TriangleCount}.", asset.AssetId));
    }

    private static void ValidateBounds(ModelAssetEntry asset, ICollection<ModelAssetValidationIssue> issues)
    {
        if (asset.Bounds is not { } bounds) return;
        if (bounds.Minimum.X > bounds.Maximum.X ||
            bounds.Minimum.Y > bounds.Maximum.Y ||
            bounds.Minimum.Z > bounds.Maximum.Z)
        {
            issues.Add(new(ModelAssetValidationSeverity.Error, "bounds.invalid",
                "Bounds minimum exceeds maximum.", asset.AssetId));
        }
    }

    private static void ValidateTextures(
        ModelAssetEntry asset,
        string packageRoot,
        long maxTextureBytes,
        ICollection<ModelAssetValidationIssue> issues)
    {
        foreach (var texture in asset.Textures)
        {
            try
            {
                var path = ResolvePackagePath(packageRoot, texture.Path);
                if (!File.Exists(path)) throw new FileNotFoundException($"Texture '{texture.Path}' is missing.");
                var info = new FileInfo(path);
                if (info.Length == 0 || info.Length > maxTextureBytes)
                    throw new FormatException($"Texture size {info.Length} is outside the accepted range.");
                var extension = info.Extension.ToLowerInvariant();
                if (extension is not (".png" or ".jpg" or ".jpeg"))
                    throw new FormatException($"Texture extension '{extension}' is unsupported.");
                var sha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
                if (!sha.Equals(texture.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new FormatException("Texture SHA-256 does not match the manifest.");
            }
            catch (Exception ex)
            {
                issues.Add(new(ModelAssetValidationSeverity.Error, "texture.invalid", ex.Message, asset.AssetId));
            }
        }
    }
}

public enum KhronosValidatorStatus
{
    Passed,
    Failed,
    Skipped
}

public sealed record KhronosValidatorResult(
    KhronosValidatorStatus Status,
    string Summary,
    JsonDocument? NormalizedReport = null);

public sealed class KhronosGltfValidatorAdapter
{
    public async Task<KhronosValidatorResult> ValidateAsync(
        string glbPath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var executable = Environment.GetEnvironmentVariable("KHRONOS_GLTF_VALIDATOR");
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
            return new(KhronosValidatorStatus.Skipped,
                "SKIPPED: KHRONOS_GLTF_VALIDATOR is not configured.");

        var tempReport = Path.Combine(Path.GetTempPath(), $"chess-gltf-{Guid.NewGuid():N}.json");
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add("-o");
        process.StartInfo.ArgumentList.Add(tempReport);
        process.StartInfo.ArgumentList.Add(Path.GetFullPath(glbPath));
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        try
        {
            process.Start();
            var stdout = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            var stderr = process.StandardError.ReadToEndAsync(timeoutCts.Token);
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            if (!File.Exists(tempReport))
                return new(KhronosValidatorStatus.Failed,
                    $"Validator exit {process.ExitCode}; no JSON report was produced.");
            var report = JsonDocument.Parse(await File.ReadAllTextAsync(tempReport, cancellationToken));
            return new(process.ExitCode == 0 ? KhronosValidatorStatus.Passed : KhronosValidatorStatus.Failed,
                $"Validator exit {process.ExitCode}.", report);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            return new(KhronosValidatorStatus.Failed, "Validator timed out or was cancelled.");
        }
        finally
        {
            try { File.Delete(tempReport); } catch { }
        }
    }
}
