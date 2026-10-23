// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/viewLines/viewLine.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.
using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

/// <remarks>Threading: UI thread only (see <see cref="TextViewRenderer"/>).</remarks>
internal sealed partial class TextViewRenderer
{
    // The decoration overrides, then the selection ones: the selection color wins (Monaco pushes `inline-selected-text` last).
    private List<StyleOverride>? GetLineOverrides(
        ViewModel viewModel, ViewportData data, IReadOnlyList<Selection> selections, int lineNumber, int length)
    {
        int maxColumn = viewModel.GetLineMaxColumn(lineNumber);
        List<StyleOverride>? decorations = GetStyleOverrides(data.GetInlineDecorations(lineNumber), lineNumber, maxColumn, length);
        List<StyleOverride>? selected = GetSelectionStyleOverrides(viewModel, selections, lineNumber, maxColumn, length);
        return selected is null ? decorations : [.. decorations ?? [], .. selected];
    }

    // Monaco: viewLine.ts 123-141, only in a high contrast theme and when `editor.selectionForeground` is not transparent.
    private List<StyleOverride>? GetSelectionStyleOverrides(
        ViewModel viewModel, IReadOnlyList<Selection> selections, int lineNumber, int maxColumn, int length)
    {
        if (Theme.Type is not (ThemeType.HighContrastDark or ThemeType.HighContrastLight)
            || Theme.SelectionForeground is not { A: > 0 } color)
        {
            return null;
        }

        List<StyleOverride>? overrides = null;
        foreach (Selection selection in selections)
        {
            Range range = selection.Range;
            if (lineNumber < range.StartLineNumber || lineNumber > range.EndLineNumber)
            {
                continue;
            }

            int startColumn = range.StartLineNumber == lineNumber ? range.StartColumn : viewModel.GetLineMinColumn(lineNumber);
            int endColumn = range.EndLineNumber == lineNumber ? range.EndColumn : maxColumn;
            if (startColumn < endColumn)
            {
                (overrides ??= []).Add(new StyleOverride(Math.Min(startColumn - 1, length), Math.Min(endColumn - 1, length), LineStyle.ToArgb(color)));
            }
        }

        return overrides;
    }
}
