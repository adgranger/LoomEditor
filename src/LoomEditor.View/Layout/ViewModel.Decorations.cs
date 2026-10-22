// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelImpl.ts (_decorations, onDidChangeDecorations handler, getDecorationsInViewport, getViewportViewLineRenderingData)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model.Events;
using LoomEditor.Themes;
using LoomEditor.View.Events;

namespace LoomEditor.View.Layout;

// Decorations: the view model tells the renderer which decorations to draw, in view coordinates. Monaco's outgoing
// ModelDecorationsChangedEvent is not ported (collections listen to the model, P5-06).
internal sealed partial class ViewModel
{
    private ViewModelDecorations _decorations = null!;

    private void AttachDecorations()
    {
        _decorations = new ViewModelDecorations(EditorId, _model, _lines, CoordinatesConverter);
        _model.DecorationsChanged += OnModelDecorationsChanged;
    }

    private void DetachDecorations()
    {
        _model.DecorationsChanged -= OnModelDecorationsChanged;
        _decorations.Dispose();
    }

    /// <summary>The decorations of a visible range, in view coordinates (Monaco: <c>getDecorationsInViewport</c>).</summary>
    public IReadOnlyList<ViewModelDecoration> GetDecorationsInViewport(Range visibleRange) =>
        _decorations.GetDecorationsViewportData(visibleRange).Decorations;

    /// <summary>The inline decorations of a line of a visible range (Monaco: the <c>inlineDecorations</c> of <c>getViewportViewLineRenderingData</c>).</summary>
    public IReadOnlyList<InlineDecoration> GetViewportInlineDecorations(Range visibleRange, int lineNumber)
    {
        ViewDecorationsCollection collection = _decorations.GetDecorationsViewportData(visibleRange);
        IReadOnlyList<InlineDecoration> modelDecorations = collection.InlineDecorations[lineNumber - visibleRange.StartLineNumber];

        // Monaco: the view line's own inline decorations (injected text) come after the model ones.
        if (_lines.GetViewLineData(lineNumber).InlineDecorations is { Count: > 0 } injected)
        {
            return [.. modelDecorations, .. injected];
        }

        return modelDecorations;
    }

    /// <summary>The decorations of a view range that may be drawn in the minimap (Monaco: <c>getMinimapDecorationsInRange</c>).</summary>
    public IReadOnlyList<ViewModelDecoration> GetMinimapDecorationsInRange(Range range) =>
        _decorations.GetMinimapDecorationsInRange(range);

    /// <summary>The rendering data of the view lines the minimap draws (Monaco: <c>getMinimapLinesRenderingData</c>).</summary>
    public MinimapLinesRenderingData GetMinimapLinesRenderingData(int startLineNumber, int endLineNumber, bool[] needed) =>
        new(_model.Options.TabSize, _lines.GetViewLinesData(startLineNumber, endLineNumber, needed));

    /// <summary>The overview ruler decorations in view line numbers, grouped by color (Monaco: <c>getAllOverviewRulerDecorations</c>).</summary>
    public List<OverviewRulerDecorationsGroup> GetAllOverviewRulerDecorations(EditorTheme theme)
    {
        var result = new OverviewRulerDecorations();
        foreach (ModelDecoration decoration in _model.GetOverviewRulerDecorations(EditorId))
        {
            ModelDecorationOverviewRulerOptions? opts = decoration.Options.OverviewRuler;
            if (opts is null)
            {
                continue;
            }

            if (DecorationColorResolver.Resolve(opts.Color, opts.DarkColor, theme) is not { } color)
            {
                continue;
            }

            Range range = decoration.Range;
            int viewStart = CoordinatesConverter.GetViewLineNumberOfModelPosition(range.StartLineNumber, range.StartColumn);
            int viewEnd = CoordinatesConverter.GetViewLineNumberOfModelPosition(range.EndLineNumber, range.EndColumn);
            result.Accept(color, decoration.Options.ZIndex, viewStart, viewEnd, (int)opts.Position);
        }

        return result.AsArray;
    }

    private void OnModelDecorationsChanged(object? sender, ModelDecorationsChangedEventArgs e)
    {
        _decorations.OnModelDecorationsChanged();
        EventDispatcher.EmitSingleViewEvent(new ViewDecorationsChangedEvent(e));
    }
}
