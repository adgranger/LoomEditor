// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetController2.ts (ISnippetInsertOptions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Snippet;

/// <summary>How <see cref="SnippetController.Insert"/> inserts a snippet (Monaco: <c>ISnippetInsertOptions</c>).</summary>
/// <remarks>Behavior difference: no <c>overtypingCapturer</c>, no <c>reason</c>. Threading: immutable value; thread-safe.</remarks>
internal sealed record SnippetInsertOptions
{
    /// <summary>The number of characters before each cursor that the snippet replaces.</summary>
    public int OverwriteBefore { get; init; }

    /// <summary>The number of characters after each cursor that the snippet replaces.</summary>
    public int OverwriteAfter { get; init; }

    /// <summary>Whether the template is re-indented and its line breaks normalized to the model.</summary>
    public bool AdjustWhitespace { get; init; } = true;

    /// <summary>Whether an undo stop is pushed before the insertion.</summary>
    public bool UndoStopBefore { get; init; } = true;

    /// <summary>Whether an undo stop is pushed after the insertion.</summary>
    public bool UndoStopAfter { get; init; } = true;

    /// <summary>The text <c>$CLIPBOARD</c> resolves to; <see langword="null"/> asks the variable host.</summary>
    public string? ClipboardText { get; init; }
}
