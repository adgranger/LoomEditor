// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/linesSliceCharSequence.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Diff;

/// <summary>
/// A slice of several lines seen as one sequence of UTF-16 code units, for the second (character) diff pass
/// (Monaco: <c>LinesSliceCharSequence</c>). Lines are joined with <c>\n</c>; leading and trailing whitespace of each
/// line is dropped unless whitespace changes are considered.
/// </summary>
/// <remarks>Threading: immutable after construction.</remarks>
internal sealed class LinesSliceCharSequence : ISequence
{
    private static readonly int[] Score =
    [
        0,  // WordLower
        0,  // WordUpper
        0,  // WordNumber
        10, // End
        2,  // Other
        30, // Separator
        3,  // Space
        10, // LineBreakCR
        10, // LineBreakLF
    ];

    private readonly List<int> _elements = [];
    private readonly List<int> _firstElementOffsetByLineIdx = [];
    private readonly List<int> _lineStartOffsets = [];
    private readonly List<int> _trimmedWsLengthsByLineIdx = [];
    private readonly Range _range;

    /// <summary>Creates the sequence of <paramref name="range"/> (end columns are clamped to the line length).</summary>
    public LinesSliceCharSequence(string[] lines, Range range, bool considerWhitespaceChanges)
    {
        Lines = lines;
        _range = range;
        ConsiderWhitespaceChanges = considerWhitespaceChanges;

        _firstElementOffsetByLineIdx.Add(0);
        for (int lineNumber = range.StartLineNumber; lineNumber <= range.EndLineNumber; lineNumber++)
        {
            string line = lines[lineNumber - 1];
            int lineStartOffset = 0;
            if (lineNumber == range.StartLineNumber && range.StartColumn > 1)
            {
                lineStartOffset = range.StartColumn - 1;
                line = line.Substring(lineStartOffset);
            }

            _lineStartOffsets.Add(lineStartOffset);

            int trimmedWsLength = 0;
            if (!considerWhitespaceChanges)
            {
                string trimmedStartLine = line.TrimStart();
                trimmedWsLength = line.Length - trimmedStartLine.Length;
                line = trimmedStartLine.TrimEnd();
            }

            _trimmedWsLengthsByLineIdx.Add(trimmedWsLength);

            int lineLength = lineNumber == range.EndLineNumber
                ? (int)Math.Min((long)range.EndColumn - 1 - lineStartOffset - trimmedWsLength, line.Length)
                : line.Length;
            for (int i = 0; i < lineLength; i++)
            {
                _elements.Add(line[i]);
            }

            if (lineNumber < range.EndLineNumber)
            {
                _elements.Add('\n');
                _firstElementOffsetByLineIdx.Add(_elements.Count);
            }
        }
    }

    /// <summary>The lines this sequence slices.</summary>
    public string[] Lines { get; }

    /// <summary><c>considerWhitespaceChanges</c>.</summary>
    public bool ConsiderWhitespaceChanges { get; }

    /// <inheritdoc />
    public int Length => _elements.Count;

    /// <summary><c>text</c>: the whole sequence as a string.</summary>
    public string Text => GetText(new OffsetRange(0, Length));

    /// <summary><c>getText</c>.</summary>
    public string GetText(OffsetRange range)
    {
        var sb = new StringBuilder(range.Length);
        for (int i = range.Start; i < range.EndExclusive; i++)
        {
            sb.Append((char)_elements[i]);
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public int GetElement(int offset) => _elements[offset];

    /// <inheritdoc />
    public int GetBoundaryScore(int length)
    {
        CharBoundaryCategory prevCategory = GetCategory(length > 0 ? _elements[length - 1] : -1);
        CharBoundaryCategory nextCategory = GetCategory(length < _elements.Count ? _elements[length] : -1);

        if (prevCategory == CharBoundaryCategory.LineBreakCR && nextCategory == CharBoundaryCategory.LineBreakLF)
        {
            // don't break between \r and \n
            return 0;
        }

        if (prevCategory == CharBoundaryCategory.LineBreakLF)
        {
            // prefer the linebreak before the change
            return 150;
        }

        int score = 0;
        if (prevCategory != nextCategory)
        {
            score += 10;
            if (prevCategory == CharBoundaryCategory.WordLower && nextCategory == CharBoundaryCategory.WordUpper)
            {
                score += 1;
            }
        }

        return score + Score[(int)prevCategory] + Score[(int)nextCategory];
    }

    /// <summary>
    /// <c>translateOffset</c>: the position of <paramref name="offset"/> in the original lines. With
    /// <paramref name="preferLeft"/>, an offset at a line start is not shifted by the trimmed leading whitespace.
    /// </summary>
    public Position TranslateOffset(int offset, bool preferLeft = false)
    {
        int i = FindLastIdx(_firstElementOffsetByLineIdx, value => value <= offset);
        int lineOffset = offset - _firstElementOffsetByLineIdx[i];
        return new Position(
            _range.StartLineNumber + i,
            1 + _lineStartOffsets[i] + lineOffset + (lineOffset == 0 && preferLeft ? 0 : _trimmedWsLengthsByLineIdx[i]));
    }

    /// <summary><c>translateRange</c>.</summary>
    public Range TranslateRange(OffsetRange range)
    {
        Position pos1 = TranslateOffset(range.Start);
        Position pos2 = TranslateOffset(range.EndExclusive, preferLeft: true);
        return pos2.IsBefore(pos1) ? new Range(pos2, pos2) : new Range(pos1, pos2);
    }

    /// <summary><c>findWordContaining</c>: the word that contains the character at <paramref name="offset"/>.</summary>
    public OffsetRange? FindWordContaining(int offset)
    {
        if (offset < 0 || offset >= _elements.Count || !IsWordChar(_elements[offset]))
        {
            return null;
        }

        int start = offset;
        while (start > 0 && IsWordChar(_elements[start - 1]))
        {
            start--;
        }

        int end = offset;
        while (end < _elements.Count && IsWordChar(_elements[end]))
        {
            end++;
        }

        return new OffsetRange(start, end);
    }

    /// <summary><c>findSubWordContaining</c>: <c>fooBar</c> has the two sub-words <c>foo</c> and <c>bar</c>.</summary>
    public OffsetRange? FindSubWordContaining(int offset)
    {
        if (offset < 0 || offset >= _elements.Count || !IsWordChar(_elements[offset]))
        {
            return null;
        }

        int start = offset;
        while (start > 0 && IsWordChar(_elements[start - 1]) && !IsUpperCase(_elements[start]))
        {
            start--;
        }

        int end = offset;
        while (end < _elements.Count && IsWordChar(_elements[end]) && !IsUpperCase(_elements[end]))
        {
            end++;
        }

        return new OffsetRange(start, end);
    }

    /// <summary><c>countLinesIn</c>.</summary>
    public int CountLinesIn(OffsetRange range) =>
        TranslateOffset(range.EndExclusive).LineNumber - TranslateOffset(range.Start).LineNumber;

    /// <inheritdoc />
    public bool IsStronglyEqual(int offset1, int offset2) => _elements[offset1] == _elements[offset2];

    /// <summary><c>extendToFullLines</c>.</summary>
    public OffsetRange ExtendToFullLines(OffsetRange range)
    {
        int startIdx = FindLastIdx(_firstElementOffsetByLineIdx, x => x <= range.Start);
        int start = startIdx < 0 ? 0 : _firstElementOffsetByLineIdx[startIdx];

        int end = _elements.Count;
        foreach (int x in _firstElementOffsetByLineIdx)
        {
            if (range.EndExclusive <= x)
            {
                end = x;
                break;
            }
        }

        return new OffsetRange(start, end);
    }

    // The predicate is true then false (monotonous): the index of the last true, or -1.
    private static int FindLastIdx(List<int> items, Func<int, bool> predicate)
    {
        int lo = 0;
        int hi = items.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (predicate(items[mid]))
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo - 1;
    }

    private static bool IsWordChar(int c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');

    private static bool IsUpperCase(int c) => c >= 'A' && c <= 'Z';

    private static CharBoundaryCategory GetCategory(int c)
    {
        if (c == '\n')
        {
            return CharBoundaryCategory.LineBreakLF;
        }

        if (c == '\r')
        {
            return CharBoundaryCategory.LineBreakCR;
        }

        if (c == ' ' || c == '\t')
        {
            return CharBoundaryCategory.Space;
        }

        if (c >= 'a' && c <= 'z')
        {
            return CharBoundaryCategory.WordLower;
        }

        if (c >= 'A' && c <= 'Z')
        {
            return CharBoundaryCategory.WordUpper;
        }

        if (c >= '0' && c <= '9')
        {
            return CharBoundaryCategory.WordNumber;
        }

        if (c == -1)
        {
            return CharBoundaryCategory.End;
        }

        return c == ',' || c == ';' ? CharBoundaryCategory.Separator : CharBoundaryCategory.Other;
    }

    private enum CharBoundaryCategory
    {
        WordLower,
        WordUpper,
        WordNumber,
        End,
        Other,
        Separator,
        Space,
        LineBreakCR,
        LineBreakLF,
    }
}
