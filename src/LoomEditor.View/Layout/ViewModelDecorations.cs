// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelDecorations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;

namespace LoomEditor.View.Layout;

/// <summary>The decorations of one editor, in view coordinates, with a cache for the last viewport (Monaco: <c>ViewModelDecorations</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class ViewModelDecorations
{
    private readonly int _editorId;
    private readonly ViewModelLinesFromProjectedModel _lines;
    private readonly InlineModelDecorationsComputer _inlineDecorationsComputer;
    private ViewDecorationsCollection? _cachedModelDecorationsResolver;
    private Range? _cachedModelDecorationsResolverViewRange;

    public ViewModelDecorations(int editorId, TextModel model, ViewModelLinesFromProjectedModel lines, ICoordinatesConverter coordinatesConverter)
    {
        _editorId = editorId;
        _lines = lines;
        _inlineDecorationsComputer = new InlineModelDecorationsComputer(GetModelDecorations, model, coordinatesConverter);
    }

    public void Dispose()
    {
        _inlineDecorationsComputer.Reset();
        ClearCachedModelDecorationsResolver();
    }

    public void Reset()
    {
        _inlineDecorationsComputer.Reset();
        ClearCachedModelDecorationsResolver();
    }

    public void OnModelDecorationsChanged()
    {
        _inlineDecorationsComputer.OnModelDecorationsChanged();
        ClearCachedModelDecorationsResolver();
    }

    public void OnLineMappingChanged()
    {
        _inlineDecorationsComputer.OnLineMappingChanged();
        ClearCachedModelDecorationsResolver();
    }

    /// <summary>The decorations of a view range that may be drawn in the minimap (bracket colors excluded).</summary>
    public IReadOnlyList<ViewModelDecoration> GetMinimapDecorationsInRange(Range range) =>
        _inlineDecorationsComputer.GetDecorations(range, onlyMinimapDecorations: true).Decorations;

    /// <summary>The decorations of a view range, cached for the last range.</summary>
    public ViewDecorationsCollection GetDecorationsViewportData(Range viewRange)
    {
        bool cacheIsValid = _cachedModelDecorationsResolver is not null && viewRange == _cachedModelDecorationsResolverViewRange;
        if (!cacheIsValid)
        {
            _cachedModelDecorationsResolver = _inlineDecorationsComputer.GetDecorations(viewRange);
            _cachedModelDecorationsResolverViewRange = viewRange;
        }

        return _cachedModelDecorationsResolver!;
    }

    /// <summary>The decorations of one view line, whole line.</summary>
    public ViewDecorationsCollection GetDecorationsOnLine(int lineNumber)
    {
        var range = new Range(lineNumber, _lines.GetViewLineMinColumn(lineNumber), lineNumber, _lines.GetViewLineMaxColumn(lineNumber));
        return _inlineDecorationsComputer.GetDecorations(range);
    }

    // Monaco: _linesCollection.getDecorationsInRange(viewRange, editorId, filterValidationDecorations, filterFontDecorations, ...).
    // filterValidationDecorations is false: the renderValidationDecorations option is not ported.
    private IReadOnlyList<LoomEditor.Core.Decorations.ModelDecoration> GetModelDecorations(Range viewRange, bool onlyMinimapDecorations) =>
        _lines.GetDecorationsInRange(viewRange, _editorId, filterOutValidation: false, filterFontDecorations: false, onlyMinimapDecorations);

    private void ClearCachedModelDecorationsResolver()
    {
        _cachedModelDecorationsResolver = null;
        _cachedModelDecorationsResolverViewRange = null;
    }
}
