using Avalonia;

namespace LoomEditor.Avalonia.Controls;

// Monaco: the `lineNumbers` editor option ('on' | 'off').
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="LineNumbersEnabled"/> styled property.</summary>
    public static readonly StyledProperty<bool> LineNumbersEnabledProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(LineNumbersEnabled), true);

    /// <summary>
    /// Whether the line numbers are drawn (Monaco: <c>lineNumbers: 'on' | 'off'</c>). Defaults to <see langword="true"/>. When off, the gutter keeps the
    /// folding controls and the line decorations.
    /// </summary>
    public bool LineNumbersEnabled
    {
        get => GetValue(LineNumbersEnabledProperty);
        set => SetValue(LineNumbersEnabledProperty, value);
    }

    private static void InitializeLineNumbersProperties() =>
        LineNumbersEnabledProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.ApplyHostLayoutOptions());
}
