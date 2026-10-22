// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts (occurrencesHighlight, occurrencesHighlightDelay)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.WordHighlighter;

/// <summary>Which occurrences are highlighted (Monaco: <c>occurrencesHighlight</c>).</summary>
/// <remarks>Threading: an enum; thread-safe.</remarks>
internal enum OccurrencesHighlight
{
    /// <summary>No highlight.</summary>
    Off,

    /// <summary>The occurrences in the model.</summary>
    SingleFile,

    /// <summary>Like <see cref="SingleFile"/>: there is one model.</summary>
    MultiFile,
}

/// <summary>The options of the word highlighter.</summary>
/// <param name="OccurrencesHighlight">Which occurrences are highlighted.</param>
/// <param name="OccurrencesHighlightDelay">The time to wait, in milliseconds, before the highlights are shown.</param>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed record EditorWordHighlightOptions(
    OccurrencesHighlight OccurrencesHighlight = OccurrencesHighlight.SingleFile,
    int OccurrencesHighlightDelay = 0)
{
    /// <summary>VS Code's defaults.</summary>
    public static EditorWordHighlightOptions Default { get; } = new();
}
