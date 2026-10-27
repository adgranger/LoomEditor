// Ported from microsoft/vscode: src/vs/editor/common/diff/rangeMapping.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Diff;

/// <summary>Maps a line range in the original text model to a line range in the modified text model (Monaco: <c>LineRangeMapping</c>).</summary>
/// <remarks>
/// <para>Not ported: <c>lineRangeMappingFromChange</c> (legacy <c>IChange</c>).</para>
/// <para>Threading: immutable.</para>
/// </remarks>
internal class LineRangeMapping(LineRange originalRange, LineRange modifiedRange)
{
    /// <summary>The line range in the original text model.</summary>
    public LineRange Original { get; } = originalRange;

    /// <summary>The line range in the modified text model.</summary>
    public LineRange Modified { get; } = modifiedRange;

    /// <summary><c>changedLineCount</c>.</summary>
    public int ChangedLineCount => Math.Max(Original.Length, Modified.Length);

    /// <summary><c>inverse</c>: the unchanged parts between (and around) the given mappings.</summary>
    public static List<LineRangeMapping> Inverse(IEnumerable<LineRangeMapping> mapping, int originalLineCount, int modifiedLineCount)
    {
        var result = new List<LineRangeMapping>();
        int lastOriginalEnd = 1;
        int lastModifiedEnd = 1;

        foreach (LineRangeMapping m in mapping)
        {
            var r = new LineRangeMapping(
                new LineRange(lastOriginalEnd, m.Original.StartLineNumber),
                new LineRange(lastModifiedEnd, m.Modified.StartLineNumber));
            if (!r.Modified.IsEmpty)
            {
                result.Add(r);
            }

            lastOriginalEnd = m.Original.EndLineNumberExclusive;
            lastModifiedEnd = m.Modified.EndLineNumberExclusive;
        }

        var last = new LineRangeMapping(
            new LineRange(lastOriginalEnd, originalLineCount + 1),
            new LineRange(lastModifiedEnd, modifiedLineCount + 1));
        if (!last.Modified.IsEmpty)
        {
            result.Add(last);
        }

        return result;
    }

    /// <summary><c>clip</c>: keeps the mappings whose both sides intersect the given ranges, clipped to them.</summary>
    public static List<LineRangeMapping> Clip(IEnumerable<LineRangeMapping> mapping, LineRange originalRange, LineRange modifiedRange)
    {
        var result = new List<LineRangeMapping>();
        foreach (LineRangeMapping m in mapping)
        {
            LineRange? original = m.Original.Intersect(originalRange);
            LineRange? modified = m.Modified.Intersect(modifiedRange);
            if (original is { IsEmpty: false } o && modified is { IsEmpty: false } mo)
            {
                result.Add(new LineRangeMapping(o, mo));
            }
        }

        return result;
    }

    /// <summary><c>flip</c>.</summary>
    public virtual LineRangeMapping Flip() => new(Modified, Original);

    /// <summary><c>join</c>.</summary>
    public LineRangeMapping Join(LineRangeMapping other) => new(Original.Join(other.Original), Modified.Join(other.Modified));

    /// <summary>
    /// <c>toRangeMapping</c>. Assumes that this describes a valid diff: if one range is empty, the other cannot be the entire document.
    /// </summary>
    /// <exception cref="InvalidOperationException">Not a valid diff.</exception>
    public RangeMapping ToRangeMapping()
    {
        Range? origInclusive = Original.ToInclusiveRange();
        Range? modInclusive = Modified.ToInclusiveRange();
        if (origInclusive is { } oi && modInclusive is { } mi)
        {
            return new RangeMapping(oi, mi);
        }

        if (Original.StartLineNumber == 1 || Modified.StartLineNumber == 1)
        {
            if (!(Modified.StartLineNumber == 1 && Original.StartLineNumber == 1))
            {
                throw new InvalidOperationException("not a valid diff");
            }

            // One range is empty and both start at line 1, so neither covers all lines: the exclusive end is a valid line number.
            return new RangeMapping(
                new Range(Original.StartLineNumber, 1, Original.EndLineNumberExclusive, 1),
                new Range(Modified.StartLineNumber, 1, Modified.EndLineNumberExclusive, 1));
        }

        // Both start lines are greater than 1 here. Monaco uses Number.MAX_SAFE_INTEGER as "end of line".
        return new RangeMapping(
            new Range(Original.StartLineNumber - 1, int.MaxValue, Original.EndLineNumberExclusive - 1, int.MaxValue),
            new Range(Modified.StartLineNumber - 1, int.MaxValue, Modified.EndLineNumberExclusive - 1, int.MaxValue));
    }

    /// <summary><c>toRangeMapping2</c>: like <see cref="ToRangeMapping"/> but clamps to the real line lengths.</summary>
    /// <exception cref="InvalidOperationException">Not a valid diff.</exception>
    public RangeMapping ToRangeMapping2(string[] original, string[] modified)
    {
        if (IsValidLineNumber(Original.EndLineNumberExclusive, original) && IsValidLineNumber(Modified.EndLineNumberExclusive, modified))
        {
            return new RangeMapping(
                new Range(Original.StartLineNumber, 1, Original.EndLineNumberExclusive, 1),
                new Range(Modified.StartLineNumber, 1, Modified.EndLineNumberExclusive, 1));
        }

        if (!Original.IsEmpty && !Modified.IsEmpty)
        {
            return new RangeMapping(
                new Range(
                    new Position(Original.StartLineNumber, 1),
                    NormalizePosition(new Position(Original.EndLineNumberExclusive - 1, int.MaxValue), original)),
                new Range(
                    new Position(Modified.StartLineNumber, 1),
                    NormalizePosition(new Position(Modified.EndLineNumberExclusive - 1, int.MaxValue), modified)));
        }

        if (Original.StartLineNumber > 1 && Modified.StartLineNumber > 1)
        {
            return new RangeMapping(
                new Range(
                    NormalizePosition(new Position(Original.StartLineNumber - 1, int.MaxValue), original),
                    NormalizePosition(new Position(Original.EndLineNumberExclusive - 1, int.MaxValue), original)),
                new Range(
                    NormalizePosition(new Position(Modified.StartLineNumber - 1, int.MaxValue), modified),
                    NormalizePosition(new Position(Modified.EndLineNumberExclusive - 1, int.MaxValue), modified)));
        }

        // One range is empty, one touches the last line and one starts at line 1: Monaco believes this cannot happen.
        throw new InvalidOperationException("not a valid diff");
    }

    /// <inheritdoc />
    public override string ToString() => $"{{{Original}->{Modified}}}";

    private static Position NormalizePosition(Position position, string[] content)
    {
        if (position.LineNumber < 1)
        {
            return new Position(1, 1);
        }

        if (position.LineNumber > content.Length)
        {
            return new Position(content.Length, content[^1].Length + 1);
        }

        string line = content[position.LineNumber - 1];
        return position.Column > line.Length + 1 ? new Position(position.LineNumber, line.Length + 1) : position;
    }

    private static bool IsValidLineNumber(int lineNumber, string[] lines) => lineNumber >= 1 && lineNumber <= lines.Length;
}
