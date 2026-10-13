// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBufferBuilder.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.Core.Model.PieceTree;

/// <summary>
/// The result of <see cref="PieceTreeTextBufferBuilder.Finish"/>: the chunks plus what the builder learned about
/// them, ready to create a <see cref="PieceTreeTextBuffer"/> once the default EOL is known.
/// </summary>
/// <remarks>
/// <para>Monaco: <c>PieceTreeTextBufferFactory</c>.</para>
/// <para>
/// Difference from Monaco: <see cref="Create"/> returns the buffer directly, not a <c>{ textBuffer, disposable }</c>
/// pair. Like Monaco, <see cref="Create"/> may rewrite the chunks, so call it once.
/// </para>
/// <para>Threading: not thread-safe; callers must serialize access.</para>
/// </remarks>
internal sealed partial class PieceTreeTextBufferFactory
{
    private readonly List<StringBuffer> _chunks;
    private readonly string _bom;
    private readonly int _cr;
    private readonly int _lf;
    private readonly int _crlf;
    private readonly bool _containsRtl;
    private readonly bool _containsUnusualLineTerminators;
    private readonly bool _isBasicAscii;
    private readonly bool _normalizeEol;

    internal PieceTreeTextBufferFactory(
        List<StringBuffer> chunks,
        string bom,
        int cr,
        int lf,
        int crlf,
        bool containsRtl,
        bool containsUnusualLineTerminators,
        bool isBasicAscii,
        bool normalizeEol)
    {
        _chunks = chunks;
        _bom = bom;
        _cr = cr;
        _lf = lf;
        _crlf = crlf;
        _containsRtl = containsRtl;
        _containsUnusualLineTerminators = containsUnusualLineTerminators;
        _isBasicAscii = isBasicAscii;
        _normalizeEol = normalizeEol;
    }

    [GeneratedRegex(@"\r\n|\r|\n")]
    private static partial Regex LineBreakRegex();

    /// <summary>Creates the buffer, choosing its EOL from the line breaks seen (Monaco: <c>create</c>).</summary>
    /// <param name="defaultEol">The EOL to use when the text has no line break.</param>
    public PieceTreeTextBuffer Create(DefaultEndOfLine defaultEol)
    {
        string eol = GetEol(defaultEol);

        if (_normalizeEol
            && ((eol == "\r\n" && (_cr > 0 || _lf > 0))
                || (eol == "\n" && (_cr > 0 || _crlf > 0))))
        {
            // Normalize pieces
            for (int i = 0, len = _chunks.Count; i < len; i++)
            {
                string str = LineBreakRegex().Replace(_chunks[i].ToString(), eol);
                _chunks[i] = new StringBuffer(str, LineStarts.CreateFast(str));
            }
        }

        return new PieceTreeTextBuffer(_chunks, _bom, eol, _containsRtl, _containsUnusualLineTerminators, _isBasicAscii, _normalizeEol);
    }

    /// <summary>The first line of the text, looking at no more than <paramref name="lengthLimit"/> characters (Monaco: <c>getFirstLineText</c>).</summary>
    public string GetFirstLineText(int lengthLimit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lengthLimit);
        StringBuffer first = _chunks[0];
        string head = first.Substring(0, Math.Min(lengthLimit, first.Length));
        int end = head.AsSpan().IndexOfAny('\r', '\n');
        return end < 0 ? head : head[..end];
    }

    private string GetEol(DefaultEndOfLine defaultEol)
    {
        int totalEolCount = _cr + _lf + _crlf;
        int totalCrCount = _cr + _crlf;
        if (totalEolCount == 0)
        {
            // This is an empty file or a file with precisely one line
            return defaultEol == DefaultEndOfLine.LF ? "\n" : "\r\n";
        }

        if (totalCrCount > totalEolCount / 2.0)
        {
            // More than half of the file contains \r\n ending lines
            return "\r\n";
        }

        // At least one line more ends in \n
        return "\n";
    }
}
