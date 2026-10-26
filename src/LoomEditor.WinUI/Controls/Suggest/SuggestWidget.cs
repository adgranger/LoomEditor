using LoomEditor.View.Suggest;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Suggest;

/// <summary>
/// The completion list (Monaco: <c>SuggestWidget</c>, look of <c>suggest.css</c>). Code only: no XAML file, no bindings, no
/// <c>ListView</c>; the <see cref="SuggestListState.MaxVisibleRows"/> row elements are created once and refilled by <see cref="Refresh"/>.
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
        Visibility = Visibility.Collapsed,
    };

    private SuggestListState? _state;

    public SuggestWidget()
    {
        IsTabStop = false;
        AllowFocusOnInteraction = false;
        Visibility = Visibility.Collapsed;
        Width = WidgetWidth;

        var grid = new Grid();
        for (int i = 0; i < _rows.Length; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(RowHeight) });
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
        Content = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Background = _background,
            BorderBrush = _border,
            Child = new StackPanel { Orientation = Orientation.Vertical, Children = { grid, _details } },
        };
        PointerWheelChanged += OnPointerWheelChanged;
    }

    /// <summary>Raised with the visible row index (0-based) when a row is clicked.</summary>
    public event Action<int>? RowPressed;

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (_state is null)
        {
            return;
        }

        int delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        _state.ScrollBy(delta > 0 ? -3 : 3);
        e.Handled = true;
    }

    /// <summary>The elements of one row: a badge, the label with its details, and the description.</summary>
    private sealed class SuggestRow
    {
        public Grid Root { get; } = new() { Height = RowHeight };

        public Border Badge { get; } = new() { Width = 24 };

        public TextBlock Glyph { get; } = new() { FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

        public TextBlock Label { get; } = new() { IsTextSelectionEnabled = false, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 4, 0) };

        public TextBlock Description { get; } = new() { IsTextSelectionEnabled = false, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), HorizontalAlignment = HorizontalAlignment.Right };

        public SuggestRow()
        {
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Badge.Child = Glyph;
            Grid.SetColumn(Label, 1);
            Grid.SetColumn(Description, 2);
            Root.Children.Add(Badge);
            Root.Children.Add(Label);
            Root.Children.Add(Description);
            Root.Visibility = Visibility.Collapsed;
        }
    }
}
