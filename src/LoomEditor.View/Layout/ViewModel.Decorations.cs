// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelImpl.ts (_decorations, onDidChangeDecorations handler, getDecorationsInViewport, getViewportViewLineRenderingData)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Events;
using LoomEditor.View.Events;

namespace LoomEditor.View.Layout;

// Decorations: the view model tells the renderer which decorations to draw, in view coordinates. Monaco's outgoing
// ModelDecorationsChangedEvent is not ported (collections listen to the model, P5-06).
internal sealed partial class ViewModel
{
    private ViewModelDecorations _decorations = null!;

    private void AttachDecorations()
    {
        _decorations = new ViewModelDecorations(EditorId, _model, CoordinatesConverter);
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
        return collection.InlineDecorations[lineNumber - visibleRange.StartLineNumber];
    }

    private void OnModelDecorationsChanged(object? sender, ModelDecorationsChangedEventArgs e)
    {
        _decorations.OnModelDecorationsChanged();
        EventDispatcher.EmitSingleViewEvent(new ViewDecorationsChangedEvent(e));
    }
}
