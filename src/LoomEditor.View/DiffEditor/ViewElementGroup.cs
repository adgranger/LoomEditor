// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/accessibleDiffViewer.ts (LineType, ViewElementGroup, *ViewElement)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diff;

namespace LoomEditor.View.DiffEditor;

/// <summary>The kind of a row of the accessible diff viewer.</summary>
internal enum LineType
{
    /// <summary>The summary of a group of changes.</summary>
    Header,

    /// <summary>A line that is the same on both sides.</summary>
    Unchanged,

    /// <summary>A line of the original text that is not in the modified one.</summary>
    Deleted,

    /// <summary>A line of the modified text that is not in the original one.</summary>
    Added,
}

/// <summary>A row of the accessible diff viewer.</summary>
/// <param name="Type">The kind of row.</param>
/// <remarks>Threading: immutable. Rows are compared by reference (<see cref="AccessibleDiffViewerModel.GoToLine"/>).</remarks>
internal abstract record ViewElement(LineType Type);

/// <summary>The first row of a group: the summary of its changes.</summary>
internal sealed record HeaderViewElement() : ViewElement(LineType.Header);

/// <summary>An unchanged line, with its line number on each side.</summary>
/// <param name="OriginalLineNumber">The line in the original text.</param>
/// <param name="ModifiedLineNumber">The line in the modified text.</param>
internal sealed record UnchangedLineViewElement(int OriginalLineNumber, int ModifiedLineNumber) : ViewElement(LineType.Unchanged);

/// <summary>A deleted line.</summary>
/// <param name="Diff">The change it belongs to.</param>
/// <param name="OriginalLineNumber">The line in the original text.</param>
internal sealed record DeletedLineViewElement(DetailedLineRangeMapping Diff, int OriginalLineNumber) : ViewElement(LineType.Deleted);

/// <summary>An added line.</summary>
/// <param name="Diff">The change it belongs to.</param>
/// <param name="ModifiedLineNumber">The line in the modified text.</param>
internal sealed record AddedLineViewElement(DetailedLineRangeMapping Diff, int ModifiedLineNumber) : ViewElement(LineType.Added);

/// <summary>A group of changes with its context lines, shown on one page of the viewer.</summary>
/// <param name="Range">The lines the changes of the group cover on each side (without the context lines).</param>
/// <param name="Lines">The rows: the header first.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record ViewElementGroup(LineRangeMapping Range, IReadOnlyList<ViewElement> Lines);

/// <summary>The line numbers of a row, <c>null</c> when it has none on that side.</summary>
internal static class ViewElementExtensions
{
    /// <summary>The line in the original text.</summary>
    public static int? GetOriginalLineNumber(this ViewElement element) => element switch
    {
        UnchangedLineViewElement u => u.OriginalLineNumber,
        DeletedLineViewElement d => d.OriginalLineNumber,
        _ => null,
    };

    /// <summary>The line in the modified text.</summary>
    public static int? GetModifiedLineNumber(this ViewElement element) => element switch
    {
        UnchangedLineViewElement u => u.ModifiedLineNumber,
        AddedLineViewElement a => a.ModifiedLineNumber,
        _ => null,
    };
}
