using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RubikApp;

public partial class MainWindow : Window
{
    private readonly NativeRubikEngine _engine;
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
    private double _yaw = 42;
    private double _pitch = 26;
    private double _distance = 18;
    private Vector3D _pan = new(0, 0, 0);

    public MainWindow()
    {
        InitializeComponent();
        _engine = new NativeRubikEngine();
        SetupViewport();
        RefreshScene();
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
        _scene.Children.Clear();
        _cubeVisuals.Clear();
        _cubeHitMap.Clear();
        _scene.Children.Add(new AmbientLight(Color.FromRgb(96, 100, 112)));
        _scene.Children.Add(new DirectionalLight(Color.FromRgb(238, 242, 248), new Vector3D(-0.7, -1.2, -0.9)));
        _scene.Children.Add(new DirectionalLight(Color.FromRgb(115, 155, 210), new Vector3D(0.8, 0.4, 1.1)));

        var state = _engine.GetState();
        var size = Math.Max(2, state.Size);
        var cells = _engine.GetCells();
        var surfaceOnly = SurfaceOnlyBox.IsChecked == true;
        var spacing = SpacingForSize(size);
        var cubeSize = CubeSizeForDimension(size);
        var half = (size - 1) * spacing * 0.5;

        _scene.Children.Add(CreateBasePlate(half, spacing, size));
        AddAxesAndCoordinates(size, half, spacing);

        for (var z = 0; z < size; z++)
        {
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var isSurface = x == 0 || x == size - 1 || y == 0 || y == size - 1 || z == 0 || z == size - 1;
                    if (surfaceOnly && !isSurface)
                    {
                        continue;
                    }

                    var cell = cells[IndexOf(size, x, y, z)];
                    var opacity = isSurface ? 0.96 : 0.18;
                    var selected = _selectedAxis >= 0 &&
                        ((_selectedAxis == 0 && z == _selectedLayer) ||
                         (_selectedAxis == 1 && y == _selectedLayer) ||
                         (_selectedAxis == 2 && x == _selectedLayer));
                    var color = selected ? Blend(ColorForCell(cell, size), Color.FromRgb(255, 244, 156), 0.32) : ColorForCell(cell, size);
                    var center = new Point3D(x * spacing - half, y * spacing - half, z * spacing - half);
                    var model = CreateCube(center, cubeSize, color, selected ? 1.0 : opacity);
                    _scene.Children.Add(model);
                    var visual = new CubeVisual(x, y, z, center, model);
                    _cubeVisuals.Add(visual);
                    _cubeHitMap[model] = visual;
                }
            }
        }

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var state = _engine.GetState();
        SizeBox.Text = state.Size.ToString(CultureInfo.InvariantCulture);
        StatusText.Text = $"{state.Size}x{state.Size}x{state.Size}, ячеек {state.CellCount}, история {state.HistoryCount}, " +
                          $"собран: {(state.IsSolved != 0 ? "да" : "нет")}, ручной режим: {(state.ManualState != 0 ? "да" : "нет")}";
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

    private static GeometryModel3D CreateCube(Point3D center, double size, Color color, double opacity)
    {
        return CreateBox(center, size, size, size, color, opacity);
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

        var brush = new SolidColorBrush(Color.FromArgb((byte)Math.Clamp(opacity * 255, 20, 255), color.R, color.G, color.B));
        brush.Freeze();
        var material = new MaterialGroup();
        material.Children.Add(new DiffuseMaterial(brush));
        material.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(95, 255, 255, 255)), 34));
        material.Freeze();
        var model = new GeometryModel3D(mesh, material)
        {
            BackMaterial = material
        };
        return model;
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

    private static Color ColorForCell(int value, int size)
    {
        var count = Math.Max(1, size * size * size);
        var source = Math.Abs(value) % count;
        var x = source % size;
        var y = (source / size) % size;
        var z = source / (size * size);

        if (x == 0) return Color.FromRgb(210, 58, 70);
        if (x == size - 1) return Color.FromRgb(245, 139, 48);
        if (y == 0) return Color.FromRgb(62, 126, 232);
        if (y == size - 1) return Color.FromRgb(70, 176, 96);
        if (z == 0) return Color.FromRgb(245, 214, 78);
        if (z == size - 1) return Color.FromRgb(238, 241, 246);

        return Color.FromRgb(
            (byte)(70 + source * 29 % 90),
            (byte)(76 + source * 47 % 100),
            (byte)(88 + source * 61 % 100));
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

    private sealed record CubeVisual(int X, int Y, int Z, Point3D Center, GeometryModel3D Model);
    private readonly record struct MouseLayerCandidate(CubeVisual Visual, int Axis, int Layer, Point3D HitPoint);
}
