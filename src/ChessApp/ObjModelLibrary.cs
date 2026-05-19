using System.Globalization;
using System.IO;
using System.Windows.Media.Media3D;

namespace ChessApp;

internal sealed class ObjModelLibrary
{
    private readonly Dictionary<string, MeshGeometry3D?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public string? SelectedSetPath { get; set; }

    public static IEnumerable<string> ModelRoots()
    {
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Assets", "Models"));
        yield return Path.Combine(AppContext.BaseDirectory, "Assets", "Models");
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
        _cache[fullPath] = mesh;
        return mesh;
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

    private static MeshGeometry3D? LoadObjMesh(string path)
    {
        try
        {
            var vertices = new List<Point3D>();
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
                else if (parts[0] == "f" && parts.Length >= 4)
                {
                    var indices = parts.Skip(1).Select(p => ParseObjIndex(p, vertices.Count)).ToArray();
                    for (var i = 1; i + 1 < indices.Length; ++i)
                    {
                        AddTriangle(mesh, vertices[indices[0]], vertices[indices[i]], vertices[indices[i + 1]]);
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

    private static int ParseObjIndex(string token, int vertexCount)
    {
        var first = token.Split('/')[0];
        var index = int.Parse(first, CultureInfo.InvariantCulture);
        return index < 0 ? vertexCount + index : index - 1;
    }

    private static void AddTriangle(MeshGeometry3D mesh, Point3D a, Point3D b, Point3D c)
    {
        var start = mesh.Positions.Count;
        mesh.Positions.Add(a);
        mesh.Positions.Add(b);
        mesh.Positions.Add(c);
        mesh.TriangleIndices.Add(start);
        mesh.TriangleIndices.Add(start + 1);
        mesh.TriangleIndices.Add(start + 2);
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
}
