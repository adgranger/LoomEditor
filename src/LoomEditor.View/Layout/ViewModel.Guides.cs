// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelImpl.ts (guide queries)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Guides;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

// The guide queries, in view line numbers.
internal sealed partial class ViewModel
{
    /// <summary>The active indent guide around a view line (Monaco: <c>getActiveIndentGuide</c>).</summary>
    public ActiveIndentGuideInfo GetActiveIndentGuide(int lineNumber, int minLineNumber, int maxLineNumber) =>
        _lines.GetActiveIndentGuide(lineNumber, minLineNumber, maxLineNumber);

    /// <summary>The indent guide of each view line of a range (Monaco: <c>getLinesIndentGuides</c>).</summary>
    public int[] GetLinesIndentGuides(int startLineNumber, int endLineNumber) =>
        _lines.GetViewLinesIndentGuides(startLineNumber, endLineNumber);

    /// <summary>The bracket guides of each view line of a range (Monaco: <c>getBracketGuidesInRangeByLine</c>).</summary>
    public List<IndentGuide>[] GetBracketGuidesInRangeByLine(int startLineNumber, int endLineNumber, Position? activePosition, BracketGuideOptions options) =>
        _lines.GetViewLinesBracketGuides(startLineNumber, endLineNumber, activePosition, options);
}
