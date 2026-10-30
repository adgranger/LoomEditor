using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// Monaco: the `folding` and `lineDecorationsWidth` editor options.
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="FoldingEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty FoldingEnabledProperty = DependencyProperty.Register(
        nameof(FoldingEnabled), typeof(bool), typeof(LoomEditor), new PropertyMetadata(true, (d, _) => ((LoomEditor)d).ApplyFoldingEnabled()));

    /// <summary>Identifies the <see cref="LineDecorationsWidth"/> dependency property.</summary>
    public static readonly DependencyProperty LineDecorationsWidthProperty = DependencyProperty.Register(
        nameof(LineDecorationsWidth), typeof(int), typeof(LoomEditor), new PropertyMetadata(10, OnLineDecorationsWidthChanged));

    /// <summary>
    /// Whether the text can be folded (Monaco: <c>folding</c>). Defaults to <see langword="true"/>. When off, the folding controls and their room in the
    /// gutter are gone. With <see cref="LineNumbersEnabled"/> off and <see cref="LineDecorationsWidth"/> at 0, the margin disappears.
    /// </summary>
    public bool FoldingEnabled
    {
        get => (bool)GetValue(FoldingEnabledProperty);
        set => SetValue(FoldingEnabledProperty, value);
    }

    /// <summary>
    /// The width in pixels of the line decorations column, between the line numbers and the text (Monaco: <c>lineDecorationsWidth</c>). Defaults to 10.
    /// The room of the folding controls comes on top while <see cref="FoldingEnabled"/> is on.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int LineDecorationsWidth
    {
        get => (int)GetValue(LineDecorationsWidthProperty);
        set => SetValue(LineDecorationsWidthProperty, value);
    }

    private static void OnLineDecorationsWidthChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var editor = (LoomEditor)sender;
        if ((int)e.NewValue < 0)
        {
            editor.SetValue(LineDecorationsWidthProperty, e.OldValue);
            throw new ArgumentOutOfRangeException(nameof(e), e.NewValue, "The line decorations width must not be negative.");
        }

        editor.ApplyHostLayoutOptions();
    }

    // The folding feature exists only while the control is loaded: OnLoaded applies the property again.
    private void ApplyFoldingEnabled()
    {
        ApplyHostLayoutOptions();
        if (_context.FoldingFeature is { } folding)
        {
            folding.SetOptions(folding.Options with { Folding = FoldingEnabled });
        }
    }
}
