// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Edits;

/// <summary>The outcome of applying edits to a buffer.</summary>
/// <remarks>
/// <para>Monaco: <c>ApplyEditsResult</c>. Records do not compare list members by value.</para>
/// <para>Threading: immutable.</para>
/// </remarks>
/// <param name="ReverseEdits">The edits that undo this one, or <c>null</c> when they were not requested.</param>
/// <param name="Changes">The content changes that were applied.</param>
/// <param name="TrimAutoWhitespaceLineNumbers">Lines whose auto-whitespace may be trimmed, or <c>null</c>.</param>
internal sealed record ApplyEditsResult(
    IReadOnlyList<ValidEditOperation>? ReverseEdits,
    IReadOnlyList<InternalModelContentChange> Changes,
    IReadOnlyList<int>? TrimAutoWhitespaceLineNumbers);
