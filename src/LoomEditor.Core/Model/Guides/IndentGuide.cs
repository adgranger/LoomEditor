// Ported from microsoft/vscode: src/vs/editor/common/textModelGuides.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Guides;

/// <summary>A horizontal guide: it starts at the visible column of its <see cref="IndentGuide"/> and continues until <paramref name="EndColumn"/>.</summary>
/// <param name="Top">Whether the line is drawn at the top of the line (otherwise at the bottom).</param>
/// <param name="EndColumn">The column where the line ends.</param>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
internal sealed record IndentGuideHorizontalLine(bool Top, int EndColumn);

/// <summary>An indent or bracket pair guide of a line. <c>-1</c> means "not set" for the column members.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
internal sealed record IndentGuide
{
    /// <summary>Creates a guide.</summary>
    /// <exception cref="ArgumentException">Exactly one of <paramref name="visibleColumn"/> and <paramref name="column"/> must be <c>-1</c>.</exception>
    /// <param name="visibleColumn">The visible column of the guide, or <c>-1</c>.</param>
    /// <param name="column">The model column of the guide, or <c>-1</c>.</param>
    /// <param name="className">The class name that selects the guide's color.</param>
    /// <param name="horizontalLine">If set, this guide is a horizontal guide (no vertical part).</param>
    /// <param name="forWrappedLinesAfterColumn">If not <c>-1</c>, only show this guide for wrapped lines that do not contain this model column but are after it.</param>
    /// <param name="forWrappedLinesBeforeOrAtColumn">If not <c>-1</c>, only show this guide for wrapped lines that are before or at this model column.</param>
    public IndentGuide(
        int visibleColumn,
        int column,
        string className,
        IndentGuideHorizontalLine? horizontalLine,
        int forWrappedLinesAfterColumn,
        int forWrappedLinesBeforeOrAtColumn)
    {
        // Monaco throws a bare Error() when both or neither of the columns are set.
        if ((visibleColumn != -1) == (column != -1))
        {
            throw new ArgumentException("Exactly one of visibleColumn and column must be -1.");
        }

        VisibleColumn = visibleColumn;
        Column = column;
        ClassName = className;
        HorizontalLine = horizontalLine;
        ForWrappedLinesAfterColumn = forWrappedLinesAfterColumn;
        ForWrappedLinesBeforeOrAtColumn = forWrappedLinesBeforeOrAtColumn;
    }

    /// <summary>The visible column of the guide, or <c>-1</c>.</summary>
    public int VisibleColumn { get; }

    /// <summary>The model column of the guide, or <c>-1</c>.</summary>
    public int Column { get; }

    /// <summary>The class name that selects the guide's color.</summary>
    public string ClassName { get; }

    /// <summary>If set, this guide is a horizontal guide (no vertical part).</summary>
    public IndentGuideHorizontalLine? HorizontalLine { get; }

    /// <summary>If not <c>-1</c>, only show this guide for wrapped lines that do not contain this model column but are after it.</summary>
    public int ForWrappedLinesAfterColumn { get; }

    /// <summary>If not <c>-1</c>, only show this guide for wrapped lines that are before or at this model column.</summary>
    public int ForWrappedLinesBeforeOrAtColumn { get; }
}
