using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using ChessApp;
using Microsoft.Win32;
using ModelAssets;
using ModelAssets.Wpf;

namespace ModelAssetPreview;

public partial class MainWindow : Window
{
    private readonly WpfRuntimeModelFactory _factory = new();
    private string? _currentPath;
    private string? _manifestPath;
    private ModelAssetValidationReport? _validation;
    private RuntimeModelAsset? _runtimeModel;
    private string _report = "{}";
    private Point _dragStart;
    private bool _dragging;
    private double _yaw = 36;
    private double _pitch = 22;
    private double _distance = 7.5;

    public MainWindow()
    {
        InitializeComponent();
        ApplyLighting();
        ResetCamera();
        RebuildOverlays();
    }

    private async void OpenManifest_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Model manifest|asset-manifest-v2.json|JSON|*.json" };
        if (dialog.ShowDialog(this) != true) return;
        _manifestPath = dialog.FileName;
        await ValidateAndLoadManifestAsync();
    }

    private async void OpenModel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Runtime models|*.glb;*.obj|GLB|*.glb|OBJ|*.obj" };
        if (dialog.ShowDialog(this) != true) return;
        _manifestPath = null;
        _validation = null;
        await LoadModelAsync(dialog.FileName, null);
    }

    private async void Reload_Click(object sender, RoutedEventArgs e)
    {
        if (_manifestPath is not null) await ValidateAndLoadManifestAsync();
        else if (_currentPath is not null) await LoadModelAsync(_currentPath, null);
    }

    private async void Validate_Click(object sender, RoutedEventArgs e)
    {
        if (_manifestPath is null)
        {
            StatusText.Text = "Validation requires an asset-manifest-v2.json.";
            return;
        }
        await ValidateAndLoadManifestAsync(loadModel: false);
    }

    private async Task ValidateAndLoadManifestAsync(bool loadModel = true)
    {
        if (_manifestPath is null) return;
        SetBusy("Validating manifest...");
        try
        {
            _validation = await Task.Run(() => new ModelAssetValidator().Validate(new()
            {
                ManifestPath = _manifestPath
            }));
            var manifest = ModelAssetManifestJson.Deserialize(await File.ReadAllTextAsync(_manifestPath));
            var first = manifest.Assets.FirstOrDefault();
            BuildReport(manifest);
            if (!_validation.IsValid)
            {
                StatusText.Text = "Validation failed. Model was not assigned.";
                AssetVisual.Content = null;
                return;
            }
            StatusText.Text = $"Validation PASS: {manifest.SetId}, {manifest.Assets.Count} asset(s).";
            if (loadModel && first is not null)
            {
                var path = Path.Combine(Path.GetDirectoryName(_manifestPath)!, first.Path.Replace('/', Path.DirectorySeparatorChar));
                await LoadModelAsync(path, first.Sha256);
            }
        }
        catch (Exception ex)
        {
            ShowFailure(ex);
        }
    }

    private async Task LoadModelAsync(string path, string? expectedSha)
    {
        SetBusy($"Loading {Path.GetFileName(path)}...");
        var timer = Stopwatch.StartNew();
        try
        {
            _currentPath = Path.GetFullPath(path);
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".glb")
            {
                expectedSha ??= Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))).ToLowerInvariant();
                var model = await new GlbRuntimeModelLoader().LoadAsync(new()
                {
                    Path = path,
                    ExpectedSha256 = expectedSha
                });
                var result = _factory.Create(model);
                _runtimeModel = model;
                AssetVisual.Content = result.Model;
                timer.Stop();
                StatusText.Text = $"GLB loaded in {timer.ElapsedMilliseconds} ms; WPF cache={result.FromCache}.";
                BuildReport(null, result.Warnings);
            }
            else if (extension == ".obj")
            {
                var library = new ObjModelLibrary { SelectedSetPath = Path.GetDirectoryName(path) };
                var mesh = library.LoadMesh(Path.GetFileName(path))
                    ?? throw new FormatException("OBJ has no readable mesh.");
                var model = new GeometryModel3D(mesh, ObjModelLibrary.CreateFallbackPieceMaterial(1));
                if (model.CanFreeze) model.Freeze();
                AssetVisual.Content = model;
                _runtimeModel = null;
                timer.Stop();
                StatusText.Text = $"OBJ compatibility loader PASS in {timer.ElapsedMilliseconds} ms.";
                BuildReport(null, [library.LastDiagnostics]);
            }
            else
            {
                throw new NotSupportedException($"Preview format '{extension}' is unsupported.");
            }
            ResetCamera();
            RebuildOverlays();
        }
        catch (Exception ex)
        {
            ShowFailure(ex);
        }
    }

    private void BuildReport(ModelAssetManifest? manifest, IReadOnlyList<string>? renderWarnings = null)
    {
        var report = new
        {
            format = "chess-model-preview-evidence",
            version = "1.0",
            setId = manifest?.SetId ?? _validation?.SetId,
            semanticRole = manifest?.Assets.FirstOrDefault()?.Role,
            sourceFormat = _currentPath is null ? null : Path.GetExtension(_currentPath).TrimStart('.').ToLowerInvariant(),
            sha256 = _runtimeModel?.ContentSha256 ?? manifest?.Assets.FirstOrDefault()?.Sha256,
            license = manifest?.License.SpdxId,
            units = manifest?.Units,
            coordinateSystem = manifest?.CoordinateSystem,
            bounds = _runtimeModel?.Bounds,
            meshes = _runtimeModel?.Meshes.Count,
            primitives = _runtimeModel?.Meshes.Sum(mesh => mesh.Primitives.Count),
            materials = _runtimeModel?.Materials.Count,
            textures = _runtimeModel?.Textures.Count,
            estimatedManagedBytes = _runtimeModel?.Diagnostics.EstimatedManagedBytes,
            loadMilliseconds = _runtimeModel?.Diagnostics.LoadTime.TotalMilliseconds,
            validationPass = _validation?.IsValid,
            validationIssues = _validation?.Issues.Select(issue => new { issue.Severity, issue.Code, issue.Message }),
            unsupported = _runtimeModel?.Diagnostics.UnsupportedFeatures,
            renderWarnings = renderWarnings ?? []
        };
        _report = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        DiagnosticsText.Text = _report;
    }

    private void SetBusy(string status)
    {
        StatusText.Text = status;
        DiagnosticsText.Text = status;
    }

    private void ShowFailure(Exception ex)
    {
        AssetVisual.Content = null;
        StatusText.Text = $"FAIL: {ex.Message}";
        _report = JsonSerializer.Serialize(new
        {
            format = "chess-model-preview-evidence",
            version = "1.0",
            status = "FAIL",
            errorType = ex.GetType().Name,
            error = ex.Message
        }, new JsonSerializerOptions { WriteIndented = true });
        DiagnosticsText.Text = _report;
    }

    private void ExportReport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "JSON report|*.json", FileName = "model-preview-report.json" };
        if (dialog.ShowDialog(this) != true) return;
        File.WriteAllText(dialog.FileName, _report + Environment.NewLine);
        StatusText.Text = "Report exported.";
    }

    private void SaveEvidence_Click(object sender, RoutedEventArgs e)
    {
        var root = FindRepositoryRoot() ?? AppContext.BaseDirectory;
        var evidenceRoot = Path.Combine(root, ".tmp", "model-evidence");
        Directory.CreateDirectory(evidenceRoot);
        var id = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var imagePath = Path.Combine(evidenceRoot, $"model-preview-{id}.png");
        var reportPath = Path.Combine(evidenceRoot, $"model-preview-{id}.json");
        var bitmap = new RenderTargetBitmap(
            Math.Max(1, (int)Viewport.ActualWidth),
            Math.Max(1, (int)Viewport.ActualHeight),
            96, 96, PixelFormats.Pbgra32);
        bitmap.Render(Viewport);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var stream = File.Create(imagePath)) encoder.Save(stream);
        File.WriteAllText(reportPath, _report + Environment.NewLine);
        StatusText.Text = $"Evidence saved under .tmp/model-evidence ({id}).";
    }

    private static string? FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Chess.sln")))
            current = current.Parent;
        return current?.FullName;
    }

    private void ApplyLighting()
    {
        var group = new Model3DGroup();
        group.Children.Add(new AmbientLight(Color.FromRgb(88, 92, 100)));
        var selected = LightingBox.SelectedIndex;
        group.Children.Add(new DirectionalLight(
            selected == 2 ? Colors.White : Color.FromRgb(238, 236, 224),
            selected == 1 ? new(-0.5, -1, -0.4) : new(-0.8, -1, -0.7)));
        if (selected != 1)
            group.Children.Add(new DirectionalLight(Color.FromRgb(95, 120, 145), new(0.8, -0.2, 0.7)));
        LightVisual.Content = group;
    }

    private void RebuildOverlays()
    {
        var group = new Model3DGroup();
        if (GroundPlaneBox.IsChecked == true) group.Children.Add(CreateGround());
        if (ShowOriginBox.IsChecked == true) group.Children.Add(CreateMarker(Colors.Gold, 0.08));
        if (ShowAxesBox.IsChecked == true)
        {
            group.Children.Add(CreateAxis(Colors.IndianRed, new(1.5, 0, 0)));
            group.Children.Add(CreateAxis(Colors.LightGreen, new(0, 1.5, 0)));
            group.Children.Add(CreateAxis(Colors.SkyBlue, new(0, 0, 1.5)));
        }
        if (ShowBoundsBox.IsChecked == true && _runtimeModel?.Bounds is { IsFinite: true } bounds)
            group.Children.Add(CreateBounds(bounds));
        OverlayVisual.Content = group;
    }

    private static GeometryModel3D CreateGround()
    {
        var mesh = new MeshGeometry3D
        {
            Positions = new Point3DCollection(new[]
            {
                new Point3D(-4, 0, -4), new Point3D(4, 0, -4),
                new Point3D(4, 0, 4), new Point3D(-4, 0, 4)
            }),
            TriangleIndices = new Int32Collection(new[] { 0, 1, 2, 0, 2, 3 })
        };
        var material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(90, 95, 104, 112)));
        return new(mesh, material);
    }

    private static GeometryModel3D CreateMarker(Color color, double size)
    {
        var mesh = new MeshGeometry3D
        {
            Positions = new Point3DCollection(new[]
            {
                new Point3D(-size, 0, 0), new Point3D(size, 0, 0),
                new Point3D(0, size, 0), new Point3D(0, -size, 0)
            }),
            TriangleIndices = new Int32Collection(new[] { 0, 1, 2, 0, 3, 1 })
        };
        return new(mesh, new EmissiveMaterial(new SolidColorBrush(color)));
    }

    private static GeometryModel3D CreateAxis(Color color, Vector3D end)
    {
        var length = end.Length;
        var mesh = new MeshGeometry3D
        {
            Positions = new Point3DCollection(new[]
            {
                new Point3D(0, 0, 0), new Point3D(end.X, end.Y, end.Z),
                new Point3D(end.X + 0.025, end.Y + 0.025, end.Z + 0.025)
            }),
            TriangleIndices = new Int32Collection(new[] { 0, 1, 2 })
        };
        _ = length;
        return new(mesh, new EmissiveMaterial(new SolidColorBrush(color)));
    }

    private static Model3D CreateBounds(RuntimeBounds bounds)
    {
        var center = (bounds.Minimum + bounds.Maximum) / 2;
        var size = bounds.Maximum - bounds.Minimum;
        var mesh = new MeshGeometry3D
        {
            Positions = new Point3DCollection(new[]
            {
                new Point3D(-0.5, -0.5, -0.5), new Point3D(0.5, -0.5, -0.5),
                new Point3D(0.5, 0.5, -0.5), new Point3D(-0.5, 0.5, -0.5),
                new Point3D(-0.5, -0.5, 0.5), new Point3D(0.5, -0.5, 0.5),
                new Point3D(0.5, 0.5, 0.5), new Point3D(-0.5, 0.5, 0.5)
            }),
            TriangleIndices = new Int32Collection(new[] { 0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6 })
        };
        var material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(36, 255, 210, 75)));
        return new GeometryModel3D(mesh, material)
        {
            BackMaterial = material,
            Transform = new Transform3DGroup
            {
                Children =
                {
                    new ScaleTransform3D(size.X, size.Y, size.Z),
                    new TranslateTransform3D(center.X, center.Y, center.Z)
                }
            }
        };
    }

    private void ResetCamera_Click(object sender, RoutedEventArgs e) => ResetCamera();
    private void OrbitLeft_Click(object sender, RoutedEventArgs e) { _yaw -= 12; ResetCamera(); }
    private void OrbitRight_Click(object sender, RoutedEventArgs e) { _yaw += 12; ResetCamera(); }
    private void ZoomIn_Click(object sender, RoutedEventArgs e) { _distance = Math.Max(1.2, _distance * 0.84); ResetCamera(); }
    private void ZoomOut_Click(object sender, RoutedEventArgs e) { _distance = Math.Min(40, _distance * 1.18); ResetCamera(); }
    private void Overlay_Click(object sender, RoutedEventArgs e) => RebuildOverlays();
    private void LightingBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (IsLoaded) ApplyLighting(); }

    private void BackgroundBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ViewportBorder is null) return;
        ViewportBorder.Background = new SolidColorBrush(BackgroundBox.SelectedIndex switch
        {
            1 => Color.FromRgb(92, 98, 105),
            2 => Color.FromRgb(185, 188, 192),
            _ => Color.FromRgb(69, 75, 83)
        });
    }

    private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _distance = Math.Clamp(_distance * (e.Delta > 0 ? 0.9 : 1.1), 1.2, 40);
        ResetCamera();
    }

    private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true;
        _dragStart = e.GetPosition(Viewport);
        Viewport.CaptureMouse();
    }

    private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        Viewport.ReleaseMouseCapture();
    }

    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging || e.LeftButton != MouseButtonState.Pressed) return;
        var point = e.GetPosition(Viewport);
        _yaw += (point.X - _dragStart.X) * 0.35;
        _pitch = Math.Clamp(_pitch - (point.Y - _dragStart.Y) * 0.25, -80, 80);
        _dragStart = point;
        ResetCamera();
    }

    private void ResetCamera()
    {
        var yaw = _yaw * Math.PI / 180;
        var pitch = _pitch * Math.PI / 180;
        var position = new Point3D(
            _distance * Math.Cos(pitch) * Math.Sin(yaw),
            _distance * Math.Sin(pitch),
            _distance * Math.Cos(pitch) * Math.Cos(yaw));
        Camera.Position = position;
        Camera.LookDirection = new(-position.X, -position.Y + 0.35, -position.Z);
        Camera.UpDirection = new(0, 1, 0);
    }
}
