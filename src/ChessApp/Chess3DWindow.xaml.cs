using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ChessApp;

public partial class Chess3DWindow : Window
{
    private readonly NativeChess3DEngine _engine = new();
    private readonly ObjModelLibrary _models = new();
    private readonly Chess3DNetworkEndpoint _network = new();
    private readonly Button[,] _cells = new Button[8, 8];
    private readonly Brush _light = new SolidColorBrush(Color.FromRgb(224, 226, 214));
    private readonly Brush _dark = new SolidColorBrush(Color.FromRgb(109, 126, 139));
    private readonly Brush _last = new SolidColorBrush(Color.FromRgb(186, 202, 68));
    private readonly Brush _selected = new SolidColorBrush(Color.FromRgb(246, 211, 101));
    private readonly Brush _target = new SolidColorBrush(Color.FromRgb(83, 174, 204));
    private readonly Brush _capture = new SolidColorBrush(Color.FromRgb(214, 92, 76));
    private readonly Dictionary<Model3D, Square3D> _hitSquares = new();
    private Square3D? _selectedSquare;
    private bool _dragging3D;
    private CameraDragMode _cameraDragMode = CameraDragMode.None;
    private Point _lastPoint;
    private Point _dragStartPoint;
    private double _yaw = -36;
    private double _pitch = 54;
    private double _distance = 13.5;
    private double _targetX;
    private double _targetY;
    private double _targetZ;
    private int _lastObjModels;
    private int _lastFallbackModels;
    private bool _applyingNetworkMessage;

    public Chess3DWindow()
    {
        InitializeComponent();
        _network.MessageReceived += Network_MessageReceived;
        _network.StatusChanged += status => Dispatcher.Invoke(() => NetworkStatusText.Text = $"{status}\n{_network.TopologyText}");
        _network.TopologyChanged += () => Dispatcher.Invoke(() => NetworkStatusText.Text = _network.TopologyText);
        _network.PeerConnected += () => Dispatcher.Invoke(() =>
        {
            if (_network.IsHost)
            {
                _ = BroadcastBoard3DAsync();
            }
        });
        LoadModelSets();
        BuildLayerChoices();
        BuildLayerGrid();
        BuildPalette();
        LoadRulesFromDefaultPath();
        RefreshAll();
    }

    protected override void OnClosed(EventArgs e)
    {
        _network.Dispose();
        _engine.Dispose();
        base.OnClosed(e);
    }

    private void BuildLayerChoices()
    {
        LayerBox.Items.Clear();
        var axis = SelectedAxis();
        for (var layer = 0; layer < 8; ++layer)
        {
            LayerBox.Items.Add(axis switch
            {
                SliceAxis.X => $"{(char)('a' + layer)}",
                SliceAxis.Y => $"R{layer + 1}",
                _ => $"L{layer + 1}"
            });
        }
        LayerBox.SelectedIndex = 0;
    }

    private void LoadModelSets()
    {
        ModelSetBox.Items.Clear();
        foreach (var (name, path) in ObjModelLibrary.DiscoverSets())
        {
            ModelSetBox.Items.Add(new ComboBoxItem
            {
                Content = name,
                Tag = path
            });
        }
        if (ModelSetBox.Items.Count == 0)
        {
            ModelSetBox.Items.Add(new ComboBoxItem { Content = "Procedural", Tag = null });
        }
        ModelSetBox.SelectedIndex = 0;
        _models.SelectedSetPath = (ModelSetBox.SelectedItem as ComboBoxItem)?.Tag as string;
    }

    private void BuildLayerGrid()
    {
        LayerGrid.Children.Clear();
        for (var y = 7; y >= 0; --y)
        {
            for (var x = 0; x < 8; ++x)
            {
                var button = new Button
                {
                    Style = (Style)FindResource("CubeCellStyle"),
                    Tag = new Square3D(x, y, 0)
                };
                button.Click += Cell_Click;
                button.MouseRightButtonDown += Cell_RightClick;
                _cells[x, y] = button;
                LayerGrid.Children.Add(button);
            }
        }
    }

    private void BuildPalette()
    {
        PaletteGrid.Children.Clear();
        foreach (var type in new[] { 0, 6, 5, 4, 3, 2, 1 })
        {
            var button = new Button
            {
                Content = type == 0 ? "Empty" : TypeName(type),
                Tag = type,
                Margin = new Thickness(0, 0, 8, 8)
            };
            button.Click += (_, _) => PieceBox.SelectedIndex = type switch
            {
                0 => 1,
                1 => 2,
                2 => 3,
                3 => 4,
                4 => 5,
                5 => 6,
                6 => 7,
                _ => 0
            };
            PaletteGrid.Children.Add(button);
        }
    }

    private void LoadRulesFromDefaultPath()
    {
        var path = ResolveAppPath(RulesPathBox.Text);
        if (File.Exists(path))
        {
            LoadRulesText(File.ReadAllText(path));
        }
    }

    private void LoadRulesText(string json)
    {
        if (json.Contains("\"rulesetId\"", StringComparison.Ordinal) &&
            json.Contains("\"goalProfile\"", StringComparison.Ordinal) &&
            _engine.LoadRuleProfileJson(json))
        {
            return;
        }
        _engine.LoadRulesJson(json);
    }

    private static string ResolveAppPath(string path)
    {
        if (Path.IsPathFullyQualified(path))
        {
            return path;
        }
        var output = Path.Combine(AppContext.BaseDirectory, path);
        if (File.Exists(output))
        {
            return output;
        }
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", path));
    }

    private void RefreshAll()
    {
        ApplyViewLayout();
        RefreshLayer();
        RefreshStatus();
        RefreshPreview3D();
    }

    private void RefreshLayer()
    {
        var board = _engine.GetBoard();
        var state = _engine.GetState();
        var selectedMoves = SelectedPieceMoves();
        for (var row = 0; row < 8; ++row)
        {
            for (var col = 0; col < 8; ++col)
            {
            var square = SquareForPlaneCell(col, row);
                var button = _cells[col, row];
                var piece = board[IndexOf(square.X, square.Y, square.Z)];
                button.Content = LabelForPiece(piece);
                button.Foreground = BrushForPiece(piece);
                button.Background = (square.X + square.Y + square.Z) % 2 == 0 ? _light : _dark;
                button.BorderBrush = Brushes.Black;
                button.Tag = square;
                if (state.LastFromX == square.X && state.LastFromY == square.Y && state.LastFromZ == square.Z ||
                    state.LastToX == square.X && state.LastToY == square.Y && state.LastToZ == square.Z)
                {
                    button.Background = _last;
                }
                var move = selectedMoves.FirstOrDefault(m => m.ToX == square.X && m.ToY == square.Y && m.ToZ == square.Z);
                if (MoveHintsEnabled() && move.Piece != 0)
                {
                    button.Background = move.Captured != 0 ? _capture : _target;
                }
                if (_selectedSquare == square)
                {
                    button.Background = _selected;
                }
            }
        }
    }

    private void RefreshStatus()
    {
        var state = _engine.GetState();
        var rules = _engine.GetRulesInfo();
        var selectedMoves = SelectedPieceMoves();
        var anchorCount = _engine.GetAnchorCount(state.SideToMove);
        var requiredAnchors = _engine.GetRequiredAnchorCount(state.SideToMove);
        var winner = _engine.GetWinnerSide();
        var gameOverText = _engine.IsGameOver() ? $", winner side {winner}" : string.Empty;
        var stackText = _selectedSquare is { } selected
            ? $", stack {(_engine.IsCoreStackEnabled() ? "on" : "off")} selected {_engine.GetCoreStackCount(selected.X, selected.Y, selected.Z)} projected {LabelForPiece(_engine.GetProjectedPiece(selected.X, selected.Y, selected.Z))}"
            : $", stack {(_engine.IsCoreStackEnabled() ? "on" : "off")}";
        HeaderStatus.Text = $"Side {state.SideToMove}, pieces {state.PieceCount}, moves {state.LegalMoveCount}";
        RulesText.Text = $"Board {rules.Width}x{rules.Height}x{rules.Depth}, sides {rules.ActiveSideCount}, profile {(rules.MovementProfile == 0 ? "setup-only" : "draft3d")}, max {rules.MaxPiecesPerSide}/side, view {SelectedAxis()} {(IsAllLayersView() ? "all" : "slice")}, grid {SelectedGridMode()}\nRuleset {_engine.GetCurrentRulesetId()}, goal {_engine.GetGoalProfileType()}, capture {_engine.GetCaptureProfileType()}, occupancy {_engine.GetOccupancyProfileType()}, fusion {_engine.GetFusionProfileType()}, layer {_engine.GetLayerTurnProfileType()}, anchors {anchorCount}/{requiredAnchors}{gameOverText}{stackText}";
        InfoText.Text = _engine.GetLastInfo();
        PositionText.Text = $"Models: {SelectedModelSetName()}, OBJ {_lastObjModels}, fallback {_lastFallbackModels}, hints {selectedMoves.Length}\n{_engine.GetPositionText()}";
    }

    private void RefreshPreview3D()
    {
        var group = new Model3DGroup();
        group.Children.Add(new AmbientLight(Color.FromRgb(92, 96, 104)));
        group.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-3, -5, -4)));
        _lastObjModels = 0;
        _lastFallbackModels = 0;
        _hitSquares.Clear();
        var board = _engine.GetBoard();
        var selectedMoves = SelectedPieceMoves();
        foreach (var square in VisibleBoardSquares(board))
        {
            group.Children.Add(CreateTileModel(square));
        }

        for (var index = 0; index < board.Length; ++index)
        {
            var piece = board[index];
            if (piece == 0)
            {
                continue;
            }
            var square = new Square3D(index & 7, (index >> 3) & 7, index >> 6);
            if (!IsAllLayersView() && CoordinateAlongAxis(square, SelectedAxis()) != SelectedLayer())
            {
                continue;
            }
            var model = CreatePieceModel(piece, square);
            _hitSquares[model] = square;
            group.Children.Add(model);
        }

        if (MoveHintsEnabled())
        {
            if (_selectedSquare is Square3D selected && SquareVisibleInCurrentView(selected))
            {
                group.Children.Add(CreateSelectionMarker(selected));
            }

            foreach (var move in selectedMoves)
            {
                var target = new Square3D(move.ToX, move.ToY, move.ToZ);
                if (!SquareVisibleInCurrentView(target))
                {
                    continue;
                }
                var model = CreateMoveMarker(target, move.Captured != 0);
                _hitSquares[model] = target;
                group.Children.Add(model);
            }
        }

        Preview3D.Children.Clear();
        Preview3D.Children.Add(new ModelVisual3D { Content = group });
        UpdateCamera();
    }

    private IEnumerable<Square3D> VisibleBoardSquares(int[] board)
    {
        for (var z = 0; z < 8; ++z)
        {
            for (var y = 0; y < 8; ++y)
            {
                for (var x = 0; x < 8; ++x)
                {
                    var square = new Square3D(x, y, z);
                    if (SquareVisibleInCurrentView(square) && GridModeAllowsSquare(square, board))
                    {
                        yield return square;
                    }
                }
            }
        }
    }

    private bool SquareVisibleInCurrentView(Square3D square)
    {
        return IsAllLayersView() || CoordinateAlongAxis(square, SelectedAxis()) == SelectedLayer();
    }

    private bool GridModeAllowsSquare(Square3D square, int[] board)
    {
        var coordinate = CoordinateAlongAxis(square, SelectedAxis());
        return SelectedGridMode() switch
        {
            BoardGridMode.Hidden => false,
            BoardGridMode.SelectedSlice => coordinate == SelectedLayer(),
            BoardGridMode.OuterShell => square.X is 0 or 7 || square.Y is 0 or 7 || square.Z is 0 or 7,
            BoardGridMode.TopBottom => coordinate is 0 or 7,
            BoardGridMode.Middle => coordinate is 3 or 4,
            BoardGridMode.Occupied => board[IndexOf(square.X, square.Y, square.Z)] != 0,
            _ => true
        };
    }

    private GeometryModel3D CreateTileModel(Square3D square)
    {
        var axis = SelectedAxis();
        var alpha = TileOpacity(square);
        var color = (square.X + square.Y + square.Z) % 2 == 0
            ? Color.FromArgb(alpha, 210, 214, 198)
            : Color.FromArgb(alpha, 82, 96, 110);
        var material = new DiffuseMaterial(new SolidColorBrush(color));

        if (axis == SliceAxis.Z)
        {
            var tileName = (square.X + square.Y + square.Z) % 2 == 0 ? "light_tile.obj" : "dark_tile.obj";
            var mesh = _models.LoadMesh(Path.Combine("Board", tileName));
            if (mesh != null)
            {
                _lastObjModels++;
                return new GeometryModel3D(mesh, material)
                {
                    Transform = new TranslateTransform3D(square.X - 3.5, square.Z - 3.5, square.Y - 3.5)
                };
            }
        }

        _lastFallbackModels++;
        var plate = axis switch
        {
            SliceAxis.X => CubeMesh(0.035, 0.92, 0.92),
            SliceAxis.Y => CubeMesh(0.92, 0.92, 0.035),
            _ => CubeMesh(0.92, 0.035, 0.92)
        };
        return new GeometryModel3D(plate, material)
        {
            Transform = new TranslateTransform3D(square.X - 3.5, square.Z - 3.5, square.Y - 3.5)
        };
    }

    private GeometryModel3D CreatePieceModel(int piece, Square3D square)
    {
        var side = piece / 10;
        var type = piece % 10;
        var mesh = _models.LoadMesh(Path.Combine("Pieces", ObjModelLibrary.ModelFileNameForClassicPiece(type, side % 2 == 1)));
        var color = ColorForSide(side);
        color.A = PieceOpacity(square);
        var material = new DiffuseMaterial(new SolidColorBrush(color));
        if (mesh != null)
        {
            _lastObjModels++;
            return new GeometryModel3D(mesh, material)
            {
                Transform = new TranslateTransform3D(square.X - 3.5, square.Z - 3.43, square.Y - 3.5)
            };
        }

        _lastFallbackModels++;
        return new GeometryModel3D(CubeMesh(0.42, 0.42, 0.42), material)
        {
            Transform = new TranslateTransform3D(square.X - 3.5, square.Z - 3.25, square.Y - 3.5)
        };
    }

    private GeometryModel3D CreateSelectionMarker(Square3D square)
    {
        var material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(185, 246, 211, 101)));
        return new GeometryModel3D(CubeMesh(0.78, 0.08, 0.78), material)
        {
            Transform = new TranslateTransform3D(square.X - 3.5, square.Z - 3.01, square.Y - 3.5)
        };
    }

    private GeometryModel3D CreateMoveMarker(Square3D square, bool capture)
    {
        var color = capture
            ? Color.FromArgb(210, 214, 92, 76)
            : Color.FromArgb(190, 79, 183, 214);
        var material = new DiffuseMaterial(new SolidColorBrush(color));
        return new GeometryModel3D(CubeMesh(capture ? 0.62 : 0.48, 0.1, capture ? 0.62 : 0.48), material)
        {
            Transform = new TranslateTransform3D(square.X - 3.5, square.Z - 2.96, square.Y - 3.5)
        };
    }

    private byte TileOpacity(Square3D square)
    {
        var percent = int.TryParse(OpacityBox.Text, out var value) ? Math.Clamp(value, 5, 85) : 28;
        if (!IsAllLayersView())
        {
            return 185;
        }

        var distance = Math.Abs(CoordinateAlongAxis(square, SelectedAxis()) - SelectedLayer());
        var near = Math.Clamp(percent + 18, 10, 90);
        var alpha = Math.Max(5, near - distance * LayerFadePenalty());
        return (byte)Math.Clamp(alpha * 255 / 100, 12, 230);
    }

    private byte PieceOpacity(Square3D square)
    {
        if (!IsAllLayersView())
        {
            return 255;
        }

        var percent = int.TryParse(OpacityBox.Text, out var value) ? Math.Clamp(value, 5, 85) : 28;
        var distance = Math.Abs(CoordinateAlongAxis(square, SelectedAxis()) - SelectedLayer());
        var alpha = Math.Max(18, Math.Clamp(percent + 45, 35, 100) - distance * (LayerFadePenalty() + 1));
        return (byte)Math.Clamp(alpha * 255 / 100, 32, 255);
    }

    private int LayerFadePenalty()
    {
        return _distance switch
        {
            < 12 => 11,
            < 18 => 8,
            _ => 5
        };
    }

    private Square3D SquareForPlaneCell(int col, int row)
    {
        var layer = SelectedLayer();
        return SelectedAxis() switch
        {
            SliceAxis.X => new Square3D(layer, col, 7 - row),
            SliceAxis.Y => new Square3D(col, layer, 7 - row),
            _ => new Square3D(col, 7 - row, layer)
        };
    }

    private static int CoordinateAlongAxis(Square3D square, SliceAxis axis)
    {
        return axis switch
        {
            SliceAxis.X => square.X,
            SliceAxis.Y => square.Y,
            _ => square.Z
        };
    }

    private void UpdateCamera()
    {
        var yaw = _yaw * Math.PI / 180.0;
        var pitch = _pitch * Math.PI / 180.0;
        var target = new Point3D(_targetX, _targetY, _targetZ);
        var x = target.X + Math.Cos(pitch) * Math.Sin(yaw) * _distance;
        var y = target.Y + Math.Sin(pitch) * _distance;
        var z = target.Z + Math.Cos(pitch) * Math.Cos(yaw) * _distance;
        Preview3D.Camera = new PerspectiveCamera
        {
            Position = new Point3D(x, y, z),
            LookDirection = new Vector3D(target.X - x, target.Y - y, target.Z - z),
            UpDirection = new Vector3D(0, 1, 0),
            FieldOfView = 48,
            NearPlaneDistance = 0.08,
            FarPlaneDistance = 100
        };
    }

    private void Cell_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Square3D square })
        {
            return;
        }

        if (_selectedSquare is Square3D from && from != square)
        {
            if (_engine.TryMakeMove(from.X, from.Y, from.Z, square.X, square.Y, square.Z, NativeChess3DEngine.Queen, out _))
            {
                _ = BroadcastMove3DAsync(from.X, from.Y, from.Z, square.X, square.Y, square.Z, NativeChess3DEngine.Queen);
                _selectedSquare = null;
                RefreshAll();
                return;
            }
        }

        var selectedType = SelectedPieceType();
        if (selectedType >= 0)
        {
            _engine.SetPiece(square.X, square.Y, square.Z, selectedType == 0 ? 0 : SelectedSide(), selectedType);
            _ = BroadcastBoard3DAsync();
            _selectedSquare = null;
            RefreshAll();
            return;
        }

        _selectedSquare = _engine.GetPiece(square.X, square.Y, square.Z) != 0 ? square : null;
        RefreshAll();
    }

    private void Cell_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is Button { Tag: Square3D square })
        {
            _engine.SetPiece(square.X, square.Y, square.Z, 0, 0);
            _ = BroadcastBoard3DAsync();
            _selectedSquare = null;
            RefreshAll();
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _engine.Reset();
        _ = BroadcastBoard3DAsync();
        _selectedSquare = null;
        RefreshAll();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _engine.Clear();
        _ = BroadcastBoard3DAsync();
        _selectedSquare = null;
        RefreshAll();
    }

    private void AiMove_Click(object sender, RoutedEventArgs e)
    {
        var depth = int.TryParse(DepthBox.Text, out var value) ? Math.Clamp(value, 1, 4) : 2;
        if (_engine.MakeBestMove(depth, out var move))
        {
            _ = BroadcastMove3DAsync(move.FromX, move.FromY, move.FromZ, move.ToX, move.ToY, move.ToZ, move.PromotionType);
        }
        _selectedSquare = null;
        RefreshAll();
    }

    private void LoadRules_Click(object sender, RoutedEventArgs e)
    {
        var path = ResolveAppPath(RulesPathBox.Text.Trim());
        if (!File.Exists(path))
        {
            MessageBox.Show(this, "Rules JSON not found.", "Cube Chess", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        LoadRulesText(File.ReadAllText(path));
        _ = BroadcastBoard3DAsync();
        _selectedSquare = null;
        RefreshAll();
    }

    private void RubikRotate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var turns))
        {
            return;
        }

        var axis = SelectedRubikAxis();
        var layer = int.TryParse(RubikLayerBox.Text, out var rawLayer) ? Math.Clamp(rawLayer, 1, 8) - 1 : 0;
        RubikLayerBox.Text = (layer + 1).ToString();
        if (_engine.RotateLayer(axis, layer, turns))
        {
            _selectedSquare = null;
            _ = BroadcastRotate3DAsync(axis, layer, turns);
            RefreshAll();
        }
    }

    private async void NetworkHost_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var localSeat = SelectedNetworkKind() == Chess3DNetworkPeerKind.Player ? SelectedNetworkSeat() : 0;
            if (localSeat == 0 && SelectedNetworkKind() == Chess3DNetworkPeerKind.Player)
            {
                localSeat = 1;
            }
            await _network.StartHostAsync(SelectedNetworkPort(), "cube-main", localSeat);
            await BroadcastBoard3DAsync();
        }
        catch (Exception ex)
        {
            NetworkStatusText.Text = $"3DNet: host failed, {ex.Message}";
        }
    }

    private async void NetworkConnect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _network.ConnectAsync(NetworkHostBox.Text.Trim(), SelectedNetworkPort(), SelectedNetworkKind(), SelectedNetworkSeat(), "cube-main");
        }
        catch (Exception ex)
        {
            NetworkStatusText.Text = $"3DNet: connect failed, {ex.Message}";
        }
    }

    private void NetworkStop_Click(object sender, RoutedEventArgs e)
    {
        _network.Stop();
    }

    public async Task ApplyStartupArgumentsAsync(string[] args)
    {
        if (args.Length == 0)
        {
            return;
        }

        var hostMode = HasArg(args, "--host");
        var connectHost = ValueAfter(args, "--connect");
        var port = int.TryParse(ValueAfter(args, "--port"), out var parsedPort) ? Math.Clamp(parsedPort, 1, 65535) : 5308;
        var seat = int.TryParse(ValueAfter(args, "--seat"), out var parsedSeat) ? Math.Clamp(parsedSeat, 0, 6) : 0;
        var kind = string.Equals(ValueAfter(args, "--kind"), "group", StringComparison.OrdinalIgnoreCase)
            ? Chess3DNetworkPeerKind.Group
            : Chess3DNetworkPeerKind.Player;

        NetworkPortBox.Text = port.ToString();
        NetworkSeatBox.SelectedIndex = seat;
        NetworkKindBox.SelectedIndex = kind == Chess3DNetworkPeerKind.Group ? 1 : 0;
        if (!string.IsNullOrWhiteSpace(connectHost))
        {
            NetworkHostBox.Text = connectHost;
        }

        try
        {
            if (hostMode)
            {
                await _network.StartHostAsync(port, "cube-main", kind == Chess3DNetworkPeerKind.Player ? Math.Max(1, seat) : 0);
                Title = $"Cube Chess 8x8x8 - host seat {_network.LocalSeat:0}";
                await BroadcastBoard3DAsync();
            }
            else if (!string.IsNullOrWhiteSpace(connectHost))
            {
                await _network.ConnectAsync(connectHost, port, kind, seat, "cube-main");
                Title = $"Cube Chess 8x8x8 - {kind} {seat:0}";
            }
        }
        catch (Exception ex)
        {
            NetworkStatusText.Text = $"3DNet: startup failed, {ex.Message}";
        }
    }

    private void LayerBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cells[0, 0] != null)
        {
            _selectedSquare = null;
            RefreshAll();
        }
    }

    private void AxisBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LayerBox == null)
        {
            return;
        }
        BuildLayerChoices();
        if (_cells[0, 0] != null)
        {
            _selectedSquare = null;
            RefreshAll();
        }
    }

    private void ViewControls_Changed(object sender, RoutedEventArgs e)
    {
        if (_cells[0, 0] != null)
        {
            RefreshAll();
        }
    }

    private void GridModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cells[0, 0] != null)
        {
            RefreshAll();
        }
    }

    private void ModelSetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _models.SelectedSetPath = (ModelSetBox.SelectedItem as ComboBoxItem)?.Tag as string;
        _models.ClearCache();
        if (_cells[0, 0] != null)
        {
            RefreshAll();
        }
    }

    private void Palette_Changed(object sender, SelectionChangedEventArgs e)
    {
        _selectedSquare = null;
    }

    private void Preview3D_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _dragging3D = true;
        _lastPoint = e.GetPosition(Preview3D);
        _dragStartPoint = _lastPoint;
        _cameraDragMode = e.ChangedButton == MouseButton.Right || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
            ? CameraDragMode.Pan
            : CameraDragMode.Orbit;
        PreviewHost.CaptureMouse();
    }

    private void Preview3D_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_dragging3D || (e.LeftButton != MouseButtonState.Pressed && e.RightButton != MouseButtonState.Pressed))
        {
            return;
        }
        var current = e.GetPosition(Preview3D);
        var dx = current.X - _lastPoint.X;
        var dy = current.Y - _lastPoint.Y;
        if (_cameraDragMode == CameraDragMode.Pan)
        {
            var yaw = _yaw * Math.PI / 180.0;
            var rightX = Math.Cos(yaw);
            var rightZ = -Math.Sin(yaw);
            var scale = Math.Max(0.015, _distance / 720.0);
            _targetX = Math.Clamp(_targetX - dx * rightX * scale, -6, 6);
            _targetZ = Math.Clamp(_targetZ - dx * rightZ * scale, -6, 6);
            _targetY = Math.Clamp(_targetY + dy * scale, -6, 6);
        }
        else
        {
            _yaw += dx * 0.35;
            _pitch = Math.Clamp(_pitch - dy * 0.25, -82, 82);
        }
        _lastPoint = current;
        UpdateCamera();
    }

    private void Preview3D_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var current = e.GetPosition(Preview3D);
        var isClick = Math.Abs(current.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance;
        _dragging3D = false;
        _cameraDragMode = CameraDragMode.None;
        PreviewHost.ReleaseMouseCapture();
        if (isClick && TryPickSquare(current, out var square))
        {
            HandlePickedSquare(square);
        }
    }

    private void Preview3D_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            _targetY = Math.Clamp(_targetY + e.Delta / 240.0, -6, 6);
        }
        else
        {
            _distance = Math.Clamp(_distance - e.Delta / 520.0, 1.6, 34);
        }
        if (IsAllLayersView())
        {
            RefreshPreview3D();
        }
        else
        {
            UpdateCamera();
        }
    }

    private void CameraLift_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Tag?.ToString(), out var direction))
        {
            _targetY = Math.Clamp(_targetY + direction * 0.75, -6, 6);
            UpdateCamera();
        }
    }

    private void CameraCenter_Click(object sender, RoutedEventArgs e)
    {
        _targetX = 0;
        _targetY = 0;
        _targetZ = 0;
        _distance = 13.5;
        _yaw = -36;
        _pitch = 54;
        RefreshPreview3D();
    }

    private int SelectedLayer()
    {
        return Math.Clamp(LayerBox.SelectedIndex, 0, 7);
    }

    private SliceAxis SelectedAxis()
    {
        return AxisBox?.SelectedIndex switch
        {
            2 => SliceAxis.X,
            1 => SliceAxis.Y,
            _ => SliceAxis.Z
        };
    }

    private bool IsAllLayersView()
    {
        return ViewModeBox?.SelectedIndex == 1;
    }

    private void ApplyViewLayout()
    {
        if (LayerView == null || PreviewHost == null)
        {
            return;
        }

        var allLayers = IsAllLayersView();
        var fullCubeView = allLayers && WideAllBox?.IsChecked == true;
        var showTools = SidebarBox?.IsChecked != false;

        ToolsPanel.Visibility = showTools ? Visibility.Visible : Visibility.Collapsed;
        LayerView.Visibility = fullCubeView ? Visibility.Collapsed : Visibility.Visible;

        ToolsColumn.MinWidth = showTools ? 280 : 0;
        ToolsColumn.Width = showTools ? new GridLength(320) : new GridLength(0);

        if (fullCubeView)
        {
            LayerColumn.MinWidth = 0;
            LayerColumn.Width = new GridLength(0);
            PreviewColumn.MinWidth = 320;
            PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
            Grid.SetColumn(PreviewHost, showTools ? 1 : 0);
            Grid.SetColumnSpan(PreviewHost, showTools ? 2 : 3);
        }
        else
        {
            LayerColumn.MinWidth = 260;
            PreviewColumn.MinWidth = 300;
            LayerColumn.Width = new GridLength(1.05, GridUnitType.Star);
            PreviewColumn.Width = new GridLength(1.15, GridUnitType.Star);
            Grid.SetColumn(PreviewHost, 2);
            Grid.SetColumnSpan(PreviewHost, 1);
        }
    }

    private Chess3DMoveDto[] SelectedPieceMoves()
    {
        if (_selectedSquare is not Square3D square || !MoveHintsEnabled())
        {
            return Array.Empty<Chess3DMoveDto>();
        }
        return _engine.GetPieceMoves(square.X, square.Y, square.Z);
    }

    private bool MoveHintsEnabled()
    {
        return MoveHintsBox?.IsChecked == true;
    }

    private BoardGridMode SelectedGridMode()
    {
        return GridModeBox?.SelectedIndex switch
        {
            1 => BoardGridMode.SelectedSlice,
            2 => BoardGridMode.OuterShell,
            3 => BoardGridMode.TopBottom,
            4 => BoardGridMode.Middle,
            5 => BoardGridMode.Occupied,
            6 => BoardGridMode.Hidden,
            _ => BoardGridMode.All
        };
    }

    private bool TryPickSquare(Point point, out Square3D square)
    {
        Square3D? picked = null;
        VisualTreeHelper.HitTest(Preview3D, null, result =>
        {
            if (result is RayMeshGeometry3DHitTestResult meshHit && _hitSquares.TryGetValue(meshHit.ModelHit, out var hitSquare))
            {
                picked = hitSquare;
                return HitTestResultBehavior.Stop;
            }
            return HitTestResultBehavior.Continue;
        }, new PointHitTestParameters(point));

        square = picked ?? default;
        return picked.HasValue;
    }

    private void HandlePickedSquare(Square3D square)
    {
        if (_selectedSquare is Square3D from && from != square)
        {
            if (_engine.TryMakeMove(from.X, from.Y, from.Z, square.X, square.Y, square.Z, NativeChess3DEngine.Queen, out _))
            {
                _ = BroadcastMove3DAsync(from.X, from.Y, from.Z, square.X, square.Y, square.Z, NativeChess3DEngine.Queen);
                _selectedSquare = null;
                RefreshAll();
                return;
            }
        }

        _selectedSquare = _engine.GetPiece(square.X, square.Y, square.Z) != 0 ? square : null;
        RefreshAll();
    }

    private void Network_MessageReceived(Chess3DNetworkMessage message)
    {
        Dispatcher.Invoke(() =>
        {
            _applyingNetworkMessage = true;
            try
            {
                switch (message.Type)
                {
                case "board3d":
                    if (message.Board.Length == 512)
                    {
                        _engine.SetBoard(message.Board, Math.Clamp(message.SideToMove, 1, 6));
                        _selectedSquare = null;
                        RefreshAll();
                    }
                    break;
                case "move3d":
                    _engine.TryMakeMove(message.FromX, message.FromY, message.FromZ, message.ToX, message.ToY, message.ToZ, message.Promotion, out _);
                    _selectedSquare = null;
                    RefreshAll();
                    break;
                case "rotate3d":
                    _engine.RotateLayer(message.Axis, message.Layer, message.QuarterTurns);
                    _selectedSquare = null;
                    RefreshAll();
                    break;
                }
            }
            finally
            {
                _applyingNetworkMessage = false;
            }
        });
    }

    private async Task BroadcastMove3DAsync(int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int promotion)
    {
        if (_applyingNetworkMessage)
        {
            return;
        }
        await _network.SendAsync(new Chess3DNetworkMessage
        {
            Type = "move3d",
            FromX = fromX,
            FromY = fromY,
            FromZ = fromZ,
            ToX = toX,
            ToY = toY,
            ToZ = toZ,
            Promotion = promotion
        });
    }

    private async Task BroadcastRotate3DAsync(int axis, int layer, int quarterTurns)
    {
        if (_applyingNetworkMessage)
        {
            return;
        }
        await _network.SendAsync(new Chess3DNetworkMessage
        {
            Type = "rotate3d",
            Axis = axis,
            Layer = layer,
            QuarterTurns = quarterTurns
        });
    }

    private async Task BroadcastBoard3DAsync()
    {
        if (_applyingNetworkMessage)
        {
            return;
        }
        await _network.SendAsync(new Chess3DNetworkMessage
        {
            Type = "board3d",
            Board = _engine.GetBoard(),
            SideToMove = _engine.GetState().SideToMove
        });
    }

    private string SelectedModelSetName()
    {
        return (ModelSetBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Procedural";
    }

    private int SelectedSide()
    {
        return Math.Clamp(SideBox.SelectedIndex + 1, 1, 6);
    }

    private int SelectedRubikAxis()
    {
        return RubikAxisBox?.SelectedIndex switch
        {
            2 => 2,
            1 => 1,
            _ => 0
        };
    }

    private int SelectedNetworkPort()
    {
        return int.TryParse(NetworkPortBox.Text, out var port) ? Math.Clamp(port, 1, 65535) : 5308;
    }

    private int SelectedNetworkSeat()
    {
        return Math.Clamp(NetworkSeatBox.SelectedIndex, 0, 6);
    }

    private Chess3DNetworkPeerKind SelectedNetworkKind()
    {
        return NetworkKindBox.SelectedIndex == 1 ? Chess3DNetworkPeerKind.Group : Chess3DNetworkPeerKind.Player;
    }

    private static bool HasArg(string[] args, string name)
    {
        return args.Any(arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string ValueAfter(string[] args, string name)
    {
        for (var i = 0; i + 1 < args.Length; ++i)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return string.Empty;
    }

    private int SelectedPieceType()
    {
        return PieceBox.SelectedIndex switch
        {
            0 => -1,
            1 => 0,
            2 => NativeChess3DEngine.Pawn,
            3 => NativeChess3DEngine.Knight,
            4 => NativeChess3DEngine.Bishop,
            5 => NativeChess3DEngine.Rook,
            6 => NativeChess3DEngine.Queen,
            7 => NativeChess3DEngine.King,
            _ => -1
        };
    }

    private static int IndexOf(int x, int y, int z)
    {
        return z * 64 + y * 8 + x;
    }

    private static string LabelForPiece(int piece)
    {
        if (piece == 0)
        {
            return string.Empty;
        }
        var side = piece / 10;
        var type = piece % 10;
        return $"{SideLetter(side)}{TypeLetter(type)}";
    }

    private static string SideLetter(int side)
    {
        return side switch
        {
            1 => "Wh",
            2 => "Bl",
            3 => "N",
            4 => "S",
            5 => "We",
            6 => "E",
            _ => "?"
        };
    }

    private static string TypeLetter(int type)
    {
        return type switch
        {
            1 => "P",
            2 => "N",
            3 => "B",
            4 => "R",
            5 => "Q",
            6 => "K",
            _ => "?"
        };
    }

    private static string TypeName(int type)
    {
        return type switch
        {
            1 => "Pawn",
            2 => "Knight",
            3 => "Bishop",
            4 => "Rook",
            5 => "Queen",
            6 => "King",
            _ => "Empty"
        };
    }

    private static SolidColorBrush BrushForPiece(int piece)
    {
        var side = piece / 10;
        return new SolidColorBrush(ColorForSide(side));
    }

    private static Color ColorForSide(int side)
    {
        return side switch
        {
            1 => Color.FromRgb(245, 244, 232),
            2 => Color.FromRgb(34, 38, 44),
            3 => Color.FromRgb(204, 70, 73),
            4 => Color.FromRgb(64, 130, 214),
            5 => Color.FromRgb(58, 160, 104),
            6 => Color.FromRgb(218, 176, 55),
            _ => Color.FromRgb(160, 160, 160)
        };
    }

    private static MeshGeometry3D CubeMesh(double width, double height, double depth)
    {
        var x = width / 2;
        var y = height / 2;
        var z = depth / 2;
        var mesh = new MeshGeometry3D();
        var p = new[]
        {
            new Point3D(-x, -y, -z), new Point3D(x, -y, -z), new Point3D(x, y, -z), new Point3D(-x, y, -z),
            new Point3D(-x, -y, z), new Point3D(x, -y, z), new Point3D(x, y, z), new Point3D(-x, y, z)
        };
        foreach (var point in p)
        {
            mesh.Positions.Add(point);
        }
        foreach (var i in new[]
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            2, 3, 7, 2, 7, 6,
            1, 2, 6, 1, 6, 5,
            0, 4, 7, 0, 7, 3
        })
        {
            mesh.TriangleIndices.Add(i);
        }
        mesh.Freeze();
        return mesh;
    }

    private readonly record struct Square3D(int X, int Y, int Z);

    private enum SliceAxis
    {
        Z,
        Y,
        X
    }

    private enum BoardGridMode
    {
        All,
        SelectedSlice,
        OuterShell,
        TopBottom,
        Middle,
        Occupied,
        Hidden
    }

    private enum CameraDragMode
    {
        None,
        Orbit,
        Pan
    }
}
