// Inspired by microsoft/vscode: src/vs/editor/contrib/comment/browser/comment.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Comment;

/// <summary>Line and block comments. Skeleton: the commands are ported before the actions that run them.</summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class CommentFeature : IEditorFeature
{
    /// <inheritdoc />
    public string Id => "loom.comment";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        // The actions are registered in P5-51.
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
