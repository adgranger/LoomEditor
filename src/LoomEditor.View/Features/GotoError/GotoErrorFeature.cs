// Ported from microsoft/vscode: src/vs/editor/contrib/gotoError/browser/gotoError.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Commands;

namespace LoomEditor.View.Features.GotoError;

/// <summary>Go to the next / previous problem: <c>F8</c>, <c>Shift+F8</c>, <c>Alt+F8</c>, <c>Shift+Alt+F8</c>; <c>Escape</c> ends it.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class GotoErrorFeature : IEditorFeature
{
    private readonly List<IDisposable> _registrations = [];
    private EditorContext? _editor;
    private MarkerController? _controller;

    /// <inheritdoc />
    public string Id => "loom.gotoError";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context is not EditorContext editor)
        {
            return;
        }

        _editor = editor;
        MarkerController controller = new(editor);
        _controller = controller;
        editor.MarkerController = controller;

        void Register(string id, Action run, Func<bool>? canRun = null) =>
            _registrations.Add(context.Commands.Register(new EditorCommand(id, run, canRun)));

        Register(GotoErrorCommands.Next, () => controller.Navigate(true));
        Register(GotoErrorCommands.Prev, () => controller.Navigate(false));
        Register(GotoErrorCommands.NextInFiles, () => controller.Navigate(true));
        Register(GotoErrorCommands.PrevInFiles, () => controller.Navigate(false));
        Register(GotoErrorCommands.Close, controller.Close, () => editor.MarkersNavigationVisible);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
        _controller?.Dispose();
        if (_editor is not null)
        {
            _editor.MarkerController = null;
        }

        _controller = null;
        _editor = null;
    }
}
