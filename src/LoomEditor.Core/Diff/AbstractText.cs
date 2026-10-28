// Ported from microsoft/vscode: src/vs/editor/common/core/text/abstractText.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diagnostics;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Diff;

/// <summary>A text addressed by ranges (Monaco: <c>AbstractText</c>).</summary>
/// <remarks>
/// <para>Not ported: <c>getValueOfOffsetRange</c> and <c>getTransformer</c> (need <c>PositionOffsetTransformer</c>), and <c>StringText</c>.</para>
/// <para>Threading: implementations are immutable.</para>
/// </remarks>
internal abstract class AbstractText
{
    /// <summary>The length of the text.</summary>
    public abstract TextLength Length { get; }

    /// <summary><c>endPositionExclusive</c>.</summary>
    public Position EndPositionExclusive => Length.AddToPosition(new Position(1, 1));

    /// <summary><c>lineRange</c>.</summary>
    public LineRange LineRange => Length.ToLineRange();

    /// <summary>The text of a range, with <c>\n</c> between lines.</summary>
    public abstract string GetValueOfRange(Range range);

    /// <summary><c>getValue</c>.</summary>
    public string GetValue() => GetValueOfRange(Length.ToRange());

    /// <summary><c>getLineLength</c>.</summary>
    public virtual int GetLineLength(int lineNumber) => GetLineAt(lineNumber).Length;

    /// <summary><c>getLineAt</c>.</summary>
    public string GetLineAt(int lineNumber) => GetValueOfRange(new Range(lineNumber, 1, lineNumber, int.MaxValue));

    /// <summary><c>getLines</c>.</summary>
    public string[] GetLines() => Strings.SplitLines(GetValue());

    /// <summary><c>getLinesOfRange</c>.</summary>
    public string[] GetLinesOfRange(LineRange range) => range.MapToLineArray(GetLineAt);

    /// <summary><c>equals</c>: same text value.</summary>
    public bool TextEquals(AbstractText other) => ReferenceEquals(this, other) || GetValue() == other.GetValue();
}

/// <summary>A text defined by a line accessor (Monaco: <c>LineBasedText</c>).</summary>
/// <remarks>Threading: as thread-safe as the line accessor.</remarks>
internal class LineBasedText : AbstractText
{
    private readonly Func<int, string> _getLineContent;
    private readonly int _lineCount;

    /// <summary>Creates the text; <paramref name="lineCount"/> must be at least 1.</summary>
    public LineBasedText(Func<int, string> getLineContent, int lineCount)
    {
        Guard.Assert(lineCount >= 1);
        _getLineContent = getLineContent;
        _lineCount = lineCount;
    }

    /// <inheritdoc />
    public override TextLength Length => new(_lineCount - 1, _getLineContent(_lineCount).Length);

    /// <inheritdoc />
    public override string GetValueOfRange(Range range)
    {
        if (range.StartLineNumber == range.EndLineNumber)
        {
            return Slice(_getLineContent(range.StartLineNumber), range.StartColumn - 1, range.EndColumn - 1);
        }

        string result = Slice(_getLineContent(range.StartLineNumber), range.StartColumn - 1, int.MaxValue);
        for (int i = range.StartLineNumber + 1; i < range.EndLineNumber; i++)
        {
            result += "\n" + _getLineContent(i);
        }

        return result + "\n" + Slice(_getLineContent(range.EndLineNumber), 0, range.EndColumn - 1);
    }

    /// <inheritdoc />
    public override int GetLineLength(int lineNumber) => _getLineContent(lineNumber).Length;

    // JavaScript substring semantics: both bounds are clamped to the string.
    private static string Slice(string s, int start, int end)
    {
        start = Math.Min(start, s.Length);
        end = Math.Min(end, s.Length);
        return s.Substring(start, end - start);
    }
}

/// <summary>A text backed by an array of lines (Monaco: <c>ArrayText</c>).</summary>
/// <remarks>Threading: immutable as long as the array is not mutated.</remarks>
internal sealed class ArrayText(string[] lines) : LineBasedText(lineNumber => lines[lineNumber - 1], lines.Length);
