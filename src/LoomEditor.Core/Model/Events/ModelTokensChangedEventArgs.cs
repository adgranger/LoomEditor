// Ported from microsoft/vscode: src/vs/editor/common/textModelEvents.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Events;

/// <summary>A range of lines (1-based, both inclusive) whose tokens changed (Monaco: the elements of <c>ranges</c>).</summary>
/// <param name="FromLineNumber">The first line.</param>
/// <param name="ToLineNumber">The last line.</param>
/// <remarks>Threading: a value type.</remarks>
internal readonly record struct ChangedLineRange(int FromLineNumber, int ToLineNumber);

/// <summary>The tokens of some lines changed (Monaco: <c>IModelTokensChangedEvent</c>, without <c>semanticTokensApplied</c>).</summary>
/// <remarks>Threading: immutable; raised on the thread that owns the model.</remarks>
internal sealed class ModelTokensChangedEventArgs(IReadOnlyList<ChangedLineRange> ranges) : EventArgs
{
    /// <summary><c>ranges</c>.</summary>
    public IReadOnlyList<ChangedLineRange> Ranges { get; } = ranges;
}
