using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace ChessApp;

internal sealed class ObjModelLibrary
{
    private readonly Dictionary<string, MeshGeometry3D?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Material> _materialCache = new(StringComparer.OrdinalIgnoreCase);

    public string? SelectedSetPath { get; set; }
    public string LastDiagnostics { get; private set; } = "models not loaded";

    public static IEnumerable<string> ModelRoots()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Assets", "Models");
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "models", "chess", "pieces"));
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "assets", "models", "chess", "pieces"));
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "ChessApp", "Assets", "Models"));
    }

    public static IEnumerable<(string Name, string Path)> DiscoverSets()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in ModelRoots().Distinct(StringComparer.OrdinalIgnoreCase).Where(Directory.Exists))
        {
            foreach (var dir in Directory.GetDirectories(root).OrderBy(Path.GetFileName))
            {
                var name = Path.GetFileName(dir);
                if (seen.Add(name))
                {
                    yield return (name, dir);
                }
            }
        }
    }

    public void ClearCache()
    {
        _cache.Clear();
        _materialCache.Clear();
        LastDiagnostics = "model cache cleared";
    }

    public MeshGeometry3D? LoadMesh(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(SelectedSetPath))
        {
            return null;
        }

        var fullPath = Path.Combine(SelectedSetPath, relativePath);
        if (_cache.TryGetValue(fullPath, out var cached))
        {
            return cached;
        }

        var mesh = File.Exists(fullPath) ? LoadObjMesh(fullPath) : null;
        LastDiagnostics = mesh != null
            ? $"model loaded: {Path.GetFileName(fullPath)}"
            : $"model fallback: {Path.GetFileName(fullPath)} missing or unreadable";
        _cache[fullPath] = mesh;
        return mesh;
    }

    public Material CreatePieceMaterial(string relativeObjPath, int side, int pieceType, byte opacity = 255)
    {
        var fallback = PieceColor(side, opacity);
        return CreateMaterial(relativeObjPath, fallback, preferTexture: true);
    }

    public Material CreateRoleMaterial(string relativeObjPath, Color fallback) =>
        CreateMaterial(relativeObjPath, fallback, preferTexture: true);

    public static Material CreateFallbackPieceMaterial(int side, byte opacity = 255)
    {
        return CreateMaterialGroup(PieceColor(side, opacity), Colors.White, 42, subtleEmissive: true);
    }

    public static Material CreateSurfaceMaterial(Color color, int specularAlpha = 70)
    {
        return CreateMaterialGroup(color, Color.FromArgb((byte)specularAlpha, 255, 255, 255), 28, subtleEmissive: false);
    }

    public static Material CreateSurfaceMaterial(Brush brush)
    {
        var material = new MaterialGroup();
        material.Children.Add(new DiffuseMaterial(brush));
        material.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(55, 255, 255, 255)), 24));
        return material;
    }

    public static Color PieceColor(int side, byte opacity = 255)
    {
        var color = side switch
        {
            1 => Color.FromArgb(opacity, 238, 231, 206),
            2 => Color.FromArgb(opacity, 88, 96, 108),
            3 => Color.FromArgb(opacity, 96, 142, 188),
            4 => Color.FromArgb(opacity, 166, 104, 150),
            5 => Color.FromArgb(opacity, 114, 154, 112),
            6 => Color.FromArgb(opacity, 182, 138, 88),
            _ => Color.FromArgb(opacity, 142, 150, 160)
        };
        return color;
    }

    public static string ModelFileNameForClassicPiece(int type, bool useWhiteMesh)
    {
        var color = useWhiteMesh ? "white" : "black";
        var name = Math.Abs(type) switch
        {
            1 => "pawn",
            2 => "knight",
            3 => "bishop",
            4 => "rook",
            5 => "queen",
            6 => "king",
            _ => "pawn"
        };
        return $"{color}_{name}.obj";
    }

    private Material CreateMaterial(string relativeObjPath, Color fallback, bool preferTexture)
    {
        if (string.IsNullOrWhiteSpace(SelectedSetPath))
        {
            LastDiagnostics = "material fallback: no selected model set";
            return CreateMaterialGroup(fallback, Colors.White, 42, subtleEmissive: true);
        }

        var objPath = Path.Combine(SelectedSetPath, relativeObjPath);
        var cacheKey = $"{objPath}|{fallback}";
        if (_materialCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var hint = TryReadMaterialHint(objPath);
        var texturePath = preferTexture ? ResolveTexturePath(objPath, hint.DiffuseTexturePath) : null;
        Material material;
        if (!string.IsNullOrWhiteSpace(texturePath))
        {
            var brush = CreateImageBrush(texturePath);
            material = new MaterialGroup
            {
                Children =
                {
                    new DiffuseMaterial(brush),
                    new SpecularMaterial(new SolidColorBrush(hint.SpecularColor ?? Color.FromArgb(85, 255, 255, 255)), hint.Shininess)
                }
            };
            LastDiagnostics = $"material texture: {Path.GetFileName(texturePath)}";
        }
        else
        {
            material = CreateMaterialGroup(fallback, hint.SpecularColor ?? Colors.White, hint.Shininess, subtleEmissive: true);
            var mtlStatus = hint.MtlFound ? "mtl found" : "mtl missing";
            var textureStatus = string.IsNullOrWhiteSpace(hint.DiffuseTexturePath) ? "no texture" : "texture missing";
            LastDiagnostics = $"material fallback: {Path.GetFileName(objPath)} ({mtlStatus}, {textureStatus})";
        }

        _materialCache[cacheKey] = material;
        return material;
    }

    private static Material CreateMaterialGroup(Color diffuse, Color specular, double shininess, bool subtleEmissive)
    {
        var material = new MaterialGroup();
        material.Children.Add(new DiffuseMaterial(new SolidColorBrush(diffuse)));
        material.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(95, specular.R, specular.G, specular.B)), shininess));
        if (subtleEmissive)
        {
            material.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(18, diffuse.R, diffuse.G, diffuse.B))));
        }
        return material;
    }

    private static ImageBrush CreateImageBrush(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();

        var brush = new ImageBrush(image)
        {
            ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
            TileMode = TileMode.None,
            Stretch = Stretch.Fill
        };
        brush.Freeze();
        return brush;
    }

    private static MaterialHint TryReadMaterialHint(string objPath)
    {
        var hint = new MaterialHint();
        var mtlPath = ResolveMtlPath(objPath);
        if (string.IsNullOrWhiteSpace(mtlPath) || !File.Exists(mtlPath))
        {
            return hint;
        }

        hint.MtlFound = true;
        foreach (var raw in File.ReadLines(mtlPath))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            if (parts[0].Equals("Ks", StringComparison.OrdinalIgnoreCase) && parts.Length >= 4)
            {
                hint.SpecularColor = ColorFromMtl(parts, alpha: 255);
            }
            else if (parts[0].Equals("Ns", StringComparison.OrdinalIgnoreCase) && parts.Length >= 2 &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var shininess))
            {
                hint.Shininess = Math.Clamp(shininess, 8, 96);
            }
            else if (parts[0].Equals("map_Kd", StringComparison.OrdinalIgnoreCase) && parts.Length >= 2)
            {
                hint.DiffuseTexturePath = line.Substring("map_Kd".Length).Trim().Trim('"');
            }
        }

        return hint;
    }

    private static string? ResolveMtlPath(string objPath)
    {
        try
        {
            foreach (var raw in File.ReadLines(objPath))
            {
                var line = raw.Trim();
                if (line.StartsWith("mtllib ", StringComparison.OrdinalIgnoreCase))
                {
                    var name = line.Substring("mtllib ".Length).Trim().Trim('"');
                    var candidate = Path.Combine(Path.GetDirectoryName(objPath) ?? string.Empty, name);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }
        catch
        {
            return null;
        }

        var sameStem = Path.ChangeExtension(objPath, ".mtl");
        return File.Exists(sameStem) ? sameStem : null;
    }

    private static string? ResolveTexturePath(string objPath, string? texture)
    {
        if (string.IsNullOrWhiteSpace(texture) || texture == ".")
        {
            return null;
        }

        var normalized = texture.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        if (Path.IsPathFullyQualified(normalized) && File.Exists(normalized))
        {
            return normalized;
        }

        var objDir = Path.GetDirectoryName(objPath) ?? string.Empty;
        var direct = Path.Combine(objDir, normalized);
        if (File.Exists(direct))
        {
            return direct;
        }

        var textureFile = Path.GetFileName(normalized);
        if (string.IsNullOrWhiteSpace(textureFile))
        {
            return null;
        }

        var found = Directory.EnumerateFiles(objDir, textureFile, SearchOption.AllDirectories).FirstOrDefault();
        return found;
    }

    private static Color ColorFromMtl(string[] parts, byte alpha)
    {
        static byte Component(string text)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? (byte)Math.Clamp((int)Math.Round(value * 255), 0, 255)
                : (byte)255;
        }

        return Color.FromArgb(alpha, Component(parts[1]), Component(parts[2]), Component(parts[3]));
    }

    private static MeshGeometry3D? LoadObjMesh(string path)
    {
        try
        {
            var vertices = new List<Point3D>();
            var textureCoordinates = new List<Point>();
            var mesh = new MeshGeometry3D();

            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    continue;
                }

                if (parts[0] == "v" && parts.Length >= 4)
                {
                    vertices.Add(new Point3D(
                        double.Parse(parts[1], CultureInfo.InvariantCulture),
                        double.Parse(parts[2], CultureInfo.InvariantCulture),
                        double.Parse(parts[3], CultureInfo.InvariantCulture)));
                }
                else if (parts[0] == "vt" && parts.Length >= 3)
                {
                    textureCoordinates.Add(new Point(
                        double.Parse(parts[1], CultureInfo.InvariantCulture),
                        1.0 - double.Parse(parts[2], CultureInfo.InvariantCulture)));
                }
                else if (parts[0] == "f" && parts.Length >= 4)
                {
                    var indices = parts.Skip(1).Select(p => ParseObjVertex(p, vertices.Count, textureCoordinates.Count)).ToArray();
                    for (var i = 1; i + 1 < indices.Length; ++i)
                    {
                        AddTriangle(mesh, vertices, textureCoordinates, indices[0], indices[i], indices[i + 1]);
                    }
                }
            }

            if (mesh.Positions.Count == 0)
            {
                return null;
            }
            NormalizeObjMesh(mesh, path.Contains($"{Path.DirectorySeparatorChar}Board{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
            mesh.Freeze();
            return mesh;
        }
        catch
        {
            return null;
        }
    }

    private static ObjVertex ParseObjVertex(string token, int vertexCount, int textureCount)
    {
        var parts = token.Split('/');
        var vertexIndex = ParseObjIndex(parts[0], vertexCount);
        var textureIndex = parts.Length >= 2 && parts[1].Length > 0
            ? ParseObjIndex(parts[1], textureCount)
            : -1;
        return new ObjVertex(vertexIndex, textureIndex);
    }

    private static int ParseObjIndex(string text, int count)
    {
        var index = int.Parse(text, CultureInfo.InvariantCulture);
        return index < 0 ? count + index : index - 1;
    }

    private static void AddTriangle(MeshGeometry3D mesh, List<Point3D> vertices, List<Point> textureCoordinates, ObjVertex a, ObjVertex b, ObjVertex c)
    {
        AddObjVertex(mesh, vertices, textureCoordinates, a);
        AddObjVertex(mesh, vertices, textureCoordinates, b);
        AddObjVertex(mesh, vertices, textureCoordinates, c);
    }

    private static void AddObjVertex(MeshGeometry3D mesh, List<Point3D> vertices, List<Point> textureCoordinates, ObjVertex vertex)
    {
        if (vertex.VertexIndex < 0 || vertex.VertexIndex >= vertices.Count)
        {
            return;
        }

        var index = mesh.Positions.Count;
        mesh.Positions.Add(vertices[vertex.VertexIndex]);
        mesh.TextureCoordinates.Add(vertex.TextureIndex >= 0 && vertex.TextureIndex < textureCoordinates.Count
            ? textureCoordinates[vertex.TextureIndex]
            : new Point(0, 0));
        mesh.TriangleIndices.Add(index);
    }

    private static void NormalizeObjMesh(MeshGeometry3D mesh, bool isBoardTile)
    {
        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var minZ = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;
        var maxZ = double.NegativeInfinity;

        foreach (var p in mesh.Positions)
        {
            minX = Math.Min(minX, p.X);
            minY = Math.Min(minY, p.Y);
            minZ = Math.Min(minZ, p.Z);
            maxX = Math.Max(maxX, p.X);
            maxY = Math.Max(maxY, p.Y);
            maxZ = Math.Max(maxZ, p.Z);
        }

        var width = Math.Max(0.0001, maxX - minX);
        var height = Math.Max(0.0001, maxY - minY);
        var depth = Math.Max(0.0001, maxZ - minZ);
        var scale = isBoardTile
            ? 0.96 / Math.Max(width, depth)
            : Math.Min(0.82 / height, 0.72 / Math.Max(width, depth));
        var centerX = (minX + maxX) / 2.0;
        var centerZ = (minZ + maxZ) / 2.0;

        for (var i = 0; i < mesh.Positions.Count; ++i)
        {
            var p = mesh.Positions[i];
            mesh.Positions[i] = new Point3D(
                (p.X - centerX) * scale,
                (p.Y - minY) * scale,
                (p.Z - centerZ) * scale);
        }
    }

    private sealed class MaterialHint
    {
        public bool MtlFound { get; set; }
        public string? DiffuseTexturePath { get; set; }
        public Color? SpecularColor { get; set; }
        public double Shininess { get; set; } = 42;
    }

    private readonly record struct ObjVertex(int VertexIndex, int TextureIndex);
}
