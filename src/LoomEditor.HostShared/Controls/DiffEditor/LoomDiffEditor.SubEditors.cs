using LoomEditor.View.Features;
using LoomEditor.View.Features.Folding;

#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#endif

// What the diff editor sets on its two editors. Monaco: `diffEditorEditors.ts` `_adjustOptionsForSubEditor`.
public sealed partial class LoomDiffEditor
{
    /// <summary>The features of the two editors: the default ones plus the diff navigation, without folding (Monaco turns folding off in a diff editor).</summary>
    private IReadOnlyList<Func<IEditorFeature>>? _subEditorFeatures;

    // What only one host sets on a sub-editor (WinUI turns the overview ruler off).
    static partial void ConfigureHostEditor(LoomEditor editor);

    private List<Func<IEditorFeature>> CreateSubEditorFeatures()
    {
        // Created once to recognise the folding feature by type: the factories are the only list there is.
        List<Func<IEditorFeature>> features = [];
        foreach (Func<IEditorFeature> factory in EditorFeatures.ForDiffEditor(this))
        {
            if (factory() is not FoldingFeature)
            {
                features.Add(factory);
            }
        }

        return features;
    }

    // Everything else the sub-editors get from this control. Safe to call again: it only assigns properties.
    private void ConfigureEditors()
    {
        if (_original is not { } original || _modified is not { } modified)
        {
            return;
        }

        foreach (LoomEditor editor in new[] { original, modified })
        {
            editor.Features = _subEditorFeatures ??= CreateSubEditorFeatures();
            editor.IsInDiffEditor = true;
            editor.MinimapEnabled = false;
            ConfigureHostEditor(editor);
            // Monaco: sticky scroll is turned off for both editors only while unchanged regions are hidden.
            editor.StickyScrollEnabled = !HideUnchangedRegions;
            editor.LineHeight = _lineHeight;
        }

        original.IsReadOnly = !OriginalEditable;
        modified.IsReadOnly = IsReadOnly;
    }
}
