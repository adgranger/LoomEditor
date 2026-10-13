// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model;

/// <summary>
/// The text storage used by <c>TextModel</c>: read queries and EOL changes. <c>ApplyEdits</c> is added by a later card.
/// </summary>
/// <remarks>
/// <para>Monaco: <c>IReadonlyTextBuffer</c> + <c>ITextBuffer</c> merged into one interface.</para>
/// <para>
/// Differences from Monaco: no <c>onDidChangeContent</c> event, not <c>IDisposable</c>, no
/// <c>findMatchesLineByLine</c> (search comes with the Find feature); <c>GetEol</c>/<c>SetEol</c> use strings.
/// </para>
/// <para>Threading: implementations are not thread-safe; callers must serialize access.</para>
/// </remarks>
internal interface ITextBuffer
{
    /// <summary>True when <paramref name="other"/> has the same BOM, EOL and text (Monaco: <c>equals</c>).</summary>
    bool Equals(ITextBuffer other);

    /// <summary>True when the text may contain right-to-left characters (Monaco: <c>mightContainRTL</c>).</summary>
    bool MightContainRtl();

    /// <summary>True when the text may contain U+2028 or U+2029 (Monaco: <c>mightContainUnusualLineTerminators</c>).</summary>
    bool MightContainUnusualLineTerminators();

    /// <summary>Records that the text no longer contains U+2028 or U+2029 (Monaco: <c>resetMightContainUnusualLineTerminators</c>).</summary>
    void ResetMightContainUnusualLineTerminators();

    /// <summary>True when the text may contain non basic-ASCII characters (Monaco: <c>mightContainNonBasicASCII</c>).</summary>
    bool MightContainNonBasicAscii();

    /// <summary>The BOM removed from the text, or an empty string (Monaco: <c>getBOM</c>).</summary>
    string GetBom();

    /// <summary>The EOL of the document: <c>"\n"</c> or <c>"\r\n"</c> (Monaco: <c>getEOL</c>).</summary>
    string GetEol();

    /// <summary>Changes the EOL of the document, rewriting the line breaks (Monaco: <c>setEOL</c>).</summary>
    /// <param name="newEol"><c>"\n"</c> or <c>"\r\n"</c>.</param>
    /// <exception cref="ArgumentException"><paramref name="newEol"/> is neither of those.</exception>
    void SetEol(string newEol);

    /// <summary>The offset of a position, counted with the text EOL (Monaco: <c>getOffsetAt</c>).</summary>
    int GetOffsetAt(int lineNumber, int column);

    /// <summary>The position of an offset (Monaco: <c>getPositionAt</c>).</summary>
    Position GetPositionAt(int offset);

    /// <summary>The range starting at <paramref name="start"/> and spanning <paramref name="length"/> characters (Monaco: <c>getRangeAt</c>).</summary>
    Range GetRangeAt(int start, int length);

    /// <summary>The text of a range, with the requested EOL (Monaco: <c>getValueInRange</c>).</summary>
    string GetValueInRange(Range range, EndOfLinePreference eol = EndOfLinePreference.TextDefined);

    /// <summary>Creates a snapshot of the whole text (Monaco: <c>createSnapshot</c>).</summary>
    /// <param name="preserveBom">True to emit the BOM at the start of the first chunk.</param>
    ITextSnapshot CreateSnapshot(bool preserveBom);

    /// <summary>The length of <see cref="GetValueInRange"/> without building the string (Monaco: <c>getValueLengthInRange</c>).</summary>
    int GetValueLengthInRange(Range range, EndOfLinePreference eol = EndOfLinePreference.TextDefined);

    /// <summary>The number of characters of a range, counting a surrogate pair as one (Monaco: <c>getCharacterCountInRange</c>).</summary>
    int GetCharacterCountInRange(Range range, EndOfLinePreference eol = EndOfLinePreference.TextDefined);

    /// <summary>The length of the text, in UTF-16 code units (Monaco: <c>getLength</c>).</summary>
    int GetLength();

    /// <summary>The number of lines (Monaco: <c>getLineCount</c>).</summary>
    int GetLineCount();

    /// <summary>The content of every line, without line breaks (Monaco: <c>getLinesContent</c>).</summary>
    string[] GetLinesContent();

    /// <summary>The content of a line, without its line break (Monaco: <c>getLineContent</c>).</summary>
    string GetLineContent(int lineNumber);

    /// <summary>The character at a 0-based index of a line (Monaco: <c>getLineCharCode</c>).</summary>
    char GetLineCharCode(int lineNumber, int index);

    /// <summary>The character at an offset (Monaco: <c>getCharCode</c>).</summary>
    char GetCharCode(int offset);

    /// <summary>The length of a line, without its line break (Monaco: <c>getLineLength</c>).</summary>
    int GetLineLength(int lineNumber);

    /// <summary>The minimum column of a line: always 1 (Monaco: <c>getLineMinColumn</c>).</summary>
    int GetLineMinColumn(int lineNumber);

    /// <summary>The maximum column of a line: its length plus one (Monaco: <c>getLineMaxColumn</c>).</summary>
    int GetLineMaxColumn(int lineNumber);

    /// <summary>The 1-based column of the first non-whitespace character, or 0 for a blank line (Monaco: <c>getLineFirstNonWhitespaceColumn</c>).</summary>
    int GetLineFirstNonWhitespaceColumn(int lineNumber);

    /// <summary>The 1-based column just after the last non-whitespace character, or 0 for a blank line (Monaco: <c>getLineLastNonWhitespaceColumn</c>).</summary>
    int GetLineLastNonWhitespaceColumn(int lineNumber);

    /// <summary>The nearest chunk of text after <paramref name="offset"/> (Monaco: <c>getNearestChunk</c>).</summary>
    string GetNearestChunk(int offset);
}
