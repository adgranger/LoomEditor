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
}
