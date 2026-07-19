using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using ChessGameRecords;

namespace ChessApp;

public partial class MainWindow : Window
{
    private readonly NativeChessEngine _engine = new();
    private readonly ChessGameHistory _gameHistory = new(ChessGameHistory.StandardInitialFen);
    private readonly ChessNetworkEndpoint _network = new();
    private readonly Button[,] _squares = new Button[8, 8];
    private readonly Brush _lightSquare = new SolidColorBrush(Color.FromRgb(238, 238, 210));
    private readonly Brush _darkSquare = new SolidColorBrush(Color.FromRgb(118, 150, 86));
    private readonly Brush _selectedSquare = new SolidColorBrush(Color.FromRgb(246, 211, 101));
    private readonly Brush _targetSquare = new SolidColorBrush(Color.FromRgb(137, 184, 210));
    private readonly Brush _lastMoveSquare = new SolidColorBrush(Color.FromRgb(186, 202, 68));
    private readonly Brush _whitePiece = new SolidColorBrush(Color.FromRgb(250, 250, 242));
    private readonly Brush _blackPiece = new SolidColorBrush(Color.FromRgb(24, 28, 32));
    private readonly Dictionary<int, ImageSource> _classicImages = new();
    private readonly Dictionary<int, ImageSource> _transparentImages = new();

    private ChessMoveDto[] _legalMoves = Array.Empty<ChessMoveDto>();
    private int[] _setupBoard = new int[64];
    private Square? _selected;
    private bool _busy;
    private PieceTheme _pieceTheme = PieceTheme.Transparent;
    private Point _dragStart;
    private bool _is3DDragging;
    private Point _dragStart3DPoint;
    private Point _last3DPoint;
    private double _orbitYaw = -36;
    private double _orbitPitch = 58;
    private double _orbitDistance = 10.5;
    private string? _selectedModelSetPath;
    private readonly ObjModelLibrary _models = new();
    private readonly Dictionary<string, MeshGeometry3D?> _meshCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Model3D, Square> _hitSquares3D = new();
    private int _lastLoaded3DModels;
    private int _lastMissing3DModels;
    private bool _applyingRemoteMessage;

    public MainWindow()
    {
        InitializeComponent();
        LoadPieceImages();
        SyncSetupBoardFromEngine();
        BuildBoard();
        BuildPalettes();
        LoadModelSets();
        _network.MessageReceived += Network_MessageReceived;
        _network.StatusChanged += status => Dispatcher.Invoke(() => NetworkStatusText.Text = status);
        _network.PeerConnected += () => Dispatcher.Invoke(() => _ = SendFenOverNetworkAsync());
        RefreshCoordinates();
        ApplyDrawRulesFromUi();
        ApplyTablebasePathFromUi();
        RefreshAll();
    }

    protected override void OnClosed(EventArgs e)
    {
        _network.Dispose();
        _engine.Dispose();
        base.OnClosed(e);
    }

    private void BuildBoard()
    {
        BoardGrid.Children.Clear();

        var ranks = IsBlackView()
            ? Enumerable.Range(0, 8)
            : Enumerable.Range(0, 8).Reverse();
        var files = IsBlackView()
            ? Enumerable.Range(0, 8).Reverse()
            : Enumerable.Range(0, 8);

        foreach (var displayRank in ranks)
        {
            foreach (var file in files)
            {
                var square = new Square(file, displayRank);
                var button = new Button
                {
                    Style = (Style)FindResource("BoardButtonStyle"),
                    Tag = square,
                    AllowDrop = true
                };
                button.Click += BoardSquare_Click;
                button.PreviewMouseLeftButtonDown += BoardSquare_PreviewMouseLeftButtonDown;
                button.PreviewMouseMove += BoardSquare_PreviewMouseMove;
                button.MouseRightButtonDown += BoardSquare_MouseRightButtonDown;
                button.DragOver += BoardSquare_DragOver;
                button.Drop += BoardSquare_Drop;
                _squares[file, displayRank] = button;
                BoardGrid.Children.Add(button);
            }
        }
    }

    private void BuildPalettes()
    {
        if (WhitePalette == null || BlackPalette == null)
        {
            return;
        }

        WhitePalette.Children.Clear();
        BlackPalette.Children.Clear();

        foreach (var piece in new[] { 0, NativeChessEngine.King, NativeChessEngine.Queen, NativeChessEngine.Rook, NativeChessEngine.Bishop, NativeChessEngine.Knight, NativeChessEngine.Pawn })
        {
            WhitePalette.Children.Add(CreatePaletteButton(piece));
        }

        foreach (var piece in new[] { 0, -NativeChessEngine.King, -NativeChessEngine.Queen, -NativeChessEngine.Rook, -NativeChessEngine.Bishop, -NativeChessEngine.Knight, -NativeChessEngine.Pawn })
        {
            BlackPalette.Children.Add(CreatePaletteButton(piece));
        }
    }

    private Button CreatePaletteButton(int piece)
    {
        var button = new Button
        {
            Style = (Style)FindResource("PaletteButtonStyle"),
            Tag = new SetupDragData(piece, null),
            Content = ContentForPiece(piece),
            ToolTip = piece == 0 ? "Пусто" : PieceName(piece),
            AllowDrop = true
        };
        button.PreviewMouseLeftButtonDown += PaletteButton_PreviewMouseLeftButtonDown;
        button.PreviewMouseMove += PaletteButton_PreviewMouseMove;
        button.DragOver += PaletteButton_DragOver;
        button.Drop += PaletteButton_Drop;
        return button;
    }

    private void RefreshCoordinates()
    {
        if (TopFiles == null)
        {
            return;
        }

        TopFiles.Children.Clear();
        BottomFiles.Children.Clear();
        LeftRanks.Children.Clear();
        RightRanks.Children.Clear();

        var files = IsBlackView()
            ? Enumerable.Range(0, 8).Reverse().ToArray()
            : Enumerable.Range(0, 8).ToArray();
        var ranks = IsBlackView()
            ? Enumerable.Range(0, 8).ToArray()
            : Enumerable.Range(0, 8).Reverse().ToArray();

        foreach (var file in files)
        {
            TopFiles.Children.Add(CoordinateLabel(((char)('a' + file)).ToString()));
            BottomFiles.Children.Add(CoordinateLabel(((char)('a' + file)).ToString()));
        }

        foreach (var rank in ranks)
        {
            LeftRanks.Children.Add(CoordinateLabel((rank + 1).ToString()));
            RightRanks.Children.Add(CoordinateLabel((rank + 1).ToString()));
        }
    }

    private static TextBlock CoordinateLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.FromRgb(174, 183, 194)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold
        };
    }

    private void RefreshAll()
    {
        _legalMoves = _engine.GetLegalMoves();
        RefreshCoordinates();
        RefreshBoard();
        Refresh3DScene();
        RefreshStatus();
        RefreshMoveHistory();
    }

    private void RefreshBoard()
    {
        var setupMode = IsSetupMode();
        var board = setupMode ? _setupBoard : _engine.GetBoard();
        var state = _engine.GetState();
        var selectedTargets = _selected is null
            ? Array.Empty<ChessMoveDto>()
            : setupMode
                ? Array.Empty<ChessMoveDto>()
            : _legalMoves.Where(m => m.FromFile == _selected.Value.File && m.FromRank == _selected.Value.Rank).ToArray();

        for (var rank = 0; rank < 8; ++rank)
        {
            for (var file = 0; file < 8; ++file)
            {
                var piece = board[rank * 8 + file];
                var button = _squares[file, rank];
                var square = new Square(file, rank);

                button.Content = ContentForPiece(piece);
                button.Foreground = piece > 0 ? _whitePiece : _blackPiece;
                button.Background = BaseBrush(file, rank);

                if (!setupMode && (state.LastFromFile == file && state.LastFromRank == rank ||
                    state.LastToFile == file && state.LastToRank == rank))
                {
                    button.Background = _lastMoveSquare;
                }

                if (_selected == square)
                {
                    button.Background = _selectedSquare;
                }
                else if (selectedTargets.Any(m => m.ToFile == file && m.ToRank == rank))
                {
                    button.Background = _targetSquare;
                }
            }
        }
    }

    private void RefreshStatus()
    {
        var state = _engine.GetState();
        var board = IsSetupMode() ? _setupBoard : _engine.GetBoard();
        StatusText.Text = IsSetupMode()
            ? "Расстановка позиции."
            : FormatStatus(state);
        var stats = _engine.GetLastSearchStats();
        var searchFinish = stats.StoppedByTime != 0
            ? "остановлен таймаутом"
            : stats.ReachedRequestedDepth != 0
                ? "полная глубина"
                : "остановлен";
        var searchLine = stats.RequestedDepth > 0
            ? $"Поиск: глубина {stats.CompletedDepth}/{stats.RequestedDepth}, узлов {stats.Nodes:N0}, время {stats.ElapsedMs} мс, {searchFinish}"
            : "Поиск еще не запускался.";
        var gpuStats = NativeGpuBackend.GetKernelStats();
        var gpuLine = GpuEvalBox.IsChecked == true
            ? $"GPU: {(NativeGpuBackend.IsAvailable() ? "Direct3D compute" : "CPU fallback")} v{gpuStats.EvaluatorVersion} eval {NativeGpuBackend.EvaluateBoard(board, state.SideToMove)} cp, batch {gpuStats.LastBoardCount}, gpu {gpuStats.TotalGpuBatches}, cpu {gpuStats.TotalCpuFallbackBatches}"
            : NativeGpuBackend.GetInfo();
        var tablebase = _engine.GetTablebaseInfo();
        var tbLine = $"TB: built-in {(tablebase.BuiltInEndgameTables != 0 ? "on" : "off")}, Syzygy WDL {tablebase.SyzygyWdlFiles}, DTZ {tablebase.SyzygyDtzFiles}, max {tablebase.MaxPieces}";
        var modelsLine = Mode3DBox.IsChecked == true
            ? $"3D: set {ModelSetBox.Text}, loaded {_lastLoaded3DModels}, fallback {_lastMissing3DModels}, {_models.LastDiagnostics}"
            : "3D: off";
        SearchText.Text = $"{_engine.GetLastSearchInfo()}\n{searchLine}\n{gpuLine}\n{tbLine}\n{modelsLine}";
        FenBox.Text = IsSetupMode() ? BuildSetupFen() : _engine.GetFen();
        AiMoveButton.IsEnabled = !_busy && !IsSetupMode() &&
            (state.Status == NativeChessEngine.StatusPlaying ||
             state.Status == NativeChessEngine.StatusRepetitionClaim ||
             state.Status == NativeChessEngine.StatusFiftyMoveClaim);
        BusyText.Text = _busy ? "ИИ думает..." : string.Empty;
        WhitePalette.Visibility = IsSetupMode() ? Visibility.Visible : Visibility.Collapsed;
        BlackPalette.Visibility = IsSetupMode() ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void AiMove_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
        {
            return;
        }

        var before = _engine.GetState();
        var preMoveFen = _engine.GetFen();
        var options = BuildSearchOptions();
        if (options.Depth > 8 && options.TimeLimitMs <= 0)
        {
            var answer = MessageBox.Show(this,
                "Глубина выше 8 может считаться очень долго. Лучше включить лимит времени или авто-глубину. Запустить поиск без лимита?",
                "Chess Advisor",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information);
            if (answer != MessageBoxResult.OK)
            {
                return;
            }
        }

        _busy = true;
        _selected = null;
        RefreshStatus();

        try
        {
            var result = await Task.Run(() => _engine.MakeBestMove(options, out var move) ? move : (ChessMoveDto?)null);
            if (result is ChessMoveDto move)
            {
                if (CommitMoveToHistory(preMoveFen, before, move))
                {
                    await SendMoveOverNetworkAsync(move);
                }
            }
        }
        finally
        {
            _busy = false;
            RefreshAll();
        }
    }

    private void NewGame_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
        {
            return;
        }

        _engine.Reset();
        _selected = null;
        SetupModeBox.IsChecked = false;
        SyncSetupBoardFromEngine();
        ResetGameHistory();
        RefreshAll();
        _ = SendFenOverNetworkAsync();
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || !_engine.Undo())
        {
            return;
        }

        _selected = null;
        if (!_gameHistory.TryUndo(_engine.GetFen(), out _, out var historyError))
        {
            _gameHistory.Reset(_engine.GetFen());
            MessageBox.Show(this, historyError, "Chess history", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        RefreshAll();
        _ = SendFenOverNetworkAsync();
    }

    private void LoadFen_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
        {
            return;
        }

        if (!_engine.SetFen(FenBox.Text.Trim()))
        {
            MessageBox.Show(this, "FEN не загрузился: проверьте строку позиции.", "Chess Advisor", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _selected = null;
        SetupModeBox.IsChecked = false;
        SyncSetupBoardFromEngine();
        ResetGameHistory();
        RefreshAll();
        _ = SendFenOverNetworkAsync();
    }

    private void CopyFen_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(_engine.GetFen());
    }

    private async void NetworkHost_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(NetworkPortBox.Text, out var port))
        {
            return;
        }

        try
        {
            await _network.StartHostAsync(Math.Clamp(port, 1, 65535));
        }
        catch (Exception ex)
        {
            NetworkStatusText.Text = $"Endpoint: host failed, {ex.Message}";
        }
    }

    private async void NetworkConnect_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(NetworkPortBox.Text, out var port))
        {
            return;
        }

        try
        {
            await _network.ConnectAsync(NetworkHostBox.Text.Trim(), Math.Clamp(port, 1, 65535));
            await SendFenOverNetworkAsync();
        }
        catch (Exception ex)
        {
            NetworkStatusText.Text = $"Endpoint: connect failed, {ex.Message}";
        }
    }

    private void NetworkStop_Click(object sender, RoutedEventArgs e)
    {
        _network.Stop();
    }

    private Task SendFenOverNetworkAsync()
    {
        if (_applyingRemoteMessage)
        {
            return Task.CompletedTask;
        }
        return _network.SendAsync(new ChessNetworkMessage
        {
            Type = "fen",
            Fen = _engine.GetFen()
        });
    }

    private Task SendMoveOverNetworkAsync(ChessMoveDto move)
    {
        if (_applyingRemoteMessage)
        {
            return Task.CompletedTask;
        }
        return _network.SendAsync(new ChessNetworkMessage
        {
            Type = "move",
            FromFile = move.FromFile,
            FromRank = move.FromRank,
            ToFile = move.ToFile,
            ToRank = move.ToRank,
            Promotion = move.Promotion
        });
    }

    private void Network_MessageReceived(ChessNetworkMessage message)
    {
        Dispatcher.Invoke(() =>
        {
            if (_busy)
            {
                NetworkStatusText.Text = "Endpoint: move ignored while AI is thinking";
                return;
            }

            _applyingRemoteMessage = true;
            try
            {
                if (message.Type.Equals("fen", StringComparison.OrdinalIgnoreCase))
                {
                    if (_engine.SetFen(message.Fen))
                    {
                        SetupModeBox.IsChecked = false;
                        SyncSetupBoardFromEngine();
                        ResetGameHistory();
                        RefreshAll();
                    }
                }
                else if (message.Type.Equals("move", StringComparison.OrdinalIgnoreCase))
                {
                    var before = _engine.GetState();
                    var preMoveFen = _engine.GetFen();
                    if (_engine.TryMakeMove(message.FromFile, message.FromRank, message.ToFile, message.ToRank, message.Promotion, out var played))
                    {
                        CommitMoveToHistory(preMoveFen, before, played);
                        _selected = null;
                        RefreshAll();
                    }
                }
            }
            finally
            {
                _applyingRemoteMessage = false;
            }
        });
    }

    private void BoardSquare_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || sender is not Button button || button.Tag is not Square square)
        {
            return;
        }

        HandleBoardSquareInput(square);
    }

    private void HandleBoardSquareInput(Square square)
    {
        if (_busy)
        {
            return;
        }

        if (IsSetupMode())
        {
            return;
        }

        if (_selected is Square from)
        {
            var before = _engine.GetState();
            var preMoveFen = _engine.GetFen();
            var promotion = SelectedPromotion();
            var candidates = _legalMoves
                .Where(m => m.FromFile == from.File && m.FromRank == from.Rank && m.ToFile == square.File && m.ToRank == square.Rank)
                .ToArray();

            var chosen = candidates.FirstOrDefault(m => m.Promotion == 0 || m.Promotion == promotion);
            if (candidates.Length > 0 && _engine.TryMakeMove(from.File, from.Rank, square.File, square.Rank, chosen.Promotion, out var played))
            {
                if (!CommitMoveToHistory(preMoveFen, before, played))
                {
                    _selected = null;
                    RefreshAll();
                    return;
                }
                _selected = null;
                RefreshAll();
                _ = SendMoveOverNetworkAsync(played);
                return;
            }
        }

        var outgoing = _legalMoves.Where(m => m.FromFile == square.File && m.FromRank == square.Rank).ToArray();
        _selected = outgoing.Length > 0 ? square : null;
        RefreshBoard();
        Refresh3DScene();
    }

    private void BoardSquare_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
    }

    private void BoardSquare_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!IsSetupMode() || sender is not Button button || button.Tag is not Square square ||
            e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(this);
        if (Math.Abs(current.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var piece = _setupBoard[square.Rank * 8 + square.File];
        if (piece == 0)
        {
            return;
        }

        DragDrop.DoDragDrop(button, new DataObject(typeof(SetupDragData), new SetupDragData(piece, square)), DragDropEffects.Move);
    }

    private void BoardSquare_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!IsSetupMode() || sender is not Button button || button.Tag is not Square square)
        {
            return;
        }

        _setupBoard[square.Rank * 8 + square.File] = 0;
        RefreshBoard();
        Refresh3DScene();
        RefreshStatus();
    }

    private void BoardSquare_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = IsSetupMode() && e.Data.GetDataPresent(typeof(SetupDragData))
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void BoardSquare_Drop(object sender, DragEventArgs e)
    {
        if (!IsSetupMode() || sender is not Button button || button.Tag is not Square target ||
            e.Data.GetData(typeof(SetupDragData)) is not SetupDragData data)
        {
            return;
        }

        if (data.From is Square from && from != target)
        {
            _setupBoard[from.Rank * 8 + from.File] = 0;
        }
        _setupBoard[target.Rank * 8 + target.File] = data.Piece;
        RefreshBoard();
        Refresh3DScene();
        RefreshStatus();
    }

    private void PaletteButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
    }

    private void PaletteButton_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!IsSetupMode() || sender is not Button button || button.Tag is not SetupDragData data ||
            e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(this);
        if (Math.Abs(current.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        DragDrop.DoDragDrop(button, new DataObject(typeof(SetupDragData), data), DragDropEffects.Copy);
    }

    private void PaletteButton_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = IsSetupMode() && e.Data.GetDataPresent(typeof(SetupDragData))
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void PaletteButton_Drop(object sender, DragEventArgs e)
    {
        if (!IsSetupMode() || e.Data.GetData(typeof(SetupDragData)) is not SetupDragData data)
        {
            return;
        }

        if (data.From is Square from)
        {
            _setupBoard[from.Rank * 8 + from.File] = 0;
            RefreshBoard();
            Refresh3DScene();
            RefreshStatus();
        }
    }

    private int SelectedDepth()
    {
        if (int.TryParse(DepthBox.Text, out var depth))
        {
            return Math.Max(1, depth);
        }
        return 5;
    }

    private int SelectedPromotion()
    {
        return PromotionBox.SelectedIndex switch
        {
            1 => NativeChessEngine.Rook,
            2 => NativeChessEngine.Bishop,
            3 => NativeChessEngine.Knight,
            _ => NativeChessEngine.Queen
        };
    }

    private ChessSearchOptionsDto BuildSearchOptions()
    {
        return new ChessSearchOptionsDto
        {
            Depth = SelectedDepth(),
            TimeLimitMs = int.TryParse(TimeLimitBox.Text, out var ms) ? Math.Max(0, ms) : 0,
            AutomaticDepth = AutoDepthBox.IsChecked == true ? 1 : 0,
            UseQuiescence = QuiescenceBox.IsChecked == true ? 1 : 0,
            UseTranspositionTable = TranspositionBox.IsChecked == true ? 1 : 0,
            UseMoveOrdering = MoveOrderingBox.IsChecked == true ? 1 : 0,
            UsePieceSquareTables = PieceSquareBox.IsChecked == true ? 1 : 0,
            UseBishopPairBonus = 1,
            UseKingSafetyBonus = KingSafetyBox.IsChecked == true ? 1 : 0,
            UseGpuEvaluation = GpuEvalBox.IsChecked == true ? 1 : 0,
            UseEndgameTables = EndgameBox.IsChecked == true ? 1 : 0,
            OpeningRandomness = int.TryParse(OpeningRandomBox.Text, out var random) ? Math.Clamp(random, 0, 100) : 0,
            OpeningMaxPly = int.TryParse(OpeningPlyBox.Text, out var ply) ? Math.Clamp(ply, 1, 80) : 16
        };
    }

    private void ApplyDrawRulesFromUi()
    {
        if (AutoThreefoldBox == null)
        {
            return;
        }

        _engine.SetDrawRules(new ChessDrawRulesDto
        {
            RepetitionClaimCount = 3,
            RepetitionAutoDrawCount = 5,
            AutoClaimThreefold = AutoThreefoldBox.IsChecked == true ? 1 : 0,
            FiftyMoveClaimPlies = 100,
            SeventyFiveMoveAutoPlies = 150,
            AutoClaimFiftyMove = 0
        });
    }

    private void ApplyTablebasePathFromUi()
    {
        if (TablebasePathBox == null)
        {
            return;
        }

        var path = TablebasePathBox.Text.Trim();
        if (!Path.IsPathFullyQualified(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, path);
        }
        _engine.SetTablebasePath(path);
    }

    private void ApplyTablebase_Click(object sender, RoutedEventArgs e)
    {
        ApplyTablebasePathFromUi();
        RefreshStatus();
    }

    private void DrawRules_Changed(object sender, RoutedEventArgs e)
    {
        if (BoardGrid == null || _squares[0, 0] == null)
        {
            return;
        }

        ApplyDrawRulesFromUi();
        RefreshStatus();
    }

    private bool IsSetupMode()
    {
        return SetupModeBox?.IsChecked == true;
    }

    private bool IsBlackView()
    {
        return PlayerSideBox?.SelectedIndex == 1;
    }

    private void SyncSetupBoardFromEngine()
    {
        _setupBoard = _engine.GetBoard();
        var state = _engine.GetState();
        if (SetupSideBox != null)
        {
            SetupSideBox.SelectedIndex = state.SideToMove == NativeChessEngine.White ? 0 : 1;
        }
    }

    private void SetupModeBox_Changed(object sender, RoutedEventArgs e)
    {
        if (BoardGrid == null || _squares[0, 0] == null)
        {
            return;
        }

        if (IsSetupMode())
        {
            SyncSetupBoardFromEngine();
            _selected = null;
        }
        RefreshAll();
    }

    private void ClearSetup_Click(object sender, RoutedEventArgs e)
    {
        _setupBoard = new int[64];
        RefreshBoard();
        Refresh3DScene();
        RefreshStatus();
    }

    private void StartSetup_Click(object sender, RoutedEventArgs e)
    {
        _setupBoard = StartingBoard();
        SetupSideBox.SelectedIndex = 0;
        RefreshBoard();
        Refresh3DScene();
        RefreshStatus();
    }

    private void ApplySetup_Click(object sender, RoutedEventArgs e)
    {
        var fen = BuildSetupFen();
        if (!_engine.SetFen(fen))
        {
            MessageBox.Show(this,
                "Позиция не принята движком. Проверьте, что на доске есть ровно по одному белому и черному королю.",
                "Chess Advisor",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        SetupModeBox.IsChecked = false;
        _selected = null;
        ResetGameHistory();
        RefreshAll();
        _ = SendFenOverNetworkAsync();
    }

    private void ClaimDraw_Click(object sender, RoutedEventArgs e)
    {
        if (_engine.ClaimDraw())
        {
            UpdateHistoryOutcome(_engine.GetState());
            RefreshAll();
            return;
        }

        MessageBox.Show(this,
            "Сейчас нет заявляемой ничьей: нужно три повтора позиции или 50 ходов без взятия и хода пешкой.",
            "Chess Advisor",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _pieceTheme = ThemeBox.SelectedIndex switch
        {
            1 => PieceTheme.ClassicBmp,
            2 => PieceTheme.Unicode,
            _ => PieceTheme.Transparent
        };
        BuildPalettes();
        if (BoardGrid != null && _squares[0, 0] != null)
        {
            RefreshBoard();
            Refresh3DScene();
        }
    }

    private void PlayerSideBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BoardGrid == null || _squares[0, 0] == null)
        {
            return;
        }

        BuildBoard();
        RefreshCoordinates();
        RefreshBoard();
        Refresh3DScene();
    }

    private void Mode3DBox_Changed(object sender, RoutedEventArgs e)
    {
        if (Board2DView == null || Board3DHost == null)
        {
            return;
        }

        var is3D = Mode3DBox.IsChecked == true;
        Board2DView.Visibility = is3D ? Visibility.Collapsed : Visibility.Visible;
        Board3DHost.Visibility = is3D ? Visibility.Visible : Visibility.Collapsed;
        Refresh3DScene();
        RefreshStatus();
    }

    private void LoadModelSets()
    {
        if (ModelSetBox == null)
        {
            return;
        }

        ModelSetBox.Items.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, path) in ObjModelLibrary.DiscoverSets())
        {
            if (!seen.Add(name))
            {
                continue;
            }
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
        _selectedModelSetPath = (ModelSetBox.SelectedItem as ComboBoxItem)?.Tag as string;
        _models.SelectedSetPath = _selectedModelSetPath;
    }

    private static IEnumerable<string> GetModelRoots()
    {
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Assets", "Models"));
        yield return Path.Combine(AppContext.BaseDirectory, "Assets", "Models");
    }

    private void ModelSetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedModelSetPath = (ModelSetBox.SelectedItem as ComboBoxItem)?.Tag as string;
        _models.SelectedSetPath = _selectedModelSetPath;
        _models.ClearCache();
        Refresh3DScene();
        RefreshStatus();
    }

    private string BuildSetupFen()
    {
        var ranks = new List<string>();
        for (var rank = 7; rank >= 0; --rank)
        {
            var empty = 0;
            var text = string.Empty;
            for (var file = 0; file < 8; ++file)
            {
                var piece = _setupBoard[rank * 8 + file];
                if (piece == 0)
                {
                    ++empty;
                    continue;
                }

                if (empty > 0)
                {
                    text += empty.ToString();
                    empty = 0;
                }
                text += FenCharForPiece(piece);
            }

            if (empty > 0)
            {
                text += empty.ToString();
            }
            ranks.Add(text);
        }

        var side = SetupSideBox.SelectedIndex == 1 ? "b" : "w";
        return $"{string.Join("/", ranks)} {side} - - 0 1";
    }

    private static int[] StartingBoard()
    {
        var board = new int[64];
        int[] back = { NativeChessEngine.Rook, NativeChessEngine.Knight, NativeChessEngine.Bishop, NativeChessEngine.Queen, NativeChessEngine.King, NativeChessEngine.Bishop, NativeChessEngine.Knight, NativeChessEngine.Rook };
        for (var file = 0; file < 8; ++file)
        {
            board[file] = back[file];
            board[8 + file] = NativeChessEngine.Pawn;
            board[6 * 8 + file] = -NativeChessEngine.Pawn;
            board[7 * 8 + file] = -back[file];
        }
        return board;
    }

    private void LoadPieceImages()
    {
        LoadImageSet("ClassicBmp", ".bmp", _classicImages);
        LoadImageSet("TransparentPng", ".png", _transparentImages);
    }

    private static void LoadImageSet(string folderName, string extension, Dictionary<int, ImageSource> target)
    {
        var figureDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Figures", folderName);
        var stems = new Dictionary<int, string>
        {
            [NativeChessEngine.Pawn] = "peshkaW",
            [NativeChessEngine.Knight] = "konjW",
            [NativeChessEngine.Bishop] = "slonW",
            [NativeChessEngine.Rook] = "turaW",
            [NativeChessEngine.Queen] = "ferzW",
            [NativeChessEngine.King] = "koroljW",
            [-NativeChessEngine.Pawn] = "peshkaB",
            [-NativeChessEngine.Knight] = "konjB",
            [-NativeChessEngine.Bishop] = "slonB",
            [-NativeChessEngine.Rook] = "turaB",
            [-NativeChessEngine.Queen] = "ferzB",
            [-NativeChessEngine.King] = "koroljB"
        };

        foreach (var (piece, stem) in stems)
        {
            var path = Path.Combine(figureDir, stem + extension);
            if (!File.Exists(path))
            {
                continue;
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            target[piece] = image;
        }
    }

    private Brush BaseBrush(int file, int rank)
    {
        return ((file + rank) & 1) == 0 ? _darkSquare : _lightSquare;
    }

    private void Refresh3DScene()
    {
        if (Board3DView == null || Mode3DBox?.IsChecked != true)
        {
            return;
        }

        var board = IsSetupMode() ? _setupBoard : _engine.GetBoard();
        var state = _engine.GetState();
        var selectedTargets = _selected is null || IsSetupMode()
            ? Array.Empty<ChessMoveDto>()
            : _legalMoves.Where(m => m.FromFile == _selected.Value.File && m.FromRank == _selected.Value.Rank).ToArray();
        var group = new Model3DGroup();
        group.Children.Add(new AmbientLight(Color.FromRgb(128, 132, 140)));
        group.Children.Add(new DirectionalLight(Color.FromRgb(245, 244, 235), new Vector3D(-2.4, -4.2, -3.1)));
        group.Children.Add(new DirectionalLight(Color.FromRgb(142, 166, 205), new Vector3D(2.0, -1.1, 2.8)));
        _lastLoaded3DModels = 0;
        _lastMissing3DModels = 0;
        _hitSquares3D.Clear();

        for (var rank = 0; rank < 8; ++rank)
        {
            for (var file = 0; file < 8; ++file)
            {
                var square = new Square(file, rank);
                var brush = BrushFor3DSquare(file, rank, state, selectedTargets);
                var tile = CreateTileModel(file, rank, brush);
                _hitSquares3D[tile] = square;
                group.Children.Add(tile);

                var piece = board[rank * 8 + file];
                if (piece != 0)
                {
                    var pieceModel = CreatePieceModel(piece, file - 3.5, rank - 3.5);
                    _hitSquares3D[pieceModel] = square;
                    group.Children.Add(pieceModel);
                }
            }
        }

        Board3DView.Children.Clear();
        Board3DView.Children.Add(new ModelVisual3D { Content = group });
        Update3DCamera();
    }

    private Brush BrushFor3DSquare(int file, int rank, ChessStateDto state, ChessMoveDto[] selectedTargets)
    {
        if (!IsSetupMode() && (state.LastFromFile == file && state.LastFromRank == rank ||
            state.LastToFile == file && state.LastToRank == rank))
        {
            return new SolidColorBrush(Color.FromRgb(186, 202, 68));
        }

        var square = new Square(file, rank);
        if (_selected == square)
        {
            return new SolidColorBrush(Color.FromRgb(246, 211, 101));
        }

        if (selectedTargets.Any(m => m.ToFile == file && m.ToRank == rank))
        {
            var capture = selectedTargets.Any(m => m.ToFile == file && m.ToRank == rank && (m.Flags & NativeChessEngine.MoveCapture) != 0);
            return capture
                ? new SolidColorBrush(Color.FromRgb(214, 92, 76))
                : new SolidColorBrush(Color.FromRgb(83, 174, 204));
        }

        return ((file + rank) & 1) == 0
            ? new SolidColorBrush(Color.FromRgb(154, 176, 118))
            : new SolidColorBrush(Color.FromRgb(230, 224, 194));
    }

    private GeometryModel3D CreateTileModel(int file, int rank, Brush brush)
    {
        var tileName = ((file + rank) & 1) == 0 ? "light_tile.obj" : "dark_tile.obj";
        var mesh = LoadModelMesh(Path.Combine("Board", tileName));
        if (mesh != null)
        {
            _lastLoaded3DModels++;
            var material = ObjModelLibrary.CreateSurfaceMaterial(brush);
            return new GeometryModel3D(mesh, material)
            {
                BackMaterial = material,
                Transform = new TranslateTransform3D(file - 3.5, 0, rank - 3.5)
            };
        }
        _lastMissing3DModels++;
        return CreateBox(file - 3.5, -0.04, rank - 3.5, 0.98, 0.08, 0.98, brush);
    }

    private GeometryModel3D CreatePieceModel(int piece, double x, double z)
    {
        var mesh = LoadModelMesh(Path.Combine("Pieces", ModelFileName(piece)));
        var type = Math.Abs(piece);
        var material = _models.CreatePieceMaterial(Path.Combine("Pieces", ModelFileName(piece)), piece > 0 ? 1 : 2, type);
        if (mesh != null)
        {
            _lastLoaded3DModels++;
            return new GeometryModel3D(mesh, material)
            {
                BackMaterial = material,
                Transform = new TranslateTransform3D(x, 0.02, z)
            };
        }
        _lastMissing3DModels++;

        var radius = type switch
        {
            NativeChessEngine.Pawn => 0.23,
            NativeChessEngine.Knight => 0.27,
            NativeChessEngine.Bishop => 0.28,
            NativeChessEngine.Rook => 0.30,
            NativeChessEngine.Queen => 0.33,
            NativeChessEngine.King => 0.34,
            _ => 0.25
        };
        var height = type switch
        {
            NativeChessEngine.Pawn => 0.55,
            NativeChessEngine.Knight => 0.72,
            NativeChessEngine.Bishop => 0.78,
            NativeChessEngine.Rook => 0.74,
            NativeChessEngine.Queen => 0.95,
            NativeChessEngine.King => 1.05,
            _ => 0.6
        };
        return CreateCylinder(x, 0.0, z, radius, height, 28, new SolidColorBrush(ObjModelLibrary.PieceColor(piece > 0 ? 1 : 2)));
    }

    private MeshGeometry3D? LoadModelMesh(string relativePath)
    {
        return _models.LoadMesh(relativePath);
    }

    private static string ModelFileName(int piece)
    {
        var color = piece > 0 ? "white" : "black";
        var name = Math.Abs(piece) switch
        {
            NativeChessEngine.Pawn => "pawn",
            NativeChessEngine.Knight => "knight",
            NativeChessEngine.Bishop => "bishop",
            NativeChessEngine.Rook => "rook",
            NativeChessEngine.Queen => "queen",
            NativeChessEngine.King => "king",
            _ => "empty"
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
                if (parts.Length >= 4 && parts[0] == "v")
                {
                    vertices.Add(new Point3D(ParseObjDouble(parts[1]), ParseObjDouble(parts[2]), ParseObjDouble(parts[3])));
                }
                else if (parts.Length >= 4 && parts[0] == "f")
                {
                    var face = parts.Skip(1).Select(p => ParseObjIndex(p, vertices.Count)).Where(i => i >= 0 && i < vertices.Count).ToArray();
                    if (face.Length < 3)
                    {
                        continue;
                    }

                    var first = AddObjVertex(mesh, vertices[face[0]]);
                    for (var i = 1; i < face.Length - 1; ++i)
                    {
                        var second = AddObjVertex(mesh, vertices[face[i]]);
                        var third = AddObjVertex(mesh, vertices[face[i + 1]]);
                        mesh.TriangleIndices.Add(first);
                        mesh.TriangleIndices.Add(second);
                        mesh.TriangleIndices.Add(third);
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

    private static int AddObjVertex(MeshGeometry3D mesh, Point3D point)
    {
        var index = mesh.Positions.Count;
        mesh.Positions.Add(point);
        return index;
    }

    private static void NormalizeObjMesh(MeshGeometry3D mesh, bool isBoardTile)
    {
        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var minZ = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;
        var maxZ = double.NegativeInfinity;

        foreach (var point in mesh.Positions)
        {
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            minZ = Math.Min(minZ, point.Z);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
            maxZ = Math.Max(maxZ, point.Z);
        }

        var sizeX = Math.Max(0.0001, maxX - minX);
        var sizeY = Math.Max(0.0001, maxY - minY);
        var sizeZ = Math.Max(0.0001, maxZ - minZ);
        var centerX = (minX + maxX) * 0.5;
        var centerZ = (minZ + maxZ) * 0.5;

        var scale = isBoardTile
            ? Math.Min(0.98 / sizeX, 0.98 / sizeZ)
            : Math.Min(1.05 / sizeY, 0.76 / Math.Max(sizeX, sizeZ));

        for (var i = 0; i < mesh.Positions.Count; ++i)
        {
            var point = mesh.Positions[i];
            mesh.Positions[i] = new Point3D(
                (point.X - centerX) * scale,
                (point.Y - minY) * scale,
                (point.Z - centerZ) * scale);
        }
    }

    private static int ParseObjIndex(string token, int vertexCount)
    {
        var head = token.Split('/')[0];
        if (!int.TryParse(head, out var index) || index == 0)
        {
            return -1;
        }
        return index > 0 ? index - 1 : vertexCount + index;
    }

    private static double ParseObjDouble(string text)
    {
        return double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static GeometryModel3D CreateBox(double x, double y, double z, double width, double height, double depth, Brush brush)
    {
        var hx = width / 2;
        var hy = height / 2;
        var hz = depth / 2;
        var p = new[]
        {
            new Point3D(x - hx, y - hy, z - hz), new Point3D(x + hx, y - hy, z - hz),
            new Point3D(x + hx, y + hy, z - hz), new Point3D(x - hx, y + hy, z - hz),
            new Point3D(x - hx, y - hy, z + hz), new Point3D(x + hx, y - hy, z + hz),
            new Point3D(x + hx, y + hy, z + hz), new Point3D(x - hx, y + hy, z + hz)
        };
        var mesh = new MeshGeometry3D();
        foreach (var point in p)
        {
            mesh.Positions.Add(point);
        }
        int[] indices =
        {
            0,2,1, 0,3,2, 4,5,6, 4,6,7,
            0,1,5, 0,5,4, 2,3,7, 2,7,6,
            1,2,6, 1,6,5, 0,4,7, 0,7,3
        };
        foreach (var index in indices)
        {
            mesh.TriangleIndices.Add(index);
        }
        return new GeometryModel3D(mesh, new DiffuseMaterial(brush));
    }

    private static GeometryModel3D CreateCylinder(double x, double y, double z, double radius, double height, int segments, Brush brush)
    {
        var mesh = new MeshGeometry3D();
        var bottomCenter = mesh.Positions.Count;
        mesh.Positions.Add(new Point3D(x, y, z));
        var topCenter = mesh.Positions.Count;
        mesh.Positions.Add(new Point3D(x, y + height, z));

        for (var i = 0; i < segments; ++i)
        {
            var angle = i * Math.PI * 2 / segments;
            var px = x + Math.Cos(angle) * radius;
            var pz = z + Math.Sin(angle) * radius;
            mesh.Positions.Add(new Point3D(px, y, pz));
            mesh.Positions.Add(new Point3D(px, y + height, pz));
        }

        for (var i = 0; i < segments; ++i)
        {
            var next = (i + 1) % segments;
            var b0 = 2 + i * 2;
            var t0 = b0 + 1;
            var b1 = 2 + next * 2;
            var t1 = b1 + 1;

            mesh.TriangleIndices.Add(b0); mesh.TriangleIndices.Add(t0); mesh.TriangleIndices.Add(t1);
            mesh.TriangleIndices.Add(b0); mesh.TriangleIndices.Add(t1); mesh.TriangleIndices.Add(b1);
            mesh.TriangleIndices.Add(bottomCenter); mesh.TriangleIndices.Add(b1); mesh.TriangleIndices.Add(b0);
            mesh.TriangleIndices.Add(topCenter); mesh.TriangleIndices.Add(t0); mesh.TriangleIndices.Add(t1);
        }

        return new GeometryModel3D(mesh, new DiffuseMaterial(brush));
    }

    private void Update3DCamera()
    {
        var yaw = _orbitYaw * Math.PI / 180.0;
        var pitch = _orbitPitch * Math.PI / 180.0;
        var cosPitch = Math.Cos(pitch);
        var position = new Point3D(
            Math.Sin(yaw) * cosPitch * _orbitDistance,
            Math.Sin(pitch) * _orbitDistance,
            Math.Cos(yaw) * cosPitch * _orbitDistance);

        Board3DView.Camera = new PerspectiveCamera
        {
            Position = position,
            LookDirection = new Vector3D(-position.X, -position.Y, -position.Z),
            UpDirection = new Vector3D(0, 1, 0),
            FieldOfView = 42
        };
    }

    private void Board3DView_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton != System.Windows.Input.MouseButton.Left)
        {
            return;
        }

        _is3DDragging = true;
        _last3DPoint = e.GetPosition(Board3DView);
        _dragStart3DPoint = _last3DPoint;
        Board3DView.CaptureMouse();
    }

    private void Board3DView_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_is3DDragging || e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(Board3DView);
        _orbitYaw += (current.X - _last3DPoint.X) * 0.35;
        _orbitPitch = Math.Clamp(_orbitPitch - (current.Y - _last3DPoint.Y) * 0.25, 18, 82);
        _last3DPoint = current;
        Update3DCamera();
    }

    private void Board3DView_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var current = e.GetPosition(Board3DView);
        var isClick =
            Math.Abs(current.X - _dragStart3DPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _dragStart3DPoint.Y) < SystemParameters.MinimumVerticalDragDistance;

        _is3DDragging = false;
        Board3DView.ReleaseMouseCapture();

        if (isClick && TryPick3DSquare(current, out var square))
        {
            HandleBoardSquareInput(square);
        }
    }

    private void Board3DView_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        _orbitDistance = Math.Clamp(_orbitDistance - e.Delta / 500.0, 6.0, 16.0);
        Update3DCamera();
    }

    private bool TryPick3DSquare(Point point, out Square square)
    {
        Square? picked = null;
        VisualTreeHelper.HitTest(Board3DView, null, result =>
        {
            if (result is RayMeshGeometry3DHitTestResult meshHit &&
                _hitSquares3D.TryGetValue(meshHit.ModelHit, out var hitSquare))
            {
                picked = hitSquare;
                return HitTestResultBehavior.Stop;
            }

            return HitTestResultBehavior.Continue;
        }, new PointHitTestParameters(point));

        square = picked ?? default;
        return picked.HasValue;
    }

    private object ContentForPiece(int piece)
    {
        var imageSource = _pieceTheme switch
        {
            PieceTheme.Transparent => _transparentImages.TryGetValue(piece, out var transparent) ? transparent : null,
            PieceTheme.ClassicBmp => _classicImages.TryGetValue(piece, out var classic) ? classic : null,
            _ => null
        };

        if (imageSource != null)
        {
            return new Image
            {
                Source = imageSource,
                Stretch = Stretch.Uniform,
                SnapsToDevicePixels = true
            };
        }

        if (_pieceTheme == PieceTheme.Unicode && piece != 0)
        {
            return new Viewbox
            {
                Stretch = Stretch.Uniform,
                Margin = new Thickness(4),
                Child = new TextBlock
                {
                    Text = GlyphForPiece(piece),
                    FontFamily = new FontFamily("Segoe UI Symbol"),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 72,
                    Foreground = piece > 0 ? _whitePiece : _blackPiece,
                    LineHeight = 72,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
        }

        return piece == 0 ? string.Empty : GlyphForPiece(piece);
    }

    private static char FenCharForPiece(int piece)
    {
        return piece switch
        {
            1 => 'P',
            2 => 'N',
            3 => 'B',
            4 => 'R',
            5 => 'Q',
            6 => 'K',
            -1 => 'p',
            -2 => 'n',
            -3 => 'b',
            -4 => 'r',
            -5 => 'q',
            -6 => 'k',
            _ => '1'
        };
    }

    private static string GlyphForPiece(int piece)
    {
        return piece switch
        {
            1 => "♙",
            2 => "♘",
            3 => "♗",
            4 => "♖",
            5 => "♕",
            6 => "♔",
            -1 => "♟",
            -2 => "♞",
            -3 => "♝",
            -4 => "♜",
            -5 => "♛",
            -6 => "♚",
            _ => string.Empty
        };
    }

    private static string PieceName(int piece)
    {
        return piece switch
        {
            1 => "Белая пешка",
            2 => "Белый конь",
            3 => "Белый слон",
            4 => "Белая ладья",
            5 => "Белый ферзь",
            6 => "Белый король",
            -1 => "Черная пешка",
            -2 => "Черный конь",
            -3 => "Черный слон",
            -4 => "Черная ладья",
            -5 => "Черный ферзь",
            -6 => "Черный король",
            _ => "Пусто"
        };
    }

    private static string FormatStatus(ChessStateDto state)
    {
        var side = state.SideToMove == NativeChessEngine.White ? "белых" : "черных";
        var check = state.IsCheck != 0 ? " Шах." : string.Empty;
        var baseText = state.Status switch
        {
            NativeChessEngine.StatusCheckmate => $"Мат. Проиграли {side}.",
            NativeChessEngine.StatusStalemate => "Пат.",
            NativeChessEngine.StatusFiftyMoveClaim => "Можно заявить ничью по правилу 50 ходов.",
            NativeChessEngine.StatusRepetitionClaim => $"Можно заявить ничью по повторению позиции ({state.RepetitionCount}).",
            NativeChessEngine.StatusRepetitionDraw => $"Ничья по повторению позиции ({state.RepetitionCount}).",
            NativeChessEngine.StatusSeventyFiveMoveDraw => "Автоматическая ничья по правилу 75 ходов.",
            _ => $"Ход {side}.{check}"
        };

        return $"{baseText} Легальных ходов: {state.LegalMoveCount}. Повторов: {state.RepetitionCount}.";
    }

    private bool CommitMoveToHistory(string preMoveFen, ChessStateDto before, ChessMoveDto move)
    {
        var postMoveFen = _engine.GetFen();
        try
        {
            using var verifier = new NativeChessEngine();
            if (!verifier.SetFen(preMoveFen) || !verifier.SetDrawRules(_engine.GetDrawRules()) ||
                !verifier.TryGetMoveDescriptor(move.FromFile, move.FromRank, move.ToFile, move.ToRank, move.Promotion, out var descriptor))
            {
                return RejectCommittedMove("Legal move context could not be reconstructed.");
            }

            var sanContext = new ChessSanMoveContext(
                true,
                move.FromFile,
                move.FromRank,
                move.ToFile,
                move.ToRank,
                descriptor.MovedPiece,
                descriptor.CapturedPiece,
                move.Promotion,
                (move.Flags & NativeChessEngine.MoveCapture) != 0,
                (move.Flags & NativeChessEngine.MoveEnPassant) != 0,
                (ChessCastleKind)descriptor.CastleKind,
                (ChessSanDisambiguation)descriptor.Disambiguation,
                descriptor.ResultingIsCheck != 0,
                descriptor.ResultingStatus == NativeChessEngine.StatusCheckmate);
            var san = ChessSanGenerator.Generate(sanContext);
            if (!san.Success)
            {
                return RejectCommittedMove($"SAN generation failed: {san.Message}");
            }
            if (!verifier.TryMakeMove(move.FromFile, move.FromRank, move.ToFile, move.ToRank, move.Promotion, out _) ||
                verifier.GetFen() != postMoveFen)
            {
                return RejectCommittedMove("Move replay did not reproduce the committed position.");
            }

            var state = _engine.GetState();
            var (result, termination) = OutcomeForState(state);
            var record = new ChessMoveRecord(
                _gameHistory.Moves.Count,
                before.FullmoveNumber,
                before.SideToMove,
                new ChessGameRecords.ChessSquare(move.FromFile, move.FromRank),
                new ChessGameRecords.ChessSquare(move.ToFile, move.ToRank),
                descriptor.MovedPiece,
                descriptor.CapturedPiece,
                move.Promotion,
                (ChessCastleKind)descriptor.CastleKind,
                (move.Flags & NativeChessEngine.MoveEnPassant) != 0,
                (move.Flags & NativeChessEngine.MoveCapture) != 0,
                descriptor.ResultingIsCheck != 0,
                descriptor.ResultingStatus == NativeChessEngine.StatusCheckmate,
                preMoveFen,
                postMoveFen,
                BuildUci(move),
                san.San,
                null,
                null,
                move.Score == 0 ? null : new ChessEvaluationMetadata(move.Score, null, null, null, null));
            if (!_gameHistory.TryCommit(record, result, termination, out var historyError))
            {
                return RejectCommittedMove(historyError);
            }
            return true;
        }
        catch (Exception ex)
        {
            return RejectCommittedMove($"Move record failed: {ex.Message}");
        }
    }

    private bool RejectCommittedMove(string reason)
    {
        var rolledBack = _engine.Undo();
        MessageBox.Show(this,
            rolledBack ? reason : $"{reason} Native rollback also failed.",
            "Chess history",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return false;
    }

    private void ResetGameHistory()
    {
        _gameHistory.Reset(_engine.GetFen());
        RefreshMoveHistory();
    }

    private void UpdateHistoryOutcome(ChessStateDto state)
    {
        var (result, termination) = OutcomeForState(state);
        _gameHistory.UpdateOutcome(result, termination);
    }

    private static (ChessGameResult Result, ChessTerminationReason Termination) OutcomeForState(ChessStateDto state)
    {
        return state.Status switch
        {
            NativeChessEngine.StatusCheckmate => state.SideToMove == NativeChessEngine.White
                ? (ChessGameResult.BlackWin, ChessTerminationReason.Checkmate)
                : (ChessGameResult.WhiteWin, ChessTerminationReason.Checkmate),
            NativeChessEngine.StatusStalemate => (ChessGameResult.Draw, ChessTerminationReason.Stalemate),
            NativeChessEngine.StatusRepetitionDraw => (ChessGameResult.Draw,
                state.CanClaimFiftyMove != 0 ? ChessTerminationReason.FiftyMoveRule : ChessTerminationReason.Repetition),
            NativeChessEngine.StatusSeventyFiveMoveDraw => (ChessGameResult.Draw, ChessTerminationReason.SeventyFiveMoveRule),
            _ => (ChessGameResult.Ongoing, ChessTerminationReason.None)
        };
    }

    private void RefreshMoveHistory()
    {
        if (MoveList == null || MoveSelectionBox == null)
        {
            return;
        }
        var selectedPly = (MoveSelectionBox.SelectedItem as MoveSelectionItem)?.Record.PlyIndex;
        var rows = _gameHistory.Moves
            .GroupBy(move => move.FullmoveNumber)
            .Select(group => new MoveHistoryRow(
                group.Key,
                group.FirstOrDefault(move => move.Side == NativeChessEngine.White),
                group.FirstOrDefault(move => move.Side == NativeChessEngine.Black)))
            .ToArray();
        MoveList.ItemsSource = rows;

        var selections = _gameHistory.Moves.Select(move => new MoveSelectionItem(move)).ToArray();
        MoveSelectionBox.ItemsSource = selections;
        MoveSelectionBox.SelectedItem = selections.FirstOrDefault(item => item.Record.PlyIndex == selectedPly) ?? selections.LastOrDefault();
        if (rows.Length > 0)
        {
            MoveList.ScrollIntoView(rows[^1]);
        }
        RefreshSelectedMoveDetails();
    }

    private void MoveList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MoveList.SelectedItem is not MoveHistoryRow row)
        {
            return;
        }
        var record = row.Black ?? row.White;
        if (record is null)
        {
            return;
        }
        MoveSelectionBox.SelectedItem = MoveSelectionBox.Items
            .OfType<MoveSelectionItem>()
            .FirstOrDefault(item => item.Record.PlyIndex == record.PlyIndex);
    }

    private void MoveSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshSelectedMoveDetails();
    }

    private void RefreshSelectedMoveDetails()
    {
        if (MoveHistoryStatusText == null || SelectedPreFenBox == null || SelectedPostFenBox == null)
        {
            return;
        }
        var selected = (MoveSelectionBox?.SelectedItem as MoveSelectionItem)?.Record;
        MoveHistoryStatusText.Text = selected is null
            ? $"Moves: {_gameHistory.Moves.Count}. Result: {_gameHistory.Result}."
            : $"Ply {selected.PlyIndex + 1}/{_gameHistory.Moves.Count}: {selected.San} ({selected.Uci}).";
        SelectedPreFenBox.Text = selected?.PreMoveFen ?? string.Empty;
        SelectedPostFenBox.Text = selected?.PostMoveFen ?? string.Empty;
    }

    private void CopySan_Click(object sender, RoutedEventArgs e)
    {
        if ((MoveSelectionBox.SelectedItem as MoveSelectionItem)?.Record is { } record)
        {
            Clipboard.SetText(record.San);
        }
    }

    private void CopyUci_Click(object sender, RoutedEventArgs e)
    {
        if ((MoveSelectionBox.SelectedItem as MoveSelectionItem)?.Record is { } record)
        {
            Clipboard.SetText(record.Uci);
        }
    }

    private static string BuildUci(ChessMoveDto move)
    {
        var promotion = move.Promotion switch
        {
            NativeChessEngine.Queen => "q",
            NativeChessEngine.Rook => "r",
            NativeChessEngine.Bishop => "b",
            NativeChessEngine.Knight => "n",
            _ => string.Empty
        };
        return $"{SquareName(move.FromFile, move.FromRank)}{SquareName(move.ToFile, move.ToRank)}{promotion}";
    }

    private static string SquareName(int file, int rank)
    {
        return $"{(char)('a' + file)}{rank + 1}";
    }

    private readonly record struct Square(int File, int Rank);
    private readonly record struct SetupDragData(int Piece, Square? From);
    private sealed record MoveHistoryRow(int MoveNumber, ChessMoveRecord? White, ChessMoveRecord? Black)
    {
        public string WhiteSan => White?.San ?? string.Empty;
        public string BlackSan => Black?.San ?? string.Empty;
    }

    private sealed record MoveSelectionItem(ChessMoveRecord Record)
    {
        public override string ToString() => Record.Side == NativeChessEngine.White
            ? $"{Record.FullmoveNumber}. {Record.San}"
            : $"{Record.FullmoveNumber}... {Record.San}";
    }
    private enum PieceTheme
    {
        Transparent,
        ClassicBmp,
        Unicode
    }
}
