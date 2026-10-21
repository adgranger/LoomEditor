// Inspired by microsoft/vscode: src/vs/editor/contrib/indentation/browser/indentation.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Indentation;

/// <summary>Indentation commands. Skeleton: the pure parts (conversion commands, re-indent edits) come before the actions that run them.</summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class IndentationFeature : IEditorFeature
{
    /// <inheritdoc />
    public string Id => "loom.indentation";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        // The actions are registered in P5-53, the paste handling in P5-54.
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
