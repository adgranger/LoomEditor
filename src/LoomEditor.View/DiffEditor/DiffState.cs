// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/diffEditorViewModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diff;

namespace LoomEditor.View.DiffEditor;

/// <summary>The diff a view model publishes (Monaco: <c>DiffState</c>).</summary>
/// <param name="Mappings">One mapping per changed block of lines.</param>
/// <param name="MovedTexts">The moved blocks of lines.</param>
/// <param name="Identical">True when both models have the same lines.</param>
/// <param name="QuitEarly">True when the computation was cancelled or timed out.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record DiffState(IReadOnlyList<DiffMapping> Mappings, IReadOnlyList<MovedText> MovedTexts, bool Identical, bool QuitEarly)
{
    /// <summary>Monaco: <c>DiffState.fromDiffResult</c>.</summary>
    public static DiffState FromDiffResult(DocumentDiff result) =>
        new([.. result.Changes.Select(c => new DiffMapping(c))], result.Moves, result.Identical, result.QuitEarly);
}

/// <summary>One changed block of lines (Monaco: <c>DiffMapping</c>, without the moved-text links, which Monaco comments out).</summary>
/// <param name="LineRangeMapping">The original and modified line ranges, with their inner character changes.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record DiffMapping(DetailedLineRangeMapping LineRangeMapping);
