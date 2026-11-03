using Avalonia;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls;

// Monaco: the `folding` and `lineDecorationsWidth` editor options.
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="FoldingEnabled"/> styled property.</summary>
    public static readonly StyledProperty<bool> FoldingEnabledProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(FoldingEnabled), true);

    /// <summary>Identifies the <see cref="LineDecorationsWidth"/> styled property.</summary>
    public static readonly StyledProperty<int> LineDecorationsWidthProperty =
        AvaloniaProperty.Register<LoomEditor, int>(nameof(LineDecorationsWidth), 10);

    /// <summary>
    /// Whether the text can be folded (Monaco: <c>folding</c>). Defaults to <see langword="true"/>. When off, the folding controls and their room in the
    /// gutter are gone. With <see cref="LineNumbersEnabled"/> off and <see cref="LineDecorationsWidth"/> at 0, the margin disappears.
    /// </summary>
    public bool FoldingEnabled
    {
        get => GetValue(FoldingEnabledProperty);
        set => SetValue(FoldingEnabledProperty, value);
    }

    /// <summary>
    /// The width in pixels of the line decorations column, between the line numbers and the text (Monaco: <c>lineDecorationsWidth</c>). Defaults to 10.
    /// The room of the folding controls comes on top while <see cref="FoldingEnabled"/> is on.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int LineDecorationsWidth
    {
        get => GetValue(LineDecorationsWidthProperty);
        set => SetValue(LineDecorationsWidthProperty, value);
    }

    private static void InitializeMarginProperties()
    {
        FoldingEnabledProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.ApplyFoldingEnabled());
        LineDecorationsWidthProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnLineDecorationsWidthChanged(e));
    }

    private void OnLineDecorationsWidthChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if ((int)e.NewValue! < 0)
        {
            SetCurrentValue(LineDecorationsWidthProperty, (int)e.OldValue!);
            throw new ArgumentOutOfRangeException(nameof(e), e.NewValue, "The line decorations width must not be negative.");
        }

        ApplyHostLayoutOptions();
    }

    // The folding feature exists only while the control is attached: OnAttachedToVisualTree applies the property again.
    private void ApplyFoldingEnabled()
    {
        ApplyHostLayoutOptions();
        if (_context.FoldingFeature is { } folding)
        {
            folding.SetOptions(folding.Options with { Folding = FoldingEnabled });
        }
    }

    // The WinUI twin lives in LoomEditor.HostHooks.cs and also sets the overview ruler and the glyph margin: P15-16 moves it there.
    private void ApplyHostLayoutOptions()
    {
        EditorLayoutOptions options = _viewModel.LayoutOptions;
        _viewModel.SetOptions(options with { LineNumbers = LineNumbersEnabled, Folding = FoldingEnabled, LineDecorationsWidth = LineDecorationsWidth }, _viewModel.ViewLayoutOptions);
    }
}
