using Avalonia;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls;

// The minimap. Monaco: the `minimap.enabled`, `minimap.side` and `minimap.renderCharacters` editor options.
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="MinimapEnabled"/> styled property.</summary>
    public static readonly StyledProperty<bool> MinimapEnabledProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(MinimapEnabled), true);

    /// <summary>Identifies the <see cref="MinimapSide"/> styled property.</summary>
    public static readonly StyledProperty<EditorMinimapSide> MinimapSideProperty =
        AvaloniaProperty.Register<LoomEditor, EditorMinimapSide>(nameof(MinimapSide), EditorMinimapSide.Right);

    /// <summary>Identifies the <see cref="MinimapRenderCharacters"/> styled property.</summary>
    public static readonly StyledProperty<bool> MinimapRenderCharactersProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(MinimapRenderCharacters), true);

    /// <summary>Whether the minimap is shown. Defaults to <see langword="true"/>.</summary>
    public bool MinimapEnabled
    {
        get => GetValue(MinimapEnabledProperty);
        set => SetValue(MinimapEnabledProperty, value);
    }

    /// <summary>The side of the editor where the minimap is shown. Defaults to <see cref="EditorMinimapSide.Right"/>.</summary>
    public EditorMinimapSide MinimapSide
    {
        get => GetValue(MinimapSideProperty);
        set => SetValue(MinimapSideProperty, value);
    }

    /// <summary>Whether the minimap draws the actual characters instead of color blocks. Defaults to <see langword="true"/>.</summary>
    public bool MinimapRenderCharacters
    {
        get => GetValue(MinimapRenderCharactersProperty);
        set => SetValue(MinimapRenderCharactersProperty, value);
    }

    private static void InitializeMinimapProperties()
    {
        MinimapEnabledProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.OnMinimapChanged());
        MinimapSideProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.OnMinimapChanged());
        MinimapRenderCharactersProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.OnMinimapChanged());
    }

    private void OnMinimapChanged()
    {
        ViewModel viewModel = _viewModel;
        EditorMinimapOptions minimap = viewModel.LayoutOptions.Minimap with
        {
            Enabled = MinimapEnabled,
            Side = MinimapSide == EditorMinimapSide.Left ? View.Layout.MinimapSide.Left : View.Layout.MinimapSide.Right,
            RenderCharacters = MinimapRenderCharacters,
        };
        viewModel.SetOptions(viewModel.LayoutOptions with { Minimap = minimap }, viewModel.ViewLayoutOptions);
    }
}
