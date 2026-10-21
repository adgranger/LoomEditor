// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.PieceTree;

/// <summary>
/// The text of a model: a <see cref="PieceTreeBase"/> plus the BOM and flags about its content.
/// Read queries are implemented here; edits are added by later cards.
/// </summary>
/// <remarks>
/// <para>Monaco: <c>PieceTreeTextBuffer</c>.</para>
/// <para>
/// Differences from Monaco: not <c>IDisposable</c> and no <c>onDidChangeContent</c> event (nothing to dispose in
/// this port); the EOL is a <see cref="string"/> validated by <see cref="PieceTreeBase"/>.
/// </para>
/// <para>Threading: not thread-safe; callers must serialize access.</para>
/// </remarks>
internal sealed partial class PieceTreeTextBuffer : ITextBuffer
{
    private readonly PieceTreeBase _pieceTree;
    private readonly string _bom;
    private bool _mightContainRtl;
    private bool _mightContainNonBasicAscii;
    private bool _mightContainUnusualLineTerminators;

    /// <summary>Creates a buffer over already-chunked text.</summary>
    /// <param name="chunks">The text, in order. Not copied.</param>
    /// <param name="bom">The BOM that was removed from the text, or an empty string.</param>
    /// <param name="eol">The EOL of the document: <c>"\n"</c> or <c>"\r\n"</c>.</param>
    /// <param name="containsRtl">True when the text may contain right-to-left characters.</param>
    /// <param name="containsUnusualLineTerminators">True when the text may contain U+2028 or U+2029.</param>
    /// <param name="isBasicAscii">True when the text has only basic ASCII characters.</param>
    /// <param name="eolNormalized">True when every line break of the chunks equals <paramref name="eol"/>.</param>
    public PieceTreeTextBuffer(
        IReadOnlyList<StringBuffer> chunks,
        string bom,
        string eol,
        bool containsRtl,
        bool containsUnusualLineTerminators,
        bool isBasicAscii,
        bool eolNormalized)
    {
        ArgumentNullException.ThrowIfNull(bom);
        _bom = bom;
        _mightContainNonBasicAscii = !isBasicAscii;
        _mightContainRtl = containsRtl;
        _mightContainUnusualLineTerminators = containsUnusualLineTerminators;
        _pieceTree = new PieceTreeBase(chunks, eol, eolNormalized);
    }

    /// <inheritdoc />
    public bool Equals(ITextBuffer other)
    {
        if (other is not PieceTreeTextBuffer o)
        {
            return false;
        }

        return _bom == o._bom && GetEol() == o.GetEol() && _pieceTree.Equal(o._pieceTree);
    }

    /// <inheritdoc />
    public bool MightContainRtl() => _mightContainRtl;

    /// <inheritdoc />
    public bool MightContainUnusualLineTerminators() => _mightContainUnusualLineTerminators;

    /// <inheritdoc />
    public void ResetMightContainUnusualLineTerminators() => _mightContainUnusualLineTerminators = false;

    /// <inheritdoc />
    public bool MightContainNonBasicAscii() => _mightContainNonBasicAscii;

    /// <inheritdoc />
    public string GetBom() => _bom;

    /// <inheritdoc />
    public string GetEol() => _pieceTree.GetEol();

    /// <inheritdoc />
    public void SetEol(string newEol) => _pieceTree.SetEol(newEol);

    /// <inheritdoc />
    public ITextSnapshot CreateSnapshot(bool preserveBom) => _pieceTree.CreateSnapshot(preserveBom ? _bom : string.Empty);

    /// <inheritdoc />
    public int GetOffsetAt(int lineNumber, int column) => _pieceTree.GetOffsetAt(lineNumber, column);

    /// <inheritdoc />
    public Position GetPositionAt(int offset) => _pieceTree.GetPositionAt(offset);

    /// <inheritdoc />
    public Range GetRangeAt(int start, int length)
    {
        Position startPosition = GetPositionAt(start);
        Position endPosition = GetPositionAt(start + length);
        return new Range(startPosition.LineNumber, startPosition.Column, endPosition.LineNumber, endPosition.Column);
    }

    /// <inheritdoc />
    public string GetValueInRange(Range range, EndOfLinePreference eol = EndOfLinePreference.TextDefined)
    {
        if (range.IsEmpty)
        {
            return string.Empty;
        }

        return _pieceTree.GetValueInRange(range, GetEndOfLine(eol));
    }

    /// <inheritdoc />
    public int GetValueLengthInRange(Range range, EndOfLinePreference eol = EndOfLinePreference.TextDefined)
    {
        if (range.IsEmpty)
        {
            return 0;
        }

        if (range.StartLineNumber == range.EndLineNumber)
        {
            return range.EndColumn - range.StartColumn;
        }

        int startOffset = GetOffsetAt(range.StartLineNumber, range.StartColumn);
        int endOffset = GetOffsetAt(range.EndLineNumber, range.EndColumn);

        // Offsets use the text EOL, so compensate when the requested EOL has a different length.
        int eolOffsetCompensation = 0;
        string desiredEol = GetEndOfLine(eol);
        string actualEol = GetEol();
        if (desiredEol.Length != actualEol.Length)
        {
            int delta = desiredEol.Length - actualEol.Length;
            int eolCount = range.EndLineNumber - range.StartLineNumber;
            eolOffsetCompensation = delta * eolCount;
        }

        return endOffset - startOffset + eolOffsetCompensation;
    }

    /// <inheritdoc />
    public int GetCharacterCountInRange(Range range, EndOfLinePreference eol = EndOfLinePreference.TextDefined)
    {
        if (!_mightContainNonBasicAscii)
        {
            return GetValueLengthInRange(range, eol);
        }

        // Must count by iterating: a surrogate pair is one character.
        int result = 0;
        int fromLineNumber = range.StartLineNumber;
        int toLineNumber = range.EndLineNumber;
        for (int lineNumber = fromLineNumber; lineNumber <= toLineNumber; lineNumber++)
        {
            string lineContent = GetLineContent(lineNumber);
            int fromOffset = lineNumber == fromLineNumber ? range.StartColumn - 1 : 0;
            int toOffset = lineNumber == toLineNumber ? range.EndColumn - 1 : lineContent.Length;

            for (int offset = fromOffset; offset < toOffset; offset++)
            {
                if (char.IsHighSurrogate(lineContent[offset]))
                {
                    offset++;
                }

                result++;
            }
        }

        result += GetEndOfLine(eol).Length * (toLineNumber - fromLineNumber);
        return result;
    }

    /// <inheritdoc />
    public string GetNearestChunk(int offset) => _pieceTree.GetNearestChunk(offset);

    /// <inheritdoc />
    public int GetLength() => _pieceTree.GetLength();

    /// <inheritdoc />
    public int GetLineCount() => _pieceTree.GetLineCount();

    /// <inheritdoc />
    public string[] GetLinesContent() => _pieceTree.GetLinesContent();

    /// <inheritdoc />
    public string GetLineContent(int lineNumber) => _pieceTree.GetLineContent(lineNumber);

    /// <inheritdoc />
    public List<Search.FindMatch> FindMatchesLineByLine(Range searchRange, Search.SearchData searchData, bool captureMatches, int limitResultCount) =>
        _pieceTree.FindMatchesLineByLine(searchRange, searchData, captureMatches, limitResultCount);

    /// <inheritdoc />
    public char GetLineCharCode(int lineNumber, int index) => _pieceTree.GetLineCharCode(lineNumber, index);

    /// <inheritdoc />
    public char GetCharCode(int offset) => _pieceTree.GetCharCode(offset);

    /// <inheritdoc />
    public int GetLineLength(int lineNumber) => _pieceTree.GetLineLength(lineNumber);

    /// <inheritdoc />
    public int GetLineMinColumn(int lineNumber) => 1;

    /// <inheritdoc />
    public int GetLineMaxColumn(int lineNumber) => GetLineLength(lineNumber) + 1;

    /// <inheritdoc />
    public int GetLineFirstNonWhitespaceColumn(int lineNumber)
    {
        int result = StringClassification.FirstNonWhitespaceIndex(GetLineContent(lineNumber));
        return result == -1 ? 0 : result + 1;
    }

    /// <inheritdoc />
    public int GetLineLastNonWhitespaceColumn(int lineNumber)
    {
        int result = StringClassification.LastNonWhitespaceIndex(GetLineContent(lineNumber));
        return result == -1 ? 0 : result + 2;
    }

    /// <summary>The underlying piece tree, exposed for tests (Monaco: <c>getPieceTree</c>).</summary>
    internal PieceTreeBase GetPieceTree() => _pieceTree;

    private string GetEndOfLine(EndOfLinePreference eol) => eol switch
    {
        EndOfLinePreference.LF => "\n",
        EndOfLinePreference.CRLF => "\r\n",
        EndOfLinePreference.TextDefined => GetEol(),
        _ => throw new ArgumentOutOfRangeException(nameof(eol), eol, "Unknown EOL preference"),
    };
}
