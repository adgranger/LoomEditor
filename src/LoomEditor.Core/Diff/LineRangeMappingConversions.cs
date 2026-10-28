// Ported from microsoft/vscode: src/vs/editor/common/diff/rangeMapping.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diagnostics;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Diff;

/// <summary>Conversions from character-level to line-level mappings (Monaco: <c>lineRangeMappingFromRangeMappings</c>, <c>getLineRangeMapping</c>).</summary>
/// <remarks>Threading: stateless, thread-safe.</remarks>
internal static class LineRangeMappingConversions
{
    /// <summary><c>lineRangeMappingFromRangeMappings</c>: groups range mappings that touch the same lines into line-level changes.</summary>
    public static List<DetailedLineRangeMapping> LineRangeMappingFromRangeMappings(
        IReadOnlyList<RangeMapping> alignments, AbstractText originalLines, AbstractText modifiedLines, bool dontAssertStartLine = false)
    {
        var changes = new List<DetailedLineRangeMapping>();
        var group = new List<DetailedLineRangeMapping>();
        foreach (RangeMapping alignment in alignments)
        {
            DetailedLineRangeMapping a2 = GetLineRangeMapping(alignment, originalLines, modifiedLines);
            // groupAdjacentBy: a new group starts when the item does not touch the previous one.
            if (group.Count > 0)
            {
                DetailedLineRangeMapping a1 = group[^1];
                if (!(a1.Original.IntersectsOrTouches(a2.Original) || a1.Modified.IntersectsOrTouches(a2.Modified)))
                {
                    changes.Add(Flush(group));
                    group.Clear();
                }
            }

            group.Add(a2);
        }

        if (group.Count > 0)
        {
            changes.Add(Flush(group));
        }

        Guard.Assert(AreValid(changes, originalLines, modifiedLines, dontAssertStartLine));
        return changes;

        static DetailedLineRangeMapping Flush(List<DetailedLineRangeMapping> g) =>
            new(g[0].Original.Join(g[^1].Original), g[0].Modified.Join(g[^1].Modified), g.Select(a => a.InnerChanges![0]).ToArray());
    }

    /// <summary><c>getLineRangeMapping</c>.</summary>
    public static DetailedLineRangeMapping GetLineRangeMapping(RangeMapping rangeMapping, AbstractText originalLines, AbstractText modifiedLines)
    {
        int lineStartDelta = 0;
        int lineEndDelta = 0;
        Range o = rangeMapping.OriginalRange;
        Range m = rangeMapping.ModifiedRange;

        // original: ]xxx \n <- this line is not modified
        // modified: ]xx  \n
        if (m.EndColumn == 1 && o.EndColumn == 1
            && o.StartLineNumber + lineStartDelta <= o.EndLineNumber
            && m.StartLineNumber + lineStartDelta <= m.EndLineNumber)
        {
            // We can only do this if the range is not empty yet.
            lineEndDelta = -1;
        }

        // original: xxx[ \n <- this line is not modified
        // modified: xxx[ \n
        if (m.StartColumn - 1 >= modifiedLines.GetLineLength(m.StartLineNumber)
            && o.StartColumn - 1 >= originalLines.GetLineLength(o.StartLineNumber)
            && o.StartLineNumber <= o.EndLineNumber + lineEndDelta
            && m.StartLineNumber <= m.EndLineNumber + lineEndDelta)
        {
            // We can only do this if the range is not empty yet.
            lineStartDelta = 1;
        }

        var originalLineRange = new LineRange(o.StartLineNumber + lineStartDelta, o.EndLineNumber + 1 + lineEndDelta);
        var modifiedLineRange = new LineRange(m.StartLineNumber + lineStartDelta, m.EndLineNumber + 1 + lineEndDelta);
        return new DetailedLineRangeMapping(originalLineRange, modifiedLineRange, [rangeMapping]);
    }

    private static bool AreValid(List<DetailedLineRangeMapping> changes, AbstractText originalLines, AbstractText modifiedLines, bool dontAssertStartLine)
    {
        if (!dontAssertStartLine && changes.Count > 0)
        {
            if (changes[0].Modified.StartLineNumber != changes[0].Original.StartLineNumber)
            {
                return false;
            }

            DetailedLineRangeMapping last = changes[^1];
            if (modifiedLines.Length.LineCount - last.Modified.EndLineNumberExclusive != originalLines.Length.LineCount - last.Original.EndLineNumberExclusive)
            {
                return false;
            }
        }

        for (int i = 1; i < changes.Count; i++)
        {
            LineRangeMapping m1 = changes[i - 1];
            LineRangeMapping m2 = changes[i];
            // There has to be an unchanged line in between (otherwise both diffs should have been joined).
            if (m2.Original.StartLineNumber - m1.Original.EndLineNumberExclusive != m2.Modified.StartLineNumber - m1.Modified.EndLineNumberExclusive
                || m1.Original.EndLineNumberExclusive >= m2.Original.StartLineNumber
                || m1.Modified.EndLineNumberExclusive >= m2.Modified.StartLineNumber)
            {
                return false;
            }
        }

        return true;
    }
}
