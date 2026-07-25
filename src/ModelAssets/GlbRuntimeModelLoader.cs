using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ModelAssets;

public sealed class GlbRuntimeModelLoader : IRuntimeModelLoader
{
    private const uint GlbMagic = 0x46546C67;
    private const uint JsonChunk = 0x4E4F534A;
    private const uint BinChunk = 0x004E4942;

    public async Task<RuntimeModelAsset> LoadAsync(
        RuntimeModelLoadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Limits.ThrowIfInvalid();
        var stopwatch = Stopwatch.StartNew();
        var path = Path.GetFullPath(request.Path);
        var info = new FileInfo(path);
        if (!info.Exists) throw new FileNotFoundException("GLB file is missing.", path);
        if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new FormatException("GLB path must not be a symlink or reparse point.");
        if (info.Length is < 20 || info.Length > int.MaxValue || info.Length > request.Limits.MaxFileBytes)
            throw new FormatException("GLB file size is outside the accepted range.");

        byte[] file;
        await using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                         64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            file = new byte[checked((int)stream.Length)];
            await stream.ReadExactlyAsync(file, cancellationToken).ConfigureAwait(false);
        }
        cancellationToken.ThrowIfCancellationRequested();
        var actualSha = Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant();
        if (!actualSha.Equals(request.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
            throw new FormatException("GLB SHA-256 does not match the validated manifest.");

        var container = ParseContainer(file, request.Limits);
        using var json = JsonDocument.Parse(container.Json,
            new JsonDocumentOptions { MaxDepth = request.Limits.MaxDepth });
        var parser = new Parser(json.RootElement, container.Bin, request.Limits, cancellationToken);
        var model = parser.Parse(actualSha, stopwatch);
        return model;
    }

    private static Container ParseContainer(byte[] file, RuntimeModelLoadLimits limits)
    {
        var offset = 0;
        uint ReadUInt32()
        {
            if (offset > file.Length - 4) throw new FormatException("GLB header/chunk is truncated.");
            var value = BitConverter.ToUInt32(file, offset);
            offset += 4;
            return value;
        }

        if (ReadUInt32() != GlbMagic) throw new FormatException("GLB magic is invalid.");
        if (ReadUInt32() != 2) throw new FormatException("Only GLB 2.0 is supported.");
        if (ReadUInt32() != file.Length) throw new FormatException("GLB declared length is invalid.");

        byte[]? json = null;
        byte[]? bin = null;
        while (offset < file.Length)
        {
            var length = checked((int)ReadUInt32());
            var type = ReadUInt32();
            if (length < 0 || offset > file.Length - length)
                throw new FormatException("GLB chunk exceeds the container.");
            if (type == JsonChunk)
            {
                if (json is not null) throw new FormatException("GLB contains multiple JSON chunks.");
                if (length > limits.MaxJsonBytes) throw new FormatException("GLB JSON chunk exceeds its limit.");
                json = file.AsSpan(offset, length).ToArray();
            }
            else if (type == BinChunk)
            {
                if (bin is not null) throw new FormatException("GLB contains multiple BIN chunks.");
                if (length > limits.MaxBufferBytes) throw new FormatException("GLB BIN chunk exceeds its limit.");
                bin = file.AsSpan(offset, length).ToArray();
            }
            offset += length;
        }
        if (json is null) throw new FormatException("GLB JSON chunk is missing.");
        return new(json, bin ?? []);
    }

    private sealed class Parser(
        JsonElement root,
        byte[] bin,
        RuntimeModelLoadLimits limits,
        CancellationToken cancellationToken)
    {
        private readonly List<RuntimeUnsupportedFeature> _unsupported = [];
        private View[] _views = [];
        private Accessor[] _accessors = [];

        public RuntimeModelAsset Parse(string sha, Stopwatch stopwatch)
        {
            RequireVersion();
            InspectExtensions();
            InspectUnsupportedTopology();
            ParseBuffers();
            _views = ParseViews();
            _accessors = ParseAccessors();
            var textures = ParseImages();
            var textureMap = ParseTextureMap(textures.Count);
            var materials = ParseMaterials(textureMap);
            var meshes = ParseMeshes(materials.Count);
            var nodes = ParseNodes(meshes.Count);
            var bounds = ComputeWorldBounds(nodes, meshes);
            stopwatch.Stop();
            var estimate = EstimateBytes(nodes, meshes, textures);
            return RuntimeModelAsset.Freeze(
                sha, nodes, meshes, materials, textures, bounds,
                new RuntimeModelDiagnostics(stopwatch.Elapsed, estimate, [],
                    _unsupported.AsReadOnly()));
        }

        private void RequireVersion()
        {
            if (!root.TryGetProperty("asset", out var asset) ||
                asset.GetProperty("version").GetString() != "2.0")
                throw new FormatException("glTF asset.version must be 2.0.");
        }

        private void InspectExtensions()
        {
            var required = root.TryGetProperty("extensionsRequired", out var requiredElement)
                ? requiredElement.EnumerateArray().Select(item => item.GetString() ?? "").ToHashSet()
                : [];
            foreach (var extension in required)
            {
                if (extension.Length == 0) continue;
                throw new NotSupportedException($"Required glTF extension '{extension}' is unsupported.");
            }
            if (!root.TryGetProperty("extensionsUsed", out var used)) return;
            foreach (var item in used.EnumerateArray())
            {
                var name = item.GetString() ?? "unknown";
                _unsupported.Add(new(name, required.Contains(name), "$.extensionsUsed",
                    "Extension metadata is preserved as a diagnostic but is not executed."));
            }
        }

        private void InspectUnsupportedTopology()
        {
            AddUnsupportedIfPresent("skins", "skin");
            AddUnsupportedIfPresent("animations", "animation");
            if (!root.TryGetProperty("meshes", out var meshes)) return;
            var meshIndex = 0;
            foreach (var mesh in meshes.EnumerateArray())
            {
                var primitiveIndex = 0;
                foreach (var primitive in mesh.GetProperty("primitives").EnumerateArray())
                {
                    if (primitive.TryGetProperty("targets", out _))
                        _unsupported.Add(new("morphTarget", false,
                            $"$.meshes[{meshIndex}].primitives[{primitiveIndex}].targets",
                            "Morph targets are not loaded."));
                    primitiveIndex++;
                }
                meshIndex++;
            }
        }

        private void AddUnsupportedIfPresent(string property, string feature)
        {
            if (root.TryGetProperty(property, out var value) && value.GetArrayLength() > 0)
                _unsupported.Add(new(feature, false, $"$.{property}", $"{feature} data is not loaded."));
        }

        private void ParseBuffers()
        {
            if (!root.TryGetProperty("buffers", out var buffers))
            {
                if (bin.Length != 0) throw new FormatException("BIN chunk is undeclared.");
                return;
            }
            if (buffers.GetArrayLength() != 1)
                throw new NotSupportedException("The first runtime subset supports exactly one GLB buffer.");
            var buffer = buffers[0];
            if (buffer.TryGetProperty("uri", out _))
                throw new NotSupportedException("External and data-URI buffers are not supported in GLB.");
            var length = buffer.GetProperty("byteLength").GetInt32();
            if (length < 0 || length > limits.MaxBufferBytes || length > bin.Length)
                throw new FormatException("Declared buffer length is invalid.");
        }

        private View[] ParseViews()
        {
            if (!root.TryGetProperty("bufferViews", out var values)) return [];
            var result = new View[values.GetArrayLength()];
            for (var index = 0; index < result.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = values[index];
                if (value.GetProperty("buffer").GetInt32() != 0)
                    throw new FormatException("bufferView references an unavailable buffer.");
                var offset = value.TryGetProperty("byteOffset", out var o) ? o.GetInt32() : 0;
                var length = value.GetProperty("byteLength").GetInt32();
                var stride = value.TryGetProperty("byteStride", out var s) ? s.GetInt32() : 0;
                if (offset < 0 || length < 0 || offset > bin.Length - length)
                    throw new FormatException("bufferView range is invalid.");
                if (stride is not 0 && (stride < 4 || stride > 252))
                    throw new FormatException("bufferView stride is invalid.");
                result[index] = new(offset, length, stride);
            }
            return result;
        }

        private Accessor[] ParseAccessors()
        {
            if (!root.TryGetProperty("accessors", out var values)) return [];
            var result = new Accessor[values.GetArrayLength()];
            for (var index = 0; index < result.Length; index++)
            {
                var value = values[index];
                if (value.TryGetProperty("sparse", out _))
                    throw new NotSupportedException("Sparse accessors are not supported.");
                var view = value.GetProperty("bufferView").GetInt32();
                if ((uint)view >= (uint)_views.Length) throw new FormatException("Accessor bufferView is out of range.");
                var count = value.GetProperty("count").GetInt32();
                if (count < 0 || count > Math.Max(limits.MaxVertices, limits.MaxIndices))
                    throw new FormatException("Accessor count exceeds its limit.");
                result[index] = new(
                    view,
                    value.TryGetProperty("byteOffset", out var offset) ? offset.GetInt32() : 0,
                    value.GetProperty("componentType").GetInt32(),
                    count,
                    value.GetProperty("type").GetString() ?? "",
                    value.TryGetProperty("normalized", out var normalized) && normalized.GetBoolean());
            }
            return result;
        }

        private List<RuntimeTexture> ParseImages()
        {
            var result = new List<RuntimeTexture>();
            if (!root.TryGetProperty("images", out var images)) return result;
            if (images.GetArrayLength() > limits.MaxImages) throw new FormatException("Image count exceeds its limit.");
            for (var index = 0; index < images.GetArrayLength(); index++)
            {
                var image = images[index];
                if (image.TryGetProperty("uri", out _))
                    throw new NotSupportedException("External/data-URI images are not supported.");
                var viewIndex = image.GetProperty("bufferView").GetInt32();
                if ((uint)viewIndex >= (uint)_views.Length) throw new FormatException("Image bufferView is out of range.");
                var mime = image.GetProperty("mimeType").GetString() ?? "";
                if (mime is not ("image/png" or "image/jpeg"))
                    throw new NotSupportedException($"Image MIME type '{mime}' is unsupported.");
                var view = _views[viewIndex];
                if (view.Length > limits.MaxImageBytes) throw new FormatException("Embedded image exceeds its limit.");
                var content = bin.AsSpan(view.Offset, view.Length).ToArray();
                result.Add(new(
                    image.TryGetProperty("name", out var name) ? name.GetString() ?? $"Image{index}" : $"Image{index}",
                    mime,
                    content,
                    Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant()));
            }
            return result;
        }

        private int[] ParseTextureMap(int imageCount)
        {
            if (!root.TryGetProperty("textures", out var textures)) return [];
            var result = new int[textures.GetArrayLength()];
            for (var index = 0; index < result.Length; index++)
            {
                var source = textures[index].GetProperty("source").GetInt32();
                if ((uint)source >= (uint)imageCount) throw new FormatException("Texture source is out of range.");
                result[index] = source;
            }
            return result;
        }

        private List<RuntimeMaterial> ParseMaterials(int[] textureMap)
        {
            var result = new List<RuntimeMaterial>();
            if (!root.TryGetProperty("materials", out var materials)) return result;
            for (var index = 0; index < materials.GetArrayLength(); index++)
            {
                var material = materials[index];
                var color = Vector4.One;
                int? texture = null;
                if (material.TryGetProperty("pbrMetallicRoughness", out var pbr))
                {
                    if (pbr.TryGetProperty("baseColorFactor", out var factor))
                    {
                        if (factor.GetArrayLength() != 4) throw new FormatException("baseColorFactor must have four values.");
                        color = new(
                            ReadFiniteSingle(factor[0]), ReadFiniteSingle(factor[1]),
                            ReadFiniteSingle(factor[2]), ReadFiniteSingle(factor[3]));
                    }
                    if (pbr.TryGetProperty("baseColorTexture", out var textureInfo))
                    {
                        var textureIndex = textureInfo.GetProperty("index").GetInt32();
                        if ((uint)textureIndex >= (uint)textureMap.Length)
                            throw new FormatException("Material texture index is out of range.");
                        texture = textureMap[textureIndex];
                    }
                }
                var alpha = material.TryGetProperty("alphaMode", out var alphaMode)
                    ? alphaMode.GetString() switch
                    {
                        "OPAQUE" => RuntimeAlphaMode.Opaque,
                        "MASK" => RuntimeAlphaMode.Mask,
                        "BLEND" => RuntimeAlphaMode.Blend,
                        _ => throw new FormatException("Material alphaMode is invalid.")
                    }
                    : RuntimeAlphaMode.Opaque;
                result.Add(new(
                    material.TryGetProperty("name", out var name) ? name.GetString() ?? $"Material{index}" : $"Material{index}",
                    color,
                    texture,
                    alpha,
                    material.TryGetProperty("alphaCutoff", out var cutoff) ? ReadFiniteSingle(cutoff) : 0.5f,
                    material.TryGetProperty("doubleSided", out var doubleSided) && doubleSided.GetBoolean()));
            }
            return result;
        }

        private List<RuntimeMesh> ParseMeshes(int materialCount)
        {
            var result = new List<RuntimeMesh>();
            if (!root.TryGetProperty("meshes", out var meshes)) return result;
            if (meshes.GetArrayLength() > limits.MaxMeshes) throw new FormatException("Mesh count exceeds its limit.");
            var totalPrimitives = 0;
            for (var meshIndex = 0; meshIndex < meshes.GetArrayLength(); meshIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var mesh = meshes[meshIndex];
                var primitives = new List<RuntimePrimitive>();
                foreach (var primitive in mesh.GetProperty("primitives").EnumerateArray())
                {
                    if (++totalPrimitives > limits.MaxPrimitives) throw new FormatException("Primitive count exceeds its limit.");
                    if (primitive.TryGetProperty("mode", out var mode) && mode.GetInt32() != 4)
                        throw new NotSupportedException("Only TRIANGLES primitives are supported.");
                    var attributes = primitive.GetProperty("attributes");
                    var positions = ReadVector3(attributes.GetProperty("POSITION").GetInt32(), "POSITION", limits.MaxVertices);
                    var normals = attributes.TryGetProperty("NORMAL", out var normalAccessor)
                        ? ReadVector3(normalAccessor.GetInt32(), "NORMAL", limits.MaxVertices)
                        : [];
                    var uvs = attributes.TryGetProperty("TEXCOORD_0", out var uvAccessor)
                        ? ReadVector2(uvAccessor.GetInt32(), "TEXCOORD_0", limits.MaxVertices)
                        : [];
                    if (positions.Count == 0) throw new FormatException("Primitive has no positions.");
                    if (normals.Count != 0 && normals.Count != positions.Count)
                        throw new FormatException("NORMAL count differs from POSITION.");
                    if (uvs.Count != 0 && uvs.Count != positions.Count)
                        throw new FormatException("TEXCOORD_0 count differs from POSITION.");
                    var indices = primitive.TryGetProperty("indices", out var indicesElement)
                        ? ReadIndices(indicesElement.GetInt32(), positions.Count)
                        : Enumerable.Range(0, positions.Count).Select(value => (uint)value).ToArray();
                    if (indices.Count == 0 || indices.Count % 3 != 0)
                        throw new FormatException("Triangle index count must be non-zero and divisible by three.");
                    var material = primitive.TryGetProperty("material", out var materialElement)
                        ? materialElement.GetInt32()
                        : (int?)null;
                    if (material is { } materialIndex && (uint)materialIndex >= (uint)materialCount)
                        throw new FormatException("Primitive material index is out of range.");
                    var primitiveBounds = RuntimeBounds.Empty;
                    foreach (var position in positions) primitiveBounds = primitiveBounds.Include(position);
                    primitives.Add(new(
                        new RuntimeVertexBuffer(positions, normals, uvs),
                        new RuntimeIndexBuffer(indices),
                        material,
                        primitiveBounds));
                }
                result.Add(new(
                    mesh.TryGetProperty("name", out var name) ? name.GetString() ?? $"Mesh{meshIndex}" : $"Mesh{meshIndex}",
                    primitives.AsReadOnly()));
            }
            return result;
        }

        private List<RuntimeNode> ParseNodes(int meshCount)
        {
            if (!root.TryGetProperty("nodes", out var nodeElements)) return [];
            if (nodeElements.GetArrayLength() > limits.MaxNodes) throw new FormatException("Node count exceeds its limit.");
            var locals = new RuntimeTransform[nodeElements.GetArrayLength()];
            var children = new IReadOnlyList<int>[locals.Length];
            var meshIndices = new int?[locals.Length];
            var names = new string[locals.Length];
            var parent = Enumerable.Repeat(-1, locals.Length).ToArray();
            for (var index = 0; index < locals.Length; index++)
            {
                var node = nodeElements[index];
                names[index] = node.TryGetProperty("name", out var name) ? name.GetString() ?? $"Node{index}" : $"Node{index}";
                meshIndices[index] = node.TryGetProperty("mesh", out var mesh) ? mesh.GetInt32() : null;
                if (meshIndices[index] is { } meshIndex && (uint)meshIndex >= (uint)meshCount)
                    throw new FormatException("Node mesh index is out of range.");
                var childList = node.TryGetProperty("children", out var childArray)
                    ? childArray.EnumerateArray().Select(item => item.GetInt32()).ToArray()
                    : [];
                foreach (var child in childList)
                {
                    if ((uint)child >= (uint)locals.Length || child == index)
                        throw new FormatException("Node child index is invalid.");
                    if (parent[child] != -1) throw new FormatException("Node has multiple parents.");
                    parent[child] = index;
                }
                children[index] = Array.AsReadOnly(childList);
                locals[index] = new(ParseLocalMatrix(node));
            }
            var worlds = new RuntimeTransform[locals.Length];
            var state = new byte[locals.Length];
            Matrix4x4 ResolveWorld(int index, int depth)
            {
                if (depth > limits.MaxDepth) throw new FormatException("Node hierarchy exceeds maximum depth.");
                if (state[index] == 1) throw new FormatException("Node hierarchy contains a cycle.");
                if (state[index] == 2) return worlds[index].Matrix;
                state[index] = 1;
                var world = parent[index] < 0
                    ? locals[index].Matrix
                    : locals[index].Matrix * ResolveWorld(parent[index], depth + 1);
                worlds[index] = new(world);
                state[index] = 2;
                return world;
            }
            for (var index = 0; index < locals.Length; index++) ResolveWorld(index, 0);
            return Enumerable.Range(0, locals.Length)
                .Select(index => new RuntimeNode(index, names[index], meshIndices[index], children[index],
                    locals[index], worlds[index]))
                .ToList();
        }

        private static Matrix4x4 ParseLocalMatrix(JsonElement node)
        {
            if (node.TryGetProperty("matrix", out var matrix))
            {
                if (matrix.GetArrayLength() != 16) throw new FormatException("Node matrix must have 16 values.");
                var m = Enumerable.Range(0, 16).Select(i => ReadFiniteSingle(matrix[i])).ToArray();
                return new(
                    m[0], m[1], m[2], m[3],
                    m[4], m[5], m[6], m[7],
                    m[8], m[9], m[10], m[11],
                    m[12], m[13], m[14], m[15]);
            }
            var translation = node.TryGetProperty("translation", out var t)
                ? new Vector3(ReadFiniteSingle(t[0]), ReadFiniteSingle(t[1]), ReadFiniteSingle(t[2]))
                : Vector3.Zero;
            var scale = node.TryGetProperty("scale", out var s)
                ? new Vector3(ReadFiniteSingle(s[0]), ReadFiniteSingle(s[1]), ReadFiniteSingle(s[2]))
                : Vector3.One;
            var rotation = node.TryGetProperty("rotation", out var r)
                ? Quaternion.Normalize(new(ReadFiniteSingle(r[0]), ReadFiniteSingle(r[1]),
                    ReadFiniteSingle(r[2]), ReadFiniteSingle(r[3])))
                : Quaternion.Identity;
            if (!IsFinite(rotation)) throw new FormatException("Node rotation is invalid.");
            return Matrix4x4.CreateScale(scale) *
                   Matrix4x4.CreateFromQuaternion(rotation) *
                   Matrix4x4.CreateTranslation(translation);
        }

        private static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.X) && float.IsFinite(value.Y) &&
            float.IsFinite(value.Z) && float.IsFinite(value.W);

        private RuntimeBounds ComputeWorldBounds(
            IReadOnlyList<RuntimeNode> nodes,
            IReadOnlyList<RuntimeMesh> meshes)
        {
            var bounds = RuntimeBounds.Empty;
            foreach (var node in nodes)
            {
                if (node.MeshIndex is not { } meshIndex) continue;
                foreach (var primitive in meshes[meshIndex].Primitives)
                foreach (var position in primitive.Vertices.Positions)
                    bounds = bounds.Include(Vector3.Transform(position, node.WorldTransform.Matrix));
            }
            if (!bounds.IsFinite) throw new FormatException("Scene has no finite rendered bounds.");
            return bounds;
        }

        private IReadOnlyList<Vector3> ReadVector3(int accessorIndex, string semantic, int maxCount)
        {
            var accessor = GetAccessor(accessorIndex, semantic, "VEC3", 5126, maxCount);
            var values = new Vector3[accessor.Count];
            ReadElements(accessor, 12, (span, index) =>
            {
                var value = new Vector3(ReadSingle(span), ReadSingle(span[4..]), ReadSingle(span[8..]));
                if (!IsFinite(value)) throw new FormatException($"{semantic} contains NaN or Infinity.");
                values[index] = value;
            });
            return Array.AsReadOnly(values);
        }

        private IReadOnlyList<Vector2> ReadVector2(int accessorIndex, string semantic, int maxCount)
        {
            var accessor = GetAccessor(accessorIndex, semantic, "VEC2", 5126, maxCount);
            var values = new Vector2[accessor.Count];
            ReadElements(accessor, 8, (span, index) =>
            {
                var value = new Vector2(ReadSingle(span), ReadSingle(span[4..]));
                if (!float.IsFinite(value.X) || !float.IsFinite(value.Y))
                    throw new FormatException($"{semantic} contains NaN or Infinity.");
                values[index] = value;
            });
            return Array.AsReadOnly(values);
        }

        private IReadOnlyList<uint> ReadIndices(int accessorIndex, int vertexCount)
        {
            var accessor = GetAccessor(accessorIndex, "indices", "SCALAR", null, limits.MaxIndices);
            if (accessor.Normalized || accessor.ComponentType is not (5121 or 5123 or 5125))
                throw new FormatException("Index accessor component type is unsupported.");
            var bytes = accessor.ComponentType switch { 5121 => 1, 5123 => 2, _ => 4 };
            var values = new uint[accessor.Count];
            ReadElements(accessor, bytes, (span, index) =>
            {
                var value = bytes switch
                {
                    1 => span[0],
                    2 => BitConverter.ToUInt16(span),
                    _ => BitConverter.ToUInt32(span)
                };
                if (value >= vertexCount) throw new FormatException("Primitive index is out of range.");
                values[index] = value;
            });
            return Array.AsReadOnly(values);
        }

        private Accessor GetAccessor(
            int index,
            string semantic,
            string type,
            int? componentType,
            int maxCount)
        {
            if ((uint)index >= (uint)_accessors.Length) throw new FormatException($"{semantic} accessor is out of range.");
            var accessor = _accessors[index];
            if (accessor.Type != type || (componentType is { } expected && accessor.ComponentType != expected) ||
                accessor.Count > maxCount || accessor.Count == 0)
                throw new FormatException($"{semantic} accessor shape/type/count is unsupported.");
            if (accessor.Normalized && semantic is "POSITION" or "NORMAL")
                throw new FormatException($"{semantic} must not be normalized.");
            return accessor;
        }

        private void ReadElements(Accessor accessor, int elementBytes, ElementReader reader)
        {
            var view = _views[accessor.View];
            var stride = view.Stride == 0 ? elementBytes : view.Stride;
            var relativeEnd = RuntimeModelSecurity.CheckedRangeEnd(
                accessor.Offset, accessor.Count, stride, elementBytes);
            if (relativeEnd > view.Length) throw new FormatException("Accessor exceeds its bufferView.");
            for (var index = 0; index < accessor.Count; index++)
            {
                if ((index & 0x3fff) == 0) cancellationToken.ThrowIfCancellationRequested();
                var offset = checked(view.Offset + accessor.Offset + checked(index * stride));
                reader(bin.AsSpan(offset, elementBytes), index);
            }
        }

        private static float ReadSingle(ReadOnlySpan<byte> span) =>
            BitConverter.Int32BitsToSingle(BitConverter.ToInt32(span));

        private static float ReadFiniteSingle(JsonElement element)
        {
            var value = element.GetSingle();
            if (!float.IsFinite(value)) throw new FormatException("JSON numeric value is not finite.");
            return value;
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

        private static long EstimateBytes(
            IEnumerable<RuntimeNode> nodes,
            IEnumerable<RuntimeMesh> meshes,
            IEnumerable<RuntimeTexture> textures)
        {
            long bytes = nodes.Count() * 160L;
            foreach (var primitive in meshes.SelectMany(mesh => mesh.Primitives))
            {
                bytes = checked(bytes + primitive.Vertices.Positions.Count * 12L +
                    primitive.Vertices.Normals.Count * 12L +
                    primitive.Vertices.TextureCoordinates0.Count * 8L +
                    primitive.Indices.Indices.Count * 4L);
            }
            return checked(bytes + textures.Sum(texture => (long)texture.Content.Length));
        }

        private delegate void ElementReader(ReadOnlySpan<byte> span, int index);
        private readonly record struct View(int Offset, int Length, int Stride);
        private readonly record struct Accessor(
            int View, int Offset, int ComponentType, int Count, string Type, bool Normalized);
    }

    private readonly record struct Container(byte[] Json, byte[] Bin);
}
