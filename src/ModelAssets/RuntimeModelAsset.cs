using System.Collections.ObjectModel;
using System.Numerics;
using System.Security.Cryptography;

namespace ModelAssets;

public sealed record RuntimeModelAsset
{
    public required string ContentSha256 { get; init; }
    public required IReadOnlyList<RuntimeNode> Nodes { get; init; }
    public required IReadOnlyList<RuntimeMesh> Meshes { get; init; }
    public required IReadOnlyList<RuntimeMaterial> Materials { get; init; }
    public required IReadOnlyList<RuntimeTexture> Textures { get; init; }
    public required RuntimeBounds Bounds { get; init; }
    public required RuntimeModelDiagnostics Diagnostics { get; init; }

    public static RuntimeModelAsset Freeze(
        string sha256,
        IEnumerable<RuntimeNode> nodes,
        IEnumerable<RuntimeMesh> meshes,
        IEnumerable<RuntimeMaterial> materials,
        IEnumerable<RuntimeTexture> textures,
        RuntimeBounds bounds,
        RuntimeModelDiagnostics diagnostics)
    {
        if (sha256.Length != 64 || sha256.Any(c => !Uri.IsHexDigit(c)))
            throw new ArgumentException("Runtime model cache key must be a SHA-256.", nameof(sha256));
        return new()
        {
            ContentSha256 = sha256.ToLowerInvariant(),
            Nodes = ReadOnly(nodes),
            Meshes = ReadOnly(meshes),
            Materials = ReadOnly(materials),
            Textures = ReadOnly(textures),
            Bounds = bounds,
            Diagnostics = diagnostics
        };
    }

    private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> source) =>
        new ReadOnlyCollection<T>(source.ToArray());
}

public sealed record RuntimeNode(
    int NodeIndex,
    string Name,
    int? MeshIndex,
    IReadOnlyList<int> Children,
    RuntimeTransform LocalTransform,
    RuntimeTransform WorldTransform);

public sealed record RuntimeMesh(string Name, IReadOnlyList<RuntimePrimitive> Primitives);

public sealed record RuntimePrimitive(
    RuntimeVertexBuffer Vertices,
    RuntimeIndexBuffer Indices,
    int? MaterialIndex,
    RuntimeBounds Bounds);

public sealed record RuntimeVertexBuffer(
    IReadOnlyList<Vector3> Positions,
    IReadOnlyList<Vector3> Normals,
    IReadOnlyList<Vector2> TextureCoordinates0);

public sealed record RuntimeIndexBuffer(IReadOnlyList<uint> Indices);

public enum RuntimeAlphaMode
{
    Opaque,
    Mask,
    Blend
}

public sealed record RuntimeMaterial(
    string Name,
    Vector4 BaseColor,
    int? BaseColorTextureIndex,
    RuntimeAlphaMode AlphaMode,
    float AlphaCutoff,
    bool DoubleSided);

public sealed record RuntimeTexture(
    string Name,
    string MimeType,
    ReadOnlyMemory<byte> Content,
    string ContentSha256);

public readonly record struct RuntimeTransform(Matrix4x4 Matrix)
{
    public static RuntimeTransform Identity => new(Matrix4x4.Identity);
}

public readonly record struct RuntimeBounds(Vector3 Minimum, Vector3 Maximum)
{
    public static RuntimeBounds Empty =>
        new(new(float.PositiveInfinity), new(float.NegativeInfinity));

    public bool IsFinite =>
        IsFiniteVector(Minimum) && IsFiniteVector(Maximum) &&
        Minimum.X <= Maximum.X && Minimum.Y <= Maximum.Y && Minimum.Z <= Maximum.Z;

    public RuntimeBounds Include(Vector3 point) =>
        new(Vector3.Min(Minimum, point), Vector3.Max(Maximum, point));

    private static bool IsFiniteVector(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}

public sealed record RuntimeModelDiagnostics(
    TimeSpan LoadTime,
    long EstimatedManagedBytes,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<RuntimeUnsupportedFeature> UnsupportedFeatures);

public sealed record RuntimeUnsupportedFeature(
    string Feature,
    bool Required,
    string JsonPath,
    string Message);

public sealed record RuntimeModelLoadLimits
{
    public long MaxFileBytes { get; init; } = 64L * 1024 * 1024;
    public int MaxJsonBytes { get; init; } = 8 * 1024 * 1024;
    public int MaxBufferBytes { get; init; } = 64 * 1024 * 1024;
    public int MaxNodes { get; init; } = 4096;
    public int MaxMeshes { get; init; } = 4096;
    public int MaxPrimitives { get; init; } = 16384;
    public int MaxVertices { get; init; } = 4_000_000;
    public int MaxIndices { get; init; } = 12_000_000;
    public int MaxImages { get; init; } = 256;
    public int MaxImageBytes { get; init; } = 16 * 1024 * 1024;
    public int MaxDepth { get; init; } = 128;

    public void ThrowIfInvalid()
    {
        if (MaxFileBytes is <= 0 or > int.MaxValue ||
            MaxJsonBytes <= 0 || MaxBufferBytes <= 0 || MaxNodes <= 0 ||
            MaxMeshes <= 0 || MaxPrimitives <= 0 || MaxVertices <= 0 ||
            MaxIndices <= 0 || MaxImages <= 0 || MaxImageBytes <= 0 ||
            MaxDepth is <= 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(RuntimeModelLoadLimits));
    }
}

public sealed record RuntimeModelLoadRequest
{
    public required string Path { get; init; }
    public required string ExpectedSha256 { get; init; }
    public RuntimeModelLoadLimits Limits { get; init; } = new();
}

public interface IRuntimeModelLoader
{
    Task<RuntimeModelAsset> LoadAsync(
        RuntimeModelLoadRequest request,
        CancellationToken cancellationToken = default);
}

public static class RuntimeModelSecurity
{
    public static string ResolvePackageResource(string packageRoot, string relativeUri)
    {
        if (Uri.TryCreate(relativeUri, UriKind.Absolute, out _))
            throw new FormatException("External and absolute resource URIs are not allowed.");
        ModelAssetManifestRules.ValidateRelativePath(relativeUri, "resource URI");
        var root = Path.GetFullPath(packageRoot).TrimEnd(Path.DirectorySeparatorChar) +
                   Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root,
            relativeUri.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new FormatException("Resource URI escapes the package root.");
        return path;
    }

    public static async Task<string> ComputeSha256Async(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static int CheckedRangeEnd(int offset, int count, int stride, int elementBytes)
    {
        if (offset < 0 || count < 0 || stride < elementBytes || elementBytes <= 0)
            throw new FormatException("Accessor range contains negative or undersized values.");
        if (count == 0) return offset;
        return checked(offset + checked((count - 1) * stride) + elementBytes);
    }
}
