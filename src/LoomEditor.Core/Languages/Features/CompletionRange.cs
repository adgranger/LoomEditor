// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (CompletionItemRanges)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Languages.Features;

/// <summary>The ranges a completion item edits: <see cref="Insert"/> when the cursor is at the word start, <see cref="Replace"/> to overwrite the rest of the word.</summary>
/// <remarks>Threading: immutable value, safe to share between threads.</remarks>
/// <param name="Insert">The range replaced in insert mode.</param>
/// <param name="Replace">The range replaced in replace mode.</param>
public readonly record struct CompletionRange(Range Insert, Range Replace)
{
    /// <summary>A single range: insert and replace are the same.</summary>
    public static CompletionRange FromRange(Range range) => new(range, range);
}
