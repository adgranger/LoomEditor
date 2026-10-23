// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimap.ts (IMinimapModel)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Minimap;

/// <summary>How the minimap reads the document: lines with sampling applied, selections, decorations, reveal and scroll requests.</summary>
/// <remarks>Threading: UI thread only. The section header members of Monaco's interface are not ported.</remarks>
internal interface IMinimapModel
{
    /// <summary>Monaco: <c>tokensColorTracker</c>.</summary>
    MinimapTokensColorTracker TokensColorTracker { get; }

    /// <summary>Monaco: <c>options</c>.</summary>
    MinimapOptions Options { get; }

    /// <summary>The number of minimap lines.</summary>
    int GetLineCount();

    /// <summary>The number of view lines.</summary>
    int GetRealLineCount();

    /// <summary>The content of a minimap line.</summary>
    string GetLineContent(int lineNumber);

    /// <summary>The max column of a minimap line.</summary>
    int GetLineMaxColumn(int lineNumber);

    /// <summary>The rendering data of the minimap lines <paramref name="startLineNumber"/>..<paramref name="endLineNumber"/>; <c>null</c> where <paramref name="needed"/> is false.</summary>
    ViewLineData?[] GetMinimapLinesRenderingData(int startLineNumber, int endLineNumber, bool[] needed);

    /// <summary>The selections, in minimap lines.</summary>
    IReadOnlyList<Selection> GetSelections();

    /// <summary>The decorations to draw in the minimap lines <paramref name="startLineNumber"/>..<paramref name="endLineNumber"/>, in minimap lines.</summary>
    IReadOnlyList<ViewModelDecoration> GetMinimapDecorationsInViewport(int startLineNumber, int endLineNumber);

    /// <summary>The model options (tab size).</summary>
    TextModelOptions GetOptions();

    /// <summary>Scrolls the editor so that a minimap line is in the center.</summary>
    void RevealLineNumber(int lineNumber);

    /// <summary>Scrolls the editor immediately.</summary>
    void SetScrollTop(double scrollTop);
}
