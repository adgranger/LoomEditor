// Inspired by microsoft/vscode: src/vs/editor/contrib/linesOperations/browser/linesOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.LinesOperations;

/// <summary>Line commands: copy, move, sort, trim, delete, join, transpose and case transforms. Skeleton: the commands come before their actions.</summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class LinesOperationsFeature : IEditorFeature
{
    /// <inheritdoc />
    public string Id => "loom.linesOperations";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        // The actions are registered from P5-57 on.
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
