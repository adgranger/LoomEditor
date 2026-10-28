// Ported from microsoft/vscode: src/vs/editor/common/diff/linesDiffComputer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Diff;

/// <summary>The result of a lines diff (Monaco: <c>LinesDiff</c>).</summary>
/// <param name="Changes">The line-level changes with their inner character changes.</param>
/// <param name="Moves">Sorted by original line ranges; original and modified ranges are disjoint (but can touch).</param>
/// <param name="HitTimeout">True when the time out was reached: the diff may be an approximation.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record LinesDiff(IReadOnlyList<DetailedLineRangeMapping> Changes, IReadOnlyList<MovedText> Moves, bool HitTimeout);

/// <summary>A block of lines that moved (Monaco: <c>MovedText</c>).</summary>
/// <param name="LineRangeMapping">The original and modified line ranges of the move.</param>
/// <param name="Changes">The diff from the original text to the moved text, contained in the ranges; empty if the text only moved.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record MovedText(LineRangeMapping LineRangeMapping, IReadOnlyList<DetailedLineRangeMapping> Changes)
{
    /// <summary><c>flip</c>.</summary>
    public MovedText Flip() => new(LineRangeMapping.Flip(), Changes.Select(c => c.Flip()).ToArray());
}
