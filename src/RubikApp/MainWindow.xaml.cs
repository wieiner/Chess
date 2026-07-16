using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Microsoft.Win32;
using RubikState;
using RubikVisuals;

namespace RubikApp;

public partial class MainWindow : Window
{
    private static readonly MeshGeometry3D SharedCubieBodyMesh = CreateUnitCubieMesh();
    private static readonly MeshGeometry3D[] SharedStickerMeshes = Enumerable.Range(0, 6).Select(CreateUnitStickerMesh).ToArray();
    private static readonly Dictionary<MaterialCacheKey, Material> SharedMaterialCache = new();
    private NativeRubikEngine _engine;
    private readonly RubikStateFileService _stateFileService = new();
    private readonly List<string> _recentStateFiles = new();
    private readonly Model3DGroup _scene = new();
    private readonly List<CubeVisual> _cubeVisuals = new();
    private readonly Dictionary<Model3D, CubeVisual> _cubeHitMap = new();
    private NativeRubikEngine.RubikMoveDto[] _lastSolution = Array.Empty<NativeRubikEngine.RubikMoveDto>();
    private PerspectiveCamera _camera = null!;
    private Point _lastMouse;
    private Point _mouseDownPoint;
    private bool _dragging;
    private bool _isAnimating;
    private bool _stopPlaybackRequested;
    private MouseLayerCandidate? _mouseLayerCandidate;
    private int _selectedAxis = -1;
    private int _selectedLayer = -1;
    private int _renderedCubieCount;
    private int _renderedStickerCount;
    private int _renderedCornerCount;
    private int _renderedEdgeCount;
    private int _renderedCenterCount;
    private int _renderedInternalCount;
    private int _invalidStickerCount;
    private bool _faceletsSynchronized;
    private bool _orientationAvailable;
    private bool _fallbackRendererActive;
    private double _lastSceneBuildMilliseconds;
    private long _lastSceneAllocatedBytes;
    private double _yaw = 42;
    private double _pitch = 26;
    private double _distance = 18;
    private Vector3D _pan = new(0, 0, 0);
    private string? _currentStateFile;
    private string? _savedStateHash;
    private string? _currentStateHash;

    public MainWindow()
    {
        InitializeComponent();
        _engine = new NativeRubikEngine();
        InitializeFileTracking();
        SetupViewport();
        RefreshScene();
        var arguments = Environment.GetCommandLineArgs();
        if (arguments.Any(argument =>
            string.Equals(argument, "--save-visual-evidence", StringComparison.OrdinalIgnoreCase)))
        {
            Loaded += SaveVisualEvidenceOnStartup;
        }
        else if (arguments.Any(argument =>
            string.Equals(argument, "--measure-render-performance", StringComparison.OrdinalIgnoreCase)))
        {
            Loaded += MeasureRenderPerformanceOnStartup;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _engine.Dispose();
        base.OnClosed(e);
    }

    private void SetupViewport()
    {
        _camera = new PerspectiveCamera
        {
            FieldOfView = 45
        };
        RubikViewport.Camera = _camera;
        RubikViewport.Children.Add(new ModelVisual3D { Content = _scene });
        UpdateCamera();
    }

    private void RefreshScene()
    {
        var stopwatch = Stopwatch.StartNew();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        _scene.Children.Clear();
        _cubeVisuals.Clear();
        _cubeHitMap.Clear();
        _scene.Children.Add(new AmbientLight(Color.FromRgb(96, 100, 112)));
        _scene.Children.Add(new DirectionalLight(Color.FromRgb(238, 242, 248), new Vector3D(-0.7, -1.2, -0.9)));
        _scene.Children.Add(new DirectionalLight(Color.FromRgb(115, 155, 210), new Vector3D(0.8, 0.4, 1.1)));

        var state = _engine.GetState();
        var size = Math.Max(2, state.Size);
        int[]? facelets = null;
        try
        {
            facelets = _engine.GetFacelets();
        }
        catch (InvalidOperationException)
        {
            // Legacy integer-only edits cannot provide physical sticker orientation.
        }
        _faceletsSynchronized = facelets != null;
        var cells = _engine.GetCells();
        var surfaceOnly = SurfaceOnlyBox.IsChecked == true;
        var spacing = SpacingForSize(size);
        var cubeSize = CubeSizeForDimension(size);
        var half = (size - 1) * spacing * 0.5;

        _scene.Children.Add(CreateBasePlate(half, spacing, size));
        AddAxesAndCoordinates(size, half, spacing);
        var summary = RubikVisualDescriptorBuilder.BuildScene(
            size,
            facelets,
            BuildVisualInputs(size, cells),
            surfaceOnly,
            _selectedAxis,
            _selectedLayer);
        _renderedCubieCount = summary.CubiesRendered;
        _renderedStickerCount = summary.StickersRendered;
        _renderedCornerCount = summary.CornersRendered;
        _renderedEdgeCount = summary.EdgesRendered;
        _renderedCenterCount = summary.CentersRendered;
        _renderedInternalCount = summary.InternalsRendered;
        _invalidStickerCount = summary.InvalidStickers;
        _orientationAvailable = summary.OrientationAvailable;
        _fallbackRendererActive = summary.FallbackRendererActive;

        foreach (var cubie in summary.Cubies)
        {
            var x = cubie.Coordinate.X;
            var y = cubie.Coordinate.Y;
            var z = cubie.Coordinate.Z;
            var isSurface = cubie.PhysicalStickerCount > 0;
            var opacity = isSurface ? 0.96 : 0.18;
            var center = new Point3D(x * spacing - half, y * spacing - half, z * spacing - half);
            var stickers = cubie.Stickers
                .Select(sticker => new StickerVisual(sticker.WorldFace, ColorForFacelet(sticker.ColorId)))
                .ToArray();
            var model = CreateRubikCubie(center, cubeSize, stickers, cubie.IsSelected, cubie.IsSelected ? 1.0 : opacity);
            _scene.Children.Add(model);
            var visual = new CubeVisual(x, y, z, center, model);
            _cubeVisuals.Add(visual);
            foreach (var child in model.Children.OfType<GeometryModel3D>())
            {
                _cubeHitMap[child] = visual;
            }
        }

        stopwatch.Stop();
        _lastSceneBuildMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        _lastSceneAllocatedBytes = Math.Max(0, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var state = _engine.GetState();
        SizeBox.Text = state.Size.ToString(CultureInfo.InvariantCulture);
        StatusText.Text = $"{state.Size}x{state.Size}x{state.Size}, ячеек {state.CellCount}, история {state.HistoryCount}, " +
                          $"собран: {(state.IsSolved != 0 ? "да" : "нет")}, ручной режим: {(state.ManualState != 0 ? "да" : "нет")}; " +
                          $"визуализация: {_renderedCubieCount} cubies / {_renderedStickerCount} stickers " +
                          $"(bodies {_renderedCubieCount}, corners {_renderedCornerCount}, edges {_renderedEdgeCount}, " +
                          $"centers {_renderedCenterCount}, internal {_renderedInternalCount}, invalid {_invalidStickerCount}); " +
                          $"facelets: {(_faceletsSynchronized ? "sync" : "unavailable")}, orientation: {(_orientationAvailable ? "available" : "unavailable")}, " +
                          $"fallback: {(_fallbackRendererActive ? "active" : "off")}; " +
                          $"build {_lastSceneBuildMilliseconds:0.0} ms / {_lastSceneAllocatedBytes / 1024.0:0} KiB, " +
                          $"shared meshes {SharedStickerMeshes.Length + 1}, materials {SharedMaterialCache.Count}";
        UpdateFileStatus();
        var info = _engine.GetLastInfo();
        if (!string.IsNullOrWhiteSpace(info) && string.IsNullOrWhiteSpace(OutputBox.Text))
        {
            OutputBox.Text = info;
        }
    }

    private async void Rotate_Click(object sender, RoutedEventArgs e)
    {
        var size = CurrentSize();
        var axis = AxisBox.SelectedIndex switch { 1 => 1, 2 => 2, _ => 0 };
        var layer = ReadInt(LayerBox, 1, 1, size) - 1;
        LayerBox.Text = (layer + 1).ToString();
        var turns = SelectedQuarterTurns();
        await AnimateAndCommitMoveAsync(new NativeRubikEngine.RubikMoveDto
        {
            Axis = axis,
            Layer = layer,
            QuarterTurns = turns
        });
        _lastSolution = Array.Empty<NativeRubikEngine.RubikMoveDto>();
    }

    private void ApplySize_Click(object sender, RoutedEventArgs e)
    {
        var size = ReadInt(SizeBox, 8, 2, 32);
        SizeBox.Text = size.ToString(CultureInfo.InvariantCulture);
        if (_engine.SetSize(size))
        {
            _lastSolution = Array.Empty<NativeRubikEngine.RubikMoveDto>();
            _selectedAxis = -1;
            _selectedLayer = -1;
            OutputBox.Text = _engine.GetLastInfo();
            ResetCameraToDimension(size);
            RefreshScene();
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _engine.Reset();
        _lastSolution = Array.Empty<NativeRubikEngine.RubikMoveDto>();
        _selectedAxis = -1;
        _selectedLayer = -1;
        OutputBox.Text = _engine.GetLastInfo();
        RefreshScene();
    }

    private void Scramble_Click(object sender, RoutedEventArgs e)
    {
        var seed = ReadInt(SeedBox, 2026, int.MinValue, int.MaxValue);
        var length = ReadInt(LengthBox, 24, 0, 10000);
        LengthBox.Text = length.ToString();
        if (_engine.Scramble(seed, length))
        {
            _lastSolution = Array.Empty<NativeRubikEngine.RubikMoveDto>();
            OutputBox.Text = $"{_engine.GetLastInfo()}\r\n\r\nScramble:\r\n{_engine.GetCommandText()}";
            RefreshScene();
        }
    }

    private void Solve_Click(object sender, RoutedEventArgs e)
    {
        _lastSolution = _engine.SolveByReverseHistory();
        var info = _engine.GetLastInfo();
        var commands = _engine.GetCommandText();
        OutputBox.Text = string.IsNullOrWhiteSpace(commands)
            ? info
            : $"{info}\r\n\r\nSolution:\r\n{commands}";
        UpdateStatus();
    }

    private void ApplySolution_Click(object sender, RoutedEventArgs e)
    {
        if (_lastSolution.Length == 0)
        {
            _lastSolution = _engine.SolveByReverseHistory();
        }

        if (_lastSolution.Length == 0)
        {
            OutputBox.Text = _engine.GetLastInfo();
            return;
        }

        if (_engine.ApplyMoves(_lastSolution))
        {
            _lastSolution = Array.Empty<NativeRubikEngine.RubikMoveDto>();
            OutputBox.Text = _engine.GetLastInfo();
            RefreshScene();
        }
    }

    private async void PlaySolution_Click(object sender, RoutedEventArgs e)
    {
        if (_lastSolution.Length == 0)
        {
            _lastSolution = _engine.SolveByReverseHistory();
        }

        if (_lastSolution.Length == 0)
        {
            OutputBox.Text = _engine.GetLastInfo();
            return;
        }

        await PlayMovesAsync(_lastSolution);
        _lastSolution = Array.Empty<NativeRubikEngine.RubikMoveDto>();
    }

    private void SetCell_Click(object sender, RoutedEventArgs e)
    {
        var state = _engine.GetState();
        var size = Math.Max(2, state.Size);
        var x = ReadInt(XBox, 1, 1, size) - 1;
        var y = ReadInt(YBox, 1, 1, size) - 1;
        var z = ReadInt(ZBox, 1, 1, size) - 1;
        var value = ReadInt(ValueBox, 0, 0, Math.Max(0, state.CellCount - 1));
        XBox.Text = (x + 1).ToString();
        YBox.Text = (y + 1).ToString();
        ZBox.Text = (z + 1).ToString();
        ValueBox.Text = value.ToString();

        if (_engine.SetCell(x, y, z, value))
        {
            _lastSolution = Array.Empty<NativeRubikEngine.RubikMoveDto>();
            OutputBox.Text = _engine.GetLastInfo();
            RefreshScene();
        }
    }

    private void ExportState_Click(object sender, RoutedEventArgs e)
    {
        OutputBox.Text = FormatCells(_engine.GetCells(), CurrentSize());
    }

    private void LoadState_Click(object sender, RoutedEventArgs e)
    {
        var cells = ParseCells(OutputBox.Text);
        var expected = _engine.GetState().CellCount;
        if (cells.Length != expected)
        {
            OutputBox.Text = $"Нужно ровно {expected} целых значений для текущего куба {CurrentSize()}x{CurrentSize()}x{CurrentSize()}, найдено {cells.Length}.";
            return;
        }

        if (_engine.SetCells(cells))
        {
            _lastSolution = Array.Empty<NativeRubikEngine.RubikMoveDto>();
            OutputBox.Text = _engine.GetLastInfo();
            RefreshScene();
        }
    }

    private async void SaveStateFile_Click(object sender, RoutedEventArgs e)
    {
        await SaveStateFileAsync(forceDialog: false);
    }

    private async void SaveStateAsFile_Click(object sender, RoutedEventArgs e)
    {
        await SaveStateFileAsync(forceDialog: true);
    }

    private async Task SaveStateFileAsync(bool forceDialog)
    {
        if (_isAnimating)
        {
            OutputBox.Text = "Finish or stop the current animation before saving.";
            return;
        }
        if (!TryBuildPortableDocument(out var document, out var error))
        {
            OutputBox.Text = error;
            return;
        }

        var path = _currentStateFile;
        if (forceDialog || string.IsNullOrWhiteSpace(path))
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Rubik physical state",
                Filter = "Rubik state (*.rubik.json)|*.rubik.json|JSON (*.json)|*.json",
                DefaultExt = ".rubik.json",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = path is null ? $"rubik-{document.Size}x{document.Size}.rubik.json" : Path.GetFileName(path)
            };
            if (dialog.ShowDialog(this) != true)
                return;
            path = dialog.FileName;
        }

        var result = await Task.Run(() => _stateFileService.Save(path!, document, retainBackup: File.Exists(path)));
        if (!result.Success)
        {
            OutputBox.Text = $"Save failed [{result.ErrorCode}]: {result.Message}";
            return;
        }

        _currentStateFile = result.Path;
        _savedStateHash = result.LoadPlan?.StateHash ?? RubikStateHasher.Calculate(document);
        AddRecentFile(result.Path);
        UpdateFileStatus();
        OutputBox.Text = $"State saved atomically: {result.Path}\r\nHash: {_savedStateHash}" +
                         (result.BackupPath is null ? string.Empty : $"\r\nBackup: {result.BackupPath}");
    }

    private async void LoadStateFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Load Rubik physical state",
            Filter = "Rubik state (*.rubik.json)|*.rubik.json|JSON (*.json)|*.json",
            DefaultExt = ".rubik.json",
            Multiselect = false,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == true)
            await LoadStateFileAsync(dialog.FileName);
    }

    private void OpenPhysicalEditor_Click(object sender, RoutedEventArgs e)
    {
        if (_isAnimating)
        {
            OutputBox.Text = "Finish or stop the current animation before opening the physical editor.";
            return;
        }

        RubikFaceEditorDraft draft;
        try { draft = new RubikFaceEditorDraft(CurrentSize(), _engine.GetFacelets()); }
        catch { draft = new RubikFaceEditorDraft(CurrentSize()); }
        var editor = new RubikFaceEditorWindow(draft) { Owner = this };
        if (editor.ShowDialog() != true || editor.ResultDocument is null)
            return;

        var parsed = RubikStateSerializer.Parse(RubikStateSerializer.SerializeToUtf8(editor.ResultDocument));
        if (!parsed.Success || parsed.Plan is null)
        {
            OutputBox.Text = "Editor state failed portable validation and was not applied.";
            return;
        }

        NativeRubikEngine? candidate = null;
        try
        {
            candidate = new NativeRubikEngine();
            if (!candidate.SetSize(parsed.Plan.Document.Size) || !candidate.SetFacelets(parsed.Plan.Facelets))
            {
                OutputBox.Text = $"Native engine rejected editor state: {candidate.GetLastInfo()}";
                return;
            }
            var previous = _engine;
            _engine = candidate;
            candidate = null;
            previous.Dispose();
            _currentStateFile = null;
            _savedStateHash = null;
            _lastSolution = Array.Empty<NativeRubikEngine.RubikMoveDto>();
            _selectedAxis = -1;
            _selectedLayer = -1;
            ResetCameraToDimension(parsed.Plan.Document.Size);
            RefreshScene();
            OutputBox.Text = $"Physical editor state applied transactionally.\r\nHash: {parsed.Plan.StateHash}\r\n" +
                             "Cubie decomposition/history remain intentionally untrusted.";
        }
        finally
        {
            candidate?.Dispose();
        }
    }

    private async void OpenRecentFile_Click(object sender, RoutedEventArgs e)
    {
        if (RecentFilesBox.SelectedItem is string path)
            await LoadStateFileAsync(path);
    }

    private async Task LoadStateFileAsync(string path)
    {
        if (_isAnimating)
        {
            OutputBox.Text = "Finish or stop the current animation before loading.";
            return;
        }

        var result = await Task.Run(() => _stateFileService.Read(path));
        if (!result.Success || result.LoadPlan is null)
        {
            OutputBox.Text = $"Load failed [{result.ErrorCode}]: {result.Message}";
            return;
        }

        NativeRubikEngine? candidate = null;
        try
        {
            candidate = new NativeRubikEngine();
            if (!candidate.SetSize(result.LoadPlan.Document.Size) || !candidate.SetFacelets(result.LoadPlan.Facelets))
            {
                OutputBox.Text = $"Native load rejected the validated document: {candidate.GetLastInfo()}";
                return;
            }

            var previous = _engine;
            _engine = candidate;
            candidate = null;
            previous.Dispose();

            _currentStateFile = result.Path;
            _savedStateHash = result.LoadPlan.StateHash;
            _currentStateHash = result.LoadPlan.StateHash;
            _lastSolution = Array.Empty<NativeRubikEngine.RubikMoveDto>();
            _selectedAxis = -1;
            _selectedLayer = -1;
            AddRecentFile(result.Path);
            ResetCameraToDimension(result.LoadPlan.Document.Size);
            RefreshScene();
            OutputBox.Text = $"State loaded transactionally: {result.Path}\r\nHash: {result.LoadPlan.StateHash}\r\n" +
                             "Move history is intentionally untrusted after physical facelet import.";
        }
        catch (Exception exception)
        {
            OutputBox.Text = $"Native load failed without replacing the current cube: {exception.Message}";
        }
        finally
        {
            candidate?.Dispose();
        }
    }

    private async void ExportMovesFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Rubik moves",
            Filter = "Rubik moves (*.rubikmoves)|*.rubikmoves|Text (*.txt)|*.txt",
            DefaultExt = ".rubikmoves",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = "rubik-history.rubikmoves"
        };
        if (dialog.ShowDialog(this) != true)
            return;
        var history = _engine.GetHistory();
        var text = history.Length == 0 ? "# History is empty." : RubikNotation.FormatEngineMoves(history);
        await File.WriteAllTextAsync(dialog.FileName, text, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        OutputBox.Text = $"Exported {history.Length} moves to {dialog.FileName}";
    }

    private async void ImportMovesFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Rubik moves",
            Filter = "Rubik moves (*.rubikmoves)|*.rubikmoves|Text (*.txt)|*.txt",
            DefaultExt = ".rubikmoves",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true)
            return;
        var info = new FileInfo(dialog.FileName);
        if (info.Length > RubikStateSerializer.DefaultMaximumBytes)
        {
            OutputBox.Text = $"Move file exceeds {RubikStateSerializer.DefaultMaximumBytes} bytes.";
            return;
        }
        var text = await File.ReadAllTextAsync(dialog.FileName);
        try
        {
            var moves = RubikNotation.Parse(text, CurrentSize());
            OutputBox.Text = text;
            OutputBox.Text += $"\r\n\r\n# Imported and validated {moves.Length} moves. Use Apply or Play in the Notation tab.";
        }
        catch (Exception exception)
        {
            OutputBox.Text = $"Move import rejected: {exception.Message}";
        }
    }

    private void InitializeFileTracking()
    {
        if (TryBuildPortableDocument(out var document, out _))
        {
            _currentStateHash = RubikStateHasher.Calculate(document);
            _savedStateHash = _currentStateHash;
        }
    }

    private bool TryBuildPortableDocument(out RubikStateDocument document, out string error)
    {
        try
        {
            var state = _engine.GetState();
            var facelets = _engine.GetFacelets();
            document = RubikStateDocument.Create(state.Size, facelets, "RubikApp");
            error = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            document = null!;
            error = $"Portable state is unavailable: {exception.Message}. Legacy integer edits cannot invent sticker orientation.";
            return false;
        }
    }

    private void UpdateFileStatus()
    {
        var valid = TryBuildPortableDocument(out var document, out _);
        _currentStateHash = valid ? RubikStateHasher.Calculate(document) : null;
        var dirty = !valid || !string.Equals(_currentStateHash, _savedStateHash, StringComparison.Ordinal);
        CurrentFileText.Text = $"{(dirty ? "* " : string.Empty)}{(_currentStateFile is null ? "Untitled" : Path.GetFileName(_currentStateFile))}";
        CurrentFileText.ToolTip = _currentStateFile ?? "No file path. Recent paths are memory-only.";
        FileStateText.Text = valid
            ? $"{(dirty ? "Modified" : "Saved")} | valid | hash {_currentStateHash}"
            : "Modified | physical facelets/orientation unavailable | portable save disabled";
    }

    private void AddRecentFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        _recentStateFiles.RemoveAll(item => string.Equals(item, fullPath, StringComparison.OrdinalIgnoreCase));
        _recentStateFiles.Insert(0, fullPath);
        if (_recentStateFiles.Count > 6)
            _recentStateFiles.RemoveRange(6, _recentStateFiles.Count - 6);
        RecentFilesBox.Items.Clear();
        foreach (var item in _recentStateFiles) RecentFilesBox.Items.Add(item);
        RecentFilesBox.SelectedIndex = 0;
    }

    private void ExportNotation_Click(object sender, RoutedEventArgs e)
    {
        var history = _engine.GetHistory();
        OutputBox.Text = history.Length == 0
            ? "# История ходов пуста."
            : RubikNotation.FormatEngineMoves(history);
    }

    private void ApplyNotation_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParseNotation(out var moves))
        {
            return;
        }

        if (_engine.ApplyMoves(moves))
        {
            _lastSolution = Array.Empty<NativeRubikEngine.RubikMoveDto>();
            OutputBox.Text = $"{moves.Length} ходов применено.\r\n{RubikNotation.FormatEngineMoves(moves)}";
            RefreshScene();
        }
    }

    private async void PlayNotation_Click(object sender, RoutedEventArgs e)
    {
        if (TryParseNotation(out var moves))
        {
            await PlayMovesAsync(moves);
        }
    }

    private void StopPlayback_Click(object sender, RoutedEventArgs e)
    {
        _stopPlaybackRequested = true;
    }

    private void SurfaceMode_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            RefreshScene();
        }
    }

    private void ResetCamera_Click(object sender, RoutedEventArgs e)
    {
        ResetCameraToDimension(CurrentSize());
    }

    private async void SaveVisualEvidence_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var files = await SaveVisualEvidenceAsync();
            OutputBox.Text = $"Visual evidence saved ({files.Count} scenes):\r\n" + string.Join("\r\n", files);
        }
        catch (Exception exception)
        {
            OutputBox.Text = $"Visual evidence failed: {exception.Message}";
        }
    }

    private async void SaveVisualEvidenceOnStartup(object sender, RoutedEventArgs e)
    {
        Loaded -= SaveVisualEvidenceOnStartup;
        try
        {
            var files = await SaveVisualEvidenceAsync();
            Console.WriteLine($"Rubik visual evidence PASS: {files.Count} scenes.");
            Application.Current.Shutdown(0);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Rubik visual evidence FAIL: {exception}");
            Application.Current.Shutdown(1);
        }
    }

    internal async Task<IReadOnlyList<string>> SaveVisualEvidenceAsync()
    {
        if (_isAnimating)
        {
            throw new InvalidOperationException("Wait for the current animation to finish before capturing evidence.");
        }

        var originalState = _engine.GetState();
        if (originalState.ManualState != 0)
        {
            throw new InvalidOperationException("Evidence capture is unavailable for a manual state because cubie orientation cannot be restored safely.");
        }

        var originalHistory = _engine.GetHistory();
        var originalSurfaceOnly = SurfaceOnlyBox.IsChecked;
        var originalSelection = (_selectedAxis, _selectedLayer);
        var originalCamera = (_yaw, _pitch, _distance, _pan);
        var originalFacelets = _engine.GetFacelets();
        var outputDirectory = Path.Combine(FindRepositoryRoot(), ".tmp", "rubik-visual-evidence");
        Directory.CreateDirectory(outputDirectory);
        var files = new List<string>();
        var manifestScenes = new List<object>();

        try
        {
            SurfaceOnlyBox.IsChecked = true;
            await PrepareAndCaptureEvidenceSceneAsync(outputDirectory, files, manifestScenes, "solved-3x3", 3);
            await PrepareAndCaptureEvidenceSceneAsync(outputDirectory, files, manifestScenes, "turn-r-3x3", 3,
                () => _engine.RotateLayer(2, 2, 1));
            await PrepareAndCaptureEvidenceSceneAsync(outputDirectory, files, manifestScenes, "solved-11x11", 11);
            await PrepareAndCaptureEvidenceSceneAsync(outputDirectory, files, manifestScenes, "inner-slice-11x11", 11,
                () => _engine.RotateLayer(2, 1, 1));
            await PrepareAndCaptureEvidenceSceneAsync(outputDirectory, files, manifestScenes, "scramble-11x11", 11,
                () => _engine.Scramble(20260716, 12));

            var manifestPath = Path.Combine(outputDirectory, "manifest.json");
            var manifest = new
            {
                format = "rubik.visual-evidence",
                version = 1,
                generatedUtc = DateTimeOffset.UtcNow,
                scenes = manifestScenes
            };
            await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            _engine.SetSize(originalState.Size);
            if (originalHistory.Length > 0 && !_engine.ApplyMoves(originalHistory))
            {
                throw new InvalidOperationException("Could not restore the trusted pre-capture history.");
            }
            SurfaceOnlyBox.IsChecked = originalSurfaceOnly;
            (_selectedAxis, _selectedLayer) = originalSelection;
            (_yaw, _pitch, _distance, _pan) = originalCamera;
            UpdateCamera();
            RefreshScene();
            if (!_engine.GetFacelets().SequenceEqual(originalFacelets))
            {
                throw new InvalidOperationException("Evidence capture restoration did not reproduce the original facelets.");
            }
        }

        return files;
    }

    private async Task PrepareAndCaptureEvidenceSceneAsync(
        string outputDirectory,
        ICollection<string> files,
        ICollection<object> manifestScenes,
        string name,
        int size,
        Func<bool>? mutate = null)
    {
        if (!_engine.SetSize(size) || (mutate != null && !mutate()))
        {
            throw new InvalidOperationException($"Could not prepare visual evidence scene '{name}'.");
        }
        _selectedAxis = -1;
        _selectedLayer = -1;
        ResetCameraToDimension(size);
        RefreshScene();
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
        await Task.Delay(80);

        var filePath = Path.Combine(outputDirectory, $"{name}.png");
        var (width, height) = SaveVisualToPng(ViewportCaptureHost, filePath);
        files.Add(filePath);
        manifestScenes.Add(new
        {
            name,
            size,
            file = Path.GetFileName(filePath),
            width,
            height,
            stickers = _renderedStickerCount,
            cubies = _renderedCubieCount,
            invalidStickers = _invalidStickerCount,
            fallbackRenderer = _fallbackRendererActive
        });
    }

    private static (int Width, int Height) SaveVisualToPng(FrameworkElement visual, string filePath)
    {
        visual.UpdateLayout();
        var width = Math.Max(1, (int)Math.Ceiling(visual.ActualWidth));
        var height = Math.Max(1, (int)Math.Ceiling(visual.ActualHeight));
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        encoder.Save(stream);
        stream.Flush(flushToDisk: true);
        if (stream.Length == 0)
        {
            throw new IOException($"Rendered PNG is empty: {filePath}");
        }
        return (width, height);
    }

    private static string FindRepositoryRoot()
    {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(Path.GetFullPath(start));
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Chess.sln")))
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }
        }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Chess", "RubikApp");
    }

    private async void MeasureRenderPerformance_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var reportPath = await MeasureRenderPerformanceAsync();
            OutputBox.Text = $"Render performance report saved:\r\n{reportPath}";
        }
        catch (Exception exception)
        {
            OutputBox.Text = $"Render performance probe failed: {exception.Message}";
        }
    }

    private async void MeasureRenderPerformanceOnStartup(object sender, RoutedEventArgs e)
    {
        Loaded -= MeasureRenderPerformanceOnStartup;
        try
        {
            var reportPath = await MeasureRenderPerformanceAsync();
            Console.WriteLine($"Rubik render performance PASS: {reportPath}");
            Application.Current.Shutdown(0);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Rubik render performance FAIL: {exception}");
            Application.Current.Shutdown(1);
        }
    }

    internal async Task<string> MeasureRenderPerformanceAsync()
    {
        if (_isAnimating)
        {
            throw new InvalidOperationException("Wait for the current animation to finish before measuring rendering.");
        }

        var originalState = _engine.GetState();
        if (originalState.ManualState != 0)
        {
            throw new InvalidOperationException("Performance measurement is unavailable for a manual state that cannot restore cubie orientation.");
        }

        var originalHistory = _engine.GetHistory();
        var originalFacelets = _engine.GetFacelets();
        var originalSurfaceOnly = SurfaceOnlyBox.IsChecked;
        var originalSelection = (_selectedAxis, _selectedLayer);
        var originalCamera = (_yaw, _pitch, _distance, _pan);
        var originalSpeed = AnimationSpeedSlider.Value;
        var outputDirectory = Path.Combine(FindRepositoryRoot(), ".tmp", "rubik-render-performance");
        Directory.CreateDirectory(outputDirectory);
        var reportPath = Path.Combine(outputDirectory, "result.json");

        try
        {
            _engine.SetSize(11);
            _engine.Scramble(20260716, 12);
            _selectedAxis = -1;
            _selectedLayer = -1;
            SurfaceOnlyBox.IsChecked = true;
            ResetCameraToDimension(11);

            var firstRenderStopwatch = Stopwatch.StartNew();
            RefreshScene();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
            firstRenderStopwatch.Stop();
            var firstSurfaceBuildMs = _lastSceneBuildMilliseconds;
            var firstSurfaceRenderWallMs = firstRenderStopwatch.Elapsed.TotalMilliseconds;
            var surfaceCubies = _renderedCubieCount;
            var surfaceStickers = _renderedStickerCount;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var managedBefore = GC.GetTotalMemory(forceFullCollection: false);
            var privateBefore = Process.GetCurrentProcess().PrivateMemorySize64;
            var rebuildTimes = new List<double>();
            var rebuildAllocations = new List<long>();
            for (var iteration = 0; iteration < 8; iteration++)
            {
                RefreshScene();
                rebuildTimes.Add(_lastSceneBuildMilliseconds);
                rebuildAllocations.Add(_lastSceneAllocatedBytes);
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var managedAfter = GC.GetTotalMemory(forceFullCollection: false);
            var privateAfter = Process.GetCurrentProcess().PrivateMemorySize64;

            SurfaceOnlyBox.IsChecked = false;
            var fullRenderStopwatch = Stopwatch.StartNew();
            RefreshScene();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
            fullRenderStopwatch.Stop();
            var fullBuildMs = _lastSceneBuildMilliseconds;
            var fullRenderWallMs = fullRenderStopwatch.Elapsed.TotalMilliseconds;
            var fullCubies = _renderedCubieCount;
            var fullStickers = _renderedStickerCount;

            SurfaceOnlyBox.IsChecked = true;
            RefreshScene();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
            AnimationSpeedSlider.Value = 2;
            var renderedFrames = 0;
            EventHandler frameCounter = (_, _) => renderedFrames++;
            CompositionTarget.Rendering += frameCounter;
            var animationStopwatch = Stopwatch.StartNew();
            try
            {
                await AnimateAndCommitMoveAsync(new NativeRubikEngine.RubikMoveDto
                {
                    Axis = 2,
                    Layer = 10,
                    QuarterTurns = 1
                });
            }
            finally
            {
                animationStopwatch.Stop();
                CompositionTarget.Rendering -= frameCounter;
            }

            var report = new
            {
                format = "rubik.render-performance",
                version = 1,
                generatedUtc = DateTimeOffset.UtcNow,
                size = 11,
                runtime = new
                {
                    framework = Environment.Version.ToString(),
                    os = Environment.OSVersion.VersionString,
                    processorCount = Environment.ProcessorCount
                },
                resources = new
                {
                    sharedMeshes = SharedStickerMeshes.Length + 1,
                    cachedMaterials = SharedMaterialCache.Count
                },
                surface = new
                {
                    cubies = surfaceCubies,
                    stickers = surfaceStickers,
                    firstBuildMilliseconds = firstSurfaceBuildMs,
                    firstRenderWallMilliseconds = firstSurfaceRenderWallMs,
                    repeatedBuildMilliseconds = rebuildTimes,
                    repeatedAllocatedBytes = rebuildAllocations,
                    averageBuildMilliseconds = rebuildTimes.Average(),
                    averageAllocatedBytes = rebuildAllocations.Average(),
                    managedLiveBytesBefore = managedBefore,
                    managedLiveBytesAfter = managedAfter,
                    privateBytesBefore = privateBefore,
                    privateBytesAfter = privateAfter
                },
                fullCube = new
                {
                    cubies = fullCubies,
                    stickers = fullStickers,
                    buildMilliseconds = fullBuildMs,
                    renderWallMilliseconds = fullRenderWallMs
                },
                animation = new
                {
                    axis = 2,
                    layer = 10,
                    requestedMilliseconds = 260,
                    wallMilliseconds = animationStopwatch.Elapsed.TotalMilliseconds,
                    renderingCallbacks = renderedFrames,
                    actionCommittedOnce = true
                }
            };
            await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            AnimationSpeedSlider.Value = originalSpeed;
            _engine.SetSize(originalState.Size);
            if (originalHistory.Length > 0 && !_engine.ApplyMoves(originalHistory))
            {
                throw new InvalidOperationException("Could not restore trusted history after performance measurement.");
            }
            SurfaceOnlyBox.IsChecked = originalSurfaceOnly;
            (_selectedAxis, _selectedLayer) = originalSelection;
            (_yaw, _pitch, _distance, _pan) = originalCamera;
            UpdateCamera();
            RefreshScene();
            if (!_engine.GetFacelets().SequenceEqual(originalFacelets))
            {
                throw new InvalidOperationException("Performance measurement restoration did not reproduce original facelets.");
            }
        }

        return reportPath;
    }

    private int SelectedQuarterTurns()
    {
        if (QuarterBox.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out var value))
        {
            return value;
        }
        return 1;
    }

    private static int ReadInt(TextBox box, int fallback, int min, int max)
    {
        if (!int.TryParse(box.Text, out var value))
        {
            value = fallback;
        }
        return Math.Clamp(value, min, max);
    }

    private int CurrentSize()
    {
        return Math.Max(2, _engine.GetState().Size);
    }

    private void ResetCameraToDimension(int size)
    {
        _yaw = 42;
        _pitch = 27;
        _distance = Math.Clamp(size * 2.55, 8, 88);
        _pan = new Vector3D(0, 0, 0);
        UpdateCamera();
    }

    private static string FormatCells(int[] cells, int size)
    {
        var lines = new List<string>();
        for (var z = 0; z < size; z++)
        {
            lines.Add($"# layer {z + 1}");
            for (var y = 0; y < size; y++)
            {
                var row = new int[size];
                Array.Copy(cells, IndexOf(size, 0, y, z), row, 0, size);
                lines.Add(string.Join(' ', row));
            }
            lines.Add(string.Empty);
        }
        return string.Join(Environment.NewLine, lines);
    }

    private static int[] ParseCells(string text)
    {
        var values = new List<int>();
        foreach (var rawLine in text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
        {
            var line = rawLine;
            var commentStart = line.IndexOf('#');
            if (commentStart >= 0)
            {
                line = line[..commentStart];
            }
            foreach (var token in line.Split(new[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(token, out var value))
                {
                    values.Add(value);
                }
            }
        }
        return values.ToArray();
    }

    private bool TryParseNotation(out NativeRubikEngine.RubikMoveDto[] moves)
    {
        try
        {
            moves = RubikNotation.Parse(OutputBox.Text, CurrentSize());
            if (moves.Length == 0)
            {
                OutputBox.Text = "Нотация пуста. Пример: R U R' U' или 3Rw U2 x Z5x2.";
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            moves = Array.Empty<NativeRubikEngine.RubikMoveDto>();
            OutputBox.Text = $"Ошибка нотации: {ex.Message}";
            return false;
        }
    }

    private async Task PlayMovesAsync(IReadOnlyList<NativeRubikEngine.RubikMoveDto> moves)
    {
        if (_isAnimating)
        {
            return;
        }

        _stopPlaybackRequested = false;
        for (var i = 0; i < moves.Count; i++)
        {
            if (_stopPlaybackRequested)
            {
                OutputBox.Text = $"Проигрывание остановлено на ходе {i + 1}/{moves.Count}.";
                break;
            }

            OutputBox.Text = $"Проигрывание {i + 1}/{moves.Count}: {RubikNotation.FormatEngineMove(moves[i])}";
            await AnimateAndCommitMoveAsync(moves[i]);
            await Task.Delay(TimeSpan.FromMilliseconds(45));
        }
        UpdateStatus();
    }

    private async Task AnimateAndCommitMoveAsync(NativeRubikEngine.RubikMoveDto move)
    {
        if (_isAnimating)
        {
            return;
        }

        move.QuarterTurns = NormalizeTurns(move.QuarterTurns);
        var size = CurrentSize();
        if (move.Axis < 0 || move.Axis > 2 || move.Layer < 0 || move.Layer >= size || move.QuarterTurns == 0)
        {
            return;
        }

        _isAnimating = true;
        try
        {
            await AnimateLayerAsync(move);
            if (_engine.RotateLayer(move.Axis, move.Layer, move.QuarterTurns))
            {
                OutputBox.Text = _engine.GetLastInfo();
            }
            RefreshScene();
        }
        finally
        {
            _isAnimating = false;
        }
    }

    private Task AnimateLayerAsync(NativeRubikEngine.RubikMoveDto move)
    {
        var layerModels = _cubeVisuals
            .Where(c => (move.Axis == 0 && c.Z == move.Layer) || (move.Axis == 1 && c.Y == move.Layer) || (move.Axis == 2 && c.X == move.Layer))
            .ToArray();
        if (layerModels.Length == 0)
        {
            return Task.CompletedTask;
        }

        var originals = layerModels.Select(c => c.Model.Transform).ToArray();
        var spacing = SpacingForSize(CurrentSize());
        var half = (CurrentSize() - 1) * spacing * 0.5;
        var axis = move.Axis switch
        {
            2 => new Vector3D(1, 0, 0),
            1 => new Vector3D(0, 1, 0),
            _ => new Vector3D(0, 0, 1)
        };
        var center = move.Axis switch
        {
            2 => new Point3D(move.Layer * spacing - half, 0, 0),
            1 => new Point3D(0, move.Layer * spacing - half, 0),
            _ => new Point3D(0, 0, move.Layer * spacing - half)
        };

        var speed = Math.Clamp(AnimationSpeedSlider.Value, 0.25, 4.0);
        var duration = TimeSpan.FromMilliseconds(520 / speed);
        var targetAngle = NormalizeTurns(move.QuarterTurns) * 90.0;
        var stopwatch = Stopwatch.StartNew();
        var completion = new TaskCompletionSource();

        EventHandler handler = null!;
        handler = (_, _) =>
        {
            var progress = Math.Clamp(stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds, 0, 1);
            var eased = progress * progress * (3 - 2 * progress);
            var rotation = new RotateTransform3D(new AxisAngleRotation3D(axis, targetAngle * eased), center);
            for (var i = 0; i < layerModels.Length; i++)
            {
                layerModels[i].Model.Transform = Combine(originals[i], rotation);
            }

            if (progress >= 1)
            {
                CompositionTarget.Rendering -= handler;
                completion.SetResult();
            }
        };

        CompositionTarget.Rendering += handler;
        return completion.Task;
    }

    private static Transform3D Combine(Transform3D original, Transform3D animation)
    {
        if (original == Transform3D.Identity)
        {
            return animation;
        }

        var group = new Transform3DGroup();
        group.Children.Add(original);
        group.Children.Add(animation);
        return group;
    }

    private static int NormalizeTurns(int turns)
    {
        turns %= 4;
        return turns < 0 ? turns + 4 : turns;
    }

    private void RubikViewport_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_isAnimating)
        {
            return;
        }

        _dragging = true;
        _lastMouse = e.GetPosition(RubikViewport);
        _mouseDownPoint = _lastMouse;
        _mouseLayerCandidate = e.ChangedButton == MouseButton.Left && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
            ? TryPickCube(_lastMouse, out var candidate) ? candidate : null
            : null;
        if (_mouseLayerCandidate is MouseLayerCandidate layerCandidate)
        {
            SelectLayer(layerCandidate.Axis, layerCandidate.Layer);
        }
        RubikViewport.CaptureMouse();
    }

    private async void RubikViewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        var current = e.GetPosition(RubikViewport);
        var dx = current.X - _lastMouse.X;
        var dy = current.Y - _lastMouse.Y;
        var totalDx = current.X - _mouseDownPoint.X;
        var totalDy = current.Y - _mouseDownPoint.Y;
        _lastMouse = current;

        if (_mouseLayerCandidate is MouseLayerCandidate candidate &&
            e.LeftButton == MouseButtonState.Pressed &&
            Math.Sqrt(totalDx * totalDx + totalDy * totalDy) >= SystemParameters.MinimumHorizontalDragDistance * 1.4)
        {
            _dragging = false;
            RubikViewport.ReleaseMouseCapture();
            var turns = TurnsFromMouseDrag(candidate.Axis, totalDx, totalDy);
            await AnimateAndCommitMoveAsync(new NativeRubikEngine.RubikMoveDto
            {
                Axis = candidate.Axis,
                Layer = candidate.Layer,
                QuarterTurns = turns
            });
            _lastSolution = Array.Empty<NativeRubikEngine.RubikMoveDto>();
            return;
        }
        if (_mouseLayerCandidate != null)
        {
            return;
        }

        if (e.RightButton == MouseButtonState.Pressed || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            _pan.X -= dx * 0.018;
            _pan.Y += dy * 0.018;
        }
        else
        {
            _yaw += dx * 0.35;
            _pitch = Math.Clamp(_pitch - dy * 0.28, -82, 82);
        }
        UpdateCamera();
    }

    private void RubikViewport_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        _mouseLayerCandidate = null;
        RubikViewport.ReleaseMouseCapture();
    }

    private void RubikViewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _distance = Math.Clamp(_distance * (e.Delta > 0 ? 0.9 : 1.1), 5, 70);
        UpdateCamera();
    }

    private bool TryPickCube(Point point, out MouseLayerCandidate candidate)
    {
        MouseLayerCandidate? picked = null;
        VisualTreeHelper.HitTest(RubikViewport, null, result =>
        {
            if (result is RayMeshGeometry3DHitTestResult meshHit &&
                _cubeHitMap.TryGetValue(meshHit.ModelHit, out var visual))
            {
                var axis = DominantHitAxis(visual, meshHit.PointHit);
                var layer = axis switch
                {
                    2 => visual.X,
                    1 => visual.Y,
                    _ => visual.Z
                };
                picked = new MouseLayerCandidate(visual, axis, layer, meshHit.PointHit);
                return HitTestResultBehavior.Stop;
            }

            return HitTestResultBehavior.Continue;
        }, new PointHitTestParameters(point));

        candidate = picked ?? default;
        return picked != null;
    }

    private static int DominantHitAxis(CubeVisual visual, Point3D hit)
    {
        var dx = Math.Abs(hit.X - visual.Center.X);
        var dy = Math.Abs(hit.Y - visual.Center.Y);
        var dz = Math.Abs(hit.Z - visual.Center.Z);
        if (dx >= dy && dx >= dz)
        {
            return 2;
        }
        return dy >= dz ? 1 : 0;
    }

    private static int TurnsFromMouseDrag(int axis, double dx, double dy)
    {
        var horizontal = Math.Abs(dx) >= Math.Abs(dy);
        var positive = horizontal ? dx > 0 : dy < 0;
        if (axis == 1 && !horizontal)
        {
            positive = !positive;
        }
        return positive ? 1 : 3;
    }

    private void SelectLayer(int axis, int layer)
    {
        _selectedAxis = axis;
        _selectedLayer = layer;
        AxisBox.SelectedIndex = axis switch { 2 => 2, 1 => 1, _ => 0 };
        LayerBox.Text = (layer + 1).ToString(CultureInfo.InvariantCulture);
        SelectionText.Text = $"Выбран слой {AxisName(axis)}{layer + 1}. Потяните мышью по кубику, чтобы повернуть слой.";
        RefreshScene();
    }

    private void UpdateCamera()
    {
        var yaw = _yaw * Math.PI / 180.0;
        var pitch = _pitch * Math.PI / 180.0;
        var cp = Math.Cos(pitch);
        var position = new Point3D(
            _pan.X + Math.Sin(yaw) * cp * _distance,
            _pan.Y + Math.Sin(pitch) * _distance,
            _pan.Z + Math.Cos(yaw) * cp * _distance);
        var target = new Point3D(_pan.X, _pan.Y, _pan.Z);

        _camera.Position = position;
        _camera.LookDirection = target - position;
        _camera.UpDirection = new Vector3D(0, 1, 0);
    }

    private void AddAxesAndCoordinates(int size, double half, double spacing)
    {
        var originOffset = -half - 0.78;
        var length = size * spacing;
        _scene.Children.Add(CreateBox(new Point3D(0, originOffset, originOffset), length, 0.045, 0.045, Color.FromRgb(232, 82, 82), 0.9));
        _scene.Children.Add(CreateBox(new Point3D(originOffset, 0, originOffset), 0.045, length, 0.045, Color.FromRgb(72, 184, 104), 0.9));
        _scene.Children.Add(CreateBox(new Point3D(originOffset, originOffset, 0), 0.045, 0.045, length, Color.FromRgb(86, 142, 235), 0.9));

        _scene.Children.Add(CreateTextLabel("X", new Point3D(half + 0.9, originOffset, originOffset), 0.52, 0.34, Color.FromRgb(255, 130, 130)));
        _scene.Children.Add(CreateTextLabel("Y", new Point3D(originOffset, half + 0.9, originOffset), 0.52, 0.34, Color.FromRgb(130, 235, 158)));
        _scene.Children.Add(CreateTextLabel("Z", new Point3D(originOffset, originOffset, half + 0.9), 0.52, 0.34, Color.FromRgb(150, 184, 255)));

        var step = Math.Max(1, (int)Math.Ceiling(size / 10.0));
        for (var i = 0; i < size; i += step)
        {
            var p = i * spacing - half;
            var label = (i + 1).ToString(CultureInfo.InvariantCulture);
            _scene.Children.Add(CreateBox(new Point3D(p, originOffset, originOffset), 0.035, 0.18, 0.035, Color.FromRgb(232, 82, 82), 0.95));
            _scene.Children.Add(CreateBox(new Point3D(originOffset, p, originOffset), 0.18, 0.035, 0.035, Color.FromRgb(72, 184, 104), 0.95));
            _scene.Children.Add(CreateBox(new Point3D(originOffset, originOffset, p), 0.035, 0.035, 0.18, Color.FromRgb(86, 142, 235), 0.95));
            _scene.Children.Add(CreateTextLabel(label, new Point3D(p, originOffset - 0.32, originOffset), 0.36, 0.24, Colors.White));
            _scene.Children.Add(CreateTextLabel(label, new Point3D(originOffset - 0.34, p, originOffset), 0.36, 0.24, Colors.White));
        }

        if ((size - 1) % step != 0)
        {
            var p = half;
            var label = size.ToString(CultureInfo.InvariantCulture);
            _scene.Children.Add(CreateTextLabel(label, new Point3D(p, originOffset - 0.32, originOffset), 0.42, 0.24, Colors.White));
            _scene.Children.Add(CreateTextLabel(label, new Point3D(originOffset - 0.34, p, originOffset), 0.42, 0.24, Colors.White));
        }
    }

    private static GeometryModel3D CreateBasePlate(double half, double spacing, int size)
    {
        var extent = size * spacing + 0.6;
        return CreateBox(new Point3D(0, -half - 0.55, 0), extent, 0.05, extent, Color.FromRgb(25, 31, 40), 0.72);
    }

    private static double SpacingForSize(int size)
    {
        return size <= 4 ? 1.16 : size <= 8 ? 1.04 : Math.Max(0.68, 8.4 / size);
    }

    private static double CubeSizeForDimension(int size)
    {
        var spacing = SpacingForSize(size);
        return Math.Clamp(spacing * 0.82, 0.48, 0.92);
    }

    private IReadOnlyList<RubikCubieVisualInput> BuildVisualInputs(int size, IReadOnlyList<int> cells)
    {
        var result = new List<RubikCubieVisualInput>(size * size * size);
        for (var z = 0; z < size; z++)
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            RubikCubieOrientation? orientation = null;
            if (_engine.TryGetCubieOrientation(x, y, z, out var nativeOrientation))
            {
                orientation = new RubikCubieOrientation(
                    new RubikAxisVector(nativeOrientation.LocalXWorldX, nativeOrientation.LocalXWorldY, nativeOrientation.LocalXWorldZ),
                    new RubikAxisVector(nativeOrientation.LocalYWorldX, nativeOrientation.LocalYWorldY, nativeOrientation.LocalYWorldZ),
                    new RubikAxisVector(nativeOrientation.LocalZWorldX, nativeOrientation.LocalZWorldY, nativeOrientation.LocalZWorldZ));
            }
            result.Add(new RubikCubieVisualInput(
                new RubikCoordinate(x, y, z),
                cells[IndexOf(size, x, y, z)],
                _engine.GetCubieStickerMask(x, y, z),
                orientation));
        }
        return result;
    }

    private static Color ColorForFacelet(int colorId)
    {
        return colorId switch
        {
            1 => Color.FromRgb(244, 240, 222), // white / ivory
            2 => Color.FromRgb(211, 57, 66),   // red
            3 => Color.FromRgb(47, 166, 91),   // green
            4 => Color.FromRgb(244, 205, 54),  // yellow
            5 => Color.FromRgb(238, 119, 37),  // orange
            6 => Color.FromRgb(50, 112, 214),  // blue
            _ => Color.FromRgb(180, 184, 190)
        };
    }

    private static Model3DGroup CreateRubikCubie(
        Point3D center,
        double size,
        IReadOnlyList<StickerVisual> stickers,
        bool selected,
        double opacity)
    {
        var group = new Model3DGroup();
        var bodyColor = selected ? Color.FromRgb(72, 74, 62) : Color.FromRgb(30, 34, 41);
        var modelTransform = CreateCubieModelTransform(center, size);
        var bodyMaterial = CreateMaterial(bodyColor, opacity, 34);
        group.Children.Add(new GeometryModel3D(SharedCubieBodyMesh, bodyMaterial)
        {
            Material = bodyMaterial,
            Transform = modelTransform
        });
        foreach (var sticker in stickers)
        {
            var color = selected
                ? Blend(sticker.Color, Color.FromRgb(255, 244, 156), 0.18)
                : sticker.Color;
            var stickerMaterial = CreateMaterial(color, opacity, 24);
            group.Children.Add(new GeometryModel3D(SharedStickerMeshes[sticker.Face], stickerMaterial)
            {
                BackMaterial = stickerMaterial,
                Transform = modelTransform
            });
        }
        return group;
    }

    private static Transform3D CreateCubieModelTransform(Point3D center, double size)
    {
        var transform = new Transform3DGroup();
        transform.Children.Add(new ScaleTransform3D(size, size, size));
        transform.Children.Add(new TranslateTransform3D(center.X, center.Y, center.Z));
        transform.Freeze();
        return transform;
    }

    private static MeshGeometry3D CreateUnitCubieMesh()
    {
        var mesh = new MeshGeometry3D
        {
            Positions = new Point3DCollection
            {
                new(-0.5, -0.5, -0.5), new(0.5, -0.5, -0.5), new(0.5, 0.5, -0.5), new(-0.5, 0.5, -0.5),
                new(-0.5, -0.5, 0.5), new(0.5, -0.5, 0.5), new(0.5, 0.5, 0.5), new(-0.5, 0.5, 0.5)
            },
            TriangleIndices = new Int32Collection
            {
                4, 5, 6, 4, 6, 7,
                0, 2, 1, 0, 3, 2,
                0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5,
                3, 7, 6, 3, 6, 2,
                0, 1, 5, 0, 5, 4
            }
        };
        mesh.Freeze();
        return mesh;
    }

    private static MeshGeometry3D CreateUnitStickerMesh(int face)
    {
        const double radius = 0.37;
        const double offset = 0.512;
        Point3DCollection positions = face switch
        {
            0 => new() { new(-radius, offset, -radius), new(radius, offset, -radius), new(radius, offset, radius), new(-radius, offset, radius) },
            1 => new() { new(offset, -radius, radius), new(offset, -radius, -radius), new(offset, radius, -radius), new(offset, radius, radius) },
            2 => new() { new(-radius, -radius, offset), new(radius, -radius, offset), new(radius, radius, offset), new(-radius, radius, offset) },
            3 => new() { new(-radius, -offset, radius), new(radius, -offset, radius), new(radius, -offset, -radius), new(-radius, -offset, -radius) },
            4 => new() { new(-offset, -radius, -radius), new(-offset, -radius, radius), new(-offset, radius, radius), new(-offset, radius, -radius) },
            _ => new() { new(radius, -radius, -offset), new(-radius, -radius, -offset), new(-radius, radius, -offset), new(radius, radius, -offset) }
        };
        var mesh = new MeshGeometry3D
        {
            Positions = positions,
            TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 3 }
        };
        mesh.Freeze();
        return mesh;
    }

    private static GeometryModel3D CreateBox(Point3D center, double width, double height, double depth, Color color, double opacity)
    {
        var hx = width / 2.0;
        var hy = height / 2.0;
        var hz = depth / 2.0;
        var x0 = center.X - hx;
        var x1 = center.X + hx;
        var y0 = center.Y - hy;
        var y1 = center.Y + hy;
        var z0 = center.Z - hz;
        var z1 = center.Z + hz;

        var mesh = new MeshGeometry3D
        {
            Positions = new Point3DCollection
            {
                new(x0, y0, z0), new(x1, y0, z0), new(x1, y1, z0), new(x0, y1, z0),
                new(x0, y0, z1), new(x1, y0, z1), new(x1, y1, z1), new(x0, y1, z1)
            },
            TriangleIndices = new Int32Collection
            {
                4, 5, 6, 4, 6, 7,
                0, 2, 1, 0, 3, 2,
                0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5,
                3, 7, 6, 3, 6, 2,
                0, 1, 5, 0, 5, 4
            }
        };

        var material = CreateMaterial(color, opacity, 34);
        var model = new GeometryModel3D(mesh, material)
        {
            BackMaterial = material
        };
        return model;
    }

    private static Material CreateMaterial(Color color, double opacity, double specularPower)
    {
        var alpha = (byte)Math.Clamp(opacity * 255, 20, 255);
        var key = new MaterialCacheKey(alpha, color.R, color.G, color.B, (byte)Math.Clamp(specularPower, 0, 255));
        if (SharedMaterialCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var brush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
        brush.Freeze();
        var specularBrush = new SolidColorBrush(Color.FromArgb(105, 255, 255, 255));
        specularBrush.Freeze();
        var material = new MaterialGroup();
        material.Children.Add(new DiffuseMaterial(brush));
        material.Children.Add(new SpecularMaterial(specularBrush, specularPower));
        material.Freeze();
        SharedMaterialCache[key] = material;
        return material;
    }

    private static GeometryModel3D CreateTextLabel(string text, Point3D center, double width, double height, Color color)
    {
        var drawing = new DrawingGroup();
        using (var context = drawing.Open())
        {
            var brush = new SolidColorBrush(color);
            var formatted = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                28,
                brush,
                1.25);
            context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, 48, 32));
            context.DrawText(formatted, new Point(2, 0));
        }

        var labelBrush = new DrawingBrush(drawing)
        {
            Stretch = Stretch.Uniform
        };
        var material = new DiffuseMaterial(labelBrush);
        var x0 = center.X - width / 2;
        var x1 = center.X + width / 2;
        var y0 = center.Y - height / 2;
        var y1 = center.Y + height / 2;
        var z = center.Z;
        var mesh = new MeshGeometry3D
        {
            Positions = new Point3DCollection
            {
                new(x0, y0, z), new(x1, y0, z), new(x1, y1, z), new(x0, y1, z)
            },
            TextureCoordinates = new PointCollection
            {
                new(0, 1), new(1, 1), new(1, 0), new(0, 0)
            },
            TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 3 }
        };
        return new GeometryModel3D(mesh, material) { BackMaterial = material };
    }

    private static Color Blend(Color baseColor, Color overlay, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            (byte)(baseColor.R * (1 - amount) + overlay.R * amount),
            (byte)(baseColor.G * (1 - amount) + overlay.G * amount),
            (byte)(baseColor.B * (1 - amount) + overlay.B * amount));
    }

    private static int IndexOf(int size, int x, int y, int z)
    {
        return z * size * size + y * size + x;
    }

    private static string AxisName(int axis)
    {
        return axis switch
        {
            2 => "X",
            1 => "Y",
            _ => "Z"
        };
    }

    private sealed record CubeVisual(int X, int Y, int Z, Point3D Center, Model3DGroup Model);
    private readonly record struct StickerVisual(int Face, Color Color);
    private readonly record struct MaterialCacheKey(byte Alpha, byte Red, byte Green, byte Blue, byte SpecularPower);
    private readonly record struct MouseLayerCandidate(CubeVisual Visual, int Axis, int Layer, Point3D HitPoint);
}
