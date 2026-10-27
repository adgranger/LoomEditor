// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/algorithms/diffAlgorithm.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Diff.Algorithms;

/// <summary>A synchronous diff algorithm (Monaco: <c>IDiffAlgorithm</c>).</summary>
/// <remarks>Threading: implementations are stateless; call from the thread that runs the diff.</remarks>
internal interface IDiffAlgorithm
{
    /// <summary>Computes the diffs between two sequences; a <c>null</c> <paramref name="timeout"/> means <see cref="InfiniteTimeout.Instance"/>.</summary>
    DiffAlgorithmResult Compute(ISequence sequence1, ISequence sequence2, ITimeout? timeout = null);
}
