// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetController2.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Commands;

namespace LoomEditor.View.Features.Snippet;

/// <summary>Creates the snippet controller and registers the snippet commands.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class SnippetFeature : IEditorFeature
{
    private readonly List<IDisposable> _registrations = [];
    private EditorContext? _editor;
    private SnippetController? _controller;

    /// <inheritdoc />
    public string Id => "loom.snippet";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context is not EditorContext editor)
        {
            return;
        }

        _editor = editor;
        SnippetController controller = new(editor);
        _controller = controller;
        editor.SnippetController = controller;

        void Register(string id, Action run, Func<bool> canRun) =>
            _registrations.Add(context.Commands.Register(new EditorCommand(id, run, canRun)));

        Register(SnippetCommands.JumpToNextSnippetPlaceholder, controller.Next, () => editor.InSnippetMode && editor.HasNextTabstop);
        Register(SnippetCommands.JumpToPrevSnippetPlaceholder, controller.Prev, () => editor.InSnippetMode && editor.HasPrevTabstop);
        Register(SnippetCommands.LeaveSnippet, () => controller.Cancel(true), () => editor.InSnippetMode);
        Register(SnippetCommands.AcceptSnippet, controller.Finish, () => editor.InSnippetMode);
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
            _editor.SnippetController = null;
        }

        _controller = null;
        _editor = null;
    }
}
