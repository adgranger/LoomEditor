using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using LoomEditor.View.Suggest;

namespace LoomEditor.Avalonia.Controls.Suggest;

/// <summary>
/// The completion list (Monaco: <c>SuggestWidget</c>, look of <c>suggest.css</c>). Code only: no AXAML file, no bindings, no
/// list control; the <see cref="SuggestListState.MaxVisibleRows"/> row controls are created once and refilled by <see cref="Refresh"/>, so a list of
/// any length only ever has that many rows (the virtualization of the WinUI twin).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class SuggestWidget : UserControl
{
    internal const double WidgetWidth = 430;
    internal const double RowHeight = 22;

    private readonly SolidColorBrush _background = new();
    private readonly SolidColorBrush _border = new();
    private readonly SolidColorBrush _foreground = new();
    private readonly SolidColorBrush _dim = new();
    private readonly SolidColorBrush _selectedBackground = new();
    private readonly SolidColorBrush _selectedForeground = new();
    private readonly SolidColorBrush _highlight = new();
    private readonly SolidColorBrush _focusHighlight = new();
    private readonly SuggestRow[] _rows = new SuggestRow[SuggestListState.MaxVisibleRows];
    private readonly TextBlock _details = new()
    {
        TextWrapping = TextWrapping.Wrap,
        TextTrimming = TextTrimming.CharacterEllipsis,
        MaxLines = 4,
        Margin = new Thickness(8, 4, 8, 4),
        IsVisible = false,
    };

    private SuggestListState? _state;

    public SuggestWidget()
    {
        Focusable = false;
        IsVisible = false;
        Width = WidgetWidth;

        var grid = new Grid();
        for (int i = 0; i < _rows.Length; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(RowHeight, GridUnitType.Pixel));
            _rows[i] = new SuggestRow();
            Grid.SetRow(_rows[i].Root, i);
            grid.Children.Add(_rows[i].Root);
            int slot = i;
            _rows[i].Root.PointerPressed += (_, e) =>
            {
                e.Handled = true;
                RowPressed?.Invoke(slot);
            };
        }

        _details.Foreground = _dim;
        var panel = new StackPanel { Orientation = Orientation.Vertical };
        panel.Children.Add(grid);
        panel.Children.Add(_details);
        Content = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Background = _background,
            BorderBrush = _border,
            Child = panel,
        };
        PointerWheelChanged += OnPointerWheelChanged;
    }

    /// <summary>Raised with the visible row index (0-based) when a row is clicked.</summary>
    public event Action<int>? RowPressed;

    /// <summary>The row controls that exist, for the tests.</summary>
    internal int RowControlCount => _rows.Length;

    /// <summary>The row controls that are shown, for the tests.</summary>
    internal int VisibleRowControlCount => _rows.Count(row => row.Root.IsVisible);

    /// <summary>The text of the label of a visible row, for the tests.</summary>
    internal string RowLabel(int slot) => string.Concat(_rows[slot].Label.Inlines!.OfType<global::Avalonia.Controls.Documents.Run>().Select(run => run.Text));

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_state is null)
        {
            return;
        }

        _state.ScrollBy(e.Delta.Y > 0 ? -3 : 3);
        e.Handled = true;
    }

    /// <summary>The controls of one row: a badge, the label with its details, and the description.</summary>
    private sealed class SuggestRow
    {
        public Grid Root { get; } = new() { Height = RowHeight, Background = Brushes.Transparent };

        public Border Badge { get; } = new() { Width = 24 };

        public TextBlock Glyph { get; } = new() { FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

        public TextBlock Label { get; } = new() { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 4, 0) };

        public TextBlock Description { get; } = new() { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), HorizontalAlignment = HorizontalAlignment.Right };

        public SuggestRow()
        {
            Root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            Root.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            Root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            Badge.Child = Glyph;
            Grid.SetColumn(Label, 1);
            Grid.SetColumn(Description, 2);
            Root.Children.Add(Badge);
            Root.Children.Add(Label);
            Root.Children.Add(Description);
            Root.IsVisible = false;
        }
    }
}
