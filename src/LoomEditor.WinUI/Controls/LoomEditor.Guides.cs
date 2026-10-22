using LoomEditor.View.Layout;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// Indent and bracket pair guides. Monaco: the `guides.indentation` and `guides.bracketPairs` editor options.
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="RenderIndentGuides"/> dependency property.</summary>
    public static readonly DependencyProperty RenderIndentGuidesProperty = DependencyProperty.Register(
        nameof(RenderIndentGuides), typeof(bool), typeof(LoomEditor), new PropertyMetadata(true, OnGuidesChanged));

    /// <summary>Identifies the <see cref="BracketPairGuides"/> dependency property.</summary>
    public static readonly DependencyProperty BracketPairGuidesProperty = DependencyProperty.Register(
        nameof(BracketPairGuides), typeof(EditorBracketPairGuides), typeof(LoomEditor), new PropertyMetadata(EditorBracketPairGuides.Off, OnGuidesChanged));

    /// <summary>Whether the indent guides are drawn. Defaults to <see langword="true"/>.</summary>
    public bool RenderIndentGuides
    {
        get => (bool)GetValue(RenderIndentGuidesProperty);
        set => SetValue(RenderIndentGuidesProperty, value);
    }

    /// <summary>Which bracket pair guides are drawn. Defaults to <see cref="EditorBracketPairGuides.Off"/>.</summary>
    public EditorBracketPairGuides BracketPairGuides
    {
        get => (EditorBracketPairGuides)GetValue(BracketPairGuidesProperty);
        set => SetValue(BracketPairGuidesProperty, value);
    }

    private static void OnGuidesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var editor = (LoomEditor)sender;
        ViewModel viewModel = editor._viewModel;
        EditorGuidesOptions guides = viewModel.LayoutOptions.Guides with
        {
            Indentation = editor.RenderIndentGuides,
            BracketPairs = editor.BracketPairGuides switch
            {
                EditorBracketPairGuides.On => GuideMode.On,
                EditorBracketPairGuides.Active => GuideMode.Active,
                _ => GuideMode.Off,
            },
        };
        viewModel.SetOptions(viewModel.LayoutOptions with { Guides = guides }, viewModel.ViewLayoutOptions);
    }
}
