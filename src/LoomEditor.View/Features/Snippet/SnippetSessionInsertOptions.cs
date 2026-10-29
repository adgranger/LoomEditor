// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetSession.ts (ISnippetSessionInsertOptions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Snippet;

/// <summary>How a snippet is inserted (Monaco: <c>ISnippetSessionInsertOptions</c>).</summary>
/// <remarks>
/// Behavior difference: no <c>overtypingCapturer</c>. Threading: immutable value; thread-safe.
/// </remarks>
internal sealed record SnippetSessionInsertOptions
{
    /// <summary>The default options (Monaco: <c>_defaultOptions</c>).</summary>
    public static SnippetSessionInsertOptions Default { get; } = new();

    /// <summary>The number of characters before each cursor that the snippet replaces.</summary>
    public int OverwriteBefore { get; init; }

    /// <summary>The number of characters after each cursor that the snippet replaces.</summary>
    public int OverwriteAfter { get; init; }

    /// <summary>Whether the template is re-indented and its line breaks normalized to the model.</summary>
    public bool AdjustWhitespace { get; init; } = true;

    /// <summary>The text <c>$CLIPBOARD</c> resolves to; <see langword="null"/> asks the variable host.</summary>
    public string? ClipboardText { get; init; }
}
