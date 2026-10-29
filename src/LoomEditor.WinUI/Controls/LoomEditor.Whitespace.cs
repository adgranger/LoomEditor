using LoomEditor.View.Layout;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// Whitespace rendering. Monaco: the `renderWhitespace` editor option (its default is `selection`, ours is `None`).
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="RenderWhitespace"/> dependency property.</summary>
    public static readonly DependencyProperty RenderWhitespaceProperty = DependencyProperty.Register(
        nameof(RenderWhitespace), typeof(RenderWhitespace), typeof(LoomEditor), new PropertyMetadata(View.Layout.RenderWhitespace.None, OnRenderWhitespaceChanged));

    /// <summary>Which spaces and tabs are drawn as dots and arrows. Defaults to <see cref="View.Layout.RenderWhitespace.None"/>.</summary>
    public RenderWhitespace RenderWhitespace
    {
        get => (RenderWhitespace)GetValue(RenderWhitespaceProperty);
        set => SetValue(RenderWhitespaceProperty, value);
    }

    private static void OnRenderWhitespaceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var editor = (LoomEditor)sender;
        if (editor._renderer is { } renderer)
        {
            renderer.RenderWhitespace = (RenderWhitespace)e.NewValue;
        }

        editor._canvas?.Invalidate();
    }

    /// <summary>Identifies the <see cref="RenderControlCharacters"/> dependency property.</summary>
    public static readonly DependencyProperty RenderControlCharactersProperty = DependencyProperty.Register(
        nameof(RenderControlCharacters), typeof(bool), typeof(LoomEditor), new PropertyMetadata(true, OnRenderControlCharactersChanged));

    /// <summary>Whether control characters are drawn as pictures on a dark red cell (Monaco: <c>renderControlCharacters</c>). Defaults to <see langword="true"/>.</summary>
    public bool RenderControlCharacters
    {
        get => (bool)GetValue(RenderControlCharactersProperty);
        set => SetValue(RenderControlCharactersProperty, value);
    }

    private static void OnRenderControlCharactersChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var editor = (LoomEditor)sender;
        if (editor._renderer is { } renderer)
        {
            renderer.RenderControlCharacters = (bool)e.NewValue;
        }

        editor._canvas?.Invalidate();
    }
}
