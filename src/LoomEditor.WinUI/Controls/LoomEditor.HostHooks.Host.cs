using LoomEditor.Core.Decorations;
using LoomEditor.View.Features;
using LoomEditor.View.Layout;

namespace LoomEditor.WinUI.Controls;

// What a host of this control needs: the public members for applications (phase 13) and the internal ones for the diff
// editor. Monaco: the `ICodeEditor` members used by `DiffEditorEditors`.
public sealed partial class LoomEditor
{
    private bool _overviewRulerEnabled = true;

    internal IReadOnlyList<ViewWhitespaceViewportData> GetWhitespaceViewportData() => _viewModel.ViewLayout.GetWhitespaceViewportData();

    internal void InvalidateCanvas() => _canvas?.Invalidate();

    /// <summary>Creates a collection of decorations owned by the caller.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal EditorDecorationsCollection CreateModelDecorationsCollection(IReadOnlyList<ModelDeltaDecoration>? decorations = null) =>
        _context.CreateDecorationsCollection(decorations);

    /// <summary>Whether the decorations overview ruler of the editor is on. The diff editor draws its own.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal bool OverviewRulerEnabled
    {
        get => _overviewRulerEnabled;
        set
        {
            _overviewRulerEnabled = value;
            ApplyHostLayoutOptions();
        }
    }

    private void ApplyHostLayoutOptions()
    {
        EditorLayoutOptions options = _viewModel.LayoutOptions;
        EditorOverviewRulerOptions ruler = options.OverviewRuler with { Lanes = _overviewRulerEnabled ? new EditorOverviewRulerOptions().Lanes : 0 };
        _viewModel.SetOptions(options with { OverviewRuler = ruler, GlyphMargin = _glyphMarginLaneCount > 0, LineNumbers = LineNumbersEnabled, Folding = FoldingEnabled, LineDecorationsWidth = LineDecorationsWidth }, _viewModel.ViewLayoutOptions);
    }
}
