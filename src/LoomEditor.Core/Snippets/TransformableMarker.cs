// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetParser.ts (TransformableMarker)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>A marker that can carry a <see cref="Snippets.Transform"/>: a <see cref="Placeholder"/> or a <see cref="Variable"/>.</summary>
/// <remarks>Threading: not thread-safe (a snippet is owned by one session).</remarks>
internal abstract class TransformableMarker : SnippetMarker
{
    /// <summary>The <c>/regex/format/flags</c> part, or <see langword="null"/>.</summary>
    public Transform? Transform { get; set; }
}
