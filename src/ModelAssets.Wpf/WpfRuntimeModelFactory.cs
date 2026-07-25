using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace ModelAssets.Wpf;

public sealed record WpfRuntimeModelResult(
    Model3DGroup Model,
    bool FromCache,
    IReadOnlyList<string> Warnings);

public sealed class WpfRuntimeModelFactory
{
    private readonly WpfMeshCache _meshes = new();
    private readonly WpfMaterialCache _materials = new();
    private readonly WpfTextureCache _textures = new();
    private readonly ConcurrentDictionary<string, Model3DGroup> _models =
        new(StringComparer.OrdinalIgnoreCase);

    public WpfRuntimeModelResult Create(RuntimeModelAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (_models.TryGetValue(asset.ContentSha256, out var cached))
            return new(cached, true, []);

        var warnings = new List<string>();
        var root = new Model3DGroup();
        foreach (var node in asset.Nodes)
        {
            if (node.MeshIndex is not { } meshIndex) continue;
            if ((uint)meshIndex >= (uint)asset.Meshes.Count)
                throw new FormatException("Runtime node references an unavailable mesh.");
            var nodeGroup = new Model3DGroup
            {
                Transform = new MatrixTransform3D(ToMatrix3D(node.WorldTransform.Matrix))
            };
            var mesh = asset.Meshes[meshIndex];
            for (var primitiveIndex = 0; primitiveIndex < mesh.Primitives.Count; primitiveIndex++)
            {
                var primitive = mesh.Primitives[primitiveIndex];
                var geometry = _meshes.GetOrCreate(
                    $"{asset.ContentSha256}:{meshIndex}:{primitiveIndex}", primitive);
                var materialIndex = primitive.MaterialIndex;
                var material = materialIndex is { } index && (uint)index < (uint)asset.Materials.Count
                    ? asset.Materials[index]
                    : new RuntimeMaterial("Fallback", new(0.72f, 0.74f, 0.78f, 1), null,
                        RuntimeAlphaMode.Opaque, 0.5f, false);
                var wpfMaterial = _materials.GetOrCreate(asset, material, _textures, warnings);
                var model = new GeometryModel3D(geometry, wpfMaterial);
                if (material.DoubleSided) model.BackMaterial = wpfMaterial;
                if (model.CanFreeze) model.Freeze();
                nodeGroup.Children.Add(model);
            }
            if (nodeGroup.Transform.CanFreeze) nodeGroup.Transform.Freeze();
            if (nodeGroup.CanFreeze) nodeGroup.Freeze();
            root.Children.Add(nodeGroup);
        }
        if (root.Children.Count == 0) throw new FormatException("Runtime model has no renderable node.");
        if (root.CanFreeze) root.Freeze();
        _models[asset.ContentSha256] = root;
        return new(root, false, warnings.AsReadOnly());
    }

    public void Clear()
    {
        _models.Clear();
        _meshes.Clear();
        _materials.Clear();
        _textures.Clear();
    }

    private static Matrix3D ToMatrix3D(System.Numerics.Matrix4x4 matrix) =>
        new(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44);
}

public sealed class WpfMeshCache
{
    private readonly ConcurrentDictionary<string, MeshGeometry3D> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public MeshGeometry3D GetOrCreate(string key, RuntimePrimitive primitive) =>
        _cache.GetOrAdd(key, _ => Create(primitive));

    public void Clear() => _cache.Clear();

    private static MeshGeometry3D Create(RuntimePrimitive primitive)
    {
        var positions = new Point3DCollection(primitive.Vertices.Positions.Count);
        foreach (var value in primitive.Vertices.Positions)
            positions.Add(new(value.X, value.Y, value.Z));
        var normals = new Vector3DCollection(primitive.Vertices.Normals.Count);
        foreach (var value in primitive.Vertices.Normals)
            normals.Add(new(value.X, value.Y, value.Z));
        var coordinates = new PointCollection(primitive.Vertices.TextureCoordinates0.Count);
        foreach (var value in primitive.Vertices.TextureCoordinates0)
            coordinates.Add(new(value.X, value.Y));
        var indices = new Int32Collection(primitive.Indices.Indices.Count);
        foreach (var value in primitive.Indices.Indices)
            indices.Add(checked((int)value));
        var mesh = new MeshGeometry3D
        {
            Positions = positions,
            Normals = normals,
            TextureCoordinates = coordinates,
            TriangleIndices = indices
        };
        if (mesh.CanFreeze) mesh.Freeze();
        return mesh;
    }
}

public sealed class WpfTextureCache
{
    private readonly ConcurrentDictionary<string, ImageBrush> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public ImageBrush GetOrCreate(RuntimeTexture texture) =>
        _cache.GetOrAdd(texture.ContentSha256, _ => Create(texture));

    public void Clear() => _cache.Clear();

    private static ImageBrush Create(RuntimeTexture texture)
    {
        using var stream = new MemoryStream(texture.Content.ToArray(), writable: false);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        if (image.CanFreeze) image.Freeze();
        var brush = new ImageBrush(image)
        {
            Stretch = Stretch.Fill,
            TileMode = TileMode.None,
            ViewportUnits = BrushMappingMode.RelativeToBoundingBox
        };
        if (brush.CanFreeze) brush.Freeze();
        return brush;
    }
}

public sealed class WpfMaterialCache
{
    private readonly ConcurrentDictionary<string, Material> _cache =
        new(StringComparer.Ordinal);

    public Material GetOrCreate(
        RuntimeModelAsset asset,
        RuntimeMaterial material,
        WpfTextureCache textures,
        ICollection<string> warnings)
    {
        var textureHash = material.BaseColorTextureIndex is { } textureIndex &&
                          (uint)textureIndex < (uint)asset.Textures.Count
            ? asset.Textures[textureIndex].ContentSha256
            : "none";
        var key = $"{material.BaseColor}:{textureHash}:{material.AlphaMode}:{material.DoubleSided}";
        return _cache.GetOrAdd(key, _ => Create(asset, material, textures, warnings));
    }

    public void Clear() => _cache.Clear();

    private static Material Create(
        RuntimeModelAsset asset,
        RuntimeMaterial material,
        WpfTextureCache textures,
        ICollection<string> warnings)
    {
        Brush brush;
        if (material.BaseColorTextureIndex is { } textureIndex &&
            (uint)textureIndex < (uint)asset.Textures.Count)
        {
            try { brush = textures.GetOrCreate(asset.Textures[textureIndex]).CloneCurrentValue(); }
            catch (Exception ex)
            {
                warnings.Add($"Texture '{asset.Textures[textureIndex].Name}' fallback: {ex.Message}");
                brush = ColorBrush(material.BaseColor);
            }
        }
        else
        {
            brush = ColorBrush(material.BaseColor);
        }
        brush.Opacity = Math.Clamp(material.BaseColor.W, 0, 1);
        if (brush.CanFreeze) brush.Freeze();
        var group = new MaterialGroup();
        group.Children.Add(new DiffuseMaterial(brush));
        group.Children.Add(new SpecularMaterial(
            new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)), 32));
        if (group.CanFreeze) group.Freeze();
        return group;
    }

    private static SolidColorBrush ColorBrush(System.Numerics.Vector4 color)
    {
        static byte Byte(float value) => (byte)Math.Clamp((int)Math.Round(value * 255), 0, 255);
        return new(Color.FromArgb(Byte(color.W), Byte(color.X), Byte(color.Y), Byte(color.Z)));
    }
}
