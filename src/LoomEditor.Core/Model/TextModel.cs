// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model;

/// <summary>
/// The text of an editor: a document of lines that can be read by line, offset, position or range.
/// This first version is read-only; edits, validation, undo and snapshots come with later cards.
/// </summary>
/// <remarks>
/// <para>Monaco: <c>TextModel</c> (read API only).</para>
/// <para>
/// Not ported yet: language, options (tab size...), decorations, tokens, injected text, attached editors,
/// <c>isTooLargeForSyncing</c> and disposal. Methods that take a position or range use it as given (no
/// validation yet), so callers must pass values inside the text.
/// </para>
/// <para>Threading: UI thread only. Not thread-safe.</para>
/// </remarks>
public sealed class TextModel
{
    private readonly PieceTreeTextBuffer _buffer;
    private readonly int _versionId = 1;
    private readonly int _alternativeVersionId = 1;

    /// <summary>Creates a model holding <paramref name="text"/>.</summary>
    /// <param name="text">The initial text. A leading BOM is removed and remembered; line breaks are normalized to the EOL.</param>
    /// <param name="defaultEol">The EOL to use when the text has no line break.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public TextModel(string text, DefaultEndOfLine defaultEol = DefaultEndOfLine.LF)
    {
        ArgumentNullException.ThrowIfNull(text);

        // Monaco: createTextBuffer(value, defaultEOL)
        var builder = new PieceTreeTextBufferBuilder();
        builder.AcceptChunk(text);
        _buffer = builder.Finish(normalizeEol: true).Create(defaultEol);
    }

    /// <summary>The version of the text; starts at 1 (Monaco: <c>getVersionId</c>).</summary>
    public int VersionId => _versionId;

    /// <summary>The version used to detect that the text is back to a previous state; starts at 1 (Monaco: <c>getAlternativeVersionId</c>).</summary>
    public int AlternativeVersionId => _alternativeVersionId;

    /// <summary>True when the text may contain right-to-left characters (Monaco: <c>mightContainRTL</c>).</summary>
    public bool MightContainRtl => _buffer.MightContainRtl();

    /// <summary>True when the text may contain non basic-ASCII characters (Monaco: <c>mightContainNonBasicASCII</c>).</summary>
    public bool MightContainNonBasicAscii => _buffer.MightContainNonBasicAscii();

    /// <summary>The end of line of the text: <c>"\n"</c> or <c>"\r\n"</c> (Monaco: <c>getEOL</c>).</summary>
    public string Eol => _buffer.GetEol();

    /// <summary>The end of line of the text as an enum (Monaco: <c>getEndOfLineSequence</c>).</summary>
    public EndOfLineSequence EndOfLineSequence => _buffer.GetEol() == "\n" ? EndOfLineSequence.LF : EndOfLineSequence.CRLF;

    /// <summary>The number of lines; at least 1 (Monaco: <c>getLineCount</c>).</summary>
    public int LineCount => _buffer.GetLineCount();

    /// <summary>The range of the whole text (Monaco: <c>getFullModelRange</c>).</summary>
    public Range FullModelRange
    {
        get
        {
            int lineCount = LineCount;
            return new Range(1, 1, lineCount, GetLineMaxColumn(lineCount));
        }
    }

    /// <summary>The offset of a position, counted with the text EOL (Monaco: <c>getOffsetAt</c>).</summary>
    public int GetOffsetAt(Position position) => _buffer.GetOffsetAt(position.LineNumber, position.Column);

    /// <summary>The position of an offset; an offset outside the text is clamped to it (Monaco: <c>getPositionAt</c>).</summary>
    public Position GetPositionAt(int offset) => _buffer.GetPositionAt(Math.Min(_buffer.GetLength(), Math.Max(0, offset)));

    /// <summary>The whole text (Monaco: <c>getValue</c>).</summary>
    /// <param name="eol">The EOL to use for the line breaks.</param>
    /// <param name="preserveBom">True to prefix the text with the BOM it had when the model was created.</param>
    public string GetValue(EndOfLinePreference eol = EndOfLinePreference.TextDefined, bool preserveBom = false)
    {
        string value = _buffer.GetValueInRange(FullModelRange, eol);
        return preserveBom ? _buffer.GetBom() + value : value;
    }

    /// <summary>The length of <see cref="GetValue"/> without building the string (Monaco: <c>getValueLength</c>).</summary>
    public int GetValueLength(EndOfLinePreference eol = EndOfLinePreference.TextDefined, bool preserveBom = false)
    {
        int length = _buffer.GetValueLengthInRange(FullModelRange, eol);
        return preserveBom ? _buffer.GetBom().Length + length : length;
    }

    /// <summary>The text of a range (Monaco: <c>getValueInRange</c>).</summary>
    public string GetValueInRange(Range range, EndOfLinePreference eol = EndOfLinePreference.TextDefined) =>
        _buffer.GetValueInRange(range, eol);

    /// <summary>The length of the text of a range, without building the string (Monaco: <c>getValueLengthInRange</c>).</summary>
    public int GetValueLengthInRange(Range range, EndOfLinePreference eol = EndOfLinePreference.TextDefined) =>
        _buffer.GetValueLengthInRange(range, eol);

    /// <summary>The number of characters of a range, counting a surrogate pair as one (Monaco: <c>getCharacterCountInRange</c>).</summary>
    public int GetCharacterCountInRange(Range range, EndOfLinePreference eol = EndOfLinePreference.TextDefined) =>
        _buffer.GetCharacterCountInRange(range, eol);

    /// <summary>The content of a line, without its line break (Monaco: <c>getLineContent</c>).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is not a line of the model.</exception>
    public string GetLineContent(int lineNumber)
    {
        AssertLineNumber(lineNumber);
        return _buffer.GetLineContent(lineNumber);
    }

    /// <summary>The length of a line, without its line break (Monaco: <c>getLineLength</c>).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is not a line of the model.</exception>
    public int GetLineLength(int lineNumber)
    {
        AssertLineNumber(lineNumber);
        return _buffer.GetLineLength(lineNumber);
    }

    /// <summary>The content of every line, without line breaks (Monaco: <c>getLinesContent</c>).</summary>
    public string[] GetLinesContent() => _buffer.GetLinesContent();

    /// <summary>The minimum column of a line: always 1 (Monaco: <c>getLineMinColumn</c>).</summary>
    /// <remarks>Difference from Monaco: an illegal <paramref name="lineNumber"/> throws, like the other line methods.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is not a line of the model.</exception>
    public int GetLineMinColumn(int lineNumber)
    {
        AssertLineNumber(lineNumber);
        return 1;
    }

    /// <summary>The maximum column of a line: its length plus one (Monaco: <c>getLineMaxColumn</c>).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is not a line of the model.</exception>
    public int GetLineMaxColumn(int lineNumber)
    {
        AssertLineNumber(lineNumber);
        return _buffer.GetLineLength(lineNumber) + 1;
    }

    /// <summary>The 1-based column of the first non-whitespace character of a line, or 0 for a blank line (Monaco: <c>getLineFirstNonWhitespaceColumn</c>).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is not a line of the model.</exception>
    public int GetLineFirstNonWhitespaceColumn(int lineNumber)
    {
        AssertLineNumber(lineNumber);
        return _buffer.GetLineFirstNonWhitespaceColumn(lineNumber);
    }

    /// <summary>The 1-based column just after the last non-whitespace character of a line, or 0 for a blank line (Monaco: <c>getLineLastNonWhitespaceColumn</c>).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is not a line of the model.</exception>
    public int GetLineLastNonWhitespaceColumn(int lineNumber)
    {
        AssertLineNumber(lineNumber);
        return _buffer.GetLineLastNonWhitespaceColumn(lineNumber);
    }

    // Monaco: throws BugIndicatingError('Illegal value for lineNumber')
    private void AssertLineNumber(int lineNumber)
    {
        if (lineNumber < 1 || lineNumber > _buffer.GetLineCount())
        {
            throw new ArgumentOutOfRangeException(nameof(lineNumber), lineNumber, "Illegal value for lineNumber");
        }
    }
}
