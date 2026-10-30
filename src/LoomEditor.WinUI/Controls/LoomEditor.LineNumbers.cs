using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// Monaco: the `lineNumbers` editor option ('on' | 'off').
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="LineNumbersEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty LineNumbersEnabledProperty = DependencyProperty.Register(
        nameof(LineNumbersEnabled), typeof(bool), typeof(LoomEditor), new PropertyMetadata(true, (d, _) => ((LoomEditor)d).ApplyHostLayoutOptions()));

    /// <summary>
    /// Whether the line numbers are drawn (Monaco: <c>lineNumbers: 'on' | 'off'</c>). Defaults to <see langword="true"/>. When off, the gutter keeps the
    /// folding controls and the line decorations.
    /// </summary>
    public bool LineNumbersEnabled
    {
        get => (bool)GetValue(LineNumbersEnabledProperty);
        set => SetValue(LineNumbersEnabledProperty, value);
    }
}
