// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

/// <summary>
/// The offsets at which each line of a text starts, plus the number of each kind of line break.
/// </summary>
/// <remarks>
/// <para>Monaco: <c>LineStarts</c>, <c>createLineStartsFast</c> and <c>createLineStarts</c>.</para>
/// <para>
/// Differences from Monaco: Monaco's <c>createUintArray</c> (a JavaScript memory optimization) is not ported, and the
/// reusable result array parameters are dropped.
/// </para>
/// <para>Threading: instances are immutable; the static methods are pure and thread-safe.</para>
/// </remarks>
internal sealed class LineStarts
{
    private LineStarts(List<int> starts, int carriageReturnCount, int lineFeedCount, int carriageReturnLineFeedCount, bool isBasicAscii)
    {
        Starts = starts;
        CarriageReturnCount = carriageReturnCount;
        LineFeedCount = lineFeedCount;
        CarriageReturnLineFeedCount = carriageReturnLineFeedCount;
        IsBasicAscii = isBasicAscii;
    }

    /// <summary>Offset of the first character of each line; the first entry is always 0 (Monaco <c>lineStarts</c>).</summary>
    public IReadOnlyList<int> Starts { get; }

    /// <summary>Number of lone <c>\r</c> line breaks (Monaco <c>cr</c>).</summary>
    public int CarriageReturnCount { get; }

    /// <summary>Number of lone <c>\n</c> line breaks (Monaco <c>lf</c>).</summary>
    public int LineFeedCount { get; }

    /// <summary>Number of <c>\r\n</c> line breaks (Monaco <c>crlf</c>).</summary>
    public int CarriageReturnLineFeedCount { get; }

    /// <summary>True when every non-line-break char is a tab or in 32..126 (Monaco <c>isBasicASCII</c>).</summary>
    public bool IsBasicAscii { get; }

    /// <summary>Computes the line starts of <paramref name="text"/>. Monaco: <c>createLineStartsFast</c>.</summary>
    /// <returns>A new mutable list (the change buffer appends to it later).</returns>
    public static List<int> CreateFast(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var result = new List<int> { 0 };
        for (int i = 0, len = text.Length; i < len; i++)
        {
            char chr = text[i];
            if (chr == '\r')
            {
                if (i + 1 < len && text[i + 1] == '\n')
                {
                    // \r\n... case
                    result.Add(i + 2);
                    i++; // skip \n
                }
                else
                {
                    // \r... case
                    result.Add(i + 1);
                }
            }
            else if (chr == '\n')
            {
                result.Add(i + 1);
            }
        }

        return result;
    }

    /// <summary>Computes the line starts and the line break statistics of <paramref name="text"/>. Monaco: <c>createLineStarts</c>.</summary>
    public static LineStarts Create(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var result = new List<int> { 0 };
        int cr = 0, lf = 0, crlf = 0;
        bool isBasicAscii = true;
        for (int i = 0, len = text.Length; i < len; i++)
        {
            char chr = text[i];
            if (chr == '\r')
            {
                if (i + 1 < len && text[i + 1] == '\n')
                {
                    // \r\n... case
                    crlf++;
                    result.Add(i + 2);
                    i++; // skip \n
                }
                else
                {
                    // \r... case
                    cr++;
                    result.Add(i + 1);
                }
            }
            else if (chr == '\n')
            {
                lf++;
                result.Add(i + 1);
            }
            else if (isBasicAscii && chr != '\t' && (chr < 32 || chr > 126))
            {
                isBasicAscii = false;
            }
        }

        return new LineStarts(result, cr, lf, crlf, isBasicAscii);
    }
}
