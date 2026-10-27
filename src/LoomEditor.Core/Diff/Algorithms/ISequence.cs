// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/algorithms/diffAlgorithm.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Diff.Algorithms;

/// <summary>A sequence of integer elements to diff (Monaco: <c>ISequence</c>).</summary>
/// <remarks>Threading: implementations are immutable.</remarks>
internal interface ISequence
{
    /// <summary>The number of elements.</summary>
    int Length { get; }

    /// <summary><c>getElement</c>.</summary>
    int GetElement(int offset);

    /// <summary>
    /// <c>getBoundaryScore</c>: the higher the score, the better <paramref name="length"/> can be used to split the
    /// sequence. Never negative. Optional in Monaco: the default returns 0.
    /// </summary>
    int GetBoundaryScore(int length) => 0;

    /// <summary>
    /// <c>isStronglyEqual</c>: for line sequences, <see cref="GetElement"/> returns a hash of the trimmed line; this
    /// compares the original lines, which prevents shifting to less matching lines.
    /// </summary>
    bool IsStronglyEqual(int offset1, int offset2);
}
