using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using RubikState;

namespace RubikApp;

public partial class RubikFaceEditorWindow : Window
{
    private static readonly string[] FaceNames = ["U", "R", "F", "D", "L", "B"];
    private static readonly Brush[] ColorBrushes =
    [
        BrushFrom("#2B3038"), BrushFrom("#F3F0E5"), BrushFrom("#D64A4A"), BrushFrom("#3CA66B"),
        BrushFrom("#E6C84B"), BrushFrom("#DF843E"), BrushFrom("#4A73CF")
    ];
    private RubikFaceEditorDraft _draft;
    private int _selectedColor = 1;

    public RubikFaceEditorWindow(RubikFaceEditorDraft draft)
    {
        InitializeComponent();
        _draft = draft;
        BuildFaceTabs();
        RefreshEditor();
    }

    public RubikStateDocument? ResultDocument { get; private set; }

    private void BuildFaceTabs()
    {
        FaceTabs.Items.Clear();
        var cellSize = Math.Clamp(530.0 / _draft.Size, 20, 48);
        for (var face = 0; face < 6; face++)
        {
            var grid = new UniformGrid { Rows = _draft.Size, Columns = _draft.Size, Margin = new Thickness(10) };
            for (var row = 0; row < _draft.Size; row++)
            for (var column = 0; column < _draft.Size; column++)
            {
                var button = new Button
                {
                    Width = cellSize,
                    Height = cellSize,
                    MinWidth = 0,
                    Padding = new Thickness(0),
                    Margin = new Thickness(1),
                    BorderThickness = new Thickness(1),
                    Tag = new CellAddress(face, row, column),
                    ToolTip = $"{FaceNames[face]} [{row + 1},{column + 1}]"
                };
                button.PreviewMouseLeftButtonDown += PaintCell;
                button.MouseEnter += PaintCellWhileDragging;
                grid.Children.Add(button);
            }
            FaceTabs.Items.Add(new TabItem
            {
                Header = FaceNames[face],
                Content = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = grid
                }
            });
        }
        FaceTabs.SelectedIndex = 0;
    }

    private void PaintCell(object sender, MouseButtonEventArgs e)
    {
        if (sender is Button { Tag: CellAddress cell })
        {
            _draft.Paint(cell.Face, cell.Row, cell.Column, _selectedColor);
            RefreshEditor();
            e.Handled = true;
        }
    }

    private void PaintCellWhileDragging(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is Button { Tag: CellAddress cell })
        {
            _draft.Paint(cell.Face, cell.Row, cell.Column, _selectedColor);
            RefreshEditor();
        }
    }

    private void Palette_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Tag?.ToString(), out var color))
        {
            _selectedColor = color;
            RefreshEditor();
        }
    }

    private void FillFace_Click(object sender, RoutedEventArgs e) { _draft.FillFace(CurrentFace, _selectedColor); RefreshEditor(); }
    private void RotateFace_Click(object sender, RoutedEventArgs e) { _draft.RotateFaceClockwise(CurrentFace); RefreshEditor(); }
    private void ClearFace_Click(object sender, RoutedEventArgs e) { _draft.ClearFace(CurrentFace); RefreshEditor(); }
    private void ClearAll_Click(object sender, RoutedEventArgs e) { _draft.ClearAll(); RefreshEditor(); }
    private void Undo_Click(object sender, RoutedEventArgs e) { _draft.Undo(); RefreshEditor(); }
    private void Redo_Click(object sender, RoutedEventArgs e) { _draft.Redo(); RefreshEditor(); }

    private void CopyFace_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(_draft.CopyFaceText(CurrentFace));
        EditorErrorText.Text = "Face copied as N*N color IDs.";
    }

    private void PasteFace_Click(object sender, RoutedEventArgs e)
    {
        try { _draft.PasteFaceText(CurrentFace, Clipboard.GetText()); EditorErrorText.Text = string.Empty; RefreshEditor(); }
        catch (Exception exception) { EditorErrorText.Text = $"Paste rejected: {exception.Message}"; }
    }

    private void SaveDraft_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Rubik editor draft (*.rubikdraft.json)|*.rubikdraft.json|JSON (*.json)|*.json",
            DefaultExt = ".rubikdraft.json", AddExtension = true, OverwritePrompt = true,
            FileName = $"rubik-{_draft.Size}x{_draft.Size}.rubikdraft.json"
        };
        if (dialog.ShowDialog(this) != true) return;
        try { RubikFaceEditorDraftSerializer.SaveAtomic(dialog.FileName, _draft); EditorErrorText.Text = $"Draft saved: {dialog.FileName}"; }
        catch (Exception exception) { EditorErrorText.Text = $"Draft save failed: {exception.Message}"; }
    }

    private void LoadDraft_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Rubik editor draft (*.rubikdraft.json)|*.rubikdraft.json|JSON (*.json)|*.json",
            DefaultExt = ".rubikdraft.json", CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true) return;
        try { _draft = RubikFaceEditorDraftSerializer.Load(dialog.FileName); BuildFaceTabs(); RefreshEditor(); EditorErrorText.Text = $"Draft loaded: {dialog.FileName}"; }
        catch (Exception exception) { EditorErrorText.Text = $"Draft load failed: {exception.Message}"; }
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        try { ResultDocument = _draft.ToStateDocument(); DialogResult = true; }
        catch (Exception exception) { EditorErrorText.Text = $"Apply blocked: {exception.Message}"; }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void FaceTabs_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshEditor();
    private int CurrentFace => Math.Clamp(FaceTabs.SelectedIndex, 0, 5);

    private void RefreshEditor()
    {
        for (var face = 0; face < FaceTabs.Items.Count; face++)
        {
            if (FaceTabs.Items[face] is not TabItem { Content: ScrollViewer { Content: UniformGrid grid } }) continue;
            foreach (var button in grid.Children.OfType<Button>())
            {
                var cell = (CellAddress)button.Tag;
                var color = _draft.GetCell(cell.Face, cell.Row, cell.Column);
                button.Background = ColorBrushes[color];
                button.BorderBrush = color == _selectedColor ? Brushes.White : Brushes.Black;
            }
        }
        var summary = _draft.Summarize();
        SelectedColorText.Text = $"Selected color: {_selectedColor}";
        CountText.Text = string.Join(Environment.NewLine, Enumerable.Range(1, 6).Select(color =>
            $"{color}: {summary.ColorCounts[color],4} / {_draft.Size * _draft.Size}")) + $"\nempty: {summary.EmptyCells}";
        GuidanceText.Text = summary.OrientationGuidance;
        SummaryText.Text = $"N={_draft.Size}; {(summary.BasicCountsValid ? "basic counts valid" : "draft incomplete/imbalanced")}";
        ApplyButton.IsEnabled = summary.BasicCountsValid;
        UndoButton.IsEnabled = _draft.CanUndo;
        RedoButton.IsEnabled = _draft.CanRedo;
    }

    private static Brush BrushFrom(string value)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
        brush.Freeze();
        return brush;
    }

    private readonly record struct CellAddress(int Face, int Row, int Column);
}
