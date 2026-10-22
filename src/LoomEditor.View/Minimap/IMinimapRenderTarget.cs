// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimap.ts (InnerMinimap)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Events;

namespace LoomEditor.View.Minimap;

/// <summary>The calls <see cref="MinimapModel"/> makes on Monaco's <c>InnerMinimap</c>: what changed, in minimap line numbers.</summary>
/// <remarks>
/// Threading: UI thread only. Behavior difference: the methods return nothing; the implementation decides when to redraw.
/// </remarks>
internal interface IMinimapRenderTarget
{
    /// <summary>The minimap options changed (Monaco: <c>onDidChangeOptions</c>).</summary>
    void OnDidChangeOptions();

    /// <summary>Everything changed (Monaco: <c>onFlushed</c>).</summary>
    void OnFlushed();

    /// <summary>Some minimap lines changed (Monaco: <c>onLinesChanged</c>).</summary>
    void OnLinesChanged(int changeFromLineNumber, int changeCount);

    /// <summary>Some minimap lines were deleted (Monaco: <c>onLinesDeleted</c>).</summary>
    void OnLinesDeleted(int deleteFromLineNumber, int deleteToLineNumber);

    /// <summary>Some minimap lines were inserted (Monaco: <c>onLinesInserted</c>).</summary>
    void OnLinesInserted(int insertFromLineNumber, int insertToLineNumber);

    /// <summary>The tokens of some minimap lines changed (Monaco: <c>onTokensChanged</c>).</summary>
    void OnTokensChanged(IReadOnlyList<ViewLineRange> ranges);

    /// <summary>The selections changed (Monaco: <c>onSelectionChanged</c>).</summary>
    void OnSelectionChanged();

    /// <summary>The minimap decorations changed (Monaco: <c>onDecorationsChanged</c>).</summary>
    void OnDecorationsChanged();

    /// <summary>The theme changed (Monaco: <c>onThemeChanged</c>).</summary>
    void OnThemeChanged();
}
