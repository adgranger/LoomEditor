// Ported from microsoft/vscode: src/vs/editor/contrib/comment/browser/comment.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.Comment;

/// <summary>Adds the toggle line comment, add line comment, remove line comment and toggle block comment commands.</summary>
/// <remarks>Menus (<c>menuOpts</c>) are not ported. Threading: UI thread only.</remarks>
public sealed class CommentFeature : IEditorFeature
{
    private readonly List<IDisposable> _registrations = [];

    /// <inheritdoc />
    public string Id => "loom.comment";

    internal EditorCommentsOptions Options { get; set; } = EditorCommentsOptions.Default;

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ViewModel? viewModel = context.GetViewModel();
        if (viewModel is null)
        {
            return;
        }

        // Monaco: precondition `writable`.
        Register(context, CommentCommands.CommentLine, viewModel, () => CommentActions.RunLineComment(viewModel, LineCommentType.Toggle, Options, CommentCommands.CommentLine));
        Register(context, CommentCommands.AddCommentLine, viewModel, () => CommentActions.RunLineComment(viewModel, LineCommentType.ForceAdd, Options, CommentCommands.AddCommentLine));
        Register(context, CommentCommands.RemoveCommentLine, viewModel, () => CommentActions.RunLineComment(viewModel, LineCommentType.ForceRemove, Options, CommentCommands.RemoveCommentLine));
        Register(context, CommentCommands.BlockComment, viewModel, () => CommentActions.RunBlockComment(viewModel, Options, CommentCommands.BlockComment));
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

    private void Register(IEditorContext context, string id, ViewModel viewModel, Action run) =>
        _registrations.Add(context.Commands.Register(new EditorCommand(id, run, () => !viewModel.CursorConfig.ReadOnly)));
}
