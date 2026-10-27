// Ported from microsoft/vscode: src/vs/editor/common/diff/linesDiffComputer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Diff;

/// <summary>Options of a lines diff (Monaco: <c>ILinesDiffComputerOptions</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record LinesDiffComputerOptions(bool IgnoreTrimWhitespace, int MaxComputationTimeMs, bool ComputeMoves, bool ExtendToSubwords = false);

/// <summary>Computes the differences between two texts given as lines (Monaco: <c>ILinesDiffComputer</c>).</summary>
/// <remarks>Threading: implementation specific.</remarks>
internal interface ILinesDiffComputer
{
    /// <summary><c>computeDiff</c>.</summary>
    LinesDiff ComputeDiff(string[] originalLines, string[] modifiedLines, LinesDiffComputerOptions options);
}
