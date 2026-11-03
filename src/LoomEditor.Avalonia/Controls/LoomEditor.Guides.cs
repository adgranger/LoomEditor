using Avalonia;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls;

// Indent and bracket pair guides. Monaco: the `guides.indentation` and `guides.bracketPairs` editor options.
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="RenderIndentGuides"/> styled property.</summary>
    public static readonly StyledProperty<bool> RenderIndentGuidesProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(RenderIndentGuides), true);

    /// <summary>Identifies the <see cref="BracketPairGuides"/> styled property.</summary>
    public static readonly StyledProperty<EditorBracketPairGuides> BracketPairGuidesProperty =
        AvaloniaProperty.Register<LoomEditor, EditorBracketPairGuides>(nameof(BracketPairGuides), EditorBracketPairGuides.Off);

    /// <summary>Whether the indent guides are drawn. Defaults to <see langword="true"/>.</summary>
    public bool RenderIndentGuides
    {
        get => GetValue(RenderIndentGuidesProperty);
        set => SetValue(RenderIndentGuidesProperty, value);
    }

    /// <summary>Which bracket pair guides are drawn. Defaults to <see cref="EditorBracketPairGuides.Off"/>.</summary>
    public EditorBracketPairGuides BracketPairGuides
    {
        get => GetValue(BracketPairGuidesProperty);
        set => SetValue(BracketPairGuidesProperty, value);
    }

    private static void InitializeGuidesProperties()
    {
        RenderIndentGuidesProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.OnGuidesChanged());
        BracketPairGuidesProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.OnGuidesChanged());
    }

    private void OnGuidesChanged()
    {
        ViewModel viewModel = _viewModel;
        EditorGuidesOptions guides = viewModel.LayoutOptions.Guides with
        {
            Indentation = RenderIndentGuides,
            BracketPairs = BracketPairGuides switch
            {
                EditorBracketPairGuides.On => GuideMode.On,
                EditorBracketPairGuides.Active => GuideMode.Active,
                _ => GuideMode.Off,
            },
        };
        viewModel.SetOptions(viewModel.LayoutOptions with { Guides = guides }, viewModel.ViewLayoutOptions);
    }
}
