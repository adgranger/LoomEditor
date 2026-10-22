// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Decorations;

/// <summary>The raw options of the text a decoration injects in the view (Monaco: <c>InjectedTextOptions</c>).</summary>
/// <remarks>
/// Not ported: <c>tokens</c> (pre-tokenized injected text; injected text always gets the default token metadata).
/// Threading: immutable; thread-safe.
/// </remarks>
internal sealed record InjectedTextOptions
{
    /// <summary>The text to inject. Must be a single line.</summary>
    public required string Content { get; init; }

    /// <summary>If set, the text is rendered inline with this CSS class name.</summary>
    public string? InlineClassName { get; init; }

    /// <summary>If there is an <see cref="InlineClassName"/> which affects letter spacing.</summary>
    public bool InlineClassNameAffectsLetterSpacing { get; init; }

    /// <summary>The width used to wrap this injected text, in editor-font em units.</summary>
    public double? WidthInEm { get; init; }

    /// <summary>Data attached to this injected text, readable when injected texts at a position are queried.</summary>
    public object? AttachedData { get; init; }

    /// <summary>Cursor stops around the injected text; <c>null</c> means <see cref="InjectedTextCursorStops.Both"/>.</summary>
    public InjectedTextCursorStops? CursorStops { get; init; }
}
