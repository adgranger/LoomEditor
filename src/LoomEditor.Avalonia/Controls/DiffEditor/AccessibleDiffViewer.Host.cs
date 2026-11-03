using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using LoomEditor.Themes;
using LoomEditor.View.Accessibility;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.Avalonia.Controls.DiffEditor;

/// <summary>
/// The accessible diff viewer: a list that shows one group of changes as rows, covering both editors (Monaco: <c>View</c> of
/// <c>accessibleDiffViewer.ts</c>). Code only, no bindings: the rows are <see cref="ListBoxItem"/>s built from <see cref="DiffViewerRowItem"/>s.
/// </summary>
/// <remarks>
/// Behavior differences: the lines are plain text without any syntax color (like Monaco), in the font of the modified editor; the
/// arrows are handled before the <see cref="ListBox"/> sees them, the model decides and the list follows its <c>Changed</c> event.
/// Threading: UI thread only.
/// </remarks>
internal sealed partial class AccessibleDiffViewer : UserControl
{
    private const double NumberColumnWidth = 48;
    private const double SignColumnWidth = 24;

    private readonly FontFamily _fontFamily;
    private readonly ListBox _list = new() { SelectionMode = SelectionMode.Single, Focusable = false };
    private readonly List<ListBoxItem> _items = [];

    /// <summary>Creates the viewer over a model and shows its current group.</summary>
    /// <param name="model">The model; the viewer follows its <c>Changed</c> event.</param>
    /// <param name="host">The editors, for the text of the lines.</param>
    /// <param name="theme">The colors.</param>
    /// <param name="fontFamily">The font of the modified editor.</param>
    /// <param name="fontSize">The size of the font of the modified editor.</param>
    /// <param name="tabSize">The tab size of the modified editor.</param>
    public AccessibleDiffViewer(AccessibleDiffViewerModel model, IAccessibleDiffViewerHost host, EditorTheme theme, FontFamily fontFamily, double fontSize, int tabSize)
    {
        _model = model;
        _host = host;
        _theme = theme;
        _fontFamily = fontFamily;
        _fontSize = fontSize;
        _tabSize = tabSize;

        Focusable = false;
        var close = new Button { Content = "✕", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(4) };
        AutomationProperties.SetName(close, "Close");
        close.Click += (_, _) => _model.Close();

        AutomationProperties.SetName(_list, AccessibilityMessages.DiffViewerListLabel);
        _list.SelectionChanged += OnSelectionChanged;

        var grid = new Grid { Background = Brush(theme.GetColor("editor.background")) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(close);
        Grid.SetRow(_list, 1);
        grid.Children.Add(_list);
        Content = grid;

        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        _model.Changed += OnModelChanged;
        Render();
    }

    /// <summary>The rows of the current group, for the tests.</summary>
    internal IReadOnlyList<ListBoxItem> Rows => _items;

    // Monaco: _createRow. The classes line-insert / line-delete are the background of the row, char-insert / char-delete the one of the numbers.
    private Grid CreateRow(DiffViewerRowItem row)
    {
        LineType type = row.Element.Type;
        var grid = new Grid
        {
            Background = type switch
            {
                LineType.Added => Brush(_theme.GetColor("diffEditor.insertedLineBackground")),
                LineType.Deleted => Brush(_theme.GetColor("diffEditor.removedLineBackground")),
                _ => null,
            },
        };

        if (type == LineType.Header)
        {
            grid.Children.Add(Text(row.Text, bold: true));
            return grid;
        }

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(NumberColumnWidth) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(NumberColumnWidth) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(SignColumnWidth) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        IBrush? numberBackground = type switch
        {
            LineType.Added => Brush(_theme.GetColor("diffEditor.insertedTextBackground")),
            LineType.Deleted => Brush(_theme.GetColor("diffEditor.removedTextBackground")),
            _ => null,
        };
        IBrush? numberForeground = Brush(_theme.GetColor("editorLineNumber.foreground"));
        AddCell(grid, 0, row.OriginalNumber, numberForeground, numberBackground, HorizontalAlignment.Right);
        AddCell(grid, 1, row.ModifiedNumber, numberForeground, numberBackground, HorizontalAlignment.Right);
        AddCell(grid, 2, row.Sign, Brush(_theme.GetColor("editor.foreground")), null, HorizontalAlignment.Center);
        AddCell(grid, 3, row.Text, Brush(_theme.GetColor("editor.foreground")), null, HorizontalAlignment.Left);
        return grid;
    }

    private void AddCell(Grid grid, int column, string text, IBrush? foreground, IBrush? background, HorizontalAlignment alignment)
    {
        var cell = new Border { Background = background, Child = Text(text, foreground: foreground, alignment: alignment) };
        Grid.SetColumn(cell, column);
        grid.Children.Add(cell);
    }

    private TextBlock Text(string text, IBrush? foreground = null, bool bold = false, HorizontalAlignment alignment = HorizontalAlignment.Left) =>
        new()
        {
            Text = text,
            FontFamily = _fontFamily,
            FontSize = _fontSize,
            FontWeight = bold ? FontWeight.Bold : FontWeight.Normal,
            Foreground = foreground ?? Brush(_theme.GetColor("editor.foreground")),
            HorizontalAlignment = alignment,
            Margin = new Thickness(4, 0, 4, 0),
        };

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_isSyncing && _list.SelectedIndex is >= 0 and var index && _model.CurrentGroup is { } group && index < group.Lines.Count)
        {
            _model.GoToLine(group.Lines[index]);
        }
    }

    // Monaco: the keydown listener of the view; Ctrl and Alt variants of the arrows, any modifier of Escape.
    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        bool alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        bool arrowModifiers = !shift && !(ctrl && alt);
        bool plain = !ctrl && !alt && !shift;

        switch (e.Key)
        {
            case Key.Down when arrowModifiers:
                _model.GoToNextLine();
                break;
            case Key.Up when arrowModifiers:
                _model.GoToPreviousLine();
                break;
            case Key.Escape:
                _model.Close();
                break;
            case Key.Space or Key.Enter when plain:
                _model.RevealCurrentElementInEditor();
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private static SolidColorBrush? Brush(ThemeColor? color) =>
        color is { } c ? new SolidColorBrush(global::Avalonia.Media.Color.FromArgb(c.A, c.R, c.G, c.B)) : null;

    private void ClearRows()
    {
        _items.Clear();
        _list.Items.Clear();
    }

    private void AddRow(DiffViewerRowItem row, Action onFocus)
    {
        var item = new ListBoxItem { Content = CreateRow(row), Padding = new Thickness(0), MinHeight = 0, HorizontalContentAlignment = HorizontalAlignment.Stretch };
        AutomationProperties.SetName(item, row.AutomationName);
        item.GotFocus += (_, _) => onFocus();
        _items.Add(item);
        _list.Items.Add(item);
    }

    private void ShowRow(int index)
    {
        _list.SelectedIndex = index;
        _list.ScrollIntoView(index);
        _items[index].Focus(NavigationMethod.Directional);
    }
}
