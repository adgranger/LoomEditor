// Ported from microsoft/vscode: src/vs/editor/contrib/fontZoom/browser/fontZoom.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.FontZoom;

/// <summary>Adds the font zoom commands of <see cref="FontZoomCommands"/>; they change <see cref="EditorContext.Zoom"/>.</summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class FontZoomFeature : IEditorFeature
{
    private readonly List<IDisposable> _registrations = [];

    /// <inheritdoc />
    public string Id => "loom.fontZoom";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context is not EditorContext editorContext)
        {
            throw new ArgumentException("The font zoom feature needs an EditorContext.", nameof(context));
        }

        EditorZoomState zoom = editorContext.Zoom;
        _registrations.Add(context.Commands.Register(new EditorCommand(FontZoomCommands.FontZoomIn, () => zoom.SetLevel(zoom.Level + 1))));
        _registrations.Add(context.Commands.Register(new EditorCommand(FontZoomCommands.FontZoomOut, () => zoom.SetLevel(zoom.Level - 1))));
        _registrations.Add(context.Commands.Register(new EditorCommand(FontZoomCommands.FontZoomReset, () => zoom.SetLevel(0))));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
    }
}
