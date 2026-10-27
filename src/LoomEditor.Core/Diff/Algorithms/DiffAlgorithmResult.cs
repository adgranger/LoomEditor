// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/algorithms/diffAlgorithm.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Diff.Algorithms;

/// <summary>The result of a diff algorithm (Monaco: <c>DiffAlgorithmResult</c>).</summary>
/// <param name="Diffs">The sorted differences.</param>
/// <param name="HitTimeout">True when the timeout was reached: the diffs might then be an approximation.</param>
/// <remarks>Threading: immutable once built.</remarks>
internal sealed record DiffAlgorithmResult(SequenceDiff[] Diffs, bool HitTimeout)
{
    /// <summary><c>trivial</c>: one diff that replaces the whole first sequence by the whole second one.</summary>
    public static DiffAlgorithmResult Trivial(ISequence seq1, ISequence seq2) =>
        new([new SequenceDiff(OffsetRange.OfLength(seq1.Length), OffsetRange.OfLength(seq2.Length))], false);

    /// <summary><c>trivialTimedOut</c>.</summary>
    public static DiffAlgorithmResult TrivialTimedOut(ISequence seq1, ISequence seq2) =>
        new([new SequenceDiff(OffsetRange.OfLength(seq1.Length), OffsetRange.OfLength(seq2.Length))], true);
}
