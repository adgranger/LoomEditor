using LoomEditor.View.Layout;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// The minimap. Monaco: the `minimap.enabled`, `minimap.side` and `minimap.renderCharacters` editor options.
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="MinimapEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty MinimapEnabledProperty = DependencyProperty.Register(
        nameof(MinimapEnabled), typeof(bool), typeof(LoomEditor), new PropertyMetadata(true, OnMinimapChanged));

    /// <summary>Identifies the <see cref="MinimapSide"/> dependency property.</summary>
    public static readonly DependencyProperty MinimapSideProperty = DependencyProperty.Register(
        nameof(MinimapSide), typeof(EditorMinimapSide), typeof(LoomEditor), new PropertyMetadata(EditorMinimapSide.Right, OnMinimapChanged));

    /// <summary>Identifies the <see cref="MinimapRenderCharacters"/> dependency property.</summary>
    public static readonly DependencyProperty MinimapRenderCharactersProperty = DependencyProperty.Register(
        nameof(MinimapRenderCharacters), typeof(bool), typeof(LoomEditor), new PropertyMetadata(true, OnMinimapChanged));

    /// <summary>Whether the minimap is shown. Defaults to <see langword="true"/>.</summary>
    public bool MinimapEnabled
    {
        get => (bool)GetValue(MinimapEnabledProperty);
        set => SetValue(MinimapEnabledProperty, value);
    }

    /// <summary>The side of the editor where the minimap is shown. Defaults to <see cref="EditorMinimapSide.Right"/>.</summary>
    public EditorMinimapSide MinimapSide
    {
        get => (EditorMinimapSide)GetValue(MinimapSideProperty);
        set => SetValue(MinimapSideProperty, value);
    }

    /// <summary>Whether the minimap draws the actual characters instead of color blocks. Defaults to <see langword="true"/>.</summary>
    public bool MinimapRenderCharacters
    {
        get => (bool)GetValue(MinimapRenderCharactersProperty);
        set => SetValue(MinimapRenderCharactersProperty, value);
    }

    private static void OnMinimapChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var editor = (LoomEditor)sender;
        ViewModel viewModel = editor._viewModel;
        EditorMinimapOptions minimap = viewModel.LayoutOptions.Minimap with
        {
            Enabled = editor.MinimapEnabled,
            Side = editor.MinimapSide == EditorMinimapSide.Left ? View.Layout.MinimapSide.Left : View.Layout.MinimapSide.Right,
            RenderCharacters = editor.MinimapRenderCharacters,
        };
        viewModel.SetOptions(viewModel.LayoutOptions with { Minimap = minimap }, viewModel.ViewLayoutOptions);
    }
}
