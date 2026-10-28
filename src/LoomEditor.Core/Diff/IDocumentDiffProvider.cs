// Ported from microsoft/vscode: src/vs/editor/common/diff/documentDiffProvider.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;

namespace LoomEditor.Core.Diff;

/// <summary>Diffs two text models (Monaco: <c>IDocumentDiffProvider</c>; no <c>onDidChange</c>).</summary>
/// <remarks>Threading: see the implementation.</remarks>
internal interface IDocumentDiffProvider
{
    /// <summary><c>computeDiff</c>.</summary>
    Task<DocumentDiff> ComputeDiffAsync(TextModel original, TextModel modified, LinesDiffComputerOptions options, CancellationToken token);
}
