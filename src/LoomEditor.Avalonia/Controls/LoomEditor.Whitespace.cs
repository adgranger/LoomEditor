using Avalonia;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls;

// Whitespace rendering. Monaco: the `renderWhitespace` editor option (its default is `selection`, ours is `None`).
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="RenderWhitespace"/> styled property.</summary>
    public static readonly StyledProperty<RenderWhitespace> RenderWhitespaceProperty =
        AvaloniaProperty.Register<LoomEditor, RenderWhitespace>(nameof(RenderWhitespace), View.Layout.RenderWhitespace.None);

    /// <summary>Which spaces and tabs are drawn as dots and arrows. Defaults to <see cref="View.Layout.RenderWhitespace.None"/>.</summary>
    public RenderWhitespace RenderWhitespace
    {
        get => GetValue(RenderWhitespaceProperty);
        set => SetValue(RenderWhitespaceProperty, value);
    }

    /// <summary>Identifies the <see cref="RenderControlCharacters"/> styled property.</summary>
    public static readonly StyledProperty<bool> RenderControlCharactersProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(RenderControlCharacters), true);

    /// <summary>Whether control characters are drawn as pictures on a dark red cell (Monaco: <c>renderControlCharacters</c>). Defaults to <see langword="true"/>.</summary>
    public bool RenderControlCharacters
    {
        get => GetValue(RenderControlCharactersProperty);
        set => SetValue(RenderControlCharactersProperty, value);
    }

    private static void InitializeWhitespaceProperties()
    {
        RenderWhitespaceProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnRenderWhitespaceChanged((RenderWhitespace)e.NewValue!));
        RenderControlCharactersProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnRenderControlCharactersChanged((bool)e.NewValue!));
    }

    private void OnRenderWhitespaceChanged(RenderWhitespace value)
    {
        if (_renderer is { } renderer)
        {
            renderer.RenderWhitespace = value;
        }

        Invalidate();
    }

    private void OnRenderControlCharactersChanged(bool value)
    {
        if (_renderer is { } renderer)
        {
            renderer.RenderControlCharacters = value;
        }

        Invalidate();
    }
}
