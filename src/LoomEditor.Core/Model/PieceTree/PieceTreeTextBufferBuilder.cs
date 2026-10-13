// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBufferBuilder.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

/// <summary>
/// Accumulates chunks of text (for example read from a file), detecting the BOM and counting line breaks, then
/// produces a <see cref="PieceTreeTextBufferFactory"/>.
/// </summary>
/// <remarks>
/// <para>Monaco: <c>PieceTreeTextBufferBuilder</c>.</para>
/// <para>
/// A trailing <c>\r</c> or high surrogate of a chunk is held back and prepended to the next chunk, so a CRLF or a
/// surrogate pair is never split between two buffers.
/// </para>
/// <para>Threading: not thread-safe; callers must serialize access.</para>
/// </remarks>
internal sealed class PieceTreeTextBufferBuilder
{
    private readonly List<StringBuffer> _chunks = [];
    private string _bom = "";

    private bool _hasPreviousChar;
    private char _previousChar;

    private int _cr;
    private int _lf;
    private int _crlf;
    private bool _containsRtl;
    private bool _containsUnusualLineTerminators;
    private bool _isBasicAscii = true;

    /// <summary>Adds the next chunk of text. A BOM at the start of the first chunk is removed and remembered.</summary>
    public void AcceptChunk(string chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        if (chunk.Length == 0)
        {
            return;
        }

        if (_chunks.Count == 0 && StringClassification.StartsWithUtf8Bom(chunk))
        {
            _bom = StringClassification.Utf8Bom.ToString();
            chunk = chunk[1..];
        }

        char lastChar = chunk.Length > 0 ? chunk[^1] : '\0';
        if (lastChar == '\r' || char.IsHighSurrogate(lastChar))
        {
            // last character is \r or a high surrogate => keep it back
            AcceptChunk1(chunk[..^1], false);
            _hasPreviousChar = true;
            _previousChar = lastChar;
        }
        else
        {
            AcceptChunk1(chunk, false);
            _hasPreviousChar = false;
            _previousChar = lastChar;
        }
    }

    /// <summary>Completes the build.</summary>
    /// <param name="normalizeEol">True to rewrite every line break to the chosen EOL when the factory creates the buffer.</param>
    public PieceTreeTextBufferFactory Finish(bool normalizeEol = true)
    {
        FinishCore();
        return new PieceTreeTextBufferFactory(
            _chunks,
            _bom,
            _cr,
            _lf,
            _crlf,
            _containsRtl,
            _containsUnusualLineTerminators,
            _isBasicAscii,
            normalizeEol);
    }

    private void AcceptChunk1(string chunk, bool allowEmptyStrings)
    {
        if (!allowEmptyStrings && chunk.Length == 0)
        {
            // Nothing to do
            return;
        }

        AcceptChunk2(_hasPreviousChar ? _previousChar + chunk : chunk);
    }

    private void AcceptChunk2(string chunk)
    {
        LineStarts lineStarts = LineStarts.Create(chunk);

        _chunks.Add(new StringBuffer(chunk, [.. lineStarts.Starts]));
        _cr += lineStarts.CarriageReturnCount;
        _lf += lineStarts.LineFeedCount;
        _crlf += lineStarts.CarriageReturnLineFeedCount;

        if (!lineStarts.IsBasicAscii)
        {
            // this chunk contains non basic ASCII characters
            _isBasicAscii = false;
            if (!_containsRtl)
            {
                _containsRtl = StringClassification.ContainsRtl(chunk);
            }

            if (!_containsUnusualLineTerminators)
            {
                _containsUnusualLineTerminators = StringClassification.ContainsUnusualLineTerminators(chunk);
            }
        }
    }

    private void FinishCore()
    {
        if (_chunks.Count == 0)
        {
            AcceptChunk1("", true);
        }

        if (_hasPreviousChar)
        {
            _hasPreviousChar = false;
            // recreate last chunk
            StringBuffer lastChunk = _chunks[^1];
            lastChunk.Append(_previousChar.ToString());
            lastChunk.LineStarts = LineStarts.CreateFast(lastChunk.ToString());
            if (_previousChar == '\r')
            {
                _cr++;
            }
        }
    }
}
