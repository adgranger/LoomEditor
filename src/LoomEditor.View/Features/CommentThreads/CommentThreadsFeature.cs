using LoomEditor.View.Commands;

namespace LoomEditor.View.Features.CommentThreads;

/// <summary>
/// Adds the <see cref="CommentThreadsCommands.Add"/> command. The threads themselves live in the controller the control
/// creates (<c>EditorContext.CommentThreads</c>), so the comment API works with any feature list.
/// </summary>
/// <remarks>Not a port: Monaco has no comment widget (ADR 0014). Works in a read-only editor. Threading: UI thread only.</remarks>
public sealed class CommentThreadsFeature : IEditorFeature
{
    private IDisposable? _registration;

    /// <inheritdoc />
    public string Id => "loom.commentThreads";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _registration = context.Commands.Register(new EditorCommand(
            CommentThreadsCommands.Add,
            () => context.GetCommentThreadsController()?.RequestAdd(context.Selection.PositionLineNumber)));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _registration?.Dispose();
        _registration = null;
    }
}
