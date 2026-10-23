// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.StickyScroll;

/// <summary>The model that decides which lines stick. Monaco: <c>'outlineModel' | 'foldingProviderModel' | 'indentationModel'</c>.</summary>
internal enum StickyScrollModel
{
    /// <summary>Monaco: <c>'outlineModel'</c>. Needs language features: falls back to the indentation model.</summary>
    OutlineModel = 0,

    /// <summary>Monaco: <c>'foldingProviderModel'</c>. Needs language features: falls back to the indentation model.</summary>
    FoldingProviderModel = 1,

    /// <summary>Monaco: <c>'indentationModel'</c>.</summary>
    IndentationModel = 2,
}

/// <summary>The sticky scroll options (Monaco: <c>editor.stickyScroll.*</c>, <c>EditorStickyScroll</c>).</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
internal sealed record EditorStickyScrollOptions
{
    /// <summary>Show the nested current scopes at the top of the editor. Monaco default: <c>true</c>.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Maximum number of sticky lines to show. Monaco default: <c>5</c>.</summary>
    public int MaxLineCount { get; init; } = 5;

    /// <summary>Model to choose for sticky scroll by default. Monaco default: <c>'outlineModel'</c>.</summary>
    public StickyScrollModel DefaultModel { get; init; } = StickyScrollModel.OutlineModel;

    /// <summary>Scroll the sticky lines with the horizontal scrollbar of the editor. Monaco default: <c>true</c>.</summary>
    public bool ScrollWithEditor { get; init; } = true;

    /// <summary>Monaco's <c>validate</c>: <see cref="MaxLineCount"/> clamped to 1–20.</summary>
    public EditorStickyScrollOptions Validated() => this with { MaxLineCount = Math.Clamp(MaxLineCount, 1, 20) };
}
