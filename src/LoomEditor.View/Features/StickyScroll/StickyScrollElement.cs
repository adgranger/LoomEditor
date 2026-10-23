// Ported from microsoft/vscode: src/vs/editor/contrib/stickyScroll/browser/stickyScrollElement.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.StickyScroll;

/// <summary>Range of line numbers spanned by a scope. Monaco: <c>StickyRange</c>.</summary>
internal sealed record StickyRange(int StartLineNumber, int EndLineNumber);

/// <summary>A node of the sticky scope tree. Monaco: <c>StickyElement</c>.</summary>
/// <remarks>Threading: not thread-safe; used on the UI thread only.</remarks>
internal sealed class StickyElement(StickyRange? range, List<StickyElement> children, StickyElement? parent)
{
    /// <summary>Range of line numbers spanned by the current scope.</summary>
    public StickyRange? Range { get; } = range;

    /// <summary>Must be sorted by start line number.</summary>
    public List<StickyElement> Children { get; } = children;

    /// <summary>Parent sticky element.</summary>
    public StickyElement? Parent { get; } = parent;
}

/// <summary>
/// The sticky scope tree of one model version. Monaco: <c>StickyModel</c>; its <c>uri</c> and <c>outlineProviderId</c> are
/// dropped (one model per editor, no outline).
/// </summary>
/// <remarks>Threading: not thread-safe; used on the UI thread only.</remarks>
internal sealed record StickyModel(int Version, StickyElement? Element);
