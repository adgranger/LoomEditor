// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/viewLines/viewLines.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

/// <summary>
/// A horizontal scroll that waits until the lines it needs have been laid out
/// (Monaco: <c>HorizontalRevealRangeRequest</c> | <c>HorizontalRevealSelectionsRequest</c>).
/// </summary>
/// <remarks>Threading: immutable; UI thread only.</remarks>
internal abstract record HorizontalRevealRequest(
    bool MinimalReveal,
    int MinLineNumber,
    int MaxLineNumber,
    double StartScrollTop,
    double StopScrollTop,
    ScrollType ScrollType);

/// <summary>Reveals a range on one line.</summary>
/// <remarks>Threading: immutable; UI thread only.</remarks>
internal sealed record HorizontalRevealRangeRequest(
    bool MinimalReveal,
    int LineNumber,
    int StartColumn,
    int EndColumn,
    double StartScrollTop,
    double StopScrollTop,
    ScrollType ScrollType)
    : HorizontalRevealRequest(MinimalReveal, LineNumber, LineNumber, StartScrollTop, StopScrollTop, ScrollType);

/// <summary>Reveals several selections.</summary>
/// <remarks>Threading: immutable; UI thread only.</remarks>
internal sealed record HorizontalRevealSelectionsRequest : HorizontalRevealRequest
{
    public HorizontalRevealSelectionsRequest(
        bool minimalReveal, IReadOnlyList<Selection> selections, double startScrollTop, double stopScrollTop, ScrollType scrollType)
        : base(minimalReveal, MinLine(selections), MaxLine(selections), startScrollTop, stopScrollTop, scrollType)
    {
        Selections = selections;
    }

    public IReadOnlyList<Selection> Selections { get; }

    private static int MinLine(IReadOnlyList<Selection> selections)
    {
        int min = selections[0].Range.StartLineNumber;
        for (int i = 1; i < selections.Count; i++)
        {
            min = Math.Min(min, selections[i].Range.StartLineNumber);
        }

        return min;
    }

    private static int MaxLine(IReadOnlyList<Selection> selections)
    {
        int max = selections[0].Range.EndLineNumber;
        for (int i = 1; i < selections.Count; i++)
        {
            max = Math.Max(max, selections[i].Range.EndLineNumber);
        }

        return max;
    }
}
