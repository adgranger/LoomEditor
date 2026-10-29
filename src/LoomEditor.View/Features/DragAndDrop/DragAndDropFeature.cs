// Ported from microsoft/vscode: src/vs/editor/contrib/dnd/browser/dnd.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.DragAndDrop;

/// <summary>Drag and drop of the selection with the mouse (the controller does the work).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DragAndDropFeature : IEditorFeature
{
    private DragAndDropController? _controller;

    /// <inheritdoc />
    public string Id => "loom.dragAndDrop";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context is EditorContext editor)
        {
            _controller = new DragAndDropController(editor);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _controller?.Dispose();
        _controller = null;
    }
}
